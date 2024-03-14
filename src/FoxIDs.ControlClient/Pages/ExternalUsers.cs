using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.Toast.Services;
using FoxIDs.Client.Logic;
using System.Linq;
using static FoxIDs.Constants.Models.SamlParty;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages
{
    public partial class ExternalUsers
    {
        private PageEditForm<FilterExternalUserViewModel> externalUserFilterForm;
        private List<GeneralExternalUserViewModel> externalUsers = new List<GeneralExternalUserViewModel>();
        private string usersHref;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Inject]
        public ExternalUserService ExternalUserService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            usersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/users";
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
            try
            {
                await SetGeneralExternalUsersAsync(await ExternalUserService.FilterExternalUserAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                externalUserFilterForm.SetError(ex.Message);
            }
        }


        private async Task OnExternalUserFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await SetGeneralExternalUsersAsync(await ExternalUserService.FilterExternalUserAsync(externalUserFilterForm.Model.FilterValue));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    externalUserFilterForm.SetFieldError(nameof(externalUserFilterForm.Model.FilterValue), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task SetGeneralExternalUsersAsync(IEnumerable<ExternalUser> dataExternalUsers)
        {
            externalUsers.Clear();
            foreach (var dp in dataExternalUsers)
            {
                externalUsers.Add(new GeneralExternalUserViewModel(dp));
            }

            if (externalUsers.Count() > 0)
            {
                var eups = await UpPartyService.FilterUpPartyAsync(null);
                foreach (var externalUser in externalUsers)
                {
                    var eup = eups.Where(u => u.Name == externalUser.UpPartyName).FirstOrDefault();
                    if (eup != null)
                    {
                        externalUser.UpPartyDisplayName = eup.DisplayName;
                    }
                    else
                    {
                        var subUps = await UpPartyService.FilterUpPartyAsync(externalUser.UpPartyName);
                        if(subUps.Count() > 0)
                        {
                            externalUser.UpPartyDisplayName = subUps.Where(u => u.Name == externalUser.UpPartyName).Select(u => u.DisplayName).FirstOrDefault();
                        }
                    }
                }
            }
        }

        private void ShowCreateExternalUser()
        {
            var externalUser = new GeneralExternalUserViewModel();
            externalUser.CreateMode = true;
            externalUser.Edit = true;
            externalUsers.Add(externalUser);
        }

        private async Task ShowUpdateExternalUserAsync(GeneralExternalUserViewModel generalExternalUser)
        {
            generalExternalUser.CreateMode = false;
            generalExternalUser.DeleteAcknowledge = false;
            generalExternalUser.ShowAdvanced = false;
            generalExternalUser.Error = null;
            generalExternalUser.Edit = true;

            try
            {
                var externalUser = await ExternalUserService.GetExternalUserAsync(generalExternalUser);
                await generalExternalUser.Form.InitAsync(externalUser.Map<ExternalUserViewModel>());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalExternalUser.Error = ex.Message;
            }
        }

        private void ExternalUserViewModelAfterInit(GeneralExternalUserViewModel generalExternalUser, ExternalUserViewModel externalUser)
        {
            if (generalExternalUser.CreateMode)
            {
              

            }
        }
        private void ExternalUserCancel(GeneralExternalUserViewModel externalUser)
        {
            if (externalUser.CreateMode)
            {
                externalUsers.Remove(externalUser);
            }
            else
            {
                externalUser.Edit = false;
            }
        }

        private void AddClaim(MouseEventArgs e, List<ClaimAndValues> claims)
        {
            claims.Add(new ClaimAndValues());
        }

        private void RemoveClaim(MouseEventArgs e, List<ClaimAndValues> claims, ClaimAndValues claimAndValues)
        {
            claims.Remove(claimAndValues);
        }

        private async Task OnEditExternalUserValidSubmitAsync(GeneralExternalUserViewModel generalExternalUser, EditContext editContext)
        {
            try
            {
                if (generalExternalUser.CreateMode)
                {
                    var externalUserResult = await ExternalUserService.CreateExternalUserAsync(generalExternalUser.Form.Model.Map<ExternalUserRequest>());
                    generalExternalUser.Form.UpdateModel(externalUserResult.Map<ExternalUserViewModel>());
                    generalExternalUser.CreateMode = false;
                    toastService.ShowSuccess("External user created.");
                    generalExternalUser.LinkClaim = generalExternalUser.Form.Model.LinkClaim;
                    generalExternalUser.UserId = generalExternalUser.Form.Model.UserId;
                }
                else
                {
                    var externalUserResult = await ExternalUserService.UpdateExternalUserAsync(generalExternalUser.Form.Model.Map<ExternalUserRequest>());
                    generalExternalUser.Form.UpdateModel(externalUserResult.Map<ExternalUserViewModel>());
                    toastService.ShowSuccess("External user updated.");
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalExternalUser.Form.SetFieldError(nameof(generalExternalUser.Form.Model.LinkClaim), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteExternalUserAsync(GeneralExternalUserViewModel generalExternalUser)
        {
            try
            {
                await ExternalUserService.DeleteExternalUserAsync(generalExternalUser);
                externalUsers.Remove(generalExternalUser);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalExternalUser.Form.SetError(ex.Message);
            }
        }
    }
}
