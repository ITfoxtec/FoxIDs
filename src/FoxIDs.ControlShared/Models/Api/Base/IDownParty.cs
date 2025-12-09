using System;
using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Marks a party configuration that controls which upstream parties are allowed.
    /// </summary>
    public interface IDownParty
    {
        /// <summary>
        /// Allowed upstream party links.
        /// </summary>
        List<UpPartyLink> AllowUpParties { get; set; }

        [Obsolete($"Please use {nameof(AllowUpParties)} instead.")]
        /// <summary>
        /// Legacy upstream party names. Use <see cref="AllowUpParties"/> instead.
        /// </summary>
        List<string> AllowUpPartyNames { get; set; }
    }
}
