using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;

namespace FoxIDs.Client.Shared
{
    [Authorize]
    public class PageBase : ComponentBase
    {
    }
}
