using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace FoxIDs.Client.Services
{
    public class TrackService : BaseService
    {
        private const string apiUri = "api/{tenant}/master/!track";
        private const string listApiUri = "api/{tenant}/master/!tracks";

        private const string keyContainedApiUri = "api/{tenant}/{track}/!trackkeycontained";
        private const string keyContainedSwapApiUri = "api/{tenant}/{track}/!trackkeycontainedswap";
        private const string keyTypeApiUri = "api/{tenant}/{track}/!trackkeytype";

        private const string listMasterResourceCulturesApiUri = "api/{tenant}/master/!resourcecultures";
        private const string listMasterResourceNamesApiUri = "api/{tenant}/master/!resourcenames";

        private const string listTrackOnlyResourceNamesApiUri = "api/{tenant}/{track}/!trackonlyresourcenames";
        private const string trackOnlyResourceNameApiUri = "api/{tenant}/{track}/!trackonlyresourcename";
        private const string trackOnlyResourceApiUri = "api/{tenant}/{track}/!trackonlyresource";
        private const string resourceApiUri = "api/{tenant}/{track}/!trackresource";
        private const string resourceSettingApiUri = "api/{tenant}/{track}/!trackresourcesetting";

        private const string sendEmailApiUri = "api/{tenant}/{track}/!tracksendemail";
        private const string sendSmsApiUri = "api/{tenant}/{track}/!tracksendsms";
        private const string claimMappingApiUri = "api/{tenant}/{track}/!trackclaimmapping";
        private const string logUsageApiUri = "api/{tenant}/{track}/!tracklogusage";
        private const string logAuditApiUri = "api/{tenant}/{track}/!tracklogaudit";
        private const string logApiUri = "api/{tenant}/{track}/!tracklog";
        private const string logSettingApiUri = "api/{tenant}/{track}/!tracklogsetting";
        private const string logStreamsSettingsApiUri = "api/{tenant}/{track}/!tracklogstreamssettings";

        public TrackService(IHttpClientFactory httpClientFactory, RouteBindingLogic routeBindingLogic, TrackSelectedLogic trackSelectedLogic) : base(httpClientFactory, routeBindingLogic, trackSelectedLogic)
        { }

        public async Task<PaginationResponse<Track>> GetTracksAsync(string filterName, string paginationToken = null) => await GetListAsync<Track>(listApiUri, filterName, paginationToken: paginationToken);

        public async Task<Track> GetTrackAsync(string name) => await GetAsync<Track>(apiUri, name);
        public async Task<Track> CreateTrackAsync(Track track) => await PostResponseAsync<Track, Track>(apiUri, track);
        public async Task<Track> UpdateTrackAsync(Track track) => await PutResponseAsync<Track, Track>(apiUri, track);
        public async Task DeleteTrackAsync(string name) => await DeleteAsync(apiUri, name);

        public async Task<TrackKeyItemsContained> GetTrackKeyContainedAsync() => await GetAsync<TrackKeyItemsContained>(keyContainedApiUri);
        public async Task<TrackKeyItemsContained> UpdateTrackKeyContainedAsync(TrackKeyItemContainedRequest trackKeyRequest) => await PutResponseAsync<TrackKeyItemContainedRequest, TrackKeyItemsContained>(keyContainedApiUri, trackKeyRequest);
        public async Task DeleteTrackKeyContainedAsync() => await DeleteAsync(keyContainedApiUri);

        public async Task SwapTrackKeyContainedAsync(TrackKeyItemContainedSwap trackKeySwap) => await PostAsync(keyContainedSwapApiUri, trackKeySwap);

        public async Task<TrackKey> GetTrackKeyTypeAsync() => await GetAsync<TrackKey>(keyTypeApiUri);
        public async Task UpdateTrackKeyTypeAsync(TrackKey trackKeyRequest) => await PutAsync(keyTypeApiUri, trackKeyRequest);

        public async Task<PaginationResponse<ResourceCulture>> GetMasterResourceCulturesAsync(string paginationToken = null) => await GetListAsync<ResourceCulture>(listMasterResourceCulturesApiUri, paginationToken: paginationToken);
        public async Task<PaginationResponse<ResourceName>> GetMasterResourceNamesAsync(string filterName, string paginationToken = null) => await GetListAsync<ResourceName>(listMasterResourceNamesApiUri, filterName, paginationToken: paginationToken);
        
        public async Task<PaginationResponse<ResourceName>> GetTrackOnlyResourceNamesAsync(string filterName, string paginationToken = null) => await GetListAsync<ResourceName>(listTrackOnlyResourceNamesApiUri, filterName, paginationToken: paginationToken);
        public async Task<ResourceName> UpdateTrackOnlyResourceNameAsync(TrackResourceName trackResourceName) => await PutResponseAsync<TrackResourceName, ResourceName>(trackOnlyResourceNameApiUri, trackResourceName);
        public async Task DeleteTrackOnlyResourceNameAsync(string name) => await DeleteAsync(trackOnlyResourceNameApiUri, name);

        public async Task<ResourceItem> GetTrackOnlyResourceAsync(int resourceId) => await GetAsync<ResourceItem>(trackOnlyResourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId));
        public async Task<ResourceItem> UpdateTrackOnlyResourceAsync(TrackResourceItem trackResourceItem) => await PutResponseAsync<ResourceItem, ResourceItem>(trackOnlyResourceApiUri, trackResourceItem);
        public async Task DeleteTrackOnlyResourceAsync(int resourceId) => await DeleteAsync(trackOnlyResourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId));

        public async Task<ResourceItem> GetTrackResourceAsync(int resourceId) => await GetAsync<ResourceItem>(resourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId));
        public async Task<ResourceItem> UpdateTrackResourceAsync(TrackResourceItem trackResourceItem) => await PutResponseAsync<ResourceItem, ResourceItem>(resourceApiUri, trackResourceItem);
        public async Task DeleteTrackResourceAsync(int resourceId) => await DeleteAsync(resourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId));

        public async Task<ResourceSettings> GetTrackResourceSettingAsync() => await GetAsync<ResourceSettings>(resourceSettingApiUri);
        public async Task SaveTrackResourceSettingAsync(ResourceSettings resourceSettings) => await PostAsync(resourceSettingApiUri, resourceSettings);

        public async Task<SendEmail> GetTrackSendEmailAsync() => await GetAsync<SendEmail>(sendEmailApiUri);
        public async Task UpdateTrackSendEmailAsync(SendEmail sendEmail) => await PutAsync(sendEmailApiUri, sendEmail);
        public async Task DeleteTrackSendEmailAsync() => await DeleteAsync(sendEmailApiUri);

        public async Task<SendSms> GetTrackSendSmsAsync() => await GetAsync<SendSms>(sendSmsApiUri);
        public async Task UpdateTrackSendSmsAsync(SendSms sendSms) => await PutAsync(sendSmsApiUri, sendSms);
        public async Task DeleteTrackSendSmsAsync() => await DeleteAsync(sendSmsApiUri);

        public async Task<List<ClaimMap>> GetTrackClaimMappingAsync() => await GetAsync<List<ClaimMap>>(claimMappingApiUri);
        public async Task SaveTrackClaimMappingAsync(List<ClaimMap> claimMappings) => await PostAsync(claimMappingApiUri, claimMappings);

        public async Task<UsageLogResponse> GetTrackUsageLogAsync(UsageLogRequest usageLogRequest) => await GetAsync<UsageLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest);

        public async Task<LogResponse> GetTrackAuditLogAsync(AuditLogRequest auditLogRequest) => await GetAsync<AuditLogRequest, LogResponse>(logAuditApiUri, auditLogRequest);

        public async Task<LogResponse> GetLogAsync(LogRequest logRequest) => await GetAsync<LogRequest, LogResponse>(logApiUri, logRequest);

        public async Task<LogSettings> GetTrackLogSettingAsync() => await GetAsync<LogSettings>(logSettingApiUri);
        public async Task SaveTrackLogSettingAsync(LogSettings logSettings) => await PostAsync(logSettingApiUri, logSettings);

        public async Task<LogStreams> GetTrackLogStreamSettingsAsync() => await GetAsync<LogStreams>(logStreamsSettingsApiUri);
        public async Task SaveTrackLogStreamSettingsAsync(LogStreams logStreams) => await PostAsync(logStreamsSettingsApiUri, logStreams);
    }
}
