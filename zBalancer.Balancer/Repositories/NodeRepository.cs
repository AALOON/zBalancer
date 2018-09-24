using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using zBalancer.Balancer.Context;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Repositories
{
    public class NodeRepository : INodeRepository
    {
        private readonly NodeContext _nodeContext;

        public NodeRepository(NodeContext nodeContext)
        {
            _nodeContext = nodeContext;
        }

        public void Dispose()
        {
            _nodeContext.SaveChanges();
            _nodeContext?.Dispose();
        }

        public async Task<Node[]> GetAllAsync()
        {
            return await _nodeContext.Nodes.ToArrayAsync();
        }

        public async Task<Node[]> FindByAsync(Expression<Func<Node, bool>> predicate)
        {
            return await _nodeContext.Nodes.Where(predicate).ToArrayAsync();
        }

        public async Task<Node> GetAsync(int id)
        {
            return await _nodeContext.Nodes.FindAsync(id);
        }

        public async Task AddAsync(Node node)
        {
            await _nodeContext.AddAsync(node);
            await _nodeContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Node node)
        {
            if(node.Id <= 0)
                throw new ArgumentOutOfRangeException(nameof(node));

            _nodeContext.Entry(node).State = EntityState.Modified;
            await _nodeContext.SaveChangesAsync();
        }

        public async Task RemoveAsync(int id)
        {
            var entity = new Node { Id = id };
            _nodeContext.Nodes.Attach(entity);
            _nodeContext.Nodes.Remove(entity);
            await _nodeContext.SaveChangesAsync();
        }
    }
}