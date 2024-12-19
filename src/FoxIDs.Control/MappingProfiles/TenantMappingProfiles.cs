using AutoMapper;
using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.Models.Config;
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

            CreateMap<Tenant, Api.TenantResponse>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Tenant.IdFormatAsync(s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<Customer, Api.Customer>()
                .ReverseMap();
            CreateMap<Address, Api.Address>()
                .ReverseMap();

            CreateMap<UsageSellerSettings, Seller>();

            CreateMap<Payment, Api.Payment>();

            CreateMap<Used, Api.UsedBase>()
                .ForMember(d => d.HasItems, opt => opt.MapFrom(s => s.Items != null && s.Items.Count() > 0));
            CreateMap<Used, Api.Used>()
                .ForMember(d => d.HasItems, opt => opt.MapFrom(s => s.Items != null && s.Items.Count() > 0));
            CreateMap<Used, Api.UpdateUsageRequest>()
                .ReverseMap()
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items != null && s.Items.Any() ? s.Items.OrderBy(i => i.Day) : null))
                .ForMember(d => d.TenantName, opt => opt.MapFrom(s => s.TenantName.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Used.IdFormatAsync(s.TenantName.ToLower(), s.PeriodBeginDate.Year, s.PeriodBeginDate.Month).GetAwaiter().GetResult()));
            CreateMap<UsedItem, Api.UsedItem>()
                .ReverseMap();
            CreateMap<Invoice, Api.Invoice>();
            CreateMap<InvoiceLine, Api.InvoiceLine>();

            CreateMap<Track, Api.Track>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => Track.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<UpParty, Api.UpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<UpPartyWithProfile<UpPartyProfile>, Api.UpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<DownParty, Api.DownParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<User, Api.User>()
                .ForMember(d => d.ActiveTwoFactorApp, opt => opt.MapFrom(s => !s.TwoFactorAppSecret.IsNullOrEmpty() || !s.TwoFactorAppSecretExternalName.IsNullOrEmpty()))
                .ReverseMap()
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Email.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => User.IdFormatAsync(RouteBinding, new User.IdKey { Email = s.Email.ToLower(), UserId = s.UserId }).GetAwaiter().GetResult()));

            CreateMap<User, Api.MyUser>();

            CreateMap<UserControlProfile, Api.UserControlProfile>()
                .ReverseMap();

            CreateMap<ExternalUser, Api.ExternalUserRequest>()
                .ReverseMap();

            CreateMap<ExternalUser, Api.ExternalUser>()
                .ForMember(d => d.UpPartyName, opt => opt.MapFrom(s => s.Id.Split(':', StringSplitOptions.None).SkipLast(1).Last()))
                .ReverseMap();

            CreateMap<ClaimAndValues, Api.ClaimAndValues>()
                .ReverseMap();

            CreateMap<ClaimMap, Api.ClaimMap>()
                .ReverseMap();

            CreateMap<OAuthClaimTransform, Api.OAuthClaimTransform>()
                .ForMember(d => d.Action, opt => opt.MapFrom(s => MapAction(s)))
                .ForMember(d => d.Secret, opt => opt.MapFrom(s => s.Secret != null && s.Secret.Length > 20 ? s.Secret.Substring(0, 3) : s.Secret))
                .ReverseMap();

            CreateMap<SamlClaimTransform, Api.SamlClaimTransform>()
                .ForMember(d => d.Action, opt => opt.MapFrom(s => MapAction(s)))
                .ForMember(d => d.Secret, opt => opt.MapFrom(s => s.Secret != null && s.Secret.Length > 20 ? s.Secret.Substring(0, 3) : s.Secret))
                .ReverseMap();

            CreateMap<DynamicElement, Api.DynamicElement>()
                .ReverseMap();

            CreateMap<OAuthAdditionalParameter, Api.OAuthAdditionalParameter>()
                .ReverseMap();

            CreateMap<TrackKey, Api.TrackKey>()
                .ReverseMap();

            CreateMap<JsonWebKey, Api.JwkWithCertificateInfo>()
                .ForMember(d => d.CertificateInfo, opt => opt.MapFrom(s => GetCertificateInfo(s)));

            CreateMap<ClientKey, Api.ClientKey>();

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

            CreateMap<ResourceItem, Api.ResourceItem>();
            CreateMap<ResourceItem, Api.TrackResourceItem>()
                .ReverseMap();
            CreateMap<ResourceCultureItem, Api.ResourceCultureItem>()
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

            CreateMap<Api.SamlMetadataOrganization, SamlMetadataOrganization>()
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
            CreateMap<UpPartyLink, Api.UpPartyLink>();

            CreateMap<UpPartyProfile, Api.UpPartyProfile>();

            CreateMap<LoginUpParty, Api.LoginUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));

            CreateMap<CreateUser, Api.CreateUser>()
                .ReverseMap();

            CreateMap<LinkExternalUser, Api.LinkExternalUser>()
                .ReverseMap();

            CreateMap<OAuthUpParty, Api.OAuthUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));
            CreateMap<OAuthUpClient, Api.OAuthUpClient>()
               .ReverseMap()
               .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)));

            CreateMap<OidcUpParty, Api.OidcUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()));
            CreateMap<OidcUpClient, Api.OidcUpClient>()
                .ForMember(d => d.ClientSecret, opt => opt.MapFrom(s => s.ClientSecret != null && s.ClientSecret.Length > 20 ? s.ClientSecret.Substring(0, 3) : s.ClientSecret))
               .ReverseMap()
               .ForMember(d => d.Scopes, opt => opt.MapFrom(s => s.Scopes.OrderBy(s => s)))
               .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)));
            CreateMap<OAuthUpPartyProfile, Api.OidcUpPartyProfile>()
                .ReverseMap();
            CreateMap<OAuthUpClientProfile, Api.OidcUpClientProfile>()
                .ReverseMap();

            CreateMap<SamlUpParty, Api.SamlUpParty>()
                .ForMember(d => d.Issuer, opt => opt.MapFrom(s => s.Issuers.First()))
                .ForMember(d => d.AuthnRequestBinding, opt => opt.MapFrom(s => s.AuthnBinding.RequestBinding))
                .ForMember(d => d.AuthnResponseBinding, opt => opt.MapFrom(s => s.AuthnBinding.ResponseBinding))
                .ForMember(d => d.LogoutRequestBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.RequestBinding : null))
                .ForMember(d => d.LogoutResponseBinding, opt => opt.MapFrom(s => s.LogoutBinding != null ? (Api.SamlBindingTypes?)s.LogoutBinding.ResponseBinding : null))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.Issuers, opt => opt.MapFrom(s => new List<string> { s.Issuer }))
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
            CreateMap<SamlUpPartyProfile, Api.SamlUpPartyProfile>()
                .ReverseMap();

            CreateMap<TrackLinkUpParty, Api.TrackLinkUpParty>()
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)));
            CreateMap<TrackLinkUpPartyProfile, Api.TrackLinkUpPartyProfile>()
                .ReverseMap();

            CreateMap<ExternalLoginUpParty, Api.ExternalLoginUpParty>()
                .ForMember(d => d.Secret, opt => opt.MapFrom(s => s.Secret != null && s.Secret.Length > 20 ? s.Secret.Substring(0, 3) : s.Secret))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => UpParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.Claims, opt => opt.MapFrom(s => s.Claims.OrderBy(c => c)));
            CreateMap<ExternalLoginUpPartyProfile, Api.ExternalLoginUpPartyProfile>()
                .ReverseMap();
        }

        private void DownPartyMapping()
        {
            CreateMap<OAuthDownParty, Api.OAuthDownParty>()
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name)))
                .ReverseMap()
                .ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLower()))
                .ForMember(d => d.Id, opt => opt.MapFrom(s => DownParty.IdFormatAsync(RouteBinding, s.Name.ToLower()).GetAwaiter().GetResult()))
                .ForMember(d => d.ClaimTransforms, opt => opt.MapFrom(s => OrderClaimTransforms(s.ClaimTransforms)))
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => AllowUpParties(s)));
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
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => AllowUpParties(s)))
                .ForMember(d => d.IsTest, opt => opt.Ignore())
                .ForMember(d => d.TestUrl, opt => opt.Ignore())
                .ForMember(d => d.TestExpireAt, opt => opt.Ignore());
            CreateMap<OidcDownParty, Api.OidcDownParty>()
                .ForMember(d => d.AllowUpPartyNames, opt => opt.MapFrom(s => s.AllowUpParties.Select(aup => aup.Name)));

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
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => AllowUpParties(s)))
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
                .ForMember(d => d.AllowUpParties, opt => opt.MapFrom(s => AllowUpParties(s)));      
        }

        private List<UpPartyLink> AllowUpParties(Api.IDownParty downParty)
        {
            if (downParty.AllowUpParties?.Count() > 0)
            {
                return downParty.AllowUpParties.Select(n => new UpPartyLink { Name = n.Name.ToLower(), ProfileName = n.ProfileName?.ToLower() }).ToList();
            }
            else 
            {
                return downParty.AllowUpPartyNames?.Select(n => new UpPartyLink { Name = n.ToLower() }).ToList();
            }
        }

        private List<T> OrderClaimTransforms<T>(List<T> claimTransforms) where T : Api.ClaimTransform
        {
            return claimTransforms.OrderBy(ct => ct.Order).ToList();
        }

        private Api.CertificateInfo GetCertificateInfo(JsonWebKey jsonWebKey)
        {
            if (jsonWebKey.X5c?.Count() > 0)
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
            }
            return null;
        }
    }
}
