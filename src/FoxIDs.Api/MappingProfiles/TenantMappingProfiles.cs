using AutoMapper;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Api = FoxIDs.Models.Api;

namespace FoxIDs.MappingProfiles
{
    public class TenantMappingProfiles : Profile
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        private RouteBinding RouteBinding => httpContextAccessor.HttpContext.GetRouteBinding();

        public TenantMappingProfiles(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
            Mapping();
        }

        private void Mapping()
        {
            CreateMap<OAuthDownParty, Api.OAuthDownParty>()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Id.Substring(s.Id.LastIndexOf(':') + 1)))
                .ReverseMap()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormat(RouteBinding, s.Name).GetAwaiter().GetResult()));
            CreateMap<OAuthDownClaim, Api.OAuthDownClaim>()
                .ReverseMap();
            CreateMap<OAuthDownClient, Api.OAuthDownClient>()
                .ReverseMap();
            CreateMap<OAuthDownResource, Api.OAuthDownResource>()
                .ReverseMap();
            CreateMap<OAuthDownResourceScope, Api.OAuthDownResourceScope>()
                .ReverseMap();
            CreateMap<OAuthDownScope, Api.OAuthDownScope>()
                .ReverseMap();

            CreateMap<OidcDownParty, Api.OidcDownParty>()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Id.Substring(s.Id.LastIndexOf(':') + 1)))
                .ReverseMap()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormat(RouteBinding, s.Name).GetAwaiter().GetResult()));
            CreateMap<OidcDownClaim, Api.OidcDownClaim>()
                .ReverseMap();
            CreateMap<OidcDownClient, Api.OidcDownClient>()
                .ReverseMap();
            CreateMap<OidcDownScope, Api.OidcDownScope>()
                .ReverseMap();
        }
    }
}
