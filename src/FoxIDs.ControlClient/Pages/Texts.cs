using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using System.Threading.Tasks;
using FoxIDs.Client.Logic;

namespace FoxIDs.Client.Pages
{
    public partial class Texts
    {
        private PageEditForm<FilterResourceViewModel> resourceFilterForm;
        private List<GeneralResourceViewModel> resources = new List<GeneralResourceViewModel>();
        private string textSettingsHref;

        [Inject]
        public RouteBindingLogic RouteBindingLogic { get; set; }

        [Inject]
        public TrackService TrackService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            textSettingsHref = $"{await RouteBindingLogic.GetTenantNameAsync()}/textsettings";
            await base.OnInitializedAsync();
            TrackSelectedLogic.OnTrackSelectedAsync += OnTrackSelectedAsync;
            if (TrackSelectedLogic.IsTrackSelected)
            {
                await DefaultLoadAsync();
            }
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
                SetGeneralResources(await TrackService.FilterResourceNameAsync(null));
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
                SetGeneralResources(await TrackService.FilterResourceNameAsync(resourceFilterForm.Model.FilterName));
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

        private void SetGeneralResources(IEnumerable<ResourceName> resourceNames)
        {
            resources.Clear();
            foreach(var resourceName in resourceNames)
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
                var resourceItem = await TrackService.GetTrackResourceAsync(resource.Id);
                if (resourceItem == null)
                {
                    resource.CreateMode = true;
                    await resource.Form.InitAsync(new ResourceItemViewModel { Name = resource.Name, Id = resource.Id });
                }
                else
                {
                    resource.CreateMode = false;
                    await resource.Form.InitAsync(resourceItem.Map<ResourceItemViewModel>(), afterInit: afterInit =>
                    {
                        afterInit.Name = resource.Name;
                    });
                }
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

        private void AddResourceItem(MouseEventArgs e, List<ResourceCultureItem> items)
        {
            items.Add(new ResourceCultureItem());
        }

        private void RemoveResourceItem(MouseEventArgs e, List<ResourceCultureItem> items, ResourceCultureItem removeItem)
        {
            if(items.Count > 1)
            {
                items.Remove(removeItem);
            }
            else
            {
                removeItem.Culture = null;
                removeItem.Value = null;
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
                await TrackService.UpdateTrackResourceAsync(resource.Form.Model.Map<TrackResourceItem>());
                resource.CreateMode = false;
                resource.Edit = false;
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

        private async Task DeleteResourceAsync(GeneralResourceViewModel resource)
        {
            try
            {
                await TrackService.DeleteTrackResourceAsync(resource.Id);
                resource.CreateMode = true;
                resource.Edit = false;
                resource.Form.Model.Name = null;
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (Exception ex)
            {
                resource.Form.SetError(ex.Message);
            }
        }
    }
}
