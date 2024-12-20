using FoxI = ITfoxtec.Identity;
using System.Security.Claims;
using ITfoxtec.Identity.Saml2.Claims;
using System.Net.Http;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.Xml;

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
            public const string ErrorAction = "error";

            public const string OidcDiscoveryAction = "OpenidConfiguration";
            public const string OidcDiscoveryKeyAction = "Keys";
            public const string OidcDiscoveryController = "OpenIDConfig";
            
            public const string LoginController = "login";
            public const string ExtLoginController = "extlogin";
            public const string ActionController = "action";
            public const string MfaController = "mfa";
            public const string ExtController = "ext";

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

            public const string MainTenantName = "main";

            public const string ApiPath = "api";

            public const string ApiControllerPreMasterKey = "m";
            public const string ApiControllerPreTenantTrackKey = "t";

            public const string ControlSiteName = "control";

            public const string HealthController = "health";
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
            public const string PhoneConfirmation = "phoneconfirmation";
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

        public static class TrackDefaults
        {
            public const string DefaultTrackTestDisplayName = "Test";
            public const string DefaultTrackTestName = "test";            
            public const string DefaultTrackProductionDisplayName = "Production";
            public const string DefaultTrackProductionName = "-";

            public const int DefaultSequenceLifetime = 7200;
            public const int DefaultMaxFailingLogins = 5;
            public const int DefaultFailingLoginCountLifetime = 36000;
            public const int DefaultFailingLoginObservationPeriod = 3600;
            public const int DefaultPasswordLength = 6;
        }

        public static class Logs
        {
            public const string LogName = "foxids-log";

            public const string Message = "Message";
            public const string Details = "Details";
            public const string Value = "Value";
            public const string MachineName = "MachineName";
            public const string ClientIP = "ClientIP";
            public const string Domain = "Domain";
            public const string UserAgent = "UserAgent";
            public const string OperationId = "OperationId";
            public const string RequestId = "RequestId";
            public const string RequestPath = "RequestPath";
            public const string TenantName = "TenantName";
            public const string TrackName = "TrackName";
            public const string GrantType = "GrantType";
            public const string UpPartyId = "UpPartyId";
            public const string UpPartyClientId = "UpPartyClientId";
            public const string UpPartyStatus = "UpPartyStatus";
            public const string DownPartyId = "DownPartyId";
            public const string DownPartyClientId = "DownPartyClientId";
            public const string SequenceId = "SequenceId";
            public const string ExternalSequenceId = "ExternalSequenceId";
            public const string AccountAction = "AccountAction";
            public const string SequenceCulture = "SequenceCulture";
            public const string Issuer = "Issuer";
            public const string Status = "Status";
            public const string SessionId = "SessionId";
            public const string ExternalSessionId = "ExternalSessionId";
            public const string UserId = "UserId";
            public const string Email = "Email";
            public const string LogType = "LogType";
            public const string FailingLoginCount = "FailingLoginCount";
            public const string UsageType = "UsageType";
            public const string UsageLoginType = "UsageLoginType";
            public const string UsageTokenType = "UsageTokenType";
            public const string UsageAddRating = "UsageAddRating";

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
                public const string MachineName = "MachineName";
                public const string ClientType = "ClientType";
                public const string ClientIp = "ClientIP";
                public const string UserAgent = "UserAgent";
                public const string AppRoleInstance = "AppRoleInstance";
                public const string UpPartyId = "UpPartyId";
                public const string DownPartyId = "DownPartyId";
                public const string UserId = "UserId";
                public const string Email = "Email";
            }

            public static class IndexName
            {
                public const string Errors = "errors";
                public const string Events = "events";
                public const string Traces = "traces";
                public const string Metrics = "metrics";
            }
        }

        public static class Models
        {
            public const string CosmosPartitionKeyPath = "/partition_id";

            public const int DefaultNameLength = 8;
            public const int DefaultNameMaxAttempts = 3;

            public const int MasterPartitionIdLength = 30;
            public const string MasterPartitionIdExPattern = @"^[\w:@]*$";
            public const int DocumentPartitionIdLength = 110;
            public const string DocumentPartitionIdExPattern = @"^[\w:\-]*$";

            public const int ListPageSize = 50;

            public static class DataType
            {
                public const string Master = "master";

                public const string Tenant = "tenant";
                public const string Track = "track";
                public const string Party = "party";
                public const string UpParty = "party:up";
                public const string DownParty = "party:down";
                public const string User = "user";
                public const string UserControlProfile = "ucp";                
                public const string ExternalUser = "extu";
                public const string AuthCodeTtlGrant = "acgrant";
                public const string RefreshTokenGrant = "rtgrant";
                public const string RiskPassword = "prisk";
                public const string Plan = "plan";
                public const string DataProtection = "datap";
                public const string Used = "used";
                public const string UsageSettings = "uset";

                // data type used for cache
                public const string Cache = "cache";
            }

            public static class Master
            {
                public const int IdLength = 10;
                public const string IdRegExPattern = @"^[\w@]*$";
            }

            public static class Plan
            {
                public const int PlansMax = 10;

                public const int IdLength = 70;
                public const int TextLength = 4000;
                public const string IdRegExPattern = @"^[\w@:\-]*$";
                public const int NameLength = 30;
                public const string NameRegExPattern = @"^[\w\-]*$";
                public const int DisplayNameLength = 50;
                public const string DisplayNameRegExPattern = @"^[\w;:\/\-.,+ ]*$";
                public const int CostPerMonthMin = 0;
                public const int IncludedMin = 0;
                public const int LimitedThresholdMin = 0;
                public const int FirstLevelThresholdMin = 0;
            }

            public static class Payment
            {
                public const int CardTokenLength = 100;

                public const int CustomerIdLength = 100;
                public const int MandateIdLength = 100;
                public const int CardHolderLength = 500;
                public const int CardNumberInfoLength = 10; 
                public const int CardLabelLength = 100;
            }

            public static class Address
            {
                public const int NameLength = 60;
                public const int VatNumberLength = 50;
                public const int AddressLine1Length = 50;
                public const int AddressLine2Length = 50;
                public const int PostalCodeLength = 50;
                public const int CityLength = 50;
                public const int StateRegionLength = 50;
                public const int CountryLength = 50;
            }

            public static class Customer
            {
                public const int InvoiceEmailsMin = 1;
                public const int InvoiceEmailsMax = 5;
                public const int ReferenceLength = 60;
            }

            public static class Seller
            {
                public const int BccEmailsMin = 1;
                public const int BccEmailsMax = 5;
            }

            public static class UsageSettings
            {
                public const int IdLength = 20;
                public const string IdRegExPattern = @"^[\w@:\-]*$";
                public const int CurrencyExchangesMin = 0;
                public const int CurrencyExchangesMax = 10;
                public const int HourPriceMin = 0;
                public const int InvoiceNumberMin = 0;
                public const int InvoiceNumberPrefixLength = 20;
                public const string InvoiceNumberPrefixRegExPattern = @"^[\w;:\/\-.,+ ]*$";
            }

            public static class Currency
            {
                public const int CurrencyLength = 3;
                public const string Eur = "EUR";
            }

            public static class Used
            {
                public const int IdLength = 80;
                public const string IdRegExPattern = @"^[a-z0-9_:-]*$";

                public const int PeriodYearMin = 2000;
                public const int PeriodMonthMin = 1;
                public const int PeriodMonthMax = 12;
                public const int ItemsMin = 0;
                public const int ItemsMax = 1000;
                public const int DayMin = 0;
                public const int DayMax = 31;
                public const int QuantityMin = 0;
                public const int PriceMin = 0;
                public const int InvoicesMin = 0;
                public const int InvoicesMax = 10;
                public const int InvoiceLinesMin = 1;
                public const int UsedItemTextLength = 200;
                public const int InvoiceLineTextLength = 300;
            }

            public static class Logging
            {
                public const int ScopedStreamLoggersMin = 0;
                public const int ScopedStreamLoggersMax = 5;
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

            public static class DataProtection
            {
                public const int IdLength = 70;
                public const string IdRegExPattern = @"^[\w@:\-]*$";
                public const int NameLength = 40;
                public const string NameRegExPattern = @"^[a-z0-9\-]*$";
                public const int KeyDataLength = 4000;
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

            public static class Certificate
            {
                public const int EncodeCertificateLength = 20000;
                public const int CertificateLength = SecretHash.SecretLength;
            }

            public static class Resource
            {
                public const string DefaultLanguage = "en";

                public const int SupportedCulturesMin = 0;
                public const int SupportedCulturesMax = 50;
                public const int SupportedCulturesLength = 5;
                public const int ResourcesMin = 1;
                public const int ResourcesMax = 300;
                public const int CultureLength = 5;
                public const int NameLength = 500;
                public const int ValueLength = 500;
            }

            public static class Tenant
            {
                public const int IdLength = 70;
                public const string IdRegExPattern = @"^[a-z0-9_:-]*$";
                public const int NameLength = 50;
                public const string NameRegExPattern = @"^\w[\w\-]*$";
                public const string NameDbRegExPattern = @"^[a-z0-9_][a-z0-9_-]*$";
                public const int CustomDomainLength = 200;
                public const string CustomDomainRegExPattern = @"^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]$";
            }

            public static class Track
            {
                public const int IdLength = 120;
                public const string IdRegExPattern = @"^[a-z0-9_:-]*$";
                public const int NameLength = 50;
                public const string NameRegExPattern = @"^[\w\-]*$";
                public const string NameDbRegExPattern = @"^[a-z0-9_-]*$";
                public const int DisplayNameLength = 100;
                public const string DisplayNameRegExPattern = @"^[\w;:\/\-.,+ ]*$";

                public const int KeysMin = 0;
                public const int KeysMax = 2;

                public const int KeyValidityInMonthsMin = 1;
                public const int KeyValidityInMonthsMax = 12;
                public const int KeyAutoRenewDaysBeforeExpiryMin = 4;
                public const int KeyAutoRenewDaysBeforeExpiryMax = 30;
                public const int KeyPrimaryAfterDaysMin = 2;
                public const int KeyPrimaryAfterDaysMax = 20;
                public const int KeyExternalCacheLifetimeMin = 3600;
                public const int KeyExternalCacheLifetimeMax = 86400;

                public const int ResourcesMin = 0;
                public const int ResourcesMax = 250;
                public const int SequenceLifetimeMin = 30;
                public const int SequenceLifetimeMax = 18000;

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
                    public const int FromNameLength = 100;
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
                public const int IdLength = 180;
                public const string IdRegExPattern = @"^[\w:\-.+@]*$";


                public const int AdditionalIdsMin = 0;
                public const int AdditionalIdsMax = 2;

                public const int UserIdLength = 40;
                public const string UserIdRegExPattern = @"^[\w\-]*$";
                public const int ClaimsMin = 0;
                public const int ClaimsMax = 100;
                public const int EmailLength = 60;
                public const string EmailRegExPattern = @"^[\w:\-.+@]*$";
                public const int UsernameLength = 60;
                public const string UsernameRegExPattern = @"^[\p{L}0-9:\-_.+@]*$";
                public const int PhoneLength = 30;
                public const string PhoneRegExPattern = @"^\+[0-9]*$";
                public const int ConfirmationCodeLength = 8;
                public const int TwoFactorAppCodeLength = 50;
            }

            public static class UserControlProfile
            {
                public const int IdLength = 170;
                public const string IdRegExPattern = @"^[\w:\-.+@]*$";
                public const int UserHashIdLength = 50;
            }
            
            public static class ExternalUser
            {
                public const int IdLength = 220;
                public const string IdRegExPattern = @"^[\w:\-.+@]*$";
                public const int LinkClaimValueHashLength = 50;
            }

            public static class UserLoginExt
            {
                public const int UsernameLength = 60;
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
                public const int JwtTypeLength = 100;
                public const string JwtTypeRegExPattern = @"^[\w:\/\-.+]*$";                            
                public const string JwtTypeWildcardRegExPattern = @"^[\w:\/\-.+\*]*$";
                public const int SamlTypeLength = 300;
                public const string SamlTypeRegExPattern = @"^[\w:\/\-.+]*$";
                public const string SamlTypeWildcardRegExPattern = @"^[\w:\/\-.+\*]*$";

                public const int ValuesOAuthMin = 0;
                public const int ValuesUserMin = 1;
                public const int ValuesMax = 100;
                public const int ProcessValuesMax = 1000;

                /// <summary>
                /// JWT and SAML claim value max length.
                /// </summary>
                public const int ValueLength = 10000;
                public const int LimitedValueLength = 1000;
                public const int ProcessValueLength = 200000;

                public const int IdTokenLimitedHintValueLength = 8000;

                public const int MapIdLength = 90;
                public const int MapMin = 0;
                public const int MapMax = 100;

                public const int TransformNameLength = 10;
                public const string TransformNameRegExPattern = @"^[\w\-]*$";
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
                public const int NameLength = 50;
                public const int ProfileNameLength = 20;
                public const string NameRegExPattern = @"^[\w\-]*$";
                public const int IdLength = 170;
                public const string IdRegExPattern = @"^[\w:\-]*$";
                public const int DisplayNameLength = 100;
                public const string DisplayNameRegExPattern = @"^[\w;:\/\-.,+()\[\]{} ]*$";
                public const int NoteLength = 200;

                public const int IssuerLength = 300;

                public const string NameAndGuidIdRegExPattern = @"^[\w\-]*$";
            }

            public static class DownParty
            {
                public const int PartiesMax = 1000;

                public const int AllowUpPartyNamesMin = 0;
                public const int AllowUpPartyNamesMax = 200;

                public const int UrlLengthMax = 10240;
            }

            public static class OAuthDownParty
            {
                public const int AllowCorsOriginsMin = 0;
                public const int AllowCorsOriginsMax = 40;
                public const int AllowCorsOriginLength = 200;

                public const int ScopeLength = 50;
                public const string ScopeRegExPattern = @"^[\w:;.,=\[\]\-_]*$";

                public static class Grant
                {
                    public const int IdLength = 220;
                    public const string IdRegExPattern = @"^[\w:\-_]*$";
                    public const int ClaimsMin = 1;
                    public const int ClaimsMax = 1000;
                }

                public static class Client
                {
                    public const int ResourceScopesApiMin = 0;
                    public const int ResourceScopesMin = 1;
                    public const int ResourceScopesMax = 50;
                    public const int ScopesMin = 0;
                    public const int ScopesMax = 100;
                    public const int ClaimsMin = 0;
                    public const int ClaimsMax = 100;
                    public const int VoluntaryClaimsMin = 0;
                    public const int VoluntaryClaimsMax = 100;                    
                    public const int ResponseTypesMin = 0;
                    public const int ResponseTypesMax = 5;
                    public const int ResponseTypeLength = 30;
                    public const int RedirectUrisMin = 0;
                    public const int RedirectUrisMax = 200;
                    public const int RedirectUriLength = 500;
                    public const int RedirectUriSumLength = 25000;
                    public const int SecretsMin = 0;
                    public const int SecretsMax = 10;
                    public const int ClientKeysMin = 0;
                    public const int ClientKeysMax = 4;

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
                    public const int ResponseTypesMin = 1;
                    public const int RedirectUrisMin = 1;
                    public const int IdTokenLifetimeMin = 300; // 5 minutes
                    public const int IdTokenLifetimeMax = 86400; // 24 hours
                }
            }

            public static class OidcDownPartyTest
            {
                public const char StateSplitKey = ':';

                public const int UpPartyNamesMin = 1;
                public const int ClaimsMin = 1;
            }

            public static class TrackLinkDownParty
            {
                public const int SelectedUpPartiesMin = 1;
                public const int SelectedUpPartiesMax = 4;
                public const string SelectedUpPartiesNameRegExPattern = @"^[\*\w\-]*$";
            }

            public static class UpParty
            {
                public const int PartiesMax = 1000;

                public const int IssuersBaseMin = 0;
                public const int IssuersMin = 1;
                public const int IssuersMax = 10;
                public const int SessionLifetimeMin = 0; // 0 minutes
                public const int SessionLifetimeMax = 43200; // 12 hours
                public const int SessionAbsoluteLifetimeMin = 0; // 0 minutes 
                public const int SessionAbsoluteLifetimeMax = 172800; // 48 hours
                public const int PersistentAbsoluteSessionLifetimeMin = 0; // 0 minutes 
                public const int PersistentAbsoluteSessionLifetimeMax = 31536000; // 12 month
                public const int HrdDomainMin = 0;
                public const int HrdDomainMax = 200;
                public const int HrdDomainLength = 50;
                public const int HrdDomainTotalMax = 2000;
                public const string HrdDomainRegExPattern = @"^((?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]|\*)$";
                public const int HrdDisplayNameLength = 100;
                public const string HrdDisplayNameRegExPattern = "^[^<^>]*$";
                public const int HrdLogoUrlLength = 500;
                public const string HrdLogoUrlRegExPattern = @"^https:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}";
                public const int ProfilesMin = 0;
                public const int ProfilesMax = 20;
            }

            public static class OAuthUpParty
            {
                public const int AuthorityLength = 300;
                public const int KeysApiMin = 0;
                public const int KeysMin = 1;
                public const int KeysWithX509InfoMax = 10;
                public const int KeysMax = 50;
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

                    public const int ClientKeysMin = 0;
                    public const int ClientKeysMax = 2;

                    public const int AdditionalParametersMin = 0;
                    public const int AdditionalParametersMax = 10;
                    public const int AdditionalParameterNameLength = 50;
                    public const string AdditionalParameterNameRegExPattern = @"^[\w:\-/.]*$";
                    public const int AdditionalParameterValueLength = 250;
                    public const string AdditionalParameterValueRegExPattern = @"^[\w\-/:;.,=\[\]\{\} ""']*$";

                    public const int ResponseModeLength = 30;
                    public const int ResponseTypeLength = 30;

                    public const int AuthorizeUrlLength = 500;
                    public const int TokenUrlLength = 500;  
                    public const int UserInfoUrlLength = 500;
                    public const int EndSessionUrlLength = 500;

                    public const int ClientAssertionLifetimeMin = 10; // 10 seconds 
                    public const int ClientAssertionLifetimeMax = 900; // 15 minutes                    
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

                public const int XmlCanonicalizationMethod = 100;
                public const int SignatureAlgorithmLength = 100;
                public const int KeysMax = 10;

                public const int ClaimsMin = 0;
                public const int ClaimsMax = 500;

                public const int RelayStateLength = 2000;
                public const int AcsResponseUrlLength = 2000;

                public static class Up
                {
                    public const int MetadataServiceNameLangLength = 10;

                    public const int KeysMin = 1;
                    public const int AuthnUrlLength = 500;
                    public const int LogoutUrlLength = 500;

                    public const int AuthnContextClassReferencesMin = 0;
                    public const int AuthnContextClassReferencesMax = 20;

                    public const int AuthnRequestExtensionsXmlLength = 1000;
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

            public static class ExternalApi
            {
                public const int ApiUrlLength = 500;
            }
        }

        public static class ControlClient
        {
            public const string ClientId = "foxids_control_client";
        }

        public static class ControlApi
        {
            public const string Version = "v1";
            public readonly static string[] SupportedApiHttpMethods = { HttpMethod.Get.Method, HttpMethod.Post.Method, HttpMethod.Put.Method, HttpMethod.Delete.Method };

            public const string ResourceName = "foxids_control_api";

            public static class ResourceAndScope
            {
                public readonly static string Master = $"{ResourceName}:{Access.Master}";
                public readonly static string Tenant = $"{ResourceName}:{Access.Tenant}";
            }

            public static class Access
            {
                public readonly static string Master = $"foxids{AccessElement.Master}";
                public readonly static string Tenant = $"foxids{AccessElement.Tenant}";
                public readonly static string TenantAdminRole = $"{Tenant}{AccessElement.Admin}";
            }

            public static class AccessElement
            {
                public const string Master = ":master";
                public const string Tenant = ":tenant";
                public const string Track = ":track";
                public const string Admin = ".admin";
                public const string Read = ".read";
                public const string Create = ".create";
                public const string Update = ".update";
                public const string Delete = ".delete";
            }

            public static class Segment
            {
                public const string Base = ":base"; 
                public const string Usage = ":usage";
                public const string Log = ":log";
                public const string User = ":user";
                public const string Party = ":party";
            }
        }

        public static class ExternalClaims
        {
            public static class Api
            {
                public const string Claims = "claims";
                public const string ApiId = "external_claims";

                public static class ErrorCodes
                {
                    public const string InvalidApiIdOrSecret = "invalid_api_id_secret";
                }
            }
        }

        public static class ExternalLogin
        {
            public static class Api
            {
                public const string Authentication = "authentication";
                public const string ApiId = "external_login";

                public static class ErrorCodes
                {
                    public const string InvalidApiIdOrSecret = "invalid_api_id_secret";
                    public const string InvalidUsernameOrPassword = "invalid_username_password";
                }
            }
        }

        public static class OAuth
        {
            public readonly static string[] DefaultResponseTypes =
            [
                FoxI.IdentityConstants.ResponseTypes.Code, 
                $"{FoxI.IdentityConstants.ResponseTypes.Code} {FoxI.IdentityConstants.ResponseTypes.Token}", 
                FoxI.IdentityConstants.ResponseTypes.Token
            ];

            public static class ResponseErrors
            {
                /// <summary>
                /// Login canceled.
                /// </summary>
                public const string LoginCanceled = "login_canceled";

                /// <summary>
                /// Login timeout.
                /// </summary>
                public const string LoginTimeout = "login_timeout";
            }
        }

        public static class Oidc
        {
            public readonly static string[] DefaultResponseTypes =
            [
                FoxI.IdentityConstants.ResponseTypes.Code,
                $"{FoxI.IdentityConstants.ResponseTypes.Code} {FoxI.IdentityConstants.ResponseTypes.IdToken}",
                $"{FoxI.IdentityConstants.ResponseTypes.Code} {FoxI.IdentityConstants.ResponseTypes.Token} {FoxI.IdentityConstants.ResponseTypes.IdToken}",
                $"{FoxI.IdentityConstants.ResponseTypes.Token} {FoxI.IdentityConstants.ResponseTypes.IdToken}",
                FoxI.IdentityConstants.ResponseTypes.IdToken
            ];

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

            public static class XmlCanonicalizationMethod
            {
                public const string XmlDsigExcC14NTransformUrl = SignedXml.XmlDsigExcC14NTransformUrl;
                public const string XmlDsigExcC14NWithCommentsTransformUrl = SignedXml.XmlDsigExcC14NWithCommentsTransformUrl;
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
            public readonly static string[] IdToken = FoxI.IdentityConstants.DefaultJwtClaims.IdToken.ConcatOnce(new string[] 
                { 
                    JwtClaimTypes.AuthMethod, JwtClaimTypes.AuthProfileMethod, JwtClaimTypes.AuthMethodType, JwtClaimTypes.UpParty, JwtClaimTypes.UpPartyType, 
                    JwtClaimTypes.AuthMethodIssuer, JwtClaimTypes.SubFormat, JwtClaimTypes.LocalSub
                }).ToArray();

            /// <summary>
            /// Default Access Token claims.
            /// </summary>
            public readonly static string[] AccessToken = FoxI.IdentityConstants.DefaultJwtClaims.AccessToken.ConcatOnce(new string[] 
                { 
                    JwtClaimTypes.AuthMethod, JwtClaimTypes.AuthProfileMethod, JwtClaimTypes.AuthMethodType, JwtClaimTypes.UpParty, JwtClaimTypes.UpPartyType, 
                    JwtClaimTypes.AuthMethodIssuer, JwtClaimTypes.SubFormat, FoxI.JwtClaimTypes.Actor, JwtClaimTypes.LocalSub
                }).ToArray();

            /// <summary>
            /// Default JWT Token authentication method claims.
            /// </summary>
            public readonly static string[] JwtTokenUpParty = 
            {
                FoxI.JwtClaimTypes.Subject, FoxI.JwtClaimTypes.SessionId, 
                JwtClaimTypes.AuthMethod, JwtClaimTypes.AuthProfileMethod, JwtClaimTypes.AuthMethodType, JwtClaimTypes.UpParty, JwtClaimTypes.UpPartyType, 
                JwtClaimTypes.AuthMethodIssuer, FoxI.JwtClaimTypes.AuthTime, FoxI.JwtClaimTypes.Acr, FoxI.JwtClaimTypes.Amr 
            };

            /// <summary>
            /// Exclude JWT Token authentication method claims.
            /// </summary>
            public readonly static string[] ExcludeJwtTokenUpParty = 
            {
                FoxI.JwtClaimTypes.Issuer, FoxI.JwtClaimTypes.ClientId, FoxI.JwtClaimTypes.Audience, FoxI.JwtClaimTypes.Scope, 
                FoxI.JwtClaimTypes.ExpirationTime, FoxI.JwtClaimTypes.NotBefore, FoxI.JwtClaimTypes.IssuedAt, 
                FoxI.JwtClaimTypes.Nonce, FoxI.JwtClaimTypes.Azp, FoxI.JwtClaimTypes.AtHash, FoxI.JwtClaimTypes.CHash 
            };

            /// <summary>
            /// Default SAML claims.
            /// </summary>
            public readonly static string[] SamlClaims =
            {
                ClaimTypes.NameIdentifier, Saml2ClaimTypes.NameIdFormat, Saml2ClaimTypes.SessionIndex, ClaimTypes.Upn, 
                ClaimTypes.AuthenticationInstant, ClaimTypes.AuthenticationMethod, 
                SamlClaimTypes.AuthMethod, SamlClaimTypes.AuthProfileMethod, SamlClaimTypes.AuthMethodType, SamlClaimTypes.UpParty, SamlClaimTypes.UpPartyType, 
                SamlClaimTypes.AuthMethodIssuer, SamlClaimTypes.LocalNameIdentifier
            };
        }

        public static class JwtClaimTypes
        {
            public const string AuthMethod = "auth_method";
            public const string AuthProfileMethod = "auth_profile_method";
            public const string AuthMethodType = "auth_method_type";
            [Obsolete($"Phase out and instead use the '{AuthMethod}' claim.")]
            public const string UpParty = "up_party";
            [Obsolete($"Phase out and instead use the '{AuthMethodType}' claim.")]
            public const string UpPartyType = "up_party_type";
            public const string AuthMethodIssuer = "auth_method_issuer";
            public const string SubFormat = "sub_format";
            public const string AccessToken = "access_token";
            public const string LocalSub = "local_sub";
            public const string Upn = "upn";
        }

        public static class SamlClaimTypes
        {
            public const string AuthMethod = "http://schemas.foxids.com/identity/claims/authmethod";
            public const string AuthProfileMethod = "http://schemas.foxids.com/identity/claims/authprofilemethod";
            public const string AuthMethodType = "http://schemas.foxids.com/identity/claims/authmethodtype";
            [Obsolete($"Phase out and instead use the '{AuthMethod}' claim.")]
            public const string UpParty = "http://schemas.foxids.com/identity/claims/upparty";
            [Obsolete($"Phase out and instead use the '{AuthMethodType}' claim.")]
            public const string UpPartyType = "http://schemas.foxids.com/identity/claims/uppartytype";
            public const string AuthMethodIssuer = "http://schemas.foxids.com/identity/claims/authmethodissuer";
            public const string AccessToken = "http://schemas.foxids.com/identity/claims/accesstoken";
            public const string Amr = "http://schemas.foxids.com/identity/claims/amr";
            public const string LocalNameIdentifier = "http://schemas.foxids.com/identity/claims/localnameidentifier";
            public const string EmailVerified = "http://schemas.foxids.com/ws/identity/claims/emailverified";
            public const string PreferredUsername = "http://schemas.foxids.com/ws/identity/claims/preferredusername";
            public const string ClientId = "http://schemas.foxids.com/ws/identity/claims/clientid";
        }

        public static class SamlAutoMapClaimTypes
        {
            public const string Namespace = "http://schemas.foxids.com/ws/identity/claims/";

            public static Dictionary<string, string> SamlToJwtTypeMappings = new Dictionary<string, string> { { "firstname", FoxI.JwtClaimTypes.GivenName }, { "givenname", FoxI.JwtClaimTypes.GivenName }, { "lastname", FoxI.JwtClaimTypes.FamilyName }, { "surname", FoxI.JwtClaimTypes.FamilyName } };
        }

        /// <summary>
        /// Default mappings between JWT and SAML claim types.
        /// </summary>
        public static class DefaultClaimMappings
        {
            /// <summary>
            /// Default locked claim mappings.
            /// </summary>
            public readonly static ClaimMap[] LockedMappings = 
            {
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Subject, SamlClaim = ClaimTypes.NameIdentifier },
                new ClaimMap { JwtClaim = JwtClaimTypes.SubFormat, SamlClaim = Saml2ClaimTypes.NameIdFormat },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.SessionId, SamlClaim = Saml2ClaimTypes.SessionIndex },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Amr, SamlClaim = SamlClaimTypes.Amr },
                new ClaimMap { JwtClaim = JwtClaimTypes.AuthMethod, SamlClaim = SamlClaimTypes.AuthMethod },
                new ClaimMap { JwtClaim = JwtClaimTypes.AuthProfileMethod, SamlClaim = SamlClaimTypes.AuthProfileMethod },
                new ClaimMap { JwtClaim = JwtClaimTypes.AuthMethodType, SamlClaim = SamlClaimTypes.AuthMethodType },
                new ClaimMap { JwtClaim = JwtClaimTypes.UpParty, SamlClaim = SamlClaimTypes.UpParty },
                new ClaimMap { JwtClaim = JwtClaimTypes.UpPartyType, SamlClaim = SamlClaimTypes.UpPartyType },
                new ClaimMap { JwtClaim = JwtClaimTypes.AuthMethodIssuer, SamlClaim = SamlClaimTypes.AuthMethodIssuer },
                new ClaimMap { JwtClaim = JwtClaimTypes.AccessToken, SamlClaim = SamlClaimTypes.AccessToken },
                new ClaimMap { JwtClaim = JwtClaimTypes.LocalSub, SamlClaim = SamlClaimTypes.LocalNameIdentifier }
            };

            /// <summary>
            /// Default changeable claim mappings.
            /// </summary>
            public readonly static ClaimMap[] ChangeableMappings =
            {
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Email, SamlClaim = ClaimTypes.Email },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.EmailVerified, SamlClaim = SamlClaimTypes.EmailVerified },
                new ClaimMap { JwtClaim = JwtClaimTypes.Upn, SamlClaim = ClaimTypes.Upn },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.PreferredUsername, SamlClaim = SamlClaimTypes.PreferredUsername },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Name, SamlClaim = ClaimTypes.Name },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.GivenName, SamlClaim = ClaimTypes.GivenName },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.FamilyName, SamlClaim = ClaimTypes.Surname },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.Role, SamlClaim = ClaimTypes.Role },
                new ClaimMap { JwtClaim = FoxI.JwtClaimTypes.ClientId, SamlClaim = SamlClaimTypes.ClientId },
            };

            public class ClaimMap
            {
                public string JwtClaim { get; set; }
                public string SamlClaim { get; set; }
            }
        }
    }
}