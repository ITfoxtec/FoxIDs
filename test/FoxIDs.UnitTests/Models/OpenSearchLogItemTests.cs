using System.Collections.Generic;
using FoxIDs;
using FoxIDs.Models;
using Newtonsoft.Json;
using Xunit;

namespace FoxIDs.UnitTests
{
    public class OpenSearchLogItemTests
    {
        [Fact]
        public void OpenSearchEventLogItem_Deserializes_DownPartyType()
        {
            var properties = new Dictionary<string, string>
            {
                { Constants.Logs.DownPartyId, "downparty" },
                { Constants.Logs.DownPartyType, PartyTypes.Oidc.ToString() }
            };

            var json = JsonConvert.SerializeObject(properties);
            var item = JsonConvert.DeserializeObject<OpenSearchEventLogItem>(json);

            Assert.Equal("downparty", item.DownPartyId);
            Assert.Equal(PartyTypes.Oidc.ToString(), item.DownPartyType);
        }
    }
}
