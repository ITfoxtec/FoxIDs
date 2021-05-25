using AutoMapper;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
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

            CreateMap<ClaimMap, Api.ClaimMap>()
                .ReverseMap();

            CreateMap<OAuthClaimTransform, Api.OAuthClaimTransform>()
                .ReverseMap();

            CreateMap<SamlClaimTransform, Api.SamlClaimTransform>()
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

            CreateMap<Api.TrackResourceItem, ResourceItem>()
                .ReverseMap();

            CreateMap<Api.SendEmail, SendEmail>()
                .ReverseMap();

            CreateMap<Api.LogSettings, ScopedLogger>()
                .ReverseMap();

            CreateMap<Api.LogStreamSettings, ScopedStreamLogger>()
                .ReverseMap();
            CreateMap<Api.LogStreamApplicationInsightsSettings, ScopedStreamApplicationInsightsSettings>()
                .ReverseMap();

            CreateMap<Api.SamlMetadataAttributeConsumingService, SamlMetadataAttributeConsumingService>()
                .ReverseMap();
            CreateMap<Api.SamlMetadataServiceName, SamlMetadataServiceName>()
                .ReverseMap();
            CreateMap<Api.SamlMetadataRequestedAttribute, SamlMetadataRequestedAttribute>()
                .ReverseMap();

            CreateMap<Api.SamlMetadataContactPerson, SamlMetadataContactPerson>()
                .ReverseMap();
        }

        private void UpPartyMapping()
        {
            CreateMap<LoginUpParty, Api.LoginUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<OidcUpParty, Api.OidcUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));
            CreateMap<OidcUpClient, Api.OidcUpClient>()
               .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(s => s)))
               .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)))
               .ReverseMap();

            CreateMap<SamlUpParty, Api.SamlUpParty>()
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)))
                .ForMember(d => d.AuthnRequestBinding, opt => opt.MapFrom(s => s.AuthnBinding.RequestBinding))
                .ForMember(d => d.AuthnResponseBinding, opt => opt.MapFrom(s => s.AuthnBinding.ResponseBinding))
                .ForMember(d => d.LogoutRequestBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.RequestBinding : null))
                .ForMember(d => d.LogoutResponseBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.ResponseBinding : null))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.AuthnBinding, opt => opt.MapFrom(s => new SamlBinding
                {
                    RequestBinding = s.AuthnRequestBinding.HasValue ? (SamlBindingTypes)s.AuthnRequestBinding.Value : SamlBindingTypes.Post,
                    ResponseBinding = s.AuthnResponseBinding.HasValue ? (SamlBindingTypes)s.AuthnResponseBinding.Value : SamlBindingTypes.Post,
                }))
                .ForMember(d => d.LogoutBinding, opt => opt.MapFrom(s => new SamlBinding
                {
                    RequestBinding = s.LogoutRequestBinding.HasValue ? (SamlBindingTypes)s.LogoutRequestBinding.Value : SamlBindingTypes.Post,
                    ResponseBinding = s.LogoutResponseBinding.HasValue ? (SamlBindingTypes)s.LogoutResponseBinding.Value : SamlBindingTypes.Post,
                }));
        }

        private void DownPartyMapping()
        {
            CreateMap<OAuthDownParty, Api.OAuthDownParty>()
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name)))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.ClaimTransforms, opt => opt.MapFrom(s => OrderClaimTransforms(s.ClaimTransforms)))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n.ToLower() })));
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
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name)))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.ClaimTransforms, opt => opt.MapFrom(s => OrderClaimTransforms(s.ClaimTransforms)))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n.ToLower() })));
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
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name)))
                .ForMember(d => d.AuthnRequestBinding, opt => opt.MapFrom(s => s.AuthnBinding.RequestBinding))
                .ForMember(d => d.AuthnResponseBinding, opt => opt.MapFrom(s => s.AuthnBinding.ResponseBinding))
                .ForMember(d => d.LogoutRequestBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.RequestBinding : null))
                .ForMember(d => d.LogoutResponseBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.ResponseBinding : null))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.ClaimTransforms, opt => opt.MapFrom(s => OrderClaimTransforms(s.ClaimTransforms)))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n.ToLower() })))
                .ForMember(d => d.AuthnBinding, opt => opt.MapFrom(s => new SamlBinding 
                    { 
                        RequestBinding = s.AuthnRequestBinding.HasValue ? (SamlBindingTypes)s.AuthnRequestBinding.Value : SamlBindingTypes.Post,
                        ResponseBinding = s.AuthnResponseBinding.HasValue ? (SamlBindingTypes)s.AuthnResponseBinding.Value : SamlBindingTypes.Post,
                    }))
                .ForMember(d => d.LogoutBinding, opt => opt.MapFrom(s => new SamlBinding
                {
                    RequestBinding = s.LogoutRequestBinding.HasValue ? (SamlBindingTypes)s.LogoutRequestBinding.Value : SamlBindingTypes.Post,
                    ResponseBinding = s.LogoutResponseBinding.HasValue ? (SamlBindingTypes)s.LogoutResponseBinding.Value : SamlBindingTypes.Post,
                }));
        }

        private List<T> OrderClaimTransforms<T>(List<T> claimTransforms) where T : Api.ClaimTransform
        {
            return claimTransforms.OrderBy(ct => ct.Order).ToList();
        }
    }
}
