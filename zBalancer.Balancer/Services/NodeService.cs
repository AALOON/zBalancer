using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using zBalancer.Balancer.Models;
using zBalancer.Balancer.Repositories;

namespace zBalancer.Balancer.Services
{
    public class NodeService : INodeService
    {
        private readonly IMemoryCache _cache;
        private readonly INodeRepository _nodeRepository;
        

        public NodeService(IMemoryCache cache, INodeRepository nodeRepository)
        {
            _cache = cache;
            _nodeRepository = nodeRepository;
        }

        public async Task<Node[]> GetGreensAsync()
        {
            return await _cache.GetOrCreateAsync(CacheKeys.Greens, 
                async entry => await _nodeRepository.FindByAsync(node => node.Color == ColorMark.Green));
        }

        public async Task<Node> GetByIdAsync(int id)
        {
            return await _nodeRepository.GetAsync(id);
        }

        public async Task<Node[]> GetBluesAsync()
        {
            return await _cache.GetOrCreateAsync(CacheKeys.Blues,
                async entry => await _nodeRepository.FindByAsync(node => node.Color == ColorMark.Blue));
        }

        public async Task AddBlueAsync(Node node)
        {
            node.Color = ColorMark.Blue;
            await _nodeRepository.AddAsync(node);

            _cache.Remove(CacheKeys.Blues);
        }

        public async Task MakeGreenAsync(Node node)
        {
            node.Color = ColorMark.Green;
            await _nodeRepository.UpdateAsync(node);

            _cache.Remove(CacheKeys.Blues);
            _cache.Remove(CacheKeys.Greens);
        }

        public async Task MakeBlueAsync(Node node)
        {
            node.Color = ColorMark.Blue;
            await _nodeRepository.UpdateAsync(node);

            _cache.Remove(CacheKeys.Greens);
            _cache.Remove(CacheKeys.Blues);
        }

        public async Task RemoveAsync(Node node)
        {
            await _nodeRepository.RemoveAsync(node.Id);
            
            _cache.Remove(node.Color == ColorMark.Blue? CacheKeys.Blues : CacheKeys.Greens);
        }
    }
}