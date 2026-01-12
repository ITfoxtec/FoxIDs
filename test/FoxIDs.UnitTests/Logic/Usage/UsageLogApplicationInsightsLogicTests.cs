using System.Reflection;
using Azure.Core;
using FoxIDs.Logic;
using FoxIDs.Models.Config;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace FoxIDs.UnitTests.Logic.Usage
{
    public class UsageLogApplicationInsightsLogicTests
    {
        [Fact]
        public void GetQuery_ExcludeTrackLinkLogins_UsesConditionalCount()
        {
            var settings = new FoxIDsControlSettings { ApplicationInsights = new ApplicationInsightsSettings() };
            var logic = new UsageLogApplicationInsightsLogic(settings, new LogAnalyticsWorkspaceProvider(Mock.Of<TokenCredential>()), new HttpContextAccessor());
            var method = typeof(UsageLogApplicationInsightsLogic).GetMethod("GetQuery", BindingFlags.NonPublic | BindingFlags.Instance);

            var query = (string)method.Invoke(logic, new object[] { "AppEvents", string.Empty, "UsageType == 'Login'", string.Empty, string.Empty, false, true });

            Assert.Contains("DownPartyType", query);
            Assert.Contains("sum(iif", query);
        }

        [Fact]
        public void GetQuery_DefaultLoginCount_UsesCount()
        {
            var settings = new FoxIDsControlSettings { ApplicationInsights = new ApplicationInsightsSettings() };
            var logic = new UsageLogApplicationInsightsLogic(settings, new LogAnalyticsWorkspaceProvider(Mock.Of<TokenCredential>()), new HttpContextAccessor());
            var method = typeof(UsageLogApplicationInsightsLogic).GetMethod("GetQuery", BindingFlags.NonPublic | BindingFlags.Instance);

            var query = (string)method.Invoke(logic, new object[] { "AppEvents", string.Empty, "UsageType == 'Login'", string.Empty, string.Empty, false, false });

            Assert.Contains("UsageCount = count()", query);
        }
    }
}
