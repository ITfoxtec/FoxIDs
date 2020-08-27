using System;
using System.Runtime.Serialization;

namespace FoxIDs.Models.Api
{
    public class TrackResourceItem : ResourceItem, INameValue
    {
        [IgnoreDataMember]
        public string Name { get => Convert.ToString(Id); }
    }
}
