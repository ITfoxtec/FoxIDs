using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Net.Http;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;
using Blazored.Toast.Services;
using FoxIDs.Client.Models.Config;
using System.Linq;

namespace FoxIDs.Client.Pages.Settings
{
    public partial class Text
    {
        private string trackSettingsHref;
        private string mailSettingsHref;
        private string claimMappingsHref;
        private string plansHref;
        private string smsPricesHref;
        private string riskPasswordsHref;
        private string smsSettingsHref;

        private List<string> supportedCultures;
        private PageEditForm<FilterResourceViewModel> resourceFilterForm;
        private List<GeneralResourceViewModel> trackOnlyResources = new List<GeneralResourceViewModel>();
        private List<GeneralResourceViewModel> resources = new List<GeneralResourceViewModel>();

        private GeneralResourceSettingsViewModel generalTextSettings = new GeneralResourceSettingsViewModel();
        private Modal textSettingsModal;

        [Inject]
        public ClientSettings ClientSettings { get; set; }

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
            smsSettingsHref = $"{TenantName}/smssettings";
            claimMappingsHref = $"{TenantName}/claimmappings";
            plansHref = $"{TenantName}/plans";
            smsPricesHref = $"{TenantName}/smsprices";
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
            resourceFilterForm?.ClearError();
            try
            {
                var culturesResponse = await TrackService.GetMasterResourceCulturesAsync(cancellationToken: PageCancellationToken);
                supportedCultures = culturesResponse?.Data?.Select(c => c.Culture)?.ToList();

                SetTrackOnlyResources(await TrackService.GetTrackOnlyResourceNamesAsync(null, cancellationToken: PageCancellationToken));
                SetGeneralResources(await TrackService.GetMasterResourceNamesAsync(null, cancellationToken: PageCancellationToken));
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                resourceFilterForm.SetError(ex.Message);
            }
        }

        private async Task OnResourceFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                SetTrackOnlyResources(await TrackService.GetTrackOnlyResourceNamesAsync(resourceFilterForm.Model.FilterName, cancellationToken: PageCancellationToken));
                SetGeneralResources(await TrackService.GetMasterResourceNamesAsync(resourceFilterForm.Model.FilterName, cancellationToken: PageCancellationToken));
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    resourceFilterForm.SetFieldError(nameof(resourceFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        #region TrackOnlyResources
        private void SetTrackOnlyResources(PaginationResponse<ResourceName> trackResourceNames)
        {
            trackOnlyResources.Clear();
            if (trackResourceNames.Data?.Count() > 0)
            {
                foreach (var resourceName in trackResourceNames.Data)
                {
                    trackOnlyResources.Add(new GeneralResourceViewModel(resourceName));
                }
            }
        }

        private void ShowCreateTrackResource()
        {
            if (!string.IsNullOrWhiteSpace(resourceFilterForm?.Model?.FilterName))
            {
                resourceFilterForm.Model.FilterName = null;
            }            

            var trackOnlyResource = new GeneralResourceViewModel();
            trackOnlyResource.CreateMode = true;
            trackOnlyResource.Edit = true;
            trackOnlyResources.Add(trackOnlyResource);
        }

        private async Task ShowUpdateTrackOnlyResourceAsync(GeneralResourceViewModel trackOnlyResource)
        {
            trackOnlyResource.DeleteAcknowledge = false;
            trackOnlyResource.ShowAdvanced = false;
            trackOnlyResource.Error = null;
            trackOnlyResource.Edit = true;

            try
            {
                var resourceItem = await TrackService.GetTrackOnlyResourceAsync(trackOnlyResource.Id, cancellationToken: PageCancellationToken);
                if (resourceItem == null)
                {
                    trackOnlyResource.CreateMode = true;
                    await trackOnlyResource.Form.InitAsync(new ResourceItemViewModel { Name = trackOnlyResource.Name, Id = trackOnlyResource.Id });
                }
                else
                {
                    trackOnlyResource.CreateMode = false;
                    await trackOnlyResource.Form.InitAsync(resourceItem.Map<ResourceItemViewModel>(), afterInit: afterInit =>
                    {
                        afterInit.Name = trackOnlyResource.Name;
                    });
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                trackOnlyResource.Error = ex.Message;
            }
        }

        private void TrackOnlyResourceCancel(GeneralResourceViewModel trackOnlyResource)
        {
            trackOnlyResource.Edit = false;
            if (trackOnlyResource.CreateMode)
            {
                trackOnlyResources.Remove(trackOnlyResource);
            }
        }

        private void TrackOnlyResourceAfterInit(GeneralResourceViewModel trackOnlyResource, ResourceItemViewModel resourceItem)
        {
            if (trackOnlyResource.CreateMode)
            {
                resourceItem.Items = supportedCultures?.Select(c => new ResourceCultureItem { Culture = c })?.ToList();
            }
        }

        private async Task OnEditTrackOnlyResourceValidSubmitAsync(GeneralResourceViewModel trackOnlyResource, EditContext editContext)
        {
            try
            {
                var resourceName = await TrackService.UpdateTrackOnlyResourceNameAsync(new TrackResourceName { Id = trackOnlyResource.CreateMode ? 0 : trackOnlyResource.Id, Name = trackOnlyResource.Form.Model.Name }, cancellationToken: PageCancellationToken);
                if (trackOnlyResource.CreateMode)
                {
                    trackOnlyResource.Id = resourceName.Id;
                    trackOnlyResource.Form.Model.Id = resourceName.Id;
                    var enText = trackOnlyResource.Form.Model.Items.Where(r => r.Culture == "en").FirstOrDefault();
                    if (string.IsNullOrWhiteSpace(enText?.Value))
                    {
                        enText.Value = resourceName.Name;
                    }
                }
                trackOnlyResource.Name = resourceName.Name;
                trackOnlyResource.Form.Model.Name = resourceName.Name;

                var resourceItem = await TrackService.UpdateTrackOnlyResourceAsync(trackOnlyResource.Form.Model.Map<TrackResourceItem>(), cancellationToken: PageCancellationToken);
                var resourceItemViewModel = resourceItem.Map<ResourceItemViewModel>();
                resourceItemViewModel.Name = trackOnlyResource.Name;
                trackOnlyResource.Form.UpdateModel(resourceItemViewModel);
                if (trackOnlyResource.CreateMode)
                {
                    trackOnlyResource.CreateMode = false;
                    toastService.ShowSuccess("New text created.");
                }
                else
                {
                    toastService.ShowSuccess("Text updated.");
                }
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    trackOnlyResource.Form.SetFieldError(nameof(trackOnlyResource.Form.Model.Items), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task DeleteTrackOnlyResourceAsync(GeneralResourceViewModel trackOnlyResource)
        {
            try
            {
                await TrackService.DeleteTrackOnlyResourceNameAsync(trackOnlyResource.Name, cancellationToken: PageCancellationToken);
                trackOnlyResources.Remove(trackOnlyResource);
                toastService.ShowSuccess("Text deleted.");
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                trackOnlyResource.Form.SetError(ex.Message);
            }
        }
        #endregion

        #region Resources
        private void SetGeneralResources(PaginationResponse<ResourceName> resourceNames)
        {
            resources.Clear();
            foreach (var resourceName in resourceNames.Data)
            {
                resources.Add(new GeneralResourceViewModel(resourceName));
            }
        }

        private async Task ShowUpdateResourceAsync(GeneralResourceViewModel resource)
        {           
            resource.DeleteAcknowledge = false;
            resource.ShowAdvanced = false;
            resource.Error = null;
            resource.Edit = true;

            try
            {
                var resourceItem = await TrackService.GetTrackResourceAsync(resource.Id, cancellationToken: PageCancellationToken);
                await resource.Form.InitAsync(resourceItem.Map<ResourceItemViewModel>(), afterInit: afterInit =>
                {
                    afterInit.Name = resource.Name;
                });
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                resource.Error = ex.Message;
            }
        }

        private void ResourceCancel(GeneralResourceViewModel resource)
        {
            resource.Edit = false;
        }

        private async Task OnEditResourceValidSubmitAsync(GeneralResourceViewModel resource, EditContext editContext)
        {
            try
            {
                var resourceItem = await TrackService.UpdateTrackResourceAsync(resource.Form.Model.Map<TrackResourceItem>(), cancellationToken: PageCancellationToken);
                var resourceItemViewModel = resourceItem.Map<ResourceItemViewModel>();
                resourceItemViewModel.Name = resource.Name;
                resource.Form.UpdateModel(resourceItemViewModel);
                toastService.ShowSuccess("Text updated.");
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    resource.Form.SetFieldError(nameof(resource.Form.Model.Items), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion

        private async Task ShowUpdateTextSettingsModalAsync()
        {
            generalTextSettings.Error = null;
            generalTextSettings.Edit = true;

            try
            {
                var textSettings = await TrackService.GetTrackResourceSettingAsync(cancellationToken: PageCancellationToken);
                await generalTextSettings.Form.InitAsync(textSettings);
                textSettingsModal.Show();
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (HttpRequestException ex)
            {
                generalTextSettings.Error = ex.Message;
            }
        }

        private async Task OnUpdateTextSettingsValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await TrackService.SaveTrackResourceSettingAsync(generalTextSettings.Form.Model, cancellationToken: PageCancellationToken);
                generalTextSettings.Edit = false;
                textSettingsModal.Hide();
                toastService.ShowSuccess("Text settings updated.");
            }
            catch (Exception ex)
            {
                generalTextSettings.Form.SetError(ex.Message);
            }
        }
    }
}
