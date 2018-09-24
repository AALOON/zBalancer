using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace zBalancer.Balancer.Middlewares
{
    public abstract class MiddlewareBase : IMiddleware
    {
        private readonly RequestDelegate _next;

        protected MiddlewareBase(RequestDelegate next)
        {
            _next = next;
        }

        protected abstract Task MiddlewareInvoke(HttpContext context);

        public virtual async Task InvokeAsync(HttpContext context)
        {
            await MiddlewareInvoke(context);
            await _next.Invoke(context);
        }
    }
}