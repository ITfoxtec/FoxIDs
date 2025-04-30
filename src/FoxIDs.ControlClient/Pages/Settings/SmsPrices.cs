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
using System.Net.Http;
using System.Threading.Tasks;
using Blazored.Toast.Services;
using FoxIDs.Client.Logic;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class SmsPrices
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string textsHref;
        private string plansHref;
        private string riskPasswordsHref;

        private PageEditForm<FilterSmsPriceViewModel> smsPriceFilterForm;
        private List<GeneralSmsPriceViewModel> smsPrices = new List<GeneralSmsPriceViewModel>();

        [Inject]
        public IToastService toastService { get; set; }

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public SmsPriceService SmsPriceService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        private bool IsMasterTenant => RouteBindingLogic.IsMasterTenant;

        protected override async Task OnInitializedAsync()
        {
            trackSettingsHref = $"{TenantName}/envsettings";
            mailSettingsHref = $"{TenantName}/mailsettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
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
            smsPriceFilterForm?.ClearError();
            try
            {
                SetGeneralSmsPrices(await SmsPriceService.GetSmsPricesAsync(null));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                smsPriceFilterForm.SetError(ex.Message);
            }
        }


        private async Task OnSmsPriceFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetGeneralSmsPrices(await SmsPriceService.GetSmsPricesAsync(smsPriceFilterForm.Model.FilterName));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    smsPriceFilterForm.SetFieldError(nameof(smsPriceFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private void SetGeneralSmsPrices(PaginationResponse<SmsPrice> dataSmsPrices)
        {
            smsPrices.Clear();
            foreach (var dp in dataSmsPrices.Data)
            {
                smsPrices.Add(new GeneralSmsPriceViewModel(dp));
            }
        }

        private void ShowCreateSmsPrice()
        {
            var smsPrice = new GeneralSmsPriceViewModel();
            smsPrice.CreateMode = true;
            smsPrice.Edit = true;
            smsPrices.Add(smsPrice);
        }

        private async Task ShowUpdateSmsPriceAsync(GeneralSmsPriceViewModel generalSmsPrice)
        {
            generalSmsPrice.CreateMode = false;
            generalSmsPrice.DeleteAcknowledge = false;
            generalSmsPrice.Error = null;
            generalSmsPrice.Edit = true;

            try
            {
                var smsPrice = await SmsPriceService.GetSmsPriceAsync(generalSmsPrice.Iso2);
                await generalSmsPrice.Form.InitAsync(smsPrice);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalSmsPrice.Error = ex.Message;
            }
        }

        private string SmsPriceDisplayName(GeneralSmsPriceViewModel generalSmsPrice)
        {
            return $"{generalSmsPrice.CountryName} ({generalSmsPrice.Iso2})";
        }

        private void SmsPriceCancel(GeneralSmsPriceViewModel smsPrice)
        {
            if (smsPrice.CreateMode)
            {
                smsPrices.Remove(smsPrice);
            }
            else
            {
                smsPrice.Edit = false;
            }
        }

        private async Task OnEditSmsPriceValidSubmitAsync(GeneralSmsPriceViewModel generalSmsPrice, EditContext editContext)
        {
            try
            {
                generalSmsPrice.Form.Model.Iso2 = generalSmsPrice.Form.Model.Iso2.Trim().ToUpper();
                if (generalSmsPrice.CreateMode)
                {
                    await SmsPriceService.CreateSmsPriceAsync(generalSmsPrice.Form.Model);
                    generalSmsPrice.Form.UpdateModel(generalSmsPrice.Form.Model);
                    generalSmsPrice.CreateMode = false;
                    toastService.ShowSuccess("SMS price created.");
                }
                else
                {
                    await SmsPriceService.UpdateSmsPriceAsync(generalSmsPrice.Form.Model);
                    generalSmsPrice.Form.UpdateModel(generalSmsPrice.Form.Model);
                    toastService.ShowSuccess("SMS price updated.");
                }

                generalSmsPrice.CountryName = generalSmsPrice.Form.Model.CountryName;
                generalSmsPrice.Iso2 = generalSmsPrice.Form.Model.Iso2;
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    generalSmsPrice.Form.SetFieldError(nameof(generalSmsPrice.Form.Model.Iso2), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteSmsPriceAsync(GeneralSmsPriceViewModel generalSmsPrice)
        {
            try
            {
                await SmsPriceService.DeleteSmsPriceAsync(generalSmsPrice.Iso2);
                smsPrices.Remove(generalSmsPrice);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                generalSmsPrice.Form.SetError(ex.Message);
            }
        }
    }
}
