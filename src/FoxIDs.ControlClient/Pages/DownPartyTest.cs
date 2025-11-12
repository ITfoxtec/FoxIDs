using FoxIDs.Client.Logic;
using Microsoft.AspNetCore.Components;
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
using System.Linq;
using Blazored.SessionStorage;

namespace FoxIDs.Client.Pages
{
    public partial class DownPartyTest
    {
        private string error;
        private DownPartyTestResultResponse response;
        private bool loggedOut;
        private ElementReference submitIdTokenButton;
        private ElementReference submitAccessTokenButton;
        private TestSessionData testSessionData;

        private const string SessionStorageKeyPrefix = "foxids.downpartytest.";
        private const int SessionStorageMaxItems = 10;

        [Inject]
        public IJSRuntime JSRuntime { get; set; }

        [Inject]
        public NavigationManager navigationManager { get; set; }

        [Inject]
        public TrackSelectedLogic TrackSelectedLogic { get; set; }

        [Inject]
        public HelpersNoAccessTokenService HelpersNoAccessTokenService { get; set; }

        [Inject]
        public ISessionStorageService SessionStorage { get; set; }

        [Parameter]
        public string TenantName { get; set; }

        protected override async Task OnInitializedAsync()
        {
            error = string.Empty;
            response = null;
            loggedOut = false;
            await base.OnInitializedAsync();

            await LoadTestSessionDataAsync();

            try
            {
                var responseQuery = GetResponseQuery(navigationManager.Uri);
                if (responseQuery?.Where(q => q.Key != "id")?.Count() > 0)
                {
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

                    await StoreTestSessionDataAsync(trackName, response);
                }
                else
                {
                    loggedOut = true;
                }
            }
            catch (ResponseErrorException rex)
            {
                error = rex.Message;
            }
            catch (FoxIDsApiException aex)
            {
                error = aex.Message;
            }

            await MaintainSessionStorageAsync();
        }

        private Dictionary<string, string> GetResponseQuery(string responseUrl)
        {
            var rUri = new Uri(responseUrl);
            if (rUri.Query.IsNullOrWhiteSpace() && rUri.Fragment.IsNullOrWhiteSpace())
            {
                return null;
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

        private async Task LoadTestSessionDataAsync()
        {
            var key = GetSessionStorageKey();
            if (key.IsNullOrEmpty())
            {
                return;
            }

            try
            {
                if (!await SessionStorage.ContainKeyAsync(key))
                {
                    return;
                }

                var data = await SessionStorage.GetItemAsync<TestSessionData>(key);
                if (data == null)
                {
                    await ClearTestSessionDataAsync();
                    return;
                }

                testSessionData = data;
                if (IsExpired(data))
                {
                    await ClearTestSessionDataAsync();
                }
            }
            catch (Exception)
            {
                testSessionData = null;
                await ClearTestSessionDataAsync();
            }
        }

        private async Task StoreTestSessionDataAsync(string trackName, DownPartyTestResultResponse result)
        {
            var key = GetSessionStorageKey();
            if (key.IsNullOrEmpty())
            {
                return;
            }

            if (result == null)
            {
                testSessionData = null;
                await ClearTestSessionDataAsync();
                return;
            }

            testSessionData = new TestSessionData(trackName, result.TestUrl, result.TestExpireAt, result.TestExpireInSeconds);
            try
            {
                await SessionStorage.SetItemAsync(key, testSessionData);
            }
            catch (Exception)
            {
                // sessionStorage is not available or serialization failed, clear and continue.
                testSessionData = null;
                await ClearTestSessionDataAsync();
            }
        }

        private async Task ClearTestSessionDataAsync(string key = null)
        {
            key ??= GetSessionStorageKey();
            if (key.IsNullOrEmpty())
            {
                return;
            }

            try
            {
                await SessionStorage.RemoveItemAsync(key);
            }
            catch (Exception)
            {
                // sessionStorage is not available, ignore.
            }
        }

        private async Task MaintainSessionStorageAsync()
        {
            try
            {
                var keys = await SessionStorage.KeysAsync();
                if (keys == null)
                {
                    return;
                }

                var activeKey = GetSessionStorageKey();

                var relevantKeys = keys
                    .Where(key => key != null && key != activeKey && key.StartsWith(SessionStorageKeyPrefix, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (relevantKeys.Count == 0)
                {
                    return;
                }

                var entries = new List<(string Key, TestSessionData Data)>(relevantKeys.Count);
                foreach (var key in relevantKeys)
                {
                    TestSessionData data = null;
                    try
                    {
                        data = await SessionStorage.GetItemAsync<TestSessionData>(key);
                    }
                    catch (Exception)
                    {
                        await ClearTestSessionDataAsync(key);
                        continue;
                    }

                    if (data == null || IsExpired(data))
                    {
                        await ClearTestSessionDataAsync(key);
                        continue;
                    }

                    entries.Add((key, data));
                }

                var overLimit = entries.Count - SessionStorageMaxItems;
                if (overLimit <= 0)
                {
                    return;
                }

                var itemsToRemove = entries
                    .OrderBy(entry => HasExpiration(entry.Data) ? entry.Data.TestExpireAt : long.MaxValue)
                    .Take(overLimit)
                    .ToList();

                foreach (var entry in itemsToRemove)
                {
                    await ClearTestSessionDataAsync(entry.Key);
                }
            }
            catch (Exception)
            {
                // sessionStorage is not available, ignore cleanup.
            }
        }

        private string GetSessionStorageKey()
        {
            var responseQuery = GetResponseQuery(navigationManager.Uri);
            var id = responseQuery?.Where(q => q.Key == "id")?.Select(q => q.Value).FirstOrDefault();

            if (id.IsNullOrWhiteSpace())
            {
                return null;
            }

            return string.Concat(SessionStorageKeyPrefix, id.ToLowerInvariant());
        }

        private static bool HasExpiration(TestSessionData data) => data.TestExpireInSeconds > 0;

        private static bool IsExpired(TestSessionData data) => HasExpiration(data) && DateTimeOffset.FromUnixTimeSeconds(data.TestExpireAt) <= DateTimeOffset.UtcNow;

        private string RetryTestUrl => testSessionData?.TestUrl;

        private bool CanRetryLogin => testSessionData != null && !RetryTestUrl.IsNullOrWhiteSpace() && (!HasExpiration(testSessionData) || !IsExpired(testSessionData));

        private bool TestSessionExpired => testSessionData != null && IsExpired(testSessionData);

        private string TestValidityText
        {
            get
            {
                if (testSessionData == null)
                {
                    return null;
                }

                if (!HasExpiration(testSessionData))
                {
                    return "The test application does not expire.";
                }

                var expireUtc = DateTimeOffset.FromUnixTimeSeconds(testSessionData.TestExpireAt);
                var expireLocal = expireUtc.ToLocalTime();
                var remaining = expireUtc - DateTimeOffset.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    return $"The test application expired at {expireLocal.LocalDateTime.ToShortTimeString()}.";
                }

                return $"The test application is valid until {expireLocal.LocalDateTime.ToShortTimeString()} ({FormatRemaining(remaining)} remaining).";
            }
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining.TotalHours >= 1)
            {
                var hours = (int)Math.Floor(remaining.TotalHours);
                return $"{hours} hour{(hours == 1 ? string.Empty : "s")}";
            }

            if (remaining.TotalMinutes >= 1)
            {
                var minutes = Math.Max(1, (int)Math.Ceiling(remaining.TotalMinutes));
                return $"{minutes} minute{(minutes == 1 ? string.Empty : "s")}";
            }

            var seconds = Math.Max(1, (int)Math.Ceiling(remaining.TotalSeconds));
            return $"{seconds} second{(seconds == 1 ? string.Empty : "s")}";
        }

        private sealed record TestSessionData(string TrackName, string TestUrl, long TestExpireAt, int TestExpireInSeconds);
    }
}
