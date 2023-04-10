using FoxI = ITfoxtec.Identity;
using System.Security.Claims;
using ITfoxtec.Identity.Saml2.Claims;

namespace FoxIDs
{
    public static class Constants
    {
        public static class DefaultAdminAccount
        {
            public const string Email = "admin@foxids.com";
            public const string Password = "FirstAccess!";
        }

        public static class DefaultLogin
        {
            public const string Name = "login";
        }

        public static class Routes
        {
            public const string RouteTransformerPathKey = "path";

            public const string DefaultAction = "index";
            public const string DefaultSiteController = "w";
            public const string ErrorController = "error";
            public const string DefaultClientController = "client";

            public const string OidcDiscoveryAction = "OpenidConfiguration";
            public const string OidcDiscoveryKeyAction = "Keys";
            public const string OidcDiscoveryController = "OpenIDConfig";
            
            public const string LoginController = "login";
            public const string ActionController = "action";
            public const string MfaController = "mfa";

            public const string OAuthController = "oauth";
            public const string SamlController = "saml";

            public const string OAuthUpJumpController = "oauthupjump";
            public const string SamlUpJumpController = "samlupjump";

            public const string TrackLinkController = "tracklink";

            public const string RouteControllerKey = "controller";
            public const string RouteActionKey = "action";
            public const string RouteBindingKey = "binding";
            public const string RouteBindingCustomDomainHeader = "domainheader";

            public const string SequenceStringKey = Sequence.String;
            public const string KeySequenceKey = "ks";

            public const string PreApikey = "!";
            public const string MasterApiName = "@master";
            public const string MasterTenantName = "master";
            public const string MasterTrackName = "master";

            public const string ApiControllerPreMasterKey = "m";
            public const string ApiControllerPreTenantTrackKey = "t";
        }

        public static class Logs
        {
            public const string TenantName = "f_TenantName";
            public const string TrackName = "f_TrackName";
            public const string GrantType = "f_GrantType";
            public const string Domain = "f_Domain";
            public const string UserAgent = "f_UserAgent";
            public const string UpPartyId = "f_UpPartyId";
            public const string UpPartyClientId = "f_UpPartyClientId";
            public const string UpPartyStatus = "f_UpPartyStatus";
            public const string DownPartyId = "f_DownPartyId";
            public const string DownPartyClientId = "f_DownPartyClientId";
            public const string SequenceId = "f_SequenceId";
            public const string ExternalSequenceId = "f_ExternalSequenceId";            
            public const string AccountAction = "f_AccountAction";            
            public const string SequenceCulture = "f_SequenceCulture";
            public const string Issuer = "f_Issuer";
            public const string Status = "f_Status";
            public const string SessionId = "f_SessionId";
            public const string ExternalSessionId = "f_ExternalSessionId";
            public const string UserId = "f_UserId";
            public const string Email = "f_Email";
            public const string Type = "f_Type";
            public const string FailingLoginCount = "f_FailingLoginCount";
            public const string UsageType = "f_UsageType";
            public const string UsageTokenType = "f_UsageTokenType";

            public static class Results
            {
                public const int PropertiesValueMaxLength = 100;

                public const string Name = "Name";
                public const string Sum = "Sum";
                public const string Message = "Message";
                public const string Details = "Details";
                public const string Properties = "Properties";
                public const string OperationName = "OperationName";
                public const string SeverityLevel = "SeverityLevel";
                public const string TimeGenerated = "TimeGenerated";
                public const string OperationId = "OperationId";
                public const string RequestId = "RequestId";
                public const string RequestPath = "RequestPath";                
                public const string ClientType = "ClientType";
                public const string ClientIp = "ClientIP";
                public const string AppRoleInstance = "AppRoleInstance";
            }
        }

        public static class Models
        {
            public const string CosmosPartitionKeyPath = "/partition_id";

            public const int MasterPartitionIdLength = 30;
            public const string MasterPartitionIdExPattern = @"^[\w:@]*$";
            public const int DocumentPartitionIdLength = 70;
            public const string DocumentPartitionIdExPattern = @"^[\w:\-]*$";

            public static class Master
            {
                public const int IdLength = 10;
                public const string IdRegExPattern = @"^[\w@]*$";
            }

            public static class Plan
            {
                public const int IdLength = 70;
                public const int TextLength = 4000;
                public const string IdRegExPattern = @"^[\w@:\-]*$";
                public const int NameLength = 30;
                public const string NameRegExPattern = @"^[\w\-]*$";
                public const int CurrencyLength = 10;
                public const string CurrencyRegExPattern = @"^[\w]*$";
                public const int CostPerMonthMin = 0;
                public const int IncludedMin = 0;
                public const int FirstLevelThresholdMin = 0;
            }

            public static class Logging
            {
                public const int ScopedStreamLoggersMin = 0;
                public const int ScopedStreamLoggersMax = 5;
                public const int ApplicationInsightsKeyLength = 50;
                public const string ApplicationInsightsConnectionStringRegExPattern = @"^[\w\-=.:;\/]*$";
                public const int ApplicationInsightsConnectionStringLength = 4096;
                public const int LogAnalyticsWorkspaceIdLength = 40;
                public const string LogAnalyticsWorkspaceIdRegExPattern = @"^[a-f0-9\-]*$";
            }

            public static class RiskPassword
            {
                public const int IdLength = 70;
                public const string IdRegExPattern = @"^[\w@:\-]*$";
                public const int CountMin = 1;
                public const int PasswordSha1HashLength = 40;
                public const string PasswordSha1HashRegExPattern = @"^[A-F0-9]*$";
            }

            public static class SecretHash
            {
                public const int IdLength = 40;
                public const int InfoLength = 3;
                public const int SecretLength = 300;
                public const int HashAlgorithmLength = 20;
                public const int HashLength = 2048;
                public const int HashSaltLength = 512;
            }

            public static class Resource
            {
                public const int SupportedCulturesMin = 0;
                public const int SupportedCulturesMax = 50;
                public const int SupportedCulturesLength = 5;
                public const int ResourcesMin = 1;
                public const int ResourcesMax = 5000;
                public const int CultureLength = 5;
                public const int NameLength = 500;
                public const int ValueLength = 500;
            }

            public static class Tenant
            {
                public const int IdLength = 50;
                public const string IdRegExPattern = @"^[a-z0-9_:-]*$";
                public const int NameLength = 30;
                public const string NameRegExPattern = @"^\w[\w\-]*$";
                public const string NameDbRegExPattern = @"^[a-z0-9_][a-z0-9_-]*$";
                public const int CustomDomainLength = 200;
                public const string CustomDomainRegExPattern = @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$";
            }

            public static class Track
            {
                public const int IdLength = 80;
                public const string IdRegExPattern = @"^[a-z0-9_:-]*$";
                public const int NameLength = 30;
                public const string NameRegExPattern = @"^[\w\-]*$";
                public const string NameDbRegExPattern = @"^[a-z0-9_-]*$";
                public const int DisplayNameLength = 100;
                public const string DisplayNameRegExPattern = @"^[\w;:\/\-.,+ ]*$";

                public const int KeysMin = 0;
                public const int KeysMax = 2;

                public const int KeyExternalValidityInMonthsMin = 1;
                public const int KeyExternalValidityInMonthsMax = 12;
                public const int KeyExternalAutoRenewDaysBeforeExpiryMin = 4;
                public const int KeyExternalAutoRenewDaysBeforeExpiryMax = 30;
                public const int KeyExternalPrimaryAfterDaysMin = 2;
                public const int KeyExternalPrimaryAfterDaysMax = 20;
                public const int KeyExternalCacheLifetimeMin = 3600;
                public const int KeyExternalCacheLifetimeMax = 86400;

                public const int ResourcesMin = 0;
                public const int ResourcesMax = 5000;
                public const int SequenceLifetimeMin = 30;
                public const int SequenceLifetimeMax = 10800;

                public const int MaxFailingLoginsMin = 2;
                public const int MaxFailingLoginsMax = 20;
                public const int FailingLoginCountLifetimeMin = 900; // 15 minutes
                public const int FailingLoginCountLifetimeMax = 345600;  // 96 hours / 4 days
                public const int FailingLoginObservationPeriodMin = 60; // 1 minute
                public const int FailingLoginObservationPeriodMax = 14400; // 4 hours

                public const int PasswordLengthMin = 4;
                public const int PasswordLengthMax = 50;

                public const int AllowIframeOnDomainsMin = 0;
                public const int AllowIframeOnDomainsMax = 40;
                public const int AllowIframeOnDomainsLength = 200;

                public const int MasterTrackControlClientBaseUri = 400;

                public static class SendEmail
                {
                    public const int SendgridApiKeyLength = 200;
                    public const int SmtpHostLength = 100;
                    public const int SmtpPortLength = 10;
                    public const int SmtpUsernameLength = 100;
                    public const int SmtpPasswordLength = 200;
                }
                public static class Logging
                {
                    public const int ScopedStreamLoggersMin = 0;
                    public const int ScopedStreamLoggersMax = 5;
                }
            }

            public static class User
            {
                public const int IdLength = 140;
                public const string IdRegExPattern = @"^[\w:\-.+@]*$";
                public const int UserIdLength = 40;
                public const int ClaimsMin = 0;
                public const int ClaimsMax = 100;
                public const int EmailLength = 60;
                public const int ConfirmationCodeLength = 8;
                public const string EmailRegExPattern = @"^[\w:\-.+@]*$";
                public const int TwoFactorAppCodeLength = 50;
            }

            public static class DynamicElements
            {
                public const int ElementsMin = 0;
                public const int ElementsMax = 20;
                public const int ElementsOrderMin = 0;
                public const int ElementsOrderMax = 100;
            }

            public static class Claim
            {
                public const int JwtTypeLength = 80;
                public const string JwtTypeRegExPattern = @"^[\w:\/\-.+]*$";                            
                public const string JwtTypeWildcardRegExPattern = @"^[\w:\/\-.+\*]*$";
                public const int SamlTypeLength = 300;
                public const string SamlTypeRegExPattern = @"^[\w:\/\-.+]*$";
                public const string SamlTypeWildcardRegExPattern = @"^[\w:\/\-.+\*]*$";

                public const int ValuesOAuthMin = 0;
                public const int ValuesUserMin = 1;
                public const int ValuesMax = 100;

                /// <summary>
                /// JWT and SAML claim value max length.
                /// </summary>
                public const int ValueLength = 1000;               

                public const int MapIdLength = 90;
                public const int MapMin = 0;
                public const int MapMax = 100;

                public const int TransformsMin = 0;
                public const int TransformsMax = 100;
                public const int TransformTransformationLength = 300;
                public const int TransformClaimsInMin = 0;
                public const int TransformClaimsInMax = 10;
                public const int TransformOrderMin = 0;
                public const int TransformOrderMax = 1000;
            }

            public static class Party
            {
                public const int NameLength = 30;
                public const string NameRegExPattern = @"^[\w\-]*$";
                public const int IdLength = 110;
                public const string IdRegExPattern = @"^[\w:\-]*$";
                public const int NoteLength = 200;

                public const string NameAndGuidIdRegExPattern = @"^[\w\-]*$";
            }

            public static class DownParty
            {
                public const int AllowUpPartyNamesMin = 0;
                public const int AllowUpPartyNamesMax = 200;
            }

            public static class OAuthDownParty
            {
                public const int AllowCorsOriginsMin = 0;
                public const int AllowCorsOriginsMax = 40;
                public const int AllowCorsOriginLength = 200;

                public const int ScopeLength = 50;
                public const string ScopeRegExPattern = @"^[\w:\-.]*$";

                public static class Client
                {
                    public const int ResourceScopesMin = 1;
                    public const int ResourceScopesMax = 50;
                    public const int ScopesMin = 0;
                    public const int ScopesMax = 100;
                    public const int ClaimsMin = 0;
                    public const int ClaimsMax = 100;
                    public const int VoluntaryClaimsMin = 0;
                    public const int VoluntaryClaimsMax = 100;                    
                    public const int ResponseTypesMin = 1;
                    public const int ResponseTypesMax = 5;
                    public const int ResponseTypeLength = 30;
                    public const int RedirectUrisMin = 0;
                    public const int RedirectUrisMax = 40;
                    public const int RedirectUriLength = 500;
                    public const int SecretsMin = 0;
                    public const int SecretsMax = 10;

                    public const int AuthorizationCodeLifetimeMin = 10; // 10 seconds 
                    public const int AuthorizationCodeLifetimeMax = 900; // 15 minutes
                    public const int AccessTokenLifetimeMin = 300; // 5 minutes
                    public const int AccessTokenLifetimeMax = 86400; // 24 hours
                    public const int RefreshTokenLifetimeMin = 900; // 15 minutes 
                    public const int RefreshTokenLifetimeMax = 15768000; // 6 month
                    public const int RefreshTokenAbsoluteLifetimeMin = 900; // 15 minutes 
                    public const int RefreshTokenAbsoluteLifetimeMax = 31536000; // 12 month
                }

                public static class Resource
                {
                    public const int ScopesMin = 1;
                    public const int ScopesMax = 100;
                }
            }

            public static class OidcDownParty
            {
                public static class Client
                {
                    public const int RedirectUrisMin = 1;
                    public const int IdTokenLifetimeMin = 300; // 5 minutes
                    public const int IdTokenLifetimeMax = 86400; // 24 hours
                }
            }

            public static class TrackLinkDownParty
            {
                public const int SelectedUpPartiesMin = 1;
                public const int SelectedUpPartiesMax = 4;
                public const string SelectedUpPartiesNameRegExPattern = @"^[\*\w\-]*$";
            }

            public static class UpParty
            {
                public const int SessionLifetimeMin = 0; // 0 minutes
                public const int SessionLifetimeMax = 43200; // 12 hours
                public const int SessionAbsoluteLifetimeMin = 0; // 0 minutes 
                public const int SessionAbsoluteLifetimeMax = 172800; // 48 hours
                public const int PersistentAbsoluteSessionLifetimeMin = 0; // 0 minutes 
                public const int PersistentAbsoluteSessionLifetimeMax = 31536000; // 12 month
                public const int HrdDomainMin = 0;
                public const int HrdDomainMax = 5;
                public const int HrdDomainLength = 100;
                public const int HrdDisplayNameLength = 30;
                public const string HrdDisplayNameRegExPattern = "^[^<^>]*$";
                public const string HrdDomainRegExPattern = @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$";
                public const int HrdLogoUrlLength = 500;
                public const string HrdLogoUrlRegExPattern = @"^https:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}";
            }

            public static class OAuthUpParty
            {
                public const int AuthorityLength = 300;
                public const int IssuersApiMin = 0;
                public const int IssuersMin = 1;
                public const int IssuersMax = 10;
                public const int IssuerLength = 300;
                public const int KeysApiMin = 0;
                public const int KeysMin = 1;
                public const int KeysMax = 10;
                public const int OidcDiscoveryUpdateRateMin = 14400; // 4 hours
                public const int OidcDiscoveryUpdateRateMax = 5184000; // 60 days

                public const int ScopeLength = 50;
                public const string ScopeRegExPattern = @"^[\w:\-/.]*$";

                public static class Client
                {
                    public const int ClientIdLength = 300;
                    public const int ScopesMin = 0;
                    public const int ScopesMax = 100;
                    public const int ClaimsMin = 0;
                    public const int ClaimsMax = 100;

                    public const int ResponseModeLength = 30;
                    public const int ResponseTypeLength = 30;

                    public const int RedirectUrisMin = 1;
                    public const int RedirectUrisMax = 40;
                    public const int AuthorizeUrlLength = 500;
                    public const int TokenUrlLength = 500;  
                    public const int UserInfoUrlLength = 500;
                    public const int EndSessionUrlLength = 500;
                }
            }

            public static class LoginUpParty
            {
                public const int TitleLength = 40;
                public const string TitleRegExPattern = "^[^<^>]*$";
                public const int IconUrlLength = 500;
                public const int CssStyleLength = 20000; 
                public const int TwoFactorAppNameLength = 50;
            }

            public static class SamlParty
            {
                public const int MetadataUrlLength = 500;
                public const int MetadataXmlSize = 200000; // 200kB
                public const int MetadataUpdateRateMin = 14400; // 4 hours
                public const int MetadataUpdateRateMax = 5184000; // 60 days
                public const int MetadataNameIdFormatsMin = 0;
                public const int MetadataNameIdFormatsMax = 5;
                public const int MetadataContactPersonsMin = 0;
                public const int MetadataContactPersonsMax = 5;
                public const int MetadataAttributeConsumingServicesMin = 0;
                public const int MetadataAttributeConsumingServicesMax = 10;
                public const int MetadataRequestedAttributesMin = 1;
                public const int MetadataRequestedAttributesMax = 100;

                public const int IssuerLength = 300;
                public const int SignatureAlgorithmLength = 100;
                public const int KeysMax = 10;

                public const int ClaimsMin = 0;
                public const int ClaimsMax = 500;

                public static class Up
                {
                    public const int MetadataServiceNameLangLength = 10;

                    public const int KeysMin = 1;
                    public const int AuthnUrlLength = 500;
                    public const int LogoutUrlLength = 500;

                    public const int AuthnContextClassReferencesMin = 0;
                    public const int AuthnContextClassReferencesMax = 20;
                }

                public static class Down
                {
                    public const int KeysMin = 0;
                    public const int SubjectConfirmationLifetimeMin = 60; // 1 minutes 
                    public const int SubjectConfirmationLifetimeMax = 900; // 15 minutes
                    public const int IssuedTokenLifetimeMin = 300; // 5 minutes 
                    public const int IssuedTokenLifetimeMax = 86400; // 24 hours
                    public const int AcsUrlsMin = 1;
                    public const int AcsUrlsMax = 10;
                    public const int AcsUrlsLength = 500;
                    public const int SingleLogoutUrlLength = 500;
                    public const int LoggedOutUrlLength = 500;
                }
            }
        }

        public static class ControlClient
        {
            public const string ClientId = "foxids_control_client";
        }

        public static class ControlApi
        {
            public const string Version = "v1";
            public readonly static string[] SupportedApiHttpMethods = { "GET", "PUT", "POST", "DELETE" };

            public const string ResourceName = "foxids_control_api";

            public static class ResourceAndScope
            {
                public readonly static string Master = $"{ResourceName}:{Scope.Master}";
                public readonly static string Tenant = $"{ResourceName}:{Scope.Tenant}";
            }

            public static class Scope
            {
                public const string Master = "foxids:master";
                public const string Tenant = "foxids:tenant";
            }

            public static class Role
            {
                public const string TenantAdmin = "foxids:tenant.admin";
            }
        }

        public static class Sequence
        {
            public const string Object = "sequence_object";
            public const string String = "sequence_string";
            public const string Start = "sequence_start";
            public const string Valid = "sequence_valid";

            public const int MaxLength = FoxI.IdentityConstants.MessageLength.StateMax;
        }

        public static class SecurityHeader
        {            
            public const string ImgSrcDomains = "img_src_domains";
            public const string FormActionDomains = "form_action_domains";
            public const string FormActionDomainsAllowAll = "form_action_domains_allow_all";
            public const string FrameSrcDomains = "frame_src_domains";
            public const string FrameSrcDomainsAllowAll = "frame_src_domains_allow_all";            
            public const string FrameAllowIframeOnDomains = "frame_allow_iframe_on_domains";            
        }

        public static class Endpoints
        {
            public const string Logout = "logout";
            public const string SingleLogoutDone = "singlelogoutdone";            
            public const string CancelLogin = "cancellogin";
            public const string CreateUser = "createuser";
            public const string ChangePassword = "changepassword";
            public const string ResetPassword = "resetpassword";
            public const string EmailConfirmation = "emailconfirmation";
            public const string RegisterTwoFactor = "regtwofactor";
            public const string TwoFactor = "twofactor";

            public const string Authorize = "authorize";
            public const string AuthorizationResponse = "authorizationresponse";
            public const string Token = "token";
            public const string UserInfo = "userinfo";
            public const string EndSession = "endsession";
            public const string EndSessionResponse = "endsessionresponse";
            public const string FrontChannelLogout = "frontchannellogout";
            public const string FrontChannelLogoutDone = "frontchannellogoutdone";

            public const string SamlAuthn = "authn";
            public const string SamlLogout = "logout";
            public const string SamlAcs = "acs";
            public const string SamlSingleLogout = "singlelogout";
            public const string SamlLoggedOut = "loggedout";
            public const string SamlIdPMetadata = "idpmetadata";
            public const string SamlSPMetadata = "spmetadata";

            public const string TrackLinkAuthRequest = "authrequest";
            public const string TrackLinkAuthResponse = "authresponse";
            public const string TrackLinkRpLogoutRequest = "rplogoutrequest";
            public const string TrackLinkRpLogoutResponse = "rplogoutresponse";

            public static class UpJump
            {
                public const string AuthenticationRequest = "authenticationrequest";
                public const string EndSessionRequest = "endsessionrequest";

                public const string AuthnRequest = "authnrequest";
                public const string LogoutRequest = "logoutrequest";
                public const string SingleLogoutRequestJump = "singlelogoutrequestjump";

                public const string TrackLinkRpLogoutRequestJump = "rplogoutrequestjump";
            }
        }

        public static class OAuth
        {
            public readonly static string[] DefaultResponseTypes = new string[] 
            {
                FoxI.IdentityConstants.ResponseTypes.Code, 
                $"{FoxI.IdentityConstants.ResponseTypes.Code} {FoxI.IdentityConstants.ResponseTypes.Token}", 
                FoxI.IdentityConstants.ResponseTypes.Token
            };

            public static class ResponseErrors
            {
                /// <summary>
                /// Login canceled by user.
                /// </summary>
                public const string LoginCanceled = "login_canceled";

            }
        }

        public static class Oidc
        {
            public readonly static string[] DefaultResponseTypes = new string[]
            {
                FoxI.IdentityConstants.ResponseTypes.Code,
                $"{FoxI.IdentityConstants.ResponseTypes.Code} {FoxI.IdentityConstants.ResponseTypes.IdToken}",
                $"{FoxI.IdentityConstants.ResponseTypes.Code} {FoxI.IdentityConstants.ResponseTypes.Token} {FoxI.IdentityConstants.ResponseTypes.IdToken}",
                $"{FoxI.IdentityConstants.ResponseTypes.Token} {FoxI.IdentityConstants.ResponseTypes.IdToken}",
                FoxI.IdentityConstants.ResponseTypes.IdToken
            };

            public static class Acr
            {
                public const string Mfa = "urn:foxids:mfa";
            }
        }

        public static class Saml
        {
            public const string RelayState = "RelayState";

            public static class AuthnContextClassTypes
            {
                public const string Mfa = "urn:foxids:mfa";
            }
        }

        /// <summary>
        /// Default claims.
        /// </summary>
        public static class DefaultClaims
        {
            /// <summary>
            /// Default ID Token claims.
            /// </summary>
            public readonly static string[] IdToken = FoxI.IdentityConstants.DefaultJwtClaims.IdToken.ConcatOnce(new string[] { JwtClaimTypes.UpParty, JwtClaimTypes.UpPartyType, JwtClaimTypes.SubFormat }).ToArray();

            /// <summary>
            /// Default Access Token claims.
            /// </summary>
            public readonly static string[] AccessToken = FoxI.IdentityConstants.DefaultJwtClaims.AccessToken;

            /// <summary>
            /// Default JWT Token up-party claims.
            /// </summary>
            public readonly static string[] JwtTokenUpParty = { FoxI.JwtClaimTypes.Subject, FoxI.JwtClaimTypes.SessionId, JwtClaimTypes.UpParty, JwtClaimTypes.UpPartyType, FoxI.JwtClaimTypes.AuthTime, FoxI.JwtClaimTypes.Acr, FoxI.JwtClaimTypes.Amr };

            /// <summary>
            /// Exclude JWT Token up-party claims.
            /// </summary>
            public readonly static string[] ExcludeJwtTokenUpParty = { FoxI.JwtClaimTypes.Issuer, FoxI.JwtClaimTypes.Audience, FoxI.JwtClaimTypes.ExpirationTime, FoxI.JwtClaimTypes.NotBefore, FoxI.JwtClaimTypes.IssuedAt, FoxI.JwtClaimTypes.Nonce, FoxI.JwtClaimTypes.Azp, FoxI.JwtClaimTypes.AtHash, FoxI.JwtClaimTypes.CHash };

            /// <summary>
            /// Default SAML claims.
            /// </summary>
            public readonly static string[] SamlClaims = { ClaimTypes.NameIdentifier, Saml2ClaimTypes.NameIdFormat, Saml2ClaimTypes.SessionIndex, ClaimTypes.Upn, ClaimTypes.AuthenticationInstant, ClaimTypes.AuthenticationMethod, SamlClaimTypes.UpParty, SamlClaimTypes.UpPartyType };
        }

        /// <summary>
        /// JWT tokens embed as a claim in a token.
        /// </summary>
        public class EmbeddedJwtToken
        {
            public readonly static string[] JwtTokenClaims = { JwtClaimTypes.AccessToken };
            public const int ValueLength = 4000;
        }

        public static class JwtClaimTypes
        {
            public const string UpParty = "up_party";
            public const string UpPartyType = "up_party_type";
            public const string SubFormat = "sub_format";
            public const string AccessToken = "access_token";
        }

        public static class SamlClaimTypes
        {
            public const string UpParty = "http://schemas.foxids.com/identity/claims/upparty";
            public const string UpPartyType = "http://schemas.foxids.com/identity/claims/uppartytype";
            public const string AccessToken = "http://schemas.foxids.com/identity/claims/accesstoken";
            public const string Amr = "http://schemas.foxids.com/identity/claims/amr";
        }

        /// <summary>
        /// Default mappings between JWT and SAML claim types.
        /// </summary>
        public static class DefaultClaimMappings
        {
            /// <summary>
            /// Default locked claim mappings.
            /// </summary>
            public readonly static ClaimMap[] LockedMappings = new ClaimMap[] 
            {
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Subject, SamlClaim = ClaimTypes.NameIdentifier },
                new ClaimMap { JwtClaim = JwtClaimTypes.SubFormat, SamlClaim = Saml2ClaimTypes.NameIdFormat },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.SessionId, SamlClaim = Saml2ClaimTypes.SessionIndex },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Amr, SamlClaim = SamlClaimTypes.Amr },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Email, SamlClaim = ClaimTypes.Email },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.GivenName, SamlClaim = ClaimTypes.GivenName },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.FamilyName, SamlClaim = ClaimTypes.Surname },
                new ClaimMap { JwtClaim = JwtClaimTypes.UpParty, SamlClaim = SamlClaimTypes.UpParty },
                new ClaimMap { JwtClaim = JwtClaimTypes.UpPartyType, SamlClaim = SamlClaimTypes.UpPartyType },
                new ClaimMap { JwtClaim = JwtClaimTypes.AccessToken, SamlClaim = SamlClaimTypes.AccessToken }
            };

            /// <summary>
            /// Default changeable claim mappings.
            /// </summary>
            public readonly static ClaimMap[] ChangeableMappings = new ClaimMap[]
            {
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.GivenName, SamlClaim = ClaimTypes.GivenName },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.FamilyName, SamlClaim = ClaimTypes.Surname },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Role, SamlClaim = ClaimTypes.Role },
            };

            public class ClaimMap
            {
                public string JwtClaim { get; set; }
                public string SamlClaim { get; set; }
            }
        }
    }
}