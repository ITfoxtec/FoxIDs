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
    public partial class SelectUpParties<TModel> where TModel : class, IAllowUpPartyNames, new()
    {
        private Modal upPartyFilterModal;        
        private PageEditForm<FilterUpPartyViewModel> upPartyNamesFilterForm;
        private List<UpParty> upParties;
        private List<UpPartyFilterViewModel> upPartyFilters;

        [Inject]
        public OpenidConnectPkce OpenidConnectPkce { get; set; }

        [Inject]
        public UpPartyService UpPartyService { get; set; }

        [Parameter]
        public PageEditForm<TModel> EditDownPartyForm { get; set; }

        [Parameter]
        public EventCallback<(IAllowUpPartyNames, List<UpPartyLink>)> OnUpdateUpParties { get; set; }

        [Parameter]
        public EventCallback<(IAllowUpPartyNames, UpPartyLink)> OnRemoveUpParty { get; set; }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await UpPartyNamesFilterAsync(null);
        }

        public void Init()
        {
            upPartyNamesFilterForm.Init();
        }

        private async Task LoadDefaultUpPartyFilter()
        {
            await UpPartyNamesFilterAsync(null);
            upPartyFilterModal.Show();
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
                var ups = (await UpPartyService.GetUpPartiesAsync(filterName)).Data;
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

                var tempUpPartyFilters = upPartyFilters;
                upPartyFilters = new List<UpPartyFilterViewModel>();
                foreach (var up in ups)
                {
                    var typeText = GetTypeText(up);

                    var item = tempUpPartyFilters?.Where(u => u.Name == up.Name && u.ProfileName.IsNullOrWhiteSpace()).FirstOrDefault();
                    upPartyFilters.Add(new UpPartyFilterViewModel
                    {
                        Name = up.Name,
                        DisplayName = up.DisplayName ?? up.Name,
                        Type = up.Type,
                        TypeText = typeText,
                        Selected = item != null ? item?.Selected == true : EditDownPartyForm.Model.AllowUpParties.Where(a => a.Name == up.Name && a.ProfileName.IsNullOrWhiteSpace()).Any()
                    });
                    if(tempUpPartyFilters != null && item != null)
                    {
                        tempUpPartyFilters.Remove(item);
                    }

                    if (up.Profiles != null)
                    {
                        foreach(var profile in up.Profiles) 
                        {
                            var itemProfile = tempUpPartyFilters?.Where(u => u.Name == up.Name && u.ProfileName == profile.Name).FirstOrDefault();
                            upPartyFilters.Add(new UpPartyFilterViewModel
                            {
                                Name = up.Name,
                                DisplayName = up.DisplayName ?? up.Name,
                                ProfileName = profile.Name,
                                ProfileDisplayName = profile.DisplayName,
                                Type = up.Type,
                                TypeText = typeText,
                                Selected = itemProfile != null ? itemProfile?.Selected == true : EditDownPartyForm.Model.AllowUpParties.Where(a => a.Name == up.Name && a.ProfileName == profile.Name).Any()
                            });
                            if (tempUpPartyFilters != null && itemProfile != null)
                            {
                                tempUpPartyFilters.Remove(itemProfile);
                            }
                        }
                    }
                }

                if(tempUpPartyFilters?.Count() > 0)
                {
                    tempUpPartyFilters.ForEach(u => u.Hide = true);
                    upPartyFilters.AddRange(tempUpPartyFilters);
                }
            }
            catch (TokenUnavailableException)
            {
                await (OpenidConnectPkce as TenantOpenidConnectPkce).TenantLoginAsync();
            }
        }

        private void OnAddUpParty(UpPartyFilterViewModel upPartyFilter)
        {
            upPartyFilter.Selected = !upPartyFilter.Selected;
        }

        private async Task OnRemoveUpPartyAsync(UpPartyLink upPartyLink)
        {
            await OnRemoveUpParty.InvokeAsync((EditDownPartyForm.Model, upPartyLink));
        }

        private async Task OnUpPartyFilterSelectAsync()
        {
            await OnUpdateUpParties.InvokeAsync((EditDownPartyForm.Model, upPartyFilters.Where(u => u.Selected).Select(u => new UpPartyLink { Name = u.Name, ProfileName = u.ProfileName } ).ToList()));
            upPartyFilterModal.Hide();
        }

        private (string displayName, string profileDisplayName, string type) UpPartyInfoText(UpPartyLink upPartyLink)
        {
            var upParty = upParties.Where(f => f.Name == upPartyLink.Name).FirstOrDefault();
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
                var profileDisplayName = upParty.Profiles.Where(p => p.Name == profileName).Select(p => p.DisplayName).FirstOrDefault();
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
