using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace zBalancer.Balancer.Middlewares
{
    /// <summary>
    /// Contract for middleware
    /// </summary>
    public interface IMiddleware
    {
        /// <summary>
        /// Invokes from first to last by previous middleware
        /// </summary>
        Task InvokeAsync(HttpContext context);
    }
}