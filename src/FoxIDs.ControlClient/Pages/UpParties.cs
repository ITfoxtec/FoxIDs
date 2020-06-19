using FoxIDs.Client;
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
using System.Security.Authentication;
using FoxIDs.Client.Infrastructure.Security;

namespace FoxIDs.Client.Pages
{
    public partial class UpParties
    {
        private Modal createLoginUpPartyModal;
        private PageEditForm<LoginUpPartyViewModel> createLoginUpPartyForm;

        private PartyTypes createUpPartyType;

        private List<string> createUpPartyReceipt = new List<string>();
        private PageEditForm<FilterPartyViewModel> upPartyFilterForm;
        private IEnumerable<UpParty> upParties = new List<UpParty>();
        private Modal upPartyModal;
        private string downPartyHref;

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
            downPartyHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/downparties";
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                upParties = await UpPartyService.FilterUpPartyAsync(null);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                upPartyFilterForm.SetError(ex.Message);
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

        private void ShowCreateUpPartyModal(PartyTypes type)
        {
            createUpPartyType = type;
            if(type == PartyTypes.Login)
            {
                createLoginUpPartyForm.Init();
                createLoginUpPartyModal.Show();
            }
            else if (type == PartyTypes.Oidc)
            {

            }
            else if (type == PartyTypes.Saml2)
            {

            }
        }

        private async Task OnCreateLoginUpPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await UpPartyService.CreateLoginUpPartyAsync(createLoginUpPartyForm.Model.Map<LoginUpParty>());
                await OnUpPartyFilterValidSubmitAsync(null);
                createLoginUpPartyModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    createLoginUpPartyForm.SetFieldError(nameof(createLoginUpPartyForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task ShowUpPartyAsync(string upPartyName)
        {

        }
    }
}
