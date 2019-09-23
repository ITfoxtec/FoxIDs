using FoxI = ITfoxtec.Identity;
using System.Security.Claims;
using ITfoxtec.Identity.Saml2.Claims;

namespace FoxIDs
{
    public static class Constants
    {
        public static class Routes
        {
            public const string DefaultWebSiteAction = "index";
            public const string DefaultWebSiteController = "w";

            public const string OidcDiscoveryAction = "OpenidConfiguration";
            public const string OidcDiscoveryKeyAction = "Keys";
            public const string OidcDiscoveryController = "OpenIDConfig";

            public const string OAuthController = "oauth";
            public const string SamlController = "saml";

            public const string RouteControllerKey = "controller";
            public const string RouteActionKey = "action";
            public const string RouteBindingKey = "binding";

            public const string SequenceStringKey = Sequence.String;

            public const char PreApikey = '!';
            public const string MasterApiName = "@master";
            public const string MasterTenantName = "master";
            public const string DefaultMasterTrackName = "master";

            public const char ApiControllerPreMasterKey = 'm';
            public const char ApiControllerPreTenantTrackKey = 't';
        }

        public static class Models
        {
            public static class OAuthParty
            {
                public const int IdLength = 110;
                public const string IdRegExPattern = @"^[\w:_-]*$";
                public const int NameLength = 30;
                public const string NameRegExPattern = @"^[\w-_]*$";

                public const int AllowUpPartyNamesMin = 0;
                public const int AllowUpPartyNamesMax = 2000;
                public const int AllowCorsOriginsMin = 0;
                public const int AllowCorsOriginsMax = 40;
                public const int AllowCorsOriginLength = 200;

                public const int ScopesLength = 50;
                public const string ScopeRegExPattern = @"^[\w-_]*$";

                public static class Client
                {
                    public const int ResourceScopesMin = 1;
                    public const int ResourceScopesMax = 50;
                    public const int ResourceScopeLength = 30;
                    public const int ScopesMin = 0;
                    public const int ScopesMax = 100;
                    public const int ClaimsMin = 0;
                    public const int ClaimsMax = 500;
                    public const int ClaimLength = 50;
                    public const int VoluntaryClaimsMin = 0;
                    public const int VoluntaryClaimsMax = 100;                    
                    public const int ResponseTypesMin = 1;
                    public const int ResponseTypesMax = 10;
                    public const int ResponseTypeLength = 30;
                    public const int RedirectUrisMin = 1;
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

            public static class OidcParty
            {
                public static class Client
                {
                    public const int IdTokenLifetimeMin = 300; // 5 minutes
                    public const int IdTokenLifetimeMax = 86400; // 24 hours
                }
            }
        }

        public static class Api
        {
            public const string Version = "v1";
            public readonly static string[] SupportedApiHttpMethods = { "GET", "PUT", "POST", "DELETE" };
        }

        public static class Sequence
        {
            public const string Object = "sequence_object";
            public const string String = "sequence_string";
            public const string Start = "sequence_start";
            public const string Valid = "sequence_valid";
        }

        public static class Endpoints
        {
            public const string SamlAuthn = "Authn";
            public const string SamlLogout = "Logout";
            public const string SamlAcs = "Acs";
            public const string SamlSingleLogout = "SingleLogout";
        }

        public static class OAuth
        {
            public static class ResponseErrors
            {
                /// <summary>
                /// Login canceled by user.
                /// </summary>
                public const string LoginCanceled = "login_canceled";

            }
        }

        public static class Saml
        {
            public const string RelayState = "RelayState";
        }

        /// Default claims.
        /// </summary>
        public static class DefaultClaims
        {
            /// <summary>
            /// Default ID Token claims.
            /// </summary>
            public readonly static string[] IdToken = FoxI.IdentityConstants.DefaultJwtClaims.IdToken.ConcatOnce(
                new string[] { JwtClaimTypes.SubFormat, FoxI.JwtClaimTypes.Email, FoxI.JwtClaimTypes.GivenName, FoxI.JwtClaimTypes.FamilyName } ).ToArray();

            /// <summary>
            /// Default Access Token claims.
            /// </summary>
            public readonly static string[] AccessToken = FoxI.IdentityConstants.DefaultJwtClaims.AccessToken.ConcatOnce(
                new string[] { JwtClaimTypes.SubFormat, FoxI.JwtClaimTypes.Email, FoxI.JwtClaimTypes.GivenName, FoxI.JwtClaimTypes.FamilyName } ).ToArray();

            /// <summary>
            /// Default SAML claims.
            /// </summary>
            public readonly static string[] SamlClaims = { ClaimTypes.NameIdentifier, Saml2ClaimTypes.NameIdFormat, Saml2ClaimTypes.SessionIndex, ClaimTypes.Upn, ClaimTypes.Email, ClaimTypes.Name, ClaimTypes.GivenName, ClaimTypes.Surname };

        }

        public static class JwtClaimTypes
        {
            public const string SubFormat = "sub_format";

        }

        public static class LengthDefinitions
        {
            public const int JwtClaimValue = 100;
        }
    }
}