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

        public (string, string) GetDownAuthorityAndOIDCDiscovery(string partyName, bool addUpParty, PartyBindingPatterns partyBindingPattern = PartyBindingPatterns.Brackets)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--application-name--" : partyName.ToLower()).ToDownPartyBinding(addUpParty, partyBindingPattern);
            var authority = $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/{partyBinding}/";
            return (authority, authority + IdentityConstants.OidcDiscovery.Path);
        }

        public string GetDownSamlMetadata(string partyName, PartyBindingPatterns partyBindingPattern = PartyBindingPatterns.Brackets)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--application-name--" : partyName.ToLower()).ToDownPartyBinding(true, partyBindingPattern);
            return $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/{partyBinding}/{Constants.Routes.SamlController}/{Constants.Endpoints.SamlIdPMetadata}";
        }

        public (string, string, string) GetUpRedirectAndLogoutUrls(string partyName, PartyBindingPatterns partyBindingPattern)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--auth-method-name--" : partyName.ToLower()).ToUpPartyBinding(partyBindingPattern);
            var oauthUrl = $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/{partyBinding}/{Constants.Routes.OAuthController}/";
            return (oauthUrl + Constants.Endpoints.AuthorizationResponse, oauthUrl + Constants.Endpoints.EndSessionResponse, oauthUrl + Constants.Endpoints.FrontChannelLogout);
        }

        public string GetUpSamlMetadata(string partyName, PartyBindingPatterns partyBindingPattern)
        {
            var partyBinding = (partyName.IsNullOrEmpty() ? "--auth-method-name--" : partyName.ToLower()).ToUpPartyBinding(partyBindingPattern);
            return $"{routeBindingLogic.GetFoxIDsTenantEndpoint()}/{(routeBindingLogic.IsMasterTenant ? "master" : trackSelectedLogic.Track.Name)}/{partyBinding}/{Constants.Routes.SamlController}/{Constants.Endpoints.SamlSPMetadata}";
        }
    }
}
