﻿using FoxIDs.Client.Logic;
using Microsoft.AspNetCore.Components;
using System.Security;
using System;
using System.Threading.Tasks;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using ITfoxtec.Identity.Messages;
using FoxIDs.Client.Services;
using FoxIDs.Models.Api;
using FoxIDs.Infrastructure;
using Microsoft.JSInterop;

namespace FoxIDs.Client.Pages
{
    public partial class DownPartyTest
    {
        private string error;
        private DownPartyTestResultResponse response;
        private ElementReference submitIdTokenButton;
        private ElementReference submitAccessTokenButton;

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public NavigationManager navigationManager { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }

        [Inject]
        public HelpersNoAccessTokenService HelpersNoAccessTokenService { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            error = string.Empty;
            response = null;
            await base.OnInitializedAsync();

            try
            {
                var responseQuery = GetResponseQuery(navigationManager.Uri);
                var authenticationResponse = responseQuery.ToObject<AuthenticationResponse>();
                authenticationResponse.Validate();
                if (authenticationResponse.State.IsNullOrEmpty()) throw new ArgumentNullException(nameof(authenticationResponse.State), authenticationResponse.GetTypeName());

                var stateSplit = authenticationResponse.State.Split(Constants.Models.OidcDownPartyTest.StateSplitKey);
                if (stateSplit.Length != 3)
                {
                    throw new Exception("Invalid state format.");
                }
                var trackName = stateSplit[0];

                response = await HelpersNoAccessTokenService.DownPartyTestResultAsync(new DownPartyTestResultRequest
                {
                    State = authenticationResponse.State,
                    Code = authenticationResponse.Code,
                }, TenantName, trackName);
            }
            catch (ResponseErrorException rex)
            {
                error = rex.Message;
            }
            catch (FoxIDsApiException aex)
            {
                error = aex.Message;
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

        public async Task DecodeIdTokenAsync()
        {
            await JSRuntime.InvokeVoidAsync("triggerClick", submitIdTokenButton);
        }
        public async Task DecodeAccessTokenAsync()
        {
            await JSRuntime.InvokeVoidAsync("triggerClick", submitAccessTokenButton);
        }
    }
}
