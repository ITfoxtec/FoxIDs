using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class TrackIssuerLogic : LogicBase
    {
        public TrackIssuerLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public string GetIssuer()
        {
            var issuerWithoutSlash = HttpContext.GetHostWithTenantAndTrack();
            return $"{issuerWithoutSlash}/";
        }
    }
}
