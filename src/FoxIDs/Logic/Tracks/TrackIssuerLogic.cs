using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using UrlCombineLib;

namespace FoxIDs.Logic
{
    public class TrackIssuerLogic : LogicBase
    {
        public TrackIssuerLogic(IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        { }

        public string GetIssuer()
        {
            var issuerWithoutSlash = UrlCombine.Combine(HttpContext.GetHost(), RouteBinding.TenantName, RouteBinding.TrackName);
            return $"{issuerWithoutSlash}/";
        }
    }
}
