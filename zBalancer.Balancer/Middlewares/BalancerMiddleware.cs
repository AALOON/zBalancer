using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using zBalancer.Balancer.Keys;
using zBalancer.Balancer.Services;

namespace zBalancer.Balancer.Middlewares
{
    public class BalancerMiddleware : MiddlewareBase
    {
        private readonly INodeService _nodeService;
        private readonly INodeSelectionService _nodeSelectionService;

        public BalancerMiddleware(RequestDelegate next, INodeService nodeService, INodeSelectionService nodeSelectionService) : base(next)
        {
            _nodeService = nodeService;
            _nodeSelectionService = nodeSelectionService;
        }

        protected override async Task MiddlewareInvoke(HttpContext context)
        {
            var greens = await _nodeService.GetGreensAsync();

            if (!greens.Any())
                throw new NotSupportedException();//TODO: make appropriate exception

            var selectedNode = _nodeSelectionService.GetNode(greens);
            context.Items[ItemsKeys.SelectedNode] = selectedNode;
        }
    }
}