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

            CreateMap<Plan, Api.Plan>()
               .ReverseMap()
               .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
               .ForMember(d => d.Id, opt => opt.MapFrom(s => Plan.IdFormatAsync(s.Name.ToLower()).GetAwaiter().GetResult()));
            CreateMap<PlanItem, Api.PlanItem>()
                .ReverseMap();

            CreateMap<ResourceEnvelope, Api.Resource>()
                .ReverseMap();
            CreateMap<ResourceName, Api.ResourceName>()
                .ReverseMap();
            CreateMap<ResourceItem, Api.ResourceItem>()
                .ReverseMap();
            CreateMap<ResourceCultureItem, Api.ResourceCultureItem>()
                .ReverseMap();

            CreateMap<RiskPassword, Api.RiskPassword>()
                .ForMember(d => d.PasswordSha1Hash, opt => opt.MapFrom(s => s.Id.Substring(s.Id.LastIndexOf(':') + 1)))
                .ReverseMap();
        }
    }
}
