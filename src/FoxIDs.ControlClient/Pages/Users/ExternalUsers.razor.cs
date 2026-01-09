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
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages.Users
{
    public partial class ExternalUsers
    {
        private PageEditForm<FilterUpPartyViewModel> SelectUpPartyFilterForm;
        private IEnumerable<UpParty> selectUpParties;
        private PageEditForm<FilterExternalUserViewModel> externalUserFilterForm;
        private List<GeneralExternalUserViewModel> externalUsers = new List<GeneralExternalUserViewModel>();
        private string paginationToken;
        private string internalUsersHref;
        private string failingLoginsHref;
        private string refreshTokenGrantsHref;
        private string activeSessionsHref;

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
            internalUsersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/internalusers";
            failingLoginsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/failingloginlocks";
            refreshTokenGrantsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/refreshtokengrants";
            activeSessionsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/activesessions";
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
            externalUserFilterForm?.ClearError();
            try
            {
                await SetGeneralExternalUsersAsync(await ExternalUserService.GetExternalUsersAsync(null, null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                externalUsers?.Clear();
                externalUserFilterForm.SetError(ex.Message);
            }
        }


        private async Task OnExternalUserFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await SetGeneralExternalUsersAsync(await ExternalUserService.GetExternalUsersAsync(externalUserFilterForm.Model.FilterValue, externalUserFilterForm.Model.FilterValue));
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

        private async Task LoadMoreExternalUsersAsync()
        {
            try
            {
                await SetGeneralExternalUsersAsync(await ExternalUserService.GetExternalUsersAsync(externalUserFilterForm.Model.FilterValue, externalUserFilterForm.Model.FilterValue, paginationToken: paginationToken), addUsers: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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

        private async Task SetGeneralExternalUsersAsync(PaginationResponse<ExternalUser> dataExternalUsers, bool addUsers = false)
        {
            if(!addUsers)
            {
                externalUsers.Clear();
            }
            foreach (var dp in dataExternalUsers.Data)
            {
                externalUsers.Add(new GeneralExternalUserViewModel(dp));
            }

            if (externalUsers.Count() > 0)
            {
                await LoadUpPartiesAsync();
                foreach (var externalUser in externalUsers)
                {
                    var eup = selectUpParties.Where(u => u.Name == externalUser.UpPartyName).FirstOrDefault();
                    if (eup != null)
                    {
                        externalUser.UpPartyDisplayName = eup.DisplayName;
                    }
                    else
                    {
                        externalUser.UpPartyDisplayName = await GetUpPartyDisplayName(externalUser.UpPartyName);
                    }
                }
            }

            paginationToken = dataExternalUsers.PaginationToken;
        }

        private async Task<string> GetUpPartyDisplayName(string upPartyName)
        {
            var subUps = (await UpPartyService.GetUpPartiesAsync(upPartyName)).Data;
            if (subUps.Count() > 0)
            {
                return subUps.Where(u => u.Name == upPartyName).Select(u => u.DisplayName)?.FirstOrDefault();
            }
            return null;
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
                var externalUser = await ExternalUserService.GetExternalUserAsync(generalExternalUser.UpPartyName, generalExternalUser.LinkClaimValue, generalExternalUser.RedemptionClaimValue);
                generalExternalUser.LinkClaimValue = externalUser.LinkClaimValue;
                generalExternalUser.RedemptionClaimValue = externalUser.RedemptionClaimValue;
                await generalExternalUser.Form.InitAsync(externalUser.Map<ExternalUserViewModel>(afterMap: afterMap =>
                {
                    afterMap.UpPartyDisplayName = generalExternalUser.UpPartyDisplayName;
                }));
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

        private async Task ExternalUserViewModelAfterInitAsync(GeneralExternalUserViewModel generalExternalUser, ExternalUserViewModel externalUser)
        {
            if (generalExternalUser.CreateMode)
            {
                await LoadUpPartiesAsync();
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

        private void ShowSelectUpParty(GeneralExternalUserViewModel generalExternalUser)
        {
            generalExternalUser.Form.ClearError();
            generalExternalUser.Form.Model.UpPartyName = null;
            generalExternalUser.Form.Model.UpPartyDisplayName = null;
        }

        private async Task LoadUpPartiesAsync(string filterName = null, bool force = false)
        {
            if (force || !(selectUpParties?.Count() > 0) || !filterName.IsNullOrEmpty())
            {
                var sup = (await UpPartyService.GetUpPartiesAsync(filterName)).Data;
                selectUpParties = sup.Where(u => u.Type != PartyTypes.Login && u.Type != PartyTypes.OAuth2);
            }
        }

        private async Task OnSelectUpPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await LoadUpPartiesAsync(SelectUpPartyFilterForm.Model.FilterName, force: true);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    SelectUpPartyFilterForm.SetFieldError(nameof(SelectUpPartyFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void SelectUpParty(GeneralExternalUserViewModel generalExternalUser, UpParty upParty)
        {
            generalExternalUser.Form.Model.UpPartyName = upParty.Name;
            generalExternalUser.Form.Model.UpPartyDisplayName = upParty.DisplayName;
        }

        private async Task OnEditExternalUserValidSubmitAsync(GeneralExternalUserViewModel generalExternalUser, EditContext editContext)
        {
            try
            {
                if (generalExternalUser.CreateMode)
                {
                    var externalUserResult = await ExternalUserService.CreateExternalUserAsync(generalExternalUser.Form.Model.Map<ExternalUserRequest>());
                    generalExternalUser.CreateMode = false;
                    toastService.ShowSuccess("External user created.");
                    generalExternalUser.LinkClaimValue = externalUserResult.LinkClaimValue;
                    generalExternalUser.RedemptionClaimValue = externalUserResult.RedemptionClaimValue;
                    generalExternalUser.UserId = externalUserResult.UserId;
                    generalExternalUser.UpPartyName = externalUserResult.UpPartyName;
                    generalExternalUser.UpPartyDisplayName = await GetUpPartyDisplayName(externalUserResult.UpPartyName);
                    generalExternalUser.LoadName(externalUserResult.Claims);
                    generalExternalUser.Form.UpdateModel(externalUserResult.Map<ExternalUserViewModel>(afterMap: afterMap =>
                    {
                        afterMap.UpPartyDisplayName = generalExternalUser.UpPartyDisplayName;
                    }));
                }
                else
                {
                    var externalUserResult = await ExternalUserService.UpdateExternalUserAsync(generalExternalUser.Form.Model.Map<ExternalUserUpdateRequest>(afterMap: afterMap => 
                    {
                        if (afterMap.RedemptionClaimValue != generalExternalUser.RedemptionClaimValue)
                        {
                            afterMap.UpdateRedemptionClaimValue = afterMap.RedemptionClaimValue;
                        }
                        if (afterMap.LinkClaimValue != generalExternalUser.LinkClaimValue)
                        {
                            afterMap.UpdateLinkClaimValue = afterMap.LinkClaimValue;
                        }
                        afterMap.RedemptionClaimValue = generalExternalUser.RedemptionClaimValue;
                        afterMap.LinkClaimValue = generalExternalUser.LinkClaimValue;
                    }));
                    toastService.ShowSuccess("External user updated.");
                    generalExternalUser.LinkClaimValue = externalUserResult.LinkClaimValue;
                    generalExternalUser.RedemptionClaimValue = externalUserResult.RedemptionClaimValue;
                    generalExternalUser.LoadName(externalUserResult.Claims);
                    generalExternalUser.Form.UpdateModel(externalUserResult.Map<ExternalUserViewModel>(afterMap: afterMap =>
                    {
                        afterMap.UpPartyDisplayName = generalExternalUser.UpPartyDisplayName;

                    }));
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalExternalUser.Form.SetError(ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private string GetInfoTextType(GeneralExternalUserViewModel externalUser)
        {
            var upParty = externalUser.UpPartyDisplayName;
            if (upParty.IsNullOrWhiteSpace())
            {
                upParty = externalUser.UpPartyName;
            }

            return upParty;
        }
        
        private string GetInfoText(GeneralExternalUserViewModel externalUser, bool includeIdentifiers = false)
        {
            var infoText = GetIdentifiersInfoText(externalUser);

            if (includeIdentifiers)
            {
                if (!externalUser.Name.IsNullOrWhiteSpace())
                {
                    infoText += $" \u2022 {externalUser.Name}";
                }
            }
            else
            {
                if (!externalUser.Name.IsNullOrWhiteSpace())
                {
                    infoText = externalUser.Name;
                }
            }

            return infoText;
        }

        private string GetIdentifiersInfoText(GeneralExternalUserViewModel externalUser)
        {
            if (!externalUser.RedemptionClaimValue.IsNullOrWhiteSpace())
            {
                return externalUser.RedemptionClaimValue;
            }
            if (!externalUser.LinkClaimValue.IsNullOrWhiteSpace())
            {
                return externalUser.LinkClaimValue;
            }
            return externalUser.UserId;
        }

        private async Task DeleteExternalUserAsync(GeneralExternalUserViewModel generalExternalUser)
        {
            try
            {
                await ExternalUserService.DeleteExternalUserAsync(generalExternalUser.UpPartyName, generalExternalUser.LinkClaimValue, generalExternalUser.RedemptionClaimValue);
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

