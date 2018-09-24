using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Repositories
{
    public interface INodeRepository : IDisposable
    {
        Task<Node[]> GetAllAsync();

        Task<Node[]> FindByAsync(Expression<Func<Node, bool>> predicate);

        Task<Node> GetAsync(int id);

        Task AddAsync(Node node);

        Task UpdateAsync(Node node);

        Task RemoveAsync(int id);
    }
}