using FoxIDs.Models.Api;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Logic
{
    public class MetadataLogic
    {
        private readonly RouteBindingLogic routeBindingLogic;
        private readonly TrackSelectedLogic trackSelectedLogic;

        public MetadataLogic(RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic)
        {
            this.routeBindingLogic = routeBindingLogic;
            this.trackSelectedLogic = trackSelectedLogic;
        }

        public (string authority, string trackIssuer, string oidcDiscovery, string authorize, string token) GetDownAuthorityAndOIDCDiscovery(string partyName, bool addUpParty, PartyBindingPatterns partyBindingPattern = PartyBindingPatterns.Brackets)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--application-name--" : partyName.ToLower()).ToDownPartyBinding(addUpParty, partyBindingPattern);
            var trackIssuer = $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/";
            var authority = $"{trackIssuer}{partyBinding}/";
            var oauthUrl = $"{authority}{Constants.Routes.OAuthController}/";
            return (authority, trackIssuer, authority + IdentityConstants.OidcDiscovery.Path, oauthUrl + Constants.Endpoints.Authorize, oauthUrl + Constants.Endpoints.Token);
        }

        public (string metadata, string trackIssuer, string authn, string logout) GetDownSamlMetadata(string partyName, PartyBindingPatterns partyBindingPattern = PartyBindingPatterns.Brackets)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--application-name--" : partyName.ToLower()).ToDownPartyBinding(true, partyBindingPattern);
            var baseUrl = $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/";
            var samlUrl = $"{baseUrl}{partyBinding}/{Constants.Routes.SamlController}/";
            return (
                metadata: samlUrl + Constants.Endpoints.SamlIdPMetadata,
                trackIssuer: baseUrl,
                authn: samlUrl + Constants.Endpoints.SamlAuthn,
                logout: samlUrl + Constants.Endpoints.SamlLogout);
        }

        public (string redirect, string postLogoutRedirect, string frontChannelLogout) GetUpRedirectAndLogoutUrls(string partyName, PartyBindingPatterns partyBindingPattern)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--auth-method-name--" : partyName.ToLower()).ToUpPartyBinding(partyBindingPattern);
            var oauthUrl = $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/{partyBinding}/{Constants.Routes.OAuthController}/";
            return (oauthUrl + Constants.Endpoints.AuthorizationResponse, oauthUrl + Constants.Endpoints.EndSessionResponse, oauthUrl + Constants.Endpoints.FrontChannelLogout);
        }

        public (string metadata, string entityId, string acs, string singleLogout) GetUpSamlMetadata(string partyName, PartyBindingPatterns partyBindingPattern)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--auth-method-name--" : partyName.ToLower()).ToUpPartyBinding(partyBindingPattern);
            var samlBaseUrl = $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/";
            var samlUrl = $"{samlBaseUrl}{partyBinding}/{Constants.Routes.SamlController}/";
            return (
                metadata: samlUrl + Constants.Endpoints.SamlSPMetadata,
                entityId: samlBaseUrl,
                acs: samlUrl + Constants.Endpoints.SamlAcs,
                singleLogout: samlUrl + Constants.Endpoints.SamlSingleLogout);
        }
    }
}
