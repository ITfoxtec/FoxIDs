using FoxIDs.Client.Logic;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class Logs
    {
        private string logSettingsHref;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            logSettingsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/logsettings";
            await base.OnInitializedAsync();
        }
    }
}
