using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Services
{
    public interface INodeSelectionService
    {
        Node GetNode(Node[] nodes);
    }
}