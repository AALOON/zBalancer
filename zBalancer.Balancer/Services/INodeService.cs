using System.Threading.Tasks;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Services
{
    public interface INodeService
    {
        Task AddBlueAsync(Node node);
        Task<Node[]> GetBluesAsync();
        Task<Node[]> GetGreensAsync();
        Task<Node> GetByIdAsync(int id);
        Task MakeGreenAsync(Node node);
        Task MakeBlueAsync(Node node);
        Task RemoveAsync(Node node);
    }
}