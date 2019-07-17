using System.Collections.Generic;

namespace FoxIDs.Infrastructure.Filters
{
    public interface IAllowIframeOnDomains
    {
        List<string> AllowIframeOnDomains { get; }
    }
}
