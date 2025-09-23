using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.UnitTests.MockHelpers;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class SecurityHeaderLogicTests
    {
        [Fact]
        public void AddImgSrc_IgnoresCommentUrlsAndStripsComments()
        {
            var routeBinding = new RouteBinding { TenantName = "tenant", TrackName = "track" };
            var httpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);
            var securityHeaderLogic = new SecurityHeaderLogic(httpContextAccessor);

            var css = "/* comment url(https://comment.example/logo.png) */ .cls { background-image: url('https://allowed.example/img.png'); }";
            var party = new LoginUpParty
            {
                Css = css,
                IconUrl = "https://cdn.example/icon.png"
            };

            securityHeaderLogic.AddImgSrc(party);

            var domains = securityHeaderLogic.GetImgSrcDomains();

            Assert.NotNull(domains);
            Assert.Contains("cdn.example", domains);
            Assert.Contains("allowed.example", domains);
            Assert.DoesNotContain("comment.example", domains);
            Assert.Equal(" .cls { background-image: url('https://allowed.example/img.png'); }", party.Css);
        }
    }
}
