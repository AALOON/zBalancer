using AutoMapper;
using zBalancer.Balancer.Dto;
using zBalancer.Balancer.Models;

namespace zBalancer.Balancer.Profiles
{
    public class GeneralProfile : Profile
    {
        public GeneralProfile()
        {
            CreateMap<Node, NodeDto>().ReverseMap();
        }
    }
}