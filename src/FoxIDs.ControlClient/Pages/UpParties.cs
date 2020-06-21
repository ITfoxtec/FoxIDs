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
        private PageEditForm<FilterPartyViewModel> upPartyFilterForm;
        private IEnumerable<UpParty> upParties = new List<UpParty>();
        private string downPartyHref;

        private string loadPartyError;
        private bool createMode;
        private bool deleteAcknowledge;
        private string currentUpPartyName;
        private Modal editLoginUpPartyModal;
        private PageEditForm<LoginUpPartyViewModel> editLoginUpPartyForm;

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
            createMode = true;
            if (type == PartyTypes.Login)
            {
                editLoginUpPartyForm.Init();
                editLoginUpPartyModal.Show();
            }
            else if (type == PartyTypes.Oidc)
            {

            }
            else if (type == PartyTypes.Saml2)
            {

            }
        }

        private async Task ShowUpdateUpPartyAsync(PartyTypes type, string upPartyName)
        {
            loadPartyError = null;
            createMode = false;
            deleteAcknowledge = false;
            if (type == PartyTypes.Login)
            {
                try
                {
                    var loginUpParty = await UpPartyService.GetLoginUpPartyAsync(upPartyName);
                    currentUpPartyName = loginUpParty.Name;
                    editLoginUpPartyForm.Init(loginUpParty.Map<LoginUpPartyViewModel>());
                    editLoginUpPartyModal.Show();
                }
                catch (AuthenticationException)
                {
                    await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
                }
                catch (Exception ex)
                {
                    loadPartyError = ex.Message;
                }
            }
            else if (type == PartyTypes.Oidc)
            {

            }
            else if (type == PartyTypes.Saml2)
            {

            }
        }

        private async Task OnEditLoginUpPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if(createMode)
                {
                    await UpPartyService.UpdateLoginUpPartyAsync(editLoginUpPartyForm.Model.Map<LoginUpParty>());
                    await OnUpPartyFilterValidSubmitAsync(null);
                }
                else
                {
                    await UpPartyService.UpdateLoginUpPartyAsync(editLoginUpPartyForm.Model.Map<LoginUpParty>());
                }
                editLoginUpPartyModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    editLoginUpPartyForm.SetFieldError(nameof(editLoginUpPartyForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }
     
        private async Task DeleteLoginUpPartyAsync(string name)
        {
            try
            {
                await UpPartyService.DeleteLoginUpPartyAsync(name);
                await OnUpPartyFilterValidSubmitAsync(null);
                editLoginUpPartyModal.Hide();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                editLoginUpPartyForm.SetError(ex.Message);
            }
        }
    }
}
