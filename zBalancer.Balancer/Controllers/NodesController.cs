using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
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

        /// <summary>
        /// Adds new <see cref="NodeDto"/> which automaticaly marks as blue (down)
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> AddNode([FromBody]NodeDto node)
        {
            var newNode = _mapper.Map<Node>(node);
            await _nodeService.AddBlueAsync(newNode);
            return CreatedAtRoute("GetNode", new { id = newNode.Id }, _mapper.Map<NodeDto>(newNode));
        }

        /// <summary>
        /// The node marks as green (up), balancer now can use this node
        /// </summary>
        /// <param name="id">identifier of the node</param>
        [Route("{id}/[action]")]
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Up([FromRoute]int id)
        {
            var node = await _nodeService.GetByIdAsync(id);
            if (node == null)
                return NotFound();
            await _nodeService.MakeGreenAsync(node);
            return NoContent();
        }

        /// <summary>
        /// The node marks as blue (down),
        /// balancer not uses this node for new requests, but ends old requests 
        /// </summary>
        /// <param name="id">identifier of the node</param>
        [Route("{id}/[action]")]
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Down([FromRoute]int id)
        {
            var node = await _nodeService.GetByIdAsync(id);
            if (node == null)
                return NotFound();
            await _nodeService.MakeBlueAsync(node);
            return NoContent();
        }

        /// <summary>
        /// Returns <see cref="NodeDto"/> by identifier
        /// </summary>
        /// <param name="id">identifier of the node</param>
        [HttpGet("{id}", Name = "GetNode")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<NodeDto>> Get(int id)
        {
            var node = await _nodeService.GetByIdAsync(id);
            if (node == null)
                return NotFound();
            return Ok(_mapper.Map<NodeDto>(node));
        }

        /// <summary>
        /// Returns all green(up) nodes
        /// </summary>
        [Route("[action]")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<NodeDto[]>> Greens()
        {
            var nodes = await _nodeService.GetGreensAsync();
            return Ok(_mapper.Map<NodeDto[]>(nodes));
        }

        /// <summary>
        /// Returns all blue(down) nodes
        /// </summary>
        [Route("[action]")]
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<NodeDto[]>> Blues()
        {
            var nodes = await _nodeService.GetBluesAsync();
            return Ok(_mapper.Map<NodeDto[]>(nodes));
        }
    }
}