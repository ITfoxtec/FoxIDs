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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.Toast.Services;
using FoxIDs.Client.Logic;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages.Users
{
    public partial class ActiveSessions
    {
        private PageEditForm<FilterActiveSessionViewModel> activeSessionFilterForm;
        private List<GeneralActiveSessionViewModel> activeSessions = new List<GeneralActiveSessionViewModel>();
        private FilterActiveSessionViewModel deleteActiveSessionFilter;
        private string deleteActiveSessionError;
        private string paginationToken;
        private string internalUsersHref;
        private string externalUsersHref;
        private string failingLoginsHref;
        private string refreshTokenGrantsHref;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public UserService UserService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            internalUsersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/internalusers";
            externalUsersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/externalusers";
            failingLoginsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/failingloginlocks";
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
            deleteActiveSessionFilter = null;
            deleteActiveSessionError = null;
            activeSessionFilterForm?.ClearError();
            try
            {
                SetGeneralActiveSessions(await UserService.GetActiveSessionsAsync(null, null, null, null, null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                activeSessions?.Clear();
                activeSessionFilterForm.SetError(ex.Message);
            }
        }

        private async Task OnActiveSessionsFilterValidSubmitAsync(EditContext editContext)
        {
            deleteActiveSessionError = null;

            try
            {
                SetGeneralActiveSessions(await UserService.GetActiveSessionsAsync(activeSessionFilterForm.Model.FilterUserIdentifier, activeSessionFilterForm.Model.FilterUserIdentifier, activeSessionFilterForm.Model.FilterDownParty, activeSessionFilterForm.Model.FilterUpParty, activeSessionFilterForm.Model.FilterSessionId));

                if (activeSessions.Count() > 0 &&
                    (!activeSessionFilterForm.Model.FilterUserIdentifier.IsNullOrWhiteSpace() || !activeSessionFilterForm.Model.FilterUpParty.IsNullOrWhiteSpace() || !activeSessionFilterForm.Model.FilterDownParty.IsNullOrWhiteSpace() || !activeSessionFilterForm.Model.FilterSessionId.IsNullOrWhiteSpace()))
                {
                    deleteActiveSessionFilter = activeSessionFilterForm.Model.Map<FilterActiveSessionViewModel>();
                }
                else
                {
                    deleteActiveSessionFilter = null;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    activeSessionFilterForm.SetFieldError(nameof(activeSessionFilterForm.Model.FilterUserIdentifier), ex.Message);
                }
                else
                {
                    throw;
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                activeSessionFilterForm.SetError(ex.Message);
            }
        }

        private async Task LoadMoreSessionsAsync()
        {
            deleteActiveSessionError = null;
            try
            {
                SetGeneralActiveSessions(await UserService.GetActiveSessionsAsync(activeSessionFilterForm.Model.FilterUserIdentifier, activeSessionFilterForm.Model.FilterUserIdentifier, activeSessionFilterForm.Model.FilterDownParty, activeSessionFilterForm.Model.FilterUpParty, activeSessionFilterForm.Model.FilterSessionId, paginationToken: paginationToken), addSessions: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (FoxIDsApiException ex)
            {
                activeSessionFilterForm.SetError(ex.Message);
            }
        }

        private void SetGeneralActiveSessions(PaginationResponse<ActiveSession> dataActiveSessions, bool addSessions = false)
        {
            if (!addSessions)
            {
                activeSessions?.Clear();
            }

            foreach (var session in dataActiveSessions.Data)
            {
                activeSessions.Add(new GeneralActiveSessionViewModel(session));
            }

            paginationToken = dataActiveSessions.PaginationToken;
        }

        private async Task ShowDetailsActiveSessionAsync(GeneralActiveSessionViewModel generalActiveSession)
        {
            try
            {
                generalActiveSession.Details = generalActiveSession.Map<ActiveSessionViewModel>();
            }
            catch (HttpRequestException ex)
            {
                generalActiveSession.Error = ex.Message;
            }
        }

        private string GetInfoText(GeneralActiveSessionViewModel generalActiveSession)
        {
            var infoText = new List<string>();
            if (!generalActiveSession.Email.IsNullOrWhiteSpace())
            {
                infoText.Add(generalActiveSession.Email);
            }
            if (!generalActiveSession.Phone.IsNullOrWhiteSpace())
            {
                infoText.Add(generalActiveSession.Phone);
            }
            if (!generalActiveSession.Username.IsNullOrWhiteSpace())
            {
                infoText.Add(generalActiveSession.Username);
            }

            return string.Join(", ", infoText);
        }

        private IEnumerable<string> GetDeleteText()
        {
            yield return deleteActiveSessionFilter.FilterUserIdentifier.IsNullOrWhiteSpace() ? "all active sessions" : $"active sessions for users with '{deleteActiveSessionFilter.FilterUserIdentifier}'";
            if (!deleteActiveSessionFilter.FilterUpParty.IsNullOrWhiteSpace())
            {
                yield return $"authenticated via '{deleteActiveSessionFilter.FilterUpParty}'";
            }
            if (!deleteActiveSessionFilter.FilterDownParty.IsNullOrWhiteSpace())
            {
                yield return $"in application '{deleteActiveSessionFilter.FilterDownParty}'";
            }
            if (!deleteActiveSessionFilter.FilterSessionId.IsNullOrWhiteSpace())
            {
                yield return $"with session id '{deleteActiveSessionFilter.FilterSessionId}'";
            }
        }

        private async Task DeleteActiveSessionsAsync()
        {
            deleteActiveSessionError = null;

            try
            {
                await UserService.DeleteActiveSessionsAsync(deleteActiveSessionFilter.FilterUserIdentifier, deleteActiveSessionFilter.FilterUserIdentifier, deleteActiveSessionFilter.FilterUpParty, deleteActiveSessionFilter.FilterDownParty, deleteActiveSessionFilter.FilterSessionId);

                activeSessionFilterForm.Model.FilterUserIdentifier = deleteActiveSessionFilter.FilterUserIdentifier;
                activeSessionFilterForm.Model.FilterUpParty = deleteActiveSessionFilter.FilterUpParty;
                activeSessionFilterForm.Model.FilterDownParty = deleteActiveSessionFilter.FilterDownParty;
                activeSessionFilterForm.Model.FilterSessionId = deleteActiveSessionFilter.FilterSessionId;
                await OnActiveSessionsFilterValidSubmitAsync(null);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteActiveSessionError = ex.Message;
            }
        }
    }
}
