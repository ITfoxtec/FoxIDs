using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using ITfoxtec.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class ClaimTransformLogicTests
    {
        [Theory]
        [InlineData("some-constant", "abc", 1)]
        [InlineData(JwtClaimTypes.Email, "abc", 2)]
        public async Task TransformConstant_AddClaim(string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            { 
                Type = ClaimTransformTypes.Constant, 
                ClaimOut = claimOut, 
                Transformation = transformation 
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.Transform(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Anders", "Test name", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "anders", "Test name", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Peter", "Test name", 0)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Anders", "Test name", 0)]
        public async Task TransformMatch_AddClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform 
            { 
                Type = ClaimTransformTypes.Match, 
                ClaimsIn = claimIn.ToList(), 
                ClaimOut = claimOut,
                Transformation = transformation, 
                TransformationExtension = transformationExtension 
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.Transform(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abcd.com$", "Test name", 0)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 0)]
        public async Task TransformRegexMatch_AddClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMatch,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.Transform(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, "given_name2", 1, "Anders")]
        [InlineData(new[] { "do-not-exist" }, "given_name2", 0, "")]
        public async Task TransformMap_AddClaim(string[] claimIn, string claimOut, int count, string result)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.Transform(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, "domain", @"^\S+@(?<map>\S+)$", 1, "abc.com")]
        [InlineData(new[] { "do-not-exist" }, "domain", @"^\S+@(?<map>\S+)$", 0, "")]
        public async Task TransformRegexMap_AddClaim(string[] claimIn, string claimOut, string transformation, int count, string result)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMap,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.Transform(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName, JwtClaimTypes.FamilyName }, JwtClaimTypes.Name, "{0} {1}", 1, "Anders Andersen")]
        [InlineData(new[] { JwtClaimTypes.Subject, JwtClaimTypes.FamilyName }, "test-claim", "{0}-{1}", 1, "12345-Andersen")]
        [InlineData(new[] { "do-not-exist", JwtClaimTypes.FamilyName }, "test-claim", "{0}-{1}", 1, "-Andersen")]
        [InlineData(new[] { "do-not-exist1", "do-not-exist1" }, "test-claim", "{0}-{1}", 0, "")]
        public async Task TransformConcatenate_AddClaim(string[] claimIn, string claimOut, string transformation, int count, string result)
        {
            var claims = new List<Claim>(GetTestClaims());
            claims.Add(new Claim(JwtClaimTypes.Subject, "abcd"));
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Concatenate, 
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut, 
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.Transform(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == count);
        }

        private IEnumerable<Claim> GetTestClaims()
        {
            yield return new Claim(JwtClaimTypes.Subject, "12345");
            yield return new Claim(JwtClaimTypes.GivenName, "Anders");
            yield return new Claim(JwtClaimTypes.FamilyName, "Andersen");
            yield return new Claim(JwtClaimTypes.Email, "andersen@abc.com");
        }

        private ClaimTransformLogic ClaimTransformLogicInstance()
        {
            var routeBinding = new RouteBinding();
            var mockHttpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);

            var telemetryScopedLogger = TelemetryLoggerHelper.ScopedLoggerObject(mockHttpContextAccessor);

            var claimTransformValidationLogic = new ClaimTransformValidationLogic(mockHttpContextAccessor);
            return new ClaimTransformLogic(telemetryScopedLogger, claimTransformValidationLogic, mockHttpContextAccessor);
        }
    }
}
