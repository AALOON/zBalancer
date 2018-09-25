using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Services
{
    public class RoundRobinSelectionService : INodeSelectionService
    {
        private int _lastIndex = -1;

        public Node GetNode(Node[] nodes)
        {
            _lastIndex = (_lastIndex + 1) % nodes.Length;
            return nodes[_lastIndex];
        }
    }
}