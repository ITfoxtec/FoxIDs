using Blazored.SessionStorage;
using FoxIDs.Client.Models.Config;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Infrastructure;

namespace FoxIDs.Client.Logic
{
    public class RouteBindingLogic
    {
        private const string tenanSessionKey = "tenant_session";
        private string tenantName;
        private TenantResponse myTenant;
        private readonly ClientSettings clientSettings;
        private readonly IServiceProvider serviceProvider;
        private readonly TrackSelectedLogic trackSelectedLogic;
        private readonly NotificationLogic notificationLogic;
        private readonly NavigationManager navigationManager;
        private readonly ISessionStorageService sessionStorage;
        private readonly AuthenticationStateProvider authenticationStateProvider;

        public RouteBindingLogic(ClientSettings clientSettings, IServiceProvider serviceProvider, TrackSelectedLogic trackSelectedLogic, NotificationLogic notificationLogic, NavigationManager navigationManager, ISessionStorageService sessionStorage, AuthenticationStateProvider authenticationStateProvider)
        {
            this.clientSettings = clientSettings;
            this.serviceProvider = serviceProvider;
            this.trackSelectedLogic = trackSelectedLogic;
            this.notificationLogic = notificationLogic;
            this.navigationManager = navigationManager;
            this.sessionStorage = sessionStorage;
            this.authenticationStateProvider = authenticationStateProvider;
        }

        public bool IsMasterTenant => Constants.Routes.MasterTenantName.Equals(GetTenantNameLocal(), StringComparison.OrdinalIgnoreCase);

        public bool IsMasterTrack => trackSelectedLogic.Track != null && Constants.Routes.MasterTrackName.Equals(trackSelectedLogic.Track.Name, StringComparison.OrdinalIgnoreCase);

        public bool RequestPayment { get; private set; }

        public void SetMyTenant(TenantResponse tenant)
        {
            myTenant = tenant;

            if (!IsMasterTenant && clientSettings.EnablePayment)
            {
                UpdatRequestPayment(myTenant);
            }
        }

        private void UpdatRequestPayment(TenantResponse myTenant)
        {
            if (myTenant.DoPayment && myTenant.Payment?.IsActive != true)
            {
                RequestPayment = true;
            }
            else
            {
                RequestPayment = false;
            }

            notificationLogic.RequestPaymentUpdated();
        }

        public async Task<string> GetTenantNameAsync()
        {
            if(tenantName.IsNullOrEmpty())
            {
                await InitRouteBindingAsync();
            }
            return tenantName;
        }

        private string GetTenantNameLocal()
        {
            if (tenantName.IsNullOrEmpty())
            {
                SetTenantName(); 
            }
            return tenantName;
        }

        public string GetFoxIDsTenantEndpoint()
        {
            if (trackSelectedLogic.Track != null && !IsMasterTrack && !IsMasterTenant && myTenant != null && myTenant.CustomDomainVerified)
            {
                return $"{(navigationManager.BaseUri.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "http" : "https")}://{myTenant.CustomDomain}";
            }
            else
            {
                return $"{clientSettings.FoxIDsEndpoint}/{tenantName}";
            }
        }

        public string GetPage()
        {
            var urlSplit = navigationManager.ToBaseRelativePath(navigationManager.Uri).Split('/');
            if(urlSplit.Count() > 1) 
            {
                return urlSplit[1];
            }
            else
            {
                return string.Empty;
            }
        }

        public string GetBaseUri()
        {
            return navigationManager.BaseUri;
        }

        public async Task InitRouteBindingAsync()
        {
            SetTenantName();
            await ValidateAndUpdateSessionTenantName();
            if (!IsMasterTenant)
            {
                await LoadMyTenantAsync();
            }
        }

        public void SetTenantName()
        {
            var urlSplit = navigationManager.ToBaseRelativePath(navigationManager.Uri).Split('/');
            tenantName = urlSplit[0];
        }

        private async Task ValidateAndUpdateSessionTenantName()
        {
            var tenanSession = await sessionStorage.GetItemAsync<string>(tenanSessionKey);
            if(tenanSession == null)
            {
                await sessionStorage.SetItemAsync(tenanSessionKey, tenantName);
            }
            else
            {
                if (!tenanSession.Equals(tenantName, StringComparison.OrdinalIgnoreCase))
                {
                    await (authenticationStateProvider as OidcAuthenticationStateProvider).DeleteSessionAsync(true);
                    await sessionStorage.SetItemAsync(tenanSessionKey, tenantName);
                }
            }
        }

        private async Task LoadMyTenantAsync()
        {
            var authenticationState = await authenticationStateProvider.GetAuthenticationStateAsync();
            if (authenticationState.User.Identity.IsAuthenticated && !IsMasterTenant)
            {
                var myTenantService = serviceProvider.GetService<MyTenantService>();
                try
                {
                    SetMyTenant(await myTenantService.GetTenantAsync());
                }
                catch (FoxIDsApiException ex)
                {
                    if (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    {
                        Console.WriteLine("Forbidden, you do not possess the required scope and role to load the tenant data.");
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }
    }
}
