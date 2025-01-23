using FoxIDs.Logic;
using FoxIDs.Models;
using FoxIDs.UnitTests.Helpers;
using FoxIDs.UnitTests.MockHelpers;
using ITfoxtec.Identity;
using Moq;
using System;
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
        public async Task TransformConstant_AddConstantClaim(string claimOut, string constandValue, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            { 
                Type = ClaimTransformTypes.Constant, 
                ClaimOut = claimOut, 
                Transformation = constandValue 
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData("some-constant", "abc", 1)]
        [InlineData(JwtClaimTypes.Email, "abc", 1)]
        public async Task TransformConstant_ReplaceConstantClaim(string claimOut, string constandValue, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Constant,
                Action = ClaimTransformActions.Replace,
                ClaimOut = claimOut,
                Transformation = constandValue
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Fact]
        public async Task TransformConstant_AddIfNotConstantClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Constant,
                Action = ClaimTransformActions.AddIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Fact]
        public async Task TransformConstant_ReplaceIfNotConstantClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Constant,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Fact]
        public async Task TransformConstant_RemoveConstantClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Constant,
                Action = ClaimTransformActions.Remove,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Test name", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "some name", 2)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Test name", 0)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.GivenName, "Test name", 1)]
        public async Task TransformMatchClaim_AddClaim(string[] claimIn, string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.MatchClaim,
                Action = ClaimTransformActions.Add,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Test name", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "some name", 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Test name", 0)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.GivenName, "Test name", 1)]
        public async Task TransformMatchClaim_ReplaceClaim(string[] claimIn, string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.MatchClaim,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Test name", 0)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "some name", 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Test name", 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.GivenName, "Test name", 2)]
        public async Task TransformMatchClaim_AddIfNotClaim(string[] claimIn, string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.MatchClaim,
                Action = ClaimTransformActions.AddIfNot,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Test name", 0)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "some name", 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Test name", 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.GivenName, "Test name", 1)]
        public async Task TransformMatchClaim_ReplaceIfNotClaim(string[] claimIn, string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.MatchClaim,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(JwtClaimTypes.Name, 0)]
        [InlineData(JwtClaimTypes.GivenName, 0)]
        [InlineData("do-not-exist", 0)]
        public async Task TransformMatchClaim_RemoveClaim(string claimOut, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.MatchClaim,
                Action = ClaimTransformActions.Remove,
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Anders", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "anders", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "Anders", "some name", 1, 2)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Peter", "Test name", 0, 0)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Anders", "Test name", 0, 0)]
        public async Task TransformMatch_AddClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
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
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Anders", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "anders", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "Anders", "some name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "anders", "some name", 0, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Peter", "Test name", 0, 0)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Anders", "Test name", 0, 0)]
        public async Task TransformMatch_ReplaceClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Match,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Anders", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "anders", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "Anders", "some name", 0, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Peter", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "Peter", "Test name", 1, 2)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Anders", "Test name", 1, 1)]
        public async Task TransformMatch_AddIfNotClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Match,
                Action = ClaimTransformActions.AddIfNot,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Anders", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "anders", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "Anders", "some name", 0, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "anders", "some name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.Name, "Peter", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.GivenName, "Peter", "Test name", 1, 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, "Anders", "Test name", 1, 1)]
        public async Task TransformMatch_ReplaceIfNotClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Match,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(JwtClaimTypes.Name, "Anders", 0)]
        [InlineData(JwtClaimTypes.GivenName, "Anders", 0)]
        [InlineData(JwtClaimTypes.GivenName, "anders", 1)]
        [InlineData(JwtClaimTypes.GivenName, "Peter", 1)]
        [InlineData("do-not-exist", "Anders", 0)]
        public async Task TransformMatch_RemoveClaim(string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Match,
                Action = ClaimTransformActions.Remove,
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abcd.com$", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@abc.com$", "Test name", 1, 2)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 0, 0)]
        public async Task TransformRegexMatch_AddClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMatch,
                Action = ClaimTransformActions.Add,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abcd.com$", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@abc.com$", "Test name", 1, 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 0, 0)]
        public async Task TransformRegexMatch_ReplaceClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMatch,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abcd.com$", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@abc.com$", "Test name", 0, 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@abcd.com$", "Test name", 1, 2)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 1, 1)]
        public async Task TransformRegexMatch_AddIfNotClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMatch,
                Action = ClaimTransformActions.AddIfNot,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 0, 0)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.Name, @"^\S+@abcd.com$", "Test name", 1, 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@abc.com$", "Test name", 0, 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@abcd.com$", "Test name", 1, 1)]
        [InlineData(new[] { "do-not-exist" }, JwtClaimTypes.Name, @"^\S+@abc.com$", "Test name", 1, 1)]
        public async Task TransformRegexMatch_ReplaceIfNotClaim(string[] claimIn, string claimOut, string transformation, string transformationExtension, int withValueCount, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMatch,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation,
                TransformationExtension = transformationExtension
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == transformationExtension).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(JwtClaimTypes.Name, @"^\S+@abc.com$", 0)]
        [InlineData(JwtClaimTypes.Name, @"^\S+@abcd.com$", 0)]
        [InlineData(JwtClaimTypes.GivenName, @"^\S+$", 0)]
        [InlineData(JwtClaimTypes.GivenName, @"^\S+@abcd.com$", 1)]
        [InlineData("do-not-exist", @"^\S+@abc.com$", 0)]
        public async Task TransformRegexMatch_RemoveClaim(string claimOut, string transformation, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMatch,
                Action = ClaimTransformActions.Remove,
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, "given_name2", 1, "Anders", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.FamilyName, 1, "Anders", 2)]
        [InlineData(new[] { "do-not-exist" }, "given_name2", 0, "", 0)]
        public async Task TransformMap_AddClaim(string[] claimIn, string claimOut, int withValueCount, string result, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, "given_name2", 1, "Anders", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.FamilyName, 1, "Anders", 1)]
        [InlineData(new[] { "do-not-exist" }, "given_name2", 0, "", 0)]
        public async Task TransformMap_ReplaceClaim(string[] claimIn, string claimOut, int withValueCount, string result, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Fact]
        public async Task TransformMap_AddIfNotClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                Action = ClaimTransformActions.AddIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName }, "given_name2", 1, "Anders", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName }, JwtClaimTypes.FamilyName, 0, "Anders", 1)]
        [InlineData(new[] { "do-not-exist" }, "given_name2", 0, "", 0)]
        public async Task TransformMap_AddIfNotOutClaim(string[] claimIn, string claimOut, int withValueCount, string result, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                Action = ClaimTransformActions.AddIfNotOut,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Fact]
        public async Task TransformMap_ReplaceIfNotClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Fact]
        public async Task TransformMap_RemoveClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Map,
                Action = ClaimTransformActions.Remove,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, "domain", @"^\S+@(?<map>\S+)$", 1, "abc.com", 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@(?<map>\S+)$", 1, "abc.com", 2)]
        [InlineData(new[] { JwtClaimTypes.Subject }, "sub_id", @"^\S+\|(?<map>\S+)$", 1, "12345", 1)]
        [InlineData(new[] { "do-not-exist" }, "domain", @"^\S+@(?<map>\S+)$", 0, "", 0)]
        public async Task TransformRegexMap_AddClaim(string[] claimIn, string claimOut, string transformation, int withValueCount, string result, int count)
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
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, "domain", @"^\S+@(?<map>\S+)$", 1, "abc.com", 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@(?<map>\S+)$", 1, "abc.com", 1)]
        [InlineData(new[] { "do-not-exist" }, "domain", @"^\S+@(?<map>\S+)$", 0, "", 0)]
        public async Task TransformRegexMap_ReplaceClaim(string[] claimIn, string claimOut, string transformation, int withValueCount, string result, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMap,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Fact]
        public async Task TransformRegexMap_AddIfNotClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMap,
                Action = ClaimTransformActions.AddIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.Email }, "domain", @"^\S+@(?<map>\S+)$", 1, "abc.com", 1)]
        [InlineData(new[] { JwtClaimTypes.Email }, JwtClaimTypes.GivenName, @"^\S+@(?<map>\S+)$", 0, "abc.com", 1)]
        [InlineData(new[] { JwtClaimTypes.Subject }, "sub_id", @"^\S+\|(?<map>\S+)$", 1, "12345", 1)]
        [InlineData(new[] { "do-not-exist" }, "domain", @"^\S+@(?<map>\S+)$", 0, "", 0)]
        public async Task TransformRegexMap_AddIfNotOutClaim(string[] claimIn, string claimOut, string transformation, int withValueCount, string result, int count)
        {
            var claims = GetTestClaims();
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMap,
                    Action = ClaimTransformActions.AddIfNotOut,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Fact]
        public async Task TransformRegexMap_ReplaceIfNotClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMap,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Fact]
        public async Task TransformRegexMap_RemoveClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.RegexMap,
                Action = ClaimTransformActions.Remove,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName, JwtClaimTypes.FamilyName }, JwtClaimTypes.Name, "{0} {1}", 1, "Anders Andersen", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName, JwtClaimTypes.FamilyName }, JwtClaimTypes.GivenName, "{0} {1}", 1, "Anders Andersen", 2)]
        [InlineData(new[] { JwtClaimTypes.Subject, JwtClaimTypes.FamilyName }, "test-claim", "{0}-{1}", 1, "auth-method-name|12345-Andersen", 1)]
        [InlineData(new[] { "do-not-exist", JwtClaimTypes.FamilyName }, "test-claim", "{0}-{1}", 1, "-Andersen", 1)]
        [InlineData(new[] { "do-not-exist1", "do-not-exist1" }, "test-claim", "{0}-{1}", 0, "", 0)]
        public async Task TransformConcatenate_AddClaim(string[] claimIn, string claimOut, string transformation, int withValueCount, string result, int count)
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
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { JwtClaimTypes.GivenName, JwtClaimTypes.FamilyName }, JwtClaimTypes.Name, "{0} {1}", 1, "Anders Andersen", 1)]
        [InlineData(new[] { JwtClaimTypes.GivenName, JwtClaimTypes.FamilyName }, JwtClaimTypes.GivenName, "{0} {1}", 1, "Anders Andersen", 1)]
        [InlineData(new[] { JwtClaimTypes.Subject, JwtClaimTypes.FamilyName }, "test-claim", "{0}-{1}", 1, "auth-method-name|12345-Andersen", 1)]
        [InlineData(new[] { "do-not-exist", JwtClaimTypes.FamilyName }, "test-claim", "{0}-{1}", 1, "-Andersen", 1)]
        [InlineData(new[] { "do-not-exist1", "do-not-exist1" }, "test-claim", "{0}-{1}", 0, "", 0)]
        public async Task TransformConcatenate_ReplaceClaim(string[] claimIn, string claimOut, string transformation, int withValueCount, string result, int count)
        {
            var claims = new List<Claim>(GetTestClaims());
            claims.Add(new Claim(JwtClaimTypes.Subject, "abcd"));
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Concatenate,
                Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut,
                Transformation = transformation
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            Assert.True(claimsResult.Where(c => c.Type == claimOut && (result != "" ? c.Value == result : true)).Count() == withValueCount);
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Fact]
        public async Task TransformConcatenate_AddIfNotClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Concatenate,
                Action = ClaimTransformActions.AddIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Fact]
        public async Task TransformConcatenate_ReplaceIfNotClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Concatenate,
                Action = ClaimTransformActions.ReplaceIfNot,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Fact]
        public async Task TransformConcatenate_RemoveClaim()
        {
            var claimTransformation = new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.Concatenate,
                Action = ClaimTransformActions.Remove,
                ClaimOut = "some-constant"
            };
            await Assert.ThrowsAsync<NotSupportedException>(async () => await claimTransformation.ValidateObjectAsync());
        }

        [Theory]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate" }, "http://schemas.foxids.com/identity/claims/privilege", true, 2, false, true)]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate2" }, "http://schemas.foxids.com/identity/claims/privilege", false, 0, false, true)]
        [InlineData(new[] { "privileges_intermediate" }, "privilege", true, 2, false, false)]
        [InlineData(new[] { "privileges_intermediate2" }, "privilege", false, 0, false, false)]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate" }, "http://schemas.foxids.com/identity/claims/privilege", true, 1, true, true)]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate2" }, "http://schemas.foxids.com/identity/claims/privilege", false, 0, true, true)]
        [InlineData(new[] { "privileges_intermediate" }, "privilege", true, 1, true, false)]
        [InlineData(new[] { "privileges_intermediate2" }, "privilege", false, 0, true, false)]
        public async Task TransformDkPrivilegeSaml_AddClaim(string[] claimIn, string claimOut, bool hasResult, int count, bool withConstraints, bool isSaml)
        {
            var claims = GetDkPrivilegeTestClaims(false, withConstraints, isSaml);
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.DkPrivilege,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            if (hasResult)
            {
                var results = GetDkPrivilegeTestJson(withConstraints);
                foreach(var result in results)
                {
                    Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == result).Count() == 1);
                }
            }
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        [Theory]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate" }, "http://schemas.foxids.com/identity/claims/privilege", true, 2, false, true)]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate2" }, "http://schemas.foxids.com/identity/claims/privilege", false, 1, false, true)]
        [InlineData(new[] { "privileges_intermediate" }, "privilege", true, 2, false, false)]
        [InlineData(new[] { "privileges_intermediate2" }, "privilege", false, 1, false, false)]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate" }, "http://schemas.foxids.com/identity/claims/privilege", true, 1, true, true)]
        [InlineData(new[] { "https://data.gov.dk/model/core/eid/privilegesIntermediate2" }, "http://schemas.foxids.com/identity/claims/privilege", false, 1, true, true)]
        [InlineData(new[] { "privileges_intermediate" }, "privilege", true, 1, true, false)]
        [InlineData(new[] { "privileges_intermediate2" }, "privilege", false, 1, true, false)]
        public async Task TransformDkPrivilegeSaml_ReplaceClaim(string[] claimIn, string claimOut, bool hasResult, int count, bool withConstraints, bool isSaml)
        {
            var claims = GetDkPrivilegeTestClaims(true, withConstraints, isSaml);
            var claimTransformations = new List<ClaimTransform> { new OAuthClaimTransform
            {
                Type = ClaimTransformTypes.DkPrivilege,
                 Action = ClaimTransformActions.Replace,
                ClaimsIn = claimIn.ToList(),
                ClaimOut = claimOut
            } };
            var claimTransformLogic = ClaimTransformLogicInstance();
            var claimsResult = await claimTransformLogic.TransformAsync(claimTransformations, claims);
            if (hasResult)
            {
                var results = GetDkPrivilegeTestJson(withConstraints);
                foreach (var result in results)
                {
                    Assert.True(claimsResult.Where(c => c.Type == claimOut && c.Value == result).Count() == 1);
                }
            }
            Assert.True(claimsResult.Where(c => c.Type == claimOut).Count() == count);
        }

        private IEnumerable<Claim> GetTestClaims()
        {
            yield return new Claim(JwtClaimTypes.Subject, "auth-method-name|12345");
            yield return new Claim(JwtClaimTypes.GivenName, "Anders");
            yield return new Claim(JwtClaimTypes.FamilyName, "Andersen");
            yield return new Claim(JwtClaimTypes.Email, "andersen@abc.com");
        }

        private IEnumerable<Claim> GetDkPrivilegeTestClaims(bool withJsonReult, bool withConstraints, bool isSaml)
        {
            foreach (var claim in GetTestClaims())
            {
                yield return claim;
            }
            if(!withConstraints)
            {
                // model 2
                yield return new Claim(isSaml ? "https://data.gov.dk/model/core/eid/privilegesIntermediate" : "privileges_intermediate", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4KPGJwcDpQcml2aWxlZ2VMaXN0IHhtbG5zOmJwcD0iaHR0cDovL2RpZ3N0LmRrL29pb3NhbWwvYmFzaWNfcHJpdmlsZWdlX3Byb2ZpbGUiIHhtbG5zOnhzaT0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEtaW5zdGFuY2UiID4KPFByaXZpbGVnZUdyb3VwIFNjb3BlPSJ1cm46ZGs6Z292OnNhbWw6Y3ZyTnVtYmVySWRlbnRpZmllcjoxMjM0NTY3OCI+CjxQcml2aWxlZ2U+dXJuOmRrOnNvbWVfZG9tYWluOm15UHJpdmlsZWdlMUE8L1ByaXZpbGVnZT4KPFByaXZpbGVnZT51cm46ZGs6c29tZV9kb21haW46bXlQcml2aWxlZ2UxQjwvUHJpdmlsZWdlPgo8L1ByaXZpbGVnZUdyb3VwPgo8UHJpdmlsZWdlR3JvdXAgU2NvcGU9InVybjpkazpnb3Y6c2FtbDpzZU51bWJlcklkZW50aWZpZXI6MjczODQyMjMiPgo8UHJpdmlsZWdlPnVybjpkazpzb21lX2RvbWFpbjpteVByaXZpbGVnZTFDPC9Qcml2aWxlZ2U+CjxQcml2aWxlZ2U+dXJuOmRrOnNvbWVfZG9tYWluOm15UHJpdmlsZWdlMUQ8L1ByaXZpbGVnZT4KPC9Qcml2aWxlZ2VHcm91cD4KPC9icHA6UHJpdmlsZWdlTGlzdD4=");
            }
            else
            {
                // model 3
                yield return new Claim(isSaml ? "https://data.gov.dk/model/core/eid/privilegesIntermediate" : "privileges_intermediate", "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0iVVRGLTgiPz4KPGJwcDpQcml2aWxlZ2VMaXN0IHhtbG5zOmJwcD0iaHR0cDovL2RpZ3N0LmRrL29pb3NhbWwvYmFzaWNfcHJpdmlsZWdlX3Byb2ZpbGUiIHhtbG5zOnhzaT0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEtaW5zdGFuY2UiID4KPFByaXZpbGVnZUdyb3VwIFNjb3BlPSJ1cm46ZGs6Z292OnNhbWw6Y3ZyTnVtYmVySWRlbnRpZmllcjoxMjM0NTY3OCI+CjxDb25zdHJhaW50IE5hbWU9InVybjpkazprb21iaXQ6S0xFIj4yNS4qPC9Db25zdHJhaW50Pgo8Q29uc3RyYWludCBOYW1lPSJ1cm46ZGs6a29tYml0OnNlbnNpdGl2aXR5Ij4zPC9Db25zdHJhaW50Pgo8UHJpdmlsZWdlPnVybjpkazprb21iaXQ6c3lzdGVtX3h5ejp2aWV3X2Nhc2U8L1ByaXZpbGVnZT4KPC9Qcml2aWxlZ2VHcm91cD4KPC9icHA6UHJpdmlsZWdlTGlzdD4=");
            }
            if (withJsonReult)
            {
                yield return new Claim(isSaml ? "http://schemas.foxids.com/identity/claims/privilege" : "privilege", "{\"cvr\":\"12345678\",\"p\":[\"urn:dk:some_domain:myPrivilege1A\",\"urn:dk:some_domain:myPrivilege1B\"]}");
            }
        }

        private IEnumerable<string> GetDkPrivilegeTestJson(bool withConstraints)
        {
            if (!withConstraints)
            {
                // model 2
                yield return "{\"cvr\":\"12345678\",\"p\":[\"urn:dk:some_domain:myPrivilege1A\",\"urn:dk:some_domain:myPrivilege1B\"]}";
                yield return "{\"se\":\"27384223\",\"p\":[\"urn:dk:some_domain:myPrivilege1C\",\"urn:dk:some_domain:myPrivilege1D\"]}";
            }
            else
            {
                // model 3
                yield return "{\"cvr\":\"12345678\",\"c\":{\"urn:dk:kombit:KLE\":\"25.*\",\"urn:dk:kombit:sensitivity\":\"3\"},\"p\":[\"urn:dk:kombit:system_xyz:view_case\"]}";
            }
        }

        private ClaimTransformLogic ClaimTransformLogicInstance()
        {
            var routeBinding = new RouteBinding();
            var mockHttpContextAccessor = HttpContextAccessorHelper.MockObject(routeBinding);

            var telemetryScopedLogger = TelemetryLoggerHelper.ScopedLoggerObject(mockHttpContextAccessor);
            var mockIServiceProvider = new Mock<IServiceProvider>();

            var claimTransformValidationLogic = new ClaimTransformValidationLogic(mockHttpContextAccessor);
            return new ClaimTransformLogic(telemetryScopedLogger, mockIServiceProvider.Object, claimTransformValidationLogic, null, mockHttpContextAccessor);
        }
    }
}
