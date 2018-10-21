using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace zBalancer.Balancer.Middlewares
{
    /// <summary>
    /// Base class for middleware
    /// </summary>
    public abstract class MiddlewareBase : IMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// .ctor which gets <see cref="RequestDelegate"/> of next middleware
        /// </summary>
        protected MiddlewareBase(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// This method invokes before next pipe midleware
        /// by <see cref="MiddlewareBase"/> <see cref="InvokeAsync"/>
        /// </summary>
        protected abstract Task MiddlewareInvoke(HttpContext context);
        
        /// <inheritdoc />
        public virtual async Task InvokeAsync(HttpContext context)
        {
            await MiddlewareInvoke(context);
            await _next.Invoke(context);
        }
    }
}