using System;

namespace FoxIDs.Infrastructure.Hosting
{
    [Serializable]
    public class RouteException : Exception
    {
        public RouteException() { }
        public RouteException(string message) : base(message) { }
        public RouteException(string message, Exception inner) : base(message, inner) { }
    }
}
