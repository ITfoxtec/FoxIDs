using FoxIDs.Infrastructure;
using FoxIDs.Client.Infrastructure.Security;
using FoxIDs.Client.Models.ViewModels;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using ITfoxtec.Identity.BlazorWebAssembly.OpenidConnect;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxIDs.Client.Shared.Components
{
    public partial class SelectUpParty<TModel> where TModel : class, IAllowUpPartyNames, new()
    {
        private PageEditForm<FilterUpPartyViewModel> upPartyNamesFilterForm;
        private IEnumerable<UpParty> upPartyFilters;

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public PageEditForm<TModel> EditDownPartyForm { get; set; }

        [Parameter]
        public EventCallback<(IAllowUpPartyNames, string)> OnAddUpPartyName { get; set; }

        [Parameter]
        public EventCallback<(IAllowUpPartyNames, string)> OnRemoveUpPartyName { get; set; }

        public void Init()
        {
            upPartyNamesFilterForm.Init();
        }

        private async Task LoadDefaultUpPartyFilter()
        {
            await OnUpPartyNamesFilterValidSubmitAsync(null);
        }

        private async Task OnUpPartyNamesFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                upPartyFilters = await UpPartyService.FilterUpPartyAsync(upPartyNamesFilterForm.Model.FilterName);
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
            catch (FoxIDsApiException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    upPartyNamesFilterForm.SetFieldError(nameof(upPartyNamesFilterForm.Model.FilterName), ex.Message);
                }
                else
                {
                    throw;
                }
            }
        }

        private async Task OnAddUpPartyNameAsync(string name)
        {
            await OnAddUpPartyName.InvokeAsync((EditDownPartyForm.Model, name));
        }

        private async Task OnRemoveUpPartyNameAsync(string name)
        {
            await OnRemoveUpPartyName.InvokeAsync((EditDownPartyForm.Model, name));
        }
    }
}
