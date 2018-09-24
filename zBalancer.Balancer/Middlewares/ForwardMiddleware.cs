using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace zBalancer.Balancer.Middlewares
{
    public class ForwardMiddleware : MiddlewareBase
    {
        public ForwardMiddleware(RequestDelegate next) : base(next)
        {
        }

#pragma warning disable 1998
        protected override async Task MiddlewareInvoke(HttpContext context)
#pragma warning restore 1998
        {
            context.Request.Headers["X-Forwarded-For"] = context.Connection.RemoteIpAddress.ToString();
            context.Request.Headers["X-Forwarded-Proto"] = context.Request.Protocol.ToString();
            int port = context.Request.Host.Port ?? (context.Request.IsHttps ? 443 : 80);
            context.Request.Headers["X-Forwarded-Port"] = port.ToString();
        }
    }
}
