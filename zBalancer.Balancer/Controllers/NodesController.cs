using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using zBalancer.Balancer.Dto;
using zBalancer.Balancer.Models;
using zBalancer.Balancer.Services;

namespace zBalancer.Balancer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NodesController : ControllerBase
    {
        private readonly IMapper _mapper;
        private readonly INodeService _nodeService;

        public NodesController(INodeService nodeService, IMapper mapper)
        {
            _nodeService = nodeService;
            _mapper = mapper;
        }
        
        [HttpPost]
        public async Task<IActionResult> AddNode(NodeDto node)
        {
            var newNode = _mapper.Map<Node>(node);
            await _nodeService.AddBlueAsync(newNode);
            return CreatedAtRoute("GetNode", new { id = newNode.Id }, _mapper.Map<NodeDto>(newNode));
        }

        [Route("{id}/[action]")]
        [HttpPatch]
        public async Task<IActionResult> Up([FromRoute]int id)
        {
            var node = await _nodeService.GetByIdAsync(id);
            if (node == null)
                return NotFound();
            await _nodeService.MakeGreenAsync(node);
            return NoContent();
        }

        [Route("{id}/[action]")]
        [HttpPatch]
        public async Task<IActionResult> Down([FromRoute]int id)
        {
            var node = await _nodeService.GetByIdAsync(id);
            if (node == null)
                return NotFound();
            await _nodeService.MakeBlueAsync(node);
            return NoContent();
        }

        [Route("[action]")]
        [HttpGet("{id}", Name = "GetNode")]
        public async Task<ActionResult<NodeDto>> Get(int id)
        {
            var node = await _nodeService.GetByIdAsync(id);
            if (node == null)
                return NotFound();
            return Ok(_mapper.Map<NodeDto>(node));
        }

        [Route("[action]")]
        [HttpGet]
        public async Task<ActionResult<NodeDto[]>> Greens()
        {
            var nodes = await _nodeService.GetGreensAsync();
            return Ok(_mapper.Map<NodeDto[]>(nodes));
        }

        [Route("[action]")]
        [HttpGet]
        public async Task<ActionResult<NodeDto[]>> Blues()
        {
            var nodes = await _nodeService.GetBluesAsync();
            return Ok(_mapper.Map<NodeDto[]>(nodes));
        }
    }
}