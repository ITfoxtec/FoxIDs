using FoxIDs.Client.Logic;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class LogSettings
    {
        private string logsHref;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            logsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logs";
            await base.OnInitializedAsync();
        }
    }
}
