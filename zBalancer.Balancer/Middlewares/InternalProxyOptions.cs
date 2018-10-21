using System;

namespace zBalancer.Balancer.Middlewares
{
    /// <summary>
    /// Internal proxy options of requests
    /// </summary>
    public class InternalProxyOptions
    {
        /// <summary>
        /// Gets or sets the WebSocket protocol keep-alive interval in milliseconds.
        /// </summary>
        public TimeSpan? WebSocketKeepAliveInterval { get; set; }

        /// <summary>
        /// If there big data queries there could be sent as chunked
        /// </summary>
        public bool SendChunked { get; set; }

        /// <summary>
        /// Buffer size of SendChunked=true of Http protocol and WebSocket protocol
        /// </summary>
        public int BufferSize { get; set; }
    }
}