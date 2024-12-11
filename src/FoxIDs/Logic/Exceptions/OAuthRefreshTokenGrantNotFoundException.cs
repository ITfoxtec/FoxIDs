using System;

namespace FoxIDs.Logic
{
    [Serializable]
    public class OAuthRefreshTokenGrantNotFoundException : OAuthRequestException
    {
        public OAuthRefreshTokenGrantNotFoundException() { }
        public OAuthRefreshTokenGrantNotFoundException(string errorDescription) : base(errorDescription)
        { }
        public OAuthRefreshTokenGrantNotFoundException(string errorDescription, Exception inner) : base(errorDescription, inner)
        { }
    }
}
