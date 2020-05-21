using AutoMapper;
using FoxIDs.Models;
using System.Linq;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.MappingProfiles
{
    public class MasterMappingProfile : Profile
    {
        public MasterMappingProfile()
        {
            Mapping();
        }

        private void Mapping()
        {
            CreateMap<ResourceEnvelope, Api.Resource>()
                .ReverseMap();
            CreateMap<ResourceName, Api.ResourceName>()
                .ReverseMap();
            CreateMap<ResourceItem, Api.ResourceItem>()
                .ReverseMap();
        }
    }
}
