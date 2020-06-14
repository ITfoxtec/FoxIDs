using FoxIDs.Client.Infrastructure;
using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Client.Pages
{
    public partial class Parties
    {
        private bool showDownPartyTab = true;
        private PageEditForm<FilterPartyViewModel> downPartyFilterForm;
        private IEnumerable<DownParty> downParties = new List<DownParty>();
        private Modal downPartyModal;
        private PageEditForm<FilterPartyViewModel> upPartyFilterForm;
        private IEnumerable<UpParty> upParties = new List<UpParty>();
        private Modal upPartyModal;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public NotificationLogic NotificationLogic { get; set; }

        [Inject]
        public DownPartyService DownPartyService { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await ShowDownPartyTabAsync(true);
        }

        private async Task ShowDownPartyTabAsync(bool force = false)
        {
            if (!showDownPartyTab || force)
            {
                //if(!force)
                //{
                //    downPartyFilterForm.Model.FilterName = string.Empty;
                //}
                showDownPartyTab = true;
                try
                {
                    downParties = await DownPartyService.FilterDownPartyAsync(null);
                }
                catch (Exception ex)
                {
                    downPartyFilterForm.SetError(ex.Message);
                }
            }
        }

        private async Task ShowUpPartyTabAsync()
        {
            if (showDownPartyTab)
            {
                //upPartyFilterForm.Model.FilterName = string.Empty;
                showDownPartyTab = false;
                try
                {
                    upParties = await UpPartyService.FilterUpPartyAsync(null);
                }
                catch (Exception ex)
                {
                    upPartyFilterForm.SetError(ex.Message);
                }
            }
        }
        
        private async Task OnDownPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                downParties = await DownPartyService.FilterDownPartyAsync(downPartyFilterForm.Model.FilterName);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    downPartyFilterForm.SetFieldError(nameof(downPartyFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task OnUpPartyFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                upParties = await UpPartyService.FilterUpPartyAsync(upPartyFilterForm.Model.FilterName);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    upPartyFilterForm.SetFieldError(nameof(upPartyFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task ShowDownPartyAsync(string upPartyName)
        {

        }

        private async Task ShowUpPartyAsync(string upPartyName)
        {

        }
    }
}
