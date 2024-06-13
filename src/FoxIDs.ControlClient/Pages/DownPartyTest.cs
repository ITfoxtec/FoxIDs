using FoxIDs.Client.Logic;
using FoxIDs.Client.Models.ViewModels;
using Microsoft.AspNetCore.Components;
using System.Security;
using System;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using ITfoxtec.Identity.Messages;

namespace FoxIDs.Client.Pages
{
    public partial class DownPartyTest
    {
        private string error;

        [Inject]
        protected NavigationManager navigationManager { get; set; }


        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            try
            {
                var responseQuery = GetResponseQuery(navigationManager.Uri);
                var authenticationResponse = responseQuery.ToObject<AuthenticationResponse>();
                authenticationResponse.Validate();
                if (authenticationResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(authenticationResponse.State), authenticationResponse.GetTypeName());




            }
            catch (ResponseErrorException rex)
            {
                error = rex.Message;
            }
        }

        private Dictionary<string, string> GetResponseQuery(string responseUrl)
        {
            var rUri = new Uri(responseUrl);
            if (rUri.Query.IsNullOrWhiteSpace() && rUri.Fragment.IsNullOrWhiteSpace())
            {
                throw new SecurityException("Invalid response URL.");
            }
            return QueryHelpers.ParseQuery(!rUri.Query.IsNullOrWhiteSpace() ? rUri.Query.TrimStart('?') : rUri.Fragment.TrimStart('#')).ToDictionary();
        }

    }
}
