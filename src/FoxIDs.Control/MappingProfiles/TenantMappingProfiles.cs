using AutoMapper;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Generic;
using System.Linq;
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
            UpMapping();
            DownMapping();
        }

        private void Mapping()
        {
            CreateMap<Api.CreateTenantRequest, Api.Tenant>();

            CreateMap<Tenant, Api.Tenant>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Tenant.IdFormat(s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<Track, Api.Track>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<UpParty, Api.UpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<DownParty, Api.DownParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<JsonWebKey, JsonWebKey>()
                .ReverseMap()
                .ForMember(d => d.X5c, opt => opt.NullSubstitute(new List<string>()))
                .ForMember(d => d.KeyOps, opt => opt.NullSubstitute(new List<string>()));

            CreateMap<OAuthClientSecret, Api.OAuthClientSecretResponse>()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Id))
                .ReverseMap()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Name.ToLower().GetFirstInDotList()));

            CreateMap<SamlBinding, Api.SamlBinding>()
                .ReverseMap();
        }

        private void UpMapping()
        {
            CreateMap<LoginUpParty, Api.LoginUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<SamlUpParty, Api.SamlUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));
        }

        private void DownMapping()
        {
            CreateMap<OAuthDownParty, Api.OAuthDownParty>()
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name.ToLower())))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n })));
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
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name.ToLower())))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n })));
            CreateMap<OidcDownClaim, Api.OidcDownClaim>()
                .ReverseMap();
            CreateMap<OidcDownClient, Api.OidcDownClient>()
                .ReverseMap();
            CreateMap<OidcDownScope, Api.OidcDownScope>()
                .ReverseMap();

            CreateMap<SamlDownParty, Api.SamlDownParty>()
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name.ToLower())))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormat(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n })));
        }
    }
}
