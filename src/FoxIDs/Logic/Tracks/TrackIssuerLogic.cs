using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class TrackIssuerLogic : LogicSequenceBase
    {
        public TrackIssuerLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public string GetIssuer(string routeUrl = null)
        {
            if (routeUrl.IsNullOrWhiteSpace())
            {
                var issuerWithoutSlash = HttpContext.GetHostWithTenantAndTrack(useConfig: true);
                return $"{issuerWithoutSlash}/";
            }
            else
            {
                var issuerWithoutSlash = HttpContext.GetHostWithRoute(routeUrl);
                return $"{issuerWithoutSlash}";
            }
        }
    }
}
