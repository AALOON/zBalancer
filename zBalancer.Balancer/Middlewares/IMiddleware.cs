using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace zBalancer.Balancer.Middlewares
{
    public interface IMiddleware
    {
        Task InvokeAsync(HttpContext context);
    }
}