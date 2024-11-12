﻿using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components.Web;
using Blazored.Toast.Services;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class ClaimMappings
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string textsHref;
        private string plansHref;
        private string riskPasswordsHref;
        private PageEditForm<ClaimMappingViewModel> trackClaimMappingForm;
        private PageEditForm<ClaimMappingDefaultViewModel> trackClaimMappingDefaultForm;

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        protected override async Task OnInitializedAsync()
        {
            trackSettingsHref = $"{TenantName}/envsettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            textsHref = $"{TenantName}/texts";
            plansHref = $"{TenantName}/plans";
            riskPasswordsHref = $"{TenantName}/riskpasswords";
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
                var trackClaimMapping = await TrackService.GetTrackClaimMappingAsync();
                await trackClaimMappingForm.InitAsync(new ClaimMappingViewModel { ClaimMappings = trackClaimMapping ?? new List<ClaimMap>() });
                await trackClaimMappingDefaultForm.InitAsync();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                trackClaimMappingForm.SetError(ex.Message);
            }
        }

        private void AddClaimMapping(MouseEventArgs e)
        {
            trackClaimMappingForm.Model.ClaimMappings.Add(new ClaimMap());
        }

        private void RemoveClaimMapping(MouseEventArgs e, ClaimMap claimMapping)
        {
            trackClaimMappingForm.Model.ClaimMappings.Remove(claimMapping);
        }

        private async Task OnUpdateClaimMappingValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.SaveTrackClaimMappingAsync(trackClaimMappingForm.Model.ClaimMappings);
                toastService.ShowSuccess("Claim mappings updated.");
            }
            catch (Exception ex)
            {
                trackClaimMappingForm.SetError(ex.Message);
            }
        }
    }
}
