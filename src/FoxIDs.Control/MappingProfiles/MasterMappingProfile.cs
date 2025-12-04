using AutoMapper;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using System;
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
            CreateMap<DateOnlySerializable, DateOnly>()
               .ReverseMap();

            CreateMap<string, string>()
                .ConvertUsing(str => str == null ? null : str.Trim());

            CreateMap<Plan, Api.Plan>()
               .ReverseMap()
               .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.Trim().ToLower()))
               .ForMember(d => d.Id, opt => opt.MapFrom(s => Plan.IdFormatAsync(s.Name.Trim().ToLower()).GetAwaiter().GetResult()));
            CreateMap<PlanItem, Api.PlanItem>()
                .ReverseMap();
            CreateMap<Plan, Api.PlanInfo>();

            CreateMap<SmsPrice, Api.SmsPrice>()
                .ReverseMap();

            CreateMap<ResourceEnvelope, Api.Resource>()
                .ReverseMap();
            CreateMap<ResourceName, Api.ResourceName>()
                .ReverseMap();

            CreateMap<RiskPassword, Api.RiskPassword>()
                .ForMember(d => d.PasswordSha1Hash, opt => opt.MapFrom(s => s.Id.Substring(s.Id.LastIndexOf(':') + 1)))
                .ReverseMap();

            CreateMap<UsageSettings, Api.UsageSettings>();
            CreateMap<UsageCurrencyExchange, Api.UsageCurrencyExchange>()
                .ReverseMap();

            CreateMap<AddressSettings, Api.SettingsAddress>();
        }
    }
}
