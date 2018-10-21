using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using zBalancer.Balancer.Keys;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Middlewares
{  
    /// <summary>
    /// This middleware sends request to the next server
    /// </summary>
    public class RequestSenderMiddleware : MiddlewareBase
    {
        private const int DefaultBufferSize = 4096;
        private readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler());

        private static readonly HashSet<string> NotForwardedWebSocketHeaders = new HashSet<string>
             { "connection", "host", "upgrade", "sec-websocket-key", "sec-websocket-version" };
        

        private readonly InternalProxyOptions _defaultOptions = new InternalProxyOptions()
        {
            BufferSize = DefaultBufferSize,
            SendChunked = false,
            WebSocketKeepAliveInterval = TimeSpan.FromHours(1)
        };

        private readonly ILogger<RequestSenderMiddleware> _logger;

        /// <inheritdoc />
        public RequestSenderMiddleware(RequestDelegate next, ILogger<RequestSenderMiddleware> logger)
            : base(next)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public override async Task InvokeAsync(HttpContext context)
        {
            await MiddlewareInvoke(context);
        }

        /// <summary>
        /// Entry point. Switch between websocket requests and regular http request
        /// </summary>
        protected override async Task MiddlewareInvoke(HttpContext context)
        {
            var options = _defaultOptions;

            var selectedNode = (Node)context.Items[ItemsKeys.SelectedNode];


            var chost = selectedNode.Host;
            var cport = selectedNode.Port;
            var scheme = selectedNode.Scheme;

            if (!context.WebSockets.IsWebSocketRequest)
            {
                await HandleHttpRequestAsync(context, options, chost, cport, scheme);
            }
            else
            {
                await HandleWebSocketRequestAsync(context, options, chost, cport, scheme);
            }
        }

        private async Task HandleHttpRequestAsync(HttpContext context,
            InternalProxyOptions options, string host, int port, string scheme)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod)
                && !HttpMethods.IsHead(requestMethod)
                && !HttpMethods.IsDelete(requestMethod)
                && !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            // All request headers and cookies must be transferend to remote server.
            // Some headers will be skipped
            foreach (var header in context.Request.Headers)
            {
                var headerValue = header.Value.ToArray();
                if (requestMessage.Headers.TryAddWithoutValidation(header.Key, headerValue))
                    continue;
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, headerValue);
            }


            requestMessage.Headers.Host = host;

            //recreate remote url
            var uriString = GetUri(context, host, port, scheme);
            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = new HttpMethod(context.Request.Method);
            using (var responseMessage
                = await _httpClient
                    .SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
            {
                context.Response.StatusCode = (int)responseMessage.StatusCode;
                foreach (var header in responseMessage.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }

                foreach (var header in responseMessage.Content.Headers)
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }


                if (!options.SendChunked)
                {
                    //tell to the browser that response is not chunked
                    context.Response.Headers.Remove("transfer-encoding");
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                }
                else
                {
                    var buffer = new byte[options.BufferSize];

                    using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
                    {
                        int len, full = 0;
                        while ((len = await responseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await context.Response.Body.WriteAsync(buffer, 0, len);

                            full += buffer.Length;
                        }
                        // context.Response.ContentLength = full;
                        context.Response.Headers.Remove("transfer-encoding");

                        _logger.LogTrace($"Readed & writed [{full}] bytes");
                    }
                }
            }

        }
        
        private async Task HandleWebSocketRequestAsync(HttpContext context,
            InternalProxyOptions options, string host, int port, string scheme)
        {

            using (var client = new ClientWebSocket())
            {
                foreach (var headerEntry in context.Request.Headers)
                {
                    if (NotForwardedWebSocketHeaders.Contains(headerEntry.Key.ToLower()))
                        continue;
                    client.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                }

                // var wsScheme = string.Equals(scheme, "https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws";
                string url = GetUri(context, host, port, scheme);

                if (options.WebSocketKeepAliveInterval.HasValue)
                {
                    client.Options.KeepAliveInterval = options.WebSocketKeepAliveInterval.Value;
                }

                try
                {
                    await client.ConnectAsync(new Uri(url), context.RequestAborted);
                }
                catch (WebSocketException)
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                using (var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol))
                {
                    await Task.WhenAll(PumpWebSocketAsync(client, server, options, context.RequestAborted),
                        PumpWebSocketAsync(server, client, options, context.RequestAborted));
                }
            }
        }
        
        private async Task PumpWebSocketAsync(WebSocket source,
            WebSocket destination, InternalProxyOptions options, CancellationToken cancellationToken)
        {
            var buffer = new byte[options.BufferSize];
            while (true)
            {
                WebSocketReceiveResult result;
                try
                {
                    result = await source
                        .ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    await destination.CloseOutputAsync(WebSocketCloseStatus.EndpointUnavailable,
                        "Operation canceled", cancellationToken);
                    return;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (source.CloseStatus != null)
                        await destination.CloseOutputAsync(source.CloseStatus.Value,
                            source.CloseStatusDescription, cancellationToken);
                    return;
                }

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count),
                    result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }

        private static string GetUri(HttpContext context, string host, int? port, string scheme)
        {
            var urlPort = "";
            if (port.HasValue
                && !(port.Value == 443 && "https".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
                && !(port.Value == 80 && "http".Equals(scheme, StringComparison.InvariantCultureIgnoreCase))
            )
            {
                urlPort = ":" + port.Value;
            }
            return $"{scheme}://" +
                   $"{host}{urlPort}" +
                   $"{context.Request.PathBase}{context.Request.Path}" +
                   $"{context.Request.QueryString}";
        }
    }
}