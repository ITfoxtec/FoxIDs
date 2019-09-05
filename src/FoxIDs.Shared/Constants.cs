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

            public const char ApiControllerPreMasterKey = 'M';
            public const char ApiControllerPreTenantTrackKey = 'T';
        }

        public static class Sequence
        {
            public const string Object = "sequence_object";
            //public const string Id = "sequence_id";
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