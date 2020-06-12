using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace FoxIDs.Shared
{
    [Authorize]
    public class PageBase : ComponentBase
    {
    }
}
