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
using BlazorInputFile;
using System.Linq;
using System.IO;
using ITfoxtec.Identity.Util;
using System.Security.Cryptography.X509Certificates;
using ITfoxtec.Identity;

namespace FoxIDs.Client.Pages
{
    public partial class UpParties
    {
        private PageEditForm<FilterPartyViewModel> upPartyFilterForm;
        private IEnumerable<UpParty> upParties = new List<UpParty>();
        private string downPartyHref;

        private string loadPartyError;
        private bool createMode;
        private bool showAdvanced;
        private bool deleteAcknowledge;
        private string currentUpPartyName;
        private Modal editLoginUpPartyModal;
        private PageEditForm<LoginUpPartyViewModel> editLoginUpPartyForm;
        private Modal editSamlUpPartyModal;
        private PageEditForm<SamlUpPartyViewModel> editSamlUpPartyForm;

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
            showAdvanced = false;
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
                editSamlUpPartyForm.Init();
                editSamlUpPartyModal.Show();
            }
        }

        private async Task ShowUpdateUpPartyAsync(PartyTypes type, string upPartyName)
        {
            loadPartyError = null;
            createMode = false;
            deleteAcknowledge = false;
            showAdvanced = false;
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
                try
                {
                    var samlUpParty = await UpPartyService.GetSamlUpPartyAsync(upPartyName);
                    currentUpPartyName = samlUpParty.Name;
                    editSamlUpPartyForm.Init(samlUpParty.Map<SamlUpPartyViewModel>());
                    editSamlUpPartyModal.Show();
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

        IFileListEntry file;
        private async Task OnSamlUpPartyCertificateFileSelected(IFileListEntry[] files)
        {
            file = files.FirstOrDefault();
        }

        const string DefaultStatus = "Drop certificate files here or click to select";
        const int MaxFileSize = 5 * 1024 * 1024; // 5MB

        private List<string> fileNames = new List<string>();
        string fileTextContents;

        string samlUpPartyCertificateFileStatus = DefaultStatus;
        private async Task OnSamlUpPartyCertificateFileSelectedAsync(IFileListEntry[] files)
        {
            editSamlUpPartyForm.ClearFieldError(nameof(editSamlUpPartyForm.Model.Keys));
            foreach (var file in files)
            {
                if (file.Size > MaxFileSize)
                {
                    samlUpPartyCertificateFileStatus = $"That's too big. Max size: {MaxFileSize} bytes.";
                }
                else
                {
                    samlUpPartyCertificateFileStatus = "Loading...";

                    using (var memoryStream = new MemoryStream())
                    {
                        await file.Data.CopyToAsync(memoryStream);

                        try
                        {
                            var certificate = new X509Certificate2(memoryStream.ToArray());
                            fileNames.Add(certificate.Thumbprint);
                            var jwk = await certificate.ToJsonWebKeyAsync();
                            //editSamlUpPartyForm.Model.Keys.Add(jwk);
                        }
                        catch (Exception ex)
                        {
                            editSamlUpPartyForm.SetFieldError(nameof(editSamlUpPartyForm.Model.Keys), ex.Message);
                        }
                    }

                    samlUpPartyCertificateFileStatus = DefaultStatus;
                }
            }
        }

        private async Task OnEditSamlUpPartyValidSubmitAsync(EditContext editContext)
        {
            try
            {
                if (createMode)
                {
                    await UpPartyService.UpdateSamlUpPartyAsync(editSamlUpPartyForm.Model.Map<SamlUpParty>());
                    await OnUpPartyFilterValidSubmitAsync(null);
                }
                else
                {
                    await UpPartyService.UpdateSamlUpPartyAsync(editSamlUpPartyForm.Model.Map<SamlUpParty>());
                }
                editSamlUpPartyModal.Hide();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    editSamlUpPartyForm.SetFieldError(nameof(editSamlUpPartyForm.Model.Name), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSamlUpPartyAsync(string name)
        {
            try
            {
                await UpPartyService.DeleteSamlUpPartyAsync(name);
                await OnUpPartyFilterValidSubmitAsync(null);
                editSamlUpPartyModal.Hide();
            }
            catch (AuthenticationException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                editSamlUpPartyForm.SetError(ex.Message);
            }
        }
    }
}
