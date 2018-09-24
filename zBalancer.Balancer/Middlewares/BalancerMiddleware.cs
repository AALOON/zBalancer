using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using zBalancer.Balancer.Services;

namespace zBalancer.Balancer.Middlewares
{  
    public class BalancerMiddleware : MiddlewareBase
    {
        private const int DefaultBufferSize = 4096;
        private readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler());
        private static readonly string[] NotForwardedWebSocketHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version" };
        

        private readonly InternalProxyOptions _defaultOptions = new InternalProxyOptions()
        {
            BackChannelMessageHandler = new HttpClientHandler(),
            BufferSize = DefaultBufferSize,
            Score = 1,
            SendChunked = false,
            UrlHost = null,
        };

        private readonly INodeService _nodeService;
        private readonly ILogger<BalancerMiddleware> _logger;

        public BalancerMiddleware(RequestDelegate next, INodeService nodeService, ILogger<BalancerMiddleware> logger) : base(next)
        {
            _nodeService = nodeService;
            _logger = logger;
        }

        public override async Task InvokeAsync(HttpContext context)
        {
            await MiddlewareInvoke(context);
        }

        /// <summary>
        /// Entry point. Switch between websocket requests and regular http request
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        protected override async Task MiddlewareInvoke(HttpContext context)
        {
            var options = _defaultOptions;

            const int index = 0;

            var greens = await _nodeService.GetGreensAsync();

            if(!greens.Any())
                throw new NotSupportedException();//TODO: make appropriate exception

            var selectedNode = greens[index];
            
            var chost = selectedNode.Host;
            var cport = selectedNode.Port;
            var scheme = selectedNode.Scheme;

            if (!context.WebSockets.IsWebSocketRequest)
            {
                await HandleHttpRequest(context, options, chost, cport, scheme);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        private async Task HandleHttpRequest(HttpContext context, InternalProxyOptions options, string host, int port, string scheme)
        {
            var requestMessage = new HttpRequestMessage();
            var requestMethod = context.Request.Method;

            if (!HttpMethods.IsGet(requestMethod) && !HttpMethods.IsHead(requestMethod) && !HttpMethods.IsDelete(requestMethod) && !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(context.Request.Body);
                requestMessage.Content = streamContent;
            }

            // All request headers and cookies must be transferend to remote server. Some headers will be skipped
            foreach (var header in context.Request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }


            requestMessage.Headers.Host = host;
            //recreate remote url
            var uriString = GetUri(context, host, port, scheme);
            requestMessage.RequestUri = new Uri(uriString);
            requestMessage.Method = new HttpMethod(context.Request.Method);
            using (var responseMessage = await _httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
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
                    var buffer = new byte[options.BufferSize ?? DefaultBufferSize];

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
            return $"{scheme}://{host}{urlPort}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
        }
    }

    public class InternalProxyOptions
    {
        private int? _bufferSize;
        public long Score { get; set; }
        public string UrlHost { get; set; }
        public HttpMessageHandler BackChannelMessageHandler { get; set; }
        public TimeSpan? WebSocketKeepAliveInterval { get; set; }
        public bool SendChunked { get; set; }
        public int? BufferSize
        {
            get => _bufferSize;
            set
            {
                if (value.HasValue && value.Value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                _bufferSize = value;
            }
        }
    }
}