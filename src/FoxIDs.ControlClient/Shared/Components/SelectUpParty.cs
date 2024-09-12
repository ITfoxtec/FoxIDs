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
using ITfoxtec.Identity;

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
        public EventCallback<(IAllowUpPartyNames, UpPartyLink)> OnAddUpPartyName { get; set; }

        [Parameter]
        public EventCallback<(IAllowUpPartyNames, UpPartyLink)> OnRemoveUpPartyName { get; set; }

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
                upPartyFilters = ups.Where(f => !EditDownPartyForm.Model.AllowUpParties.Where(a => a.Equals(f.Name)).Any());
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private async Task OnAddUpPartyNameAsync(UpParty upParty, UpPartyProfile profile = null)
        {
            await OnAddUpPartyName.InvokeAsync((EditDownPartyForm.Model, new UpPartyLink { Name = upParty.Name, ProfileName = profile?.Name } ));
        }

        private async Task OnRemoveUpPartyNameAsync(UpPartyLink upPartyLink)
        {
            await OnRemoveUpPartyName.InvokeAsync((EditDownPartyForm.Model, upPartyLink));
        }

        private (string displayName, string profileDisplayName, string type) UpPartyInfoText(UpParty upParty, string profileName = null) => UpPartyInfoText(new UpPartyLink { Name = upParty.Name, ProfileName = profileName });

        private (string displayName, string profileDisplayName, string type) UpPartyInfoText(UpPartyLink upPartyLink)
        {
            var upParty = upParties.Where(f => f.Name.Equals(upPartyLink.Name)).FirstOrDefault();
            if (upParty == null)
            {
                return (upPartyLink.Name, upPartyLink.ProfileName, string.Empty);
            }
            else
            {
                return (upParty.DisplayName ?? upParty.Name, GetProfileDisplayName(upParty, upPartyLink.ProfileName), GetTypeText(upParty));
            }
        }

        private string GetProfileDisplayName(UpParty upParty, string profileName)
        {
            if(!profileName.IsNullOrEmpty() && upParty.Profiles != null)
            {
                var profileDisplayName = upParty.Profiles.Where(p => p.Equals(profileName)).Select(p => p.DisplayName).FirstOrDefault();
                return profileDisplayName ?? profileName;
            }

            return string.Empty;
        }

        private string GetTypeText(UpParty upParty)
        {
            if (upParty.Type == PartyTypes.Login)
            {
                return "Login";
            }
            else if (upParty.Type == PartyTypes.OAuth2)
            {
                return "OAuth 2.0";
            }
            else if (upParty.Type == PartyTypes.Oidc)
            {
                return "OpenID Connect";
            }
            else if (upParty.Type == PartyTypes.Saml2)
            {
                return "SAML 2.0";
            }
            else if (upParty.Type == PartyTypes.TrackLink)
            {
                return "Environment Link";
            }
            else if (upParty.Type == PartyTypes.ExternalLogin)
            {
                return "External API Login";
            }

            throw new NotSupportedException();
        }
    }
}
