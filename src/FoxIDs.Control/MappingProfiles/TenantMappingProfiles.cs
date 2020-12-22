using AutoMapper;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
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
            UpPartyMapping();
            DownPartyMapping();
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

            CreateMap<User, Api.User>()
                .ReverseMap()
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => User.IdFormat(RouteBinding, s.Email.ToLower()).GetAwaiter().GetResult()));

            CreateMap<ClaimAndValues, Api.ClaimAndValues>()
                .ReverseMap();

            CreateMap<TrackKey, Api.TrackKey>()
                .ReverseMap();

            CreateMap<TrackKey, Api.TrackKeyItemsContained>()
                .ForMember(d => d.PrimaryKey, opt => opt.MapFrom(s => s.Keys[0].Key.GetPublicKey()))
                .ForMember(d => d.SecondaryKey, opt => opt.MapFrom(s => s.Keys.Count > 1 ? s.Keys[1].Key.GetPublicKey() : null));

            CreateMap<TrackKeyItem, Api.TrackKeyItemContained>()
                .ForMember(d => d.Key, opt => opt.MapFrom(s => s.Key.GetPublicKey()))
                .ReverseMap();

            CreateMap<Api.TrackKeyItemContainedRequest, TrackKeyItem>();

            CreateMap<OAuthClientSecret, Api.OAuthClientSecretResponse>()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Id))
                .ReverseMap()
                .ForMember(d => d.Id, opt => opt.MapFrom(s => s.Name.ToLower().GetFirstInDotList()));

            CreateMap<SamlBinding, Api.SamlBinding>()
                .ReverseMap();

            CreateMap<Api.TrackResourceItem, ResourceItem>()
                .ReverseMap();

            CreateMap<Api.SendEmail, SendEmail>()
                .ReverseMap();
        }

        private void UpPartyMapping()
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

        private void DownPartyMapping()
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
                .ForMember(d => d.ResourceScopes, opt => opt.MapFrom(s => s.ResourceScopes.OrderBy(rs => rs.Resource)))
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => sc.Scope)))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c.Claim)))
                .ReverseMap();
            CreateMap<OAuthDownResource, Api.OAuthDownResource>()
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => sc)))
                .ReverseMap();
            CreateMap<OAuthDownResourceScope, Api.OAuthDownResourceScope>()
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => s.Scopes)))
                .ReverseMap()
                .ForMember(d => d.Resource, opt => opt.MapFrom(s => s.Resource.ToLower()));
            CreateMap<OAuthDownScope, Api.OAuthDownScope>()
                .ForMember(d => d.VoluntaryClaims, opt => opt.MapFrom(s => s.VoluntaryClaims.OrderBy(vc => vc.Claim)))
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
                .ForMember(d => d.ResourceScopes, opt => opt.MapFrom(s => s.ResourceScopes.OrderBy(rs => rs.Resource)))
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => sc.Scope)))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c.Claim)))
                .ReverseMap();
            CreateMap<OidcDownScope, Api.OidcDownScope>()
                .ForMember(d => d.VoluntaryClaims, opt => opt.MapFrom(s => s.VoluntaryClaims.OrderBy(vc => vc.Claim)))
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
