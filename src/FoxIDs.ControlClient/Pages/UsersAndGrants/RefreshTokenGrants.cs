﻿using FoxIDs.Infrastructure;
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
using FoxIDs.Client.Models;
using ITfoxtec.Identity;
using System.Linq;

namespace FoxIDs.Client.Pages.UsersAndGrants
{
    public partial class RefreshTokenGrants
    {
        private PageEditForm<FilterRefreshTokenGrantViewModel> refreshTokenGrantFilterForm;
        private List<GeneralRefreshTokenGrantViewModel> refreshTokenGrants = new List<GeneralRefreshTokenGrantViewModel>();
        private FilterRefreshTokenGrantViewModel deleteRtGrantFilter;
        private string deleteRtGrantError;
        private string paginationToken;
        private string usersHref;
        private string externalUsersHref;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            usersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/users";
            externalUsersHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/externalusers";
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
            deleteRtGrantFilter = null;
            deleteRtGrantError = null;

            try
            {
                SetGeneralRefreshTokenGrants(await DownPartyService.GetRefreshTokenGrantsAsync(null, null, null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                refreshTokenGrantFilterForm.SetError(ex.Message);
            }
        }

        private async Task OnRefreshTokenGrantsFilterValidSubmitAsync(EditContext editContext)
        {
            deleteRtGrantError = null;

            try
            {
                SetGeneralRefreshTokenGrants(await DownPartyService.GetRefreshTokenGrantsAsync(refreshTokenGrantFilterForm.Model.FilterUserIdentifier, refreshTokenGrantFilterForm.Model.FilterClientId, refreshTokenGrantFilterForm.Model.FilterAuthMethod));

                if (refreshTokenGrants.Count() > 0 && 
                      (!refreshTokenGrantFilterForm.Model.FilterUserIdentifier.IsNullOrWhiteSpace() || !refreshTokenGrantFilterForm.Model.FilterClientId.IsNullOrWhiteSpace() || !refreshTokenGrantFilterForm.Model.FilterAuthMethod.IsNullOrWhiteSpace()))
                { 
                    deleteRtGrantFilter = refreshTokenGrantFilterForm.Model.Map<FilterRefreshTokenGrantViewModel>();
                }
                else
                {
                    deleteRtGrantFilter = null;
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    refreshTokenGrantFilterForm.SetFieldError(nameof(refreshTokenGrantFilterForm.Model.FilterUserIdentifier), ex.Message);
                    if (!refreshTokenGrantFilterForm.Model.FilterClientId.IsNullOrWhiteSpace())
                    {
                        refreshTokenGrantFilterForm.SetFieldError(nameof(refreshTokenGrantFilterForm.Model.FilterClientId), ex.Message);
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
                SetGeneralRefreshTokenGrants(await DownPartyService.GetRefreshTokenGrantsAsync(refreshTokenGrantFilterForm.Model.FilterUserIdentifier, refreshTokenGrantFilterForm.Model.FilterClientId, refreshTokenGrantFilterForm.Model.FilterAuthMethod, paginationToken: paginationToken), addUsers: true);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    refreshTokenGrantFilterForm.SetFieldError(nameof(refreshTokenGrantFilterForm.Model.FilterUserIdentifier), ex.Message);
                    if (!refreshTokenGrantFilterForm.Model.FilterClientId.IsNullOrWhiteSpace())
                    {
                        refreshTokenGrantFilterForm.SetFieldError(nameof(refreshTokenGrantFilterForm.Model.FilterClientId), ex.Message);
                    }
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralRefreshTokenGrants(PaginationResponse<RefreshTokenGrant> dataRefreshTokenGrants, bool addUsers = false)
        {
            if (!addUsers)
            {
                refreshTokenGrants.Clear();
            }
            foreach (var g in dataRefreshTokenGrants.Data)
            {
                refreshTokenGrants.Add(new GeneralRefreshTokenGrantViewModel(g));
            }

            paginationToken = dataRefreshTokenGrants.PaginationToken;
        }

        private async Task ShowDetailsRefreshTokenGrantAsync(GeneralRefreshTokenGrantViewModel generalRefreshTokenGrant)
        {
            try
            {
                var grant = await DownPartyService.GetRefreshTokenGrantAsync(generalRefreshTokenGrant.RefreshToken);
                generalRefreshTokenGrant.Details =  grant.Map<RefreshTokenGrantViewModel>();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalRefreshTokenGrant.Error = ex.Message;
            }
        }

        private string GetInfoText(GeneralRefreshTokenGrantViewModel generalRefreshTokenGrant)
        {
            var infoText = new List<string>();
            if (!generalRefreshTokenGrant.Email.IsNullOrWhiteSpace())
            {
                infoText.Add(generalRefreshTokenGrant.Email);
            }
            if (!generalRefreshTokenGrant.Phone.IsNullOrWhiteSpace())
            {
                infoText.Add(generalRefreshTokenGrant.Phone);
            }
            if (!generalRefreshTokenGrant.Username.IsNullOrWhiteSpace())
            {
                infoText.Add(generalRefreshTokenGrant.Username);
            }

            return string.Join(", ", infoText);
        }

        private string GetDeleteText()
        {
            var u = deleteRtGrantFilter.FilterUserIdentifier;
            var a = deleteRtGrantFilter.FilterAuthMethod;
            var c = deleteRtGrantFilter.FilterClientId;

            return $"Delete grants for {(u.IsNullOrWhiteSpace() ? "all users" : $"users with '{u}'")} {(a.IsNullOrWhiteSpace() ? string.Empty : $"authenticated with '{a}'")}{(c.IsNullOrWhiteSpace() ? string.Empty : $"in application '{c}'")}.";
        }

        private async Task DeleteRefreshTokenGrantsAsync()
        {
            deleteRtGrantError = null;

            try
            {
                await DownPartyService.DeleteRefreshTokenGrantsAsync(deleteRtGrantFilter.FilterUserIdentifier, deleteRtGrantFilter.FilterClientId, deleteRtGrantFilter.FilterAuthMethod);

                refreshTokenGrantFilterForm.Model.FilterUserIdentifier = deleteRtGrantFilter.FilterUserIdentifier;
                refreshTokenGrantFilterForm.Model.FilterClientId = deleteRtGrantFilter.FilterClientId;
                refreshTokenGrantFilterForm.Model.FilterAuthMethod = deleteRtGrantFilter.FilterAuthMethod;
                await OnRefreshTokenGrantsFilterValidSubmitAsync(null);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                deleteRtGrantError = ex.Message;
            }
        }
    }
}
