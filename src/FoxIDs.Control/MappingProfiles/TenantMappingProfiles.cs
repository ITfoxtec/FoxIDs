using AutoMapper;
using FoxIDs.Logic;
using FoxIDs.Models;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Models;
using Microsoft.AspNetCore.Http;
using System;
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
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Tenant.IdFormatAsync(s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<Track, Api.Track>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<UpParty, Api.UpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<DownParty, Api.DownParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<User, Api.User>()
                .ForMember(d => d.ActiveTwoFactorApp, opt => opt.MapFrom(s => !s.TwoFactorAppSecretExternalName.IsNullOrEmpty()))
                .ReverseMap()
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => User.IdFormatAsync(RouteBinding, s.Email.ToLower()).GetAwaiter().GetResult()));

            CreateMap<ClaimAndValues, Api.ClaimAndValues>()
                .ReverseMap();

            CreateMap<ClaimMap, Api.ClaimMap>()
                .ReverseMap();

            CreateMap<OAuthClaimTransform, Api.OAuthClaimTransform>()
                .ForMember(d => d.Action, opt => opt.MapFrom(s => MapAction(s)))
                .ReverseMap();

            CreateMap<SamlClaimTransform, Api.SamlClaimTransform>()
                .ForMember(d => d.Action, opt => opt.MapFrom(s => MapAction(s)))
                .ReverseMap();

            CreateMap<DynamicElement, Api.DynamicElement>()
                .ReverseMap();

            CreateMap<TrackKey, Api.TrackKey>()
                .ReverseMap();

            CreateMap<JsonWebKey, Api.JwtWithCertificateInfo>()
                .ForMember(d => d.CertificateInfo, opt => opt.MapFrom(s => GetCertificateInfo(s)));

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
                .ReverseMap()
                .ForMember(d => d.RequestedAttributes, opt => opt.MapFrom(s => s.RequestedAttributes.OrderBy(a => a.Name)));
            CreateMap<Api.SamlMetadataServiceName, SamlMetadataServiceName>()
                .ReverseMap();
            CreateMap<Api.SamlMetadataRequestedAttribute, SamlMetadataRequestedAttribute>()
                .ReverseMap();

            CreateMap<Api.SamlMetadataContactPerson, SamlMetadataContactPerson>()
                .ReverseMap();
        }

        [Obsolete("backwards compatibility to support spelling error, remove method when 'ClaimTransformActions.AddIfNotObsolete' and 'ClaimTransformActions.ReplaceIfNotObsolete' is removed.")]
        private static ClaimTransformActions MapAction(ClaimTransform ct)
        {
            ClaimTransformValidationLogic.HandleObsoleteActions(ct);
            return ct.Action;
        }

        private void UpPartyMapping()
        {
            CreateMap<LoginUpParty, Api.LoginUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<CreateUser, Api.CreateUser>()
                .ReverseMap();

            CreateMap<OidcUpParty, Api.OidcUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));
            CreateMap<OidcUpClient, Api.OidcUpClient>()
               .ReverseMap()
               .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(s => s)))
               .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)));

            CreateMap<SamlUpParty, Api.SamlUpParty>()
                .ForMember(d => d.AuthnRequestBinding, opt => opt.MapFrom(s => s.AuthnBinding.RequestBinding))
                .ForMember(d => d.AuthnResponseBinding, opt => opt.MapFrom(s => s.AuthnBinding.ResponseBinding))
                .ForMember(d => d.LogoutRequestBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.RequestBinding : null))
                .ForMember(d => d.LogoutResponseBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.ResponseBinding : null))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)))
                .ForMember(d => d.AuthnBinding, opt => opt.MapFrom(s => new SamlBinding
                {
                    RequestBinding = s.AuthnRequestBinding.HasValue ? (SamlBindingTypes)s.AuthnRequestBinding.Value : SamlBindingTypes.Post,
                    ResponseBinding = s.AuthnResponseBinding.HasValue ? (SamlBindingTypes)s.AuthnResponseBinding.Value : SamlBindingTypes.Post,
                }))
                .ForMember(d => d.LogoutBinding, opt => opt.MapFrom(s => new SamlBinding
                {
                    RequestBinding = s.LogoutRequestBinding.HasValue ? (SamlBindingTypes)s.LogoutRequestBinding.Value : SamlBindingTypes.Post,
                    ResponseBinding = s.LogoutResponseBinding.HasValue ? (SamlBindingTypes)s.LogoutResponseBinding.Value : SamlBindingTypes.Post,
                }))
                .ForMember(d => d.AuthnContextClassReferences, opt => opt.MapFrom(s => s.AuthnContextClassReferences.OrderBy(c => c)))
                .ForMember(d => d.MetadataNameIdFormats, opt => opt.MapFrom(s => s.MetadataNameIdFormats.OrderBy(f => f)))
                .ForMember(d => d.MetadataAttributeConsumingServices, opt => opt.MapFrom(s => s.MetadataAttributeConsumingServices.OrderBy(a => a.ServiceName.Name)))
                .ForMember(d => d.MetadataContactPersons, opt => opt.MapFrom(s => s.MetadataContactPersons.OrderBy(c => c.ContactType)));

            CreateMap<TrackLinkUpParty, Api.TrackLinkUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)));
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
                .ReverseMap()
                .ForMember(d => d.ResourceScopes, opt => opt.MapFrom(s => s.ResourceScopes.OrderBy(rs => rs.Resource)))
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => sc.Scope)))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c.Claim)));
            CreateMap<OAuthDownResource, Api.OAuthDownResource>()
                .ReverseMap()
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => sc)));
            CreateMap<OAuthDownResourceScope, Api.OAuthDownResourceScope>()
                .ReverseMap()
                .ForMember(d => d.Resource, opt => opt.MapFrom(s => s.Resource.ToLower()))
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => s.Scopes)));
            CreateMap<OAuthDownScope, Api.OAuthDownScope>()
                .ReverseMap()
                .ForMember(d => d.VoluntaryClaims, opt => opt.MapFrom(s => s.VoluntaryClaims.OrderBy(vc => vc.Claim)));

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
                .ReverseMap()
                .ForMember(d => d.ResourceScopes, opt => opt.MapFrom(s => s.ResourceScopes.OrderBy(rs => rs.Resource)))
                .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(sc => sc.Scope)))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c.Claim)));
            CreateMap<OidcDownScope, Api.OidcDownScope>()
                .ReverseMap()
                .ForMember(d => d.VoluntaryClaims, opt => opt.MapFrom(s => s.VoluntaryClaims.OrderBy(vc => vc.Claim)));

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

            CreateMap<TrackLinkDownParty, Api.TrackLinkDownParty>()
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name)))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.ClaimTransforms, opt => opt.MapFrom(s => OrderClaimTransforms(s.ClaimTransforms)))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => s.AllowUpPartyNames.Select(n => new UpPartyLink { Name = n.ToLower() })));      
        }

        private List<T> OrderClaimTransforms<T>(List<T> claimTransforms) where T : Api.ClaimTransform
        {
            return claimTransforms.OrderBy(ct => ct.Order).ToList();
        }

        private Api.CertificateInfo GetCertificateInfo(JsonWebKey jsonWebKey)
        {
            var certificate = jsonWebKey.ToX509Certificate();
            if (certificate != null)
            {
                return new Api.CertificateInfo
                {
                    Subject = certificate.Subject,
                    ValidFrom = certificate.NotBefore,
                    ValidTo = certificate.NotAfter,
                    Thumbprint = certificate.Thumbprint
                };
            }
            return null;
        }
    }
}
