using System;
using System.Runtime.Serialization;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Resource item mapped to a track with a generated name identifier.
    /// </summary>
    public class TrackResourceItem : ResourceItem, INameValue
    {
        /// <summary>
        /// String representation of the resource id.
        /// </summary>
        [IgnoreDataMember]
        public string Name { get => Convert.ToString(Id); set { } }
    }
}
