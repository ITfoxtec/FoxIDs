using AutoMapper;
using FoxIDs.Models;
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
            CreateMap<string, string>()
                .ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<ResourceEnvelope, Api.Resource>()
                .ReverseMap();
            CreateMap<ResourceName, Api.ResourceName>()
                .ReverseMap();
            CreateMap<ResourceItem, Api.ResourceItem>()
                .ReverseMap();
            CreateMap<ResourceCultureItem, Api.ResourceCultureItem>()
                .ReverseMap();

            CreateMap<RiskPassword, Api.RiskPassword>()
                .ReverseMap();
        }
    }
}
