using System;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    public interface IDownParty
    {
        List<UpPartyLink> AllowUpParties { get; set; }

        [Obsolete($"Please use {nameof(AllowUpParties)} instead.")]
        List<string> AllowUpPartyNames { get; set; }
    }
}
