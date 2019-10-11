using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public interface IDownParty
    {
        List<string> AllowUpPartyNames { get; set; }
    }
}
