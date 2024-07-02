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
using System;
using System.Linq;

namespace FoxIDs.Client.Shared.Components
{
    public partial class SelectUpParty<TModel> where TModel : class, IAllowUpPartyNames, new()
    {
        private PageEditForm<FilterUpPartyViewModel> upPartyNamesFilterForm;
        private List<UpParty> upParties;
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

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadDefaultUpPartyFilter();
        }

        public void Init()
        {
            upPartyNamesFilterForm.Init();
        }

        private async Task LoadDefaultUpPartyFilter()
        {
            await UpPartyNamesFilterAsync(null);
        }

        private async Task OnUpPartyNamesFilterValidSubmitAsync(EditContext editContext)
        {
            try
            {
                await UpPartyNamesFilterAsync(upPartyNamesFilterForm.Model.FilterName);
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

        private async Task UpPartyNamesFilterAsync(string filterName)
        {
            try
            {
                var ups = await UpPartyService.FilterUpPartyAsync(filterName);
                if (upParties?.Count() > 0)
                {
                    foreach(var up in ups)
                    {
                        if(!upParties.Where(u => u.Name == up.Name).Any())
                        {
                            upParties.Add(up);
                        }
                    }
                }
                else
                {
                    upParties = ups?.ToList();
                }
                upPartyFilters = ups.Where(f => !EditDownPartyForm.Model.AllowUpPartyNames.Where(a => a.Equals(f.Name)).Any());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
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

        private string UpPartyInfoText(string upPartyName)
        {
            var upParty = upParties.Where(f => f.Name.Equals(upPartyName)).FirstOrDefault();
            if (upParty == null)
            {
                return upPartyName;
            }
            else
            {
                return UpPartyInfoText(upParty);
            }
        }

        private string UpPartyInfoText(UpParty upParty)
        {
            if (upParty.Type == PartyTypes.Login)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (Login)";
            }
            else if (upParty.Type == PartyTypes.OAuth2)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (OAuth 2.0)";
            }
            else if (upParty.Type == PartyTypes.Oidc)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (OpenID Connect)";
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (SAML 2.0)";
            }
            else if (upParty.Type == PartyTypes.TrackLink)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (Environment Link)";
            }
            else if (upParty.Type == PartyTypes.ExternalLogin)
            {
                return $"{upParty.DisplayName ?? upParty.Name} (External Login)";
            }
            throw new NotSupportedException();
        }
    }
}
