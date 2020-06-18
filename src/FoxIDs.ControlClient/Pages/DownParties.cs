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
    public partial class DownParties 
    {
        private Modal createDownPartyModal;
        private PageEditForm<CreateTenantViewModel> createDownPartyForm;
        private PartyTypes createDownPartyType;
        private bool createDownPartyDone;
        private List<string> createDownPartyReceipt = new List<string>();
        private PageEditForm<FilterPartyViewModel> downPartyFilterForm;
        private IEnumerable<DownParty> downParties = new List<DownParty>();
        private Modal downPartyModal;
        private string upPartyHref;

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
            upPartyHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/upparties";
            await base.OnInitializedAsync();
            await DefaultLoadAsync();
        }

        private async Task DefaultLoadAsync()
        {
            try
            {
                downParties = await DownPartyService.FilterDownPartyAsync(null);
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                downPartyFilterForm.SetError(ex.Message);
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

        private void ShowCreateDownPartyModal(PartyTypes type)
        {
            createDownPartyType = type;
            createDownPartyDone = false;
            createDownPartyReceipt = new List<string>();
            createDownPartyForm.Init();
            createDownPartyModal.Show();
        }

        private string CreateTitle()
        {
            Func<string> text = () =>
            {
                switch (createDownPartyType)
                {
                    case PartyTypes.Oidc:
                        return "OpenID Connect";
                    case PartyTypes.OAuth2:
                        return "OAuth 2.0";
                    case PartyTypes.Saml2:
                        return "SAML 2.0";
                    default:
                        return string.Empty;
                }
            };

            return $"Create {text()} Down Party";
        }

        private async Task OnCreateDownPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                createDownPartyDone = true;

                await OnDownPartyFilterValidSubmitAsync(null);
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    createDownPartyForm.SetFieldError(nameof(createDownPartyForm.Model.Name), ex.Message);
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
    }
}
