using ITfoxtec.Identity;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using FoxIDs.Client.Logic;

namespace FoxIDs.Client.Shared.Components
{
    public partial class TenantLiNavLink : IDisposable
    {
        private const string liCssClassDefault = "nav-item";
        private string liCssClass = liCssClassDefault;
        private bool isActive = false;
        private string targetHref;
        private List<string> activeHrefList = new List<string>();

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NavigationManager NavigationManager { get; set; }

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Parameter]
        public string Href { get; set; }

        [Parameter]
        public string SubPages { get; set; }

        [Parameter]
        public string LinkClass { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        protected override async Task OnInitializedAsync()
        {
            NavigationManager.LocationChanged += LocationChanged;
            targetHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/{Href}";
            activeHrefList.Add(targetHref);
            if (!SubPages.IsNullOrEmpty())
            {
                var subPagesList = SubPages.Split(',');
                if (subPagesList.Count() > 0)
                {
                    foreach (var subPage in subPagesList)
                    {
                        var subPageHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/{subPage}";
                        activeHrefList.Add(subPageHref);
                    }
                }
            }
            UpdateLiCssClass(NavigationManager.Uri);
            base.OnInitialized();
        }

        private void UpdateLiCssClass(string currentLocation)
        {
            var newIsActive = activeHrefList.Where(h => currentLocation.Contains(h, StringComparison.OrdinalIgnoreCase)).Any();
            if (isActive != newIsActive)
            {
                isActive = newIsActive;
                liCssClass = isActive ? $"{liCssClassDefault} active" : liCssClassDefault;
                StateHasChanged();
            }
        }

        private void LocationChanged(object sender, LocationChangedEventArgs args)
        {
            UpdateLiCssClass(args.Location);
        }

        private async Task OnClickAsync()
        {
            await JSRuntime.InvokeVoidAsync("closeCollapsedMenu");
        }

        void IDisposable.Dispose()
        {
            NavigationManager.LocationChanged -= LocationChanged;
        }
    }
}
