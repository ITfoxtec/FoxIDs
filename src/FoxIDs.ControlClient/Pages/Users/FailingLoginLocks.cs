using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Blazored.Toast.Services;
using FoxIDs.Client.Logic;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages.Users
{
    public partial class FailingLoginLocks
    {
        private PageEditForm<FilterFailingLoginViewModel> failingLoginFilterForm;
        private List<FailingLoginLockViewModel> failingLogins = new List<FailingLoginLockViewModel>();
        private string paginationToken;
        private string internalUsersHref;
        private string externalUsersHref;
        private string refreshTokenGrantsHref;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }    

        [Inject]
        public UserService UserService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private int FailingLoginObservationPeriodMinuts => (TrackSelectedLogic.IsTrackSelected ? TrackSelectedLogic.CurrentTrack.FailingLoginObservationPeriod : Constants.TrackDefaults.DefaultFailingLoginObservationPeriod) / 60;

        protected override async Task OnInitializedAsync()
        {
            internalUsersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/internalusers";
            externalUsersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/externalusers";
            refreshTokenGrantsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/refreshtokengrants";
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
        }

        protected override void OnDispose()
        {
            TrackSelectedLogic.OnTrackSelectedAsync -= OnTrackSelectedAsync;
            base.OnDispose();
        }

        private async Task OnTrackSelectedAsync(Track track)
        {
            await DefaultLoadAsync();
            StateHasChanged();
        }

        private async Task DefaultLoadAsync()
        {
            failingLoginFilterForm?.ClearError();
            try
            {
                SetGeneralFailingLogins(await UserService.GetFailingLoginLocksAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                failingLogins?.Clear();
                failingLoginFilterForm.SetError(ex.Message);
            }
        }

        private async Task OnFailingLoginsFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralFailingLogins(await UserService.GetFailingLoginLocksAsync(failingLoginFilterForm.Model.FilterUserIdentifier));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    failingLoginFilterForm.SetFieldError(nameof(failingLoginFilterForm.Model.FilterUserIdentifier), ex.Message);
                    if (!failingLoginFilterForm.Model.FilterUserIdentifier.IsNullOrWhiteSpace())
                    {
                        failingLoginFilterForm.SetFieldError(nameof(failingLoginFilterForm.Model.FilterUserIdentifier), ex.Message);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task LoadMoreUsersAsync()
        {
            try
            {
                SetGeneralFailingLogins(await UserService.GetFailingLoginLocksAsync(failingLoginFilterForm.Model.FilterUserIdentifier, paginationToken: paginationToken), addUsers: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    failingLoginFilterForm.SetFieldError(nameof(failingLoginFilterForm.Model.FilterUserIdentifier), ex.Message);
                    if (!failingLoginFilterForm.Model.FilterUserIdentifier.IsNullOrWhiteSpace())
                    {
                        failingLoginFilterForm.SetFieldError(nameof(failingLoginFilterForm.Model.FilterUserIdentifier), ex.Message);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralFailingLogins(PaginationResponse<FailingLoginLock> dataFailingLogins, bool addUsers = false)
        {
            if (!addUsers)
            {
                failingLogins.Clear();
            }
            foreach (var g in dataFailingLogins.Data)
            {
                failingLogins.Add(g.Map<FailingLoginLockViewModel>());
            }

            paginationToken = dataFailingLogins.PaginationToken;
        }

        private string GetFailingLoginTypeText(FailingLoginTypes failingLoginType)
        {
            switch (failingLoginType)
            {
                case FailingLoginTypes.InternalLogin:
                    return "Internal user";
                case FailingLoginTypes.ExternalLogin:
                    return "External user";
                case FailingLoginTypes.SmsCode:
                    return "SMS code";
                case FailingLoginTypes.EmailCode:
                    return "Email code";
                case FailingLoginTypes.TwoFactorSmsCode:
                    return "SMS two-factor code";
                case FailingLoginTypes.TwoFactorEmailCode:
                    return "Email two-factor code";
                case FailingLoginTypes.TwoFactorAuthenticator:
                    return "Two-factor authenticator app";
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task DeleteFailingLoginAsync(FailingLoginLockViewModel failingLogin)
        {
            failingLogin.Error = null;

            try
            {
                await UserService.DeleteFailingLoginLockAsync(failingLogin.UserIdentifier, failingLogin.FailingLoginType);
                await OnFailingLoginsFilterValidSubmitAsync(null);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                failingLogin.Error = ex.Message;
            }
        }
    }
}
