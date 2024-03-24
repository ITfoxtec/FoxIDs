using FoxIDs.Models;
using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class EndpointException : Exception
    {
        public RouteBinding RouteBinding { get; set; }

        public EndpointException() { }
        public EndpointException(string message) : base(message) { }
        public EndpointException(string message, Exception inner) : base(message, inner) { }

        public override string Message => RouteBinding == null ? base.Message : $"{base.Message} [Tenant: {RouteBinding.TenantName}, Track: {RouteBinding.TrackName}, PartyNameAndBinding: {RouteBinding.PartyNameAndBinding}]";

    }
}
