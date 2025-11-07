using FoxIDs.Client.Logic;
using FoxIDs.Models.Api;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
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

        public async Task<PaginationResponse<Track>> GetTracksAsync(string filterName, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<Track>(listApiUri, filterName, paginationToken: paginationToken, cancellationToken: cancellationToken);

        public async Task<Track> GetTrackAsync(string name, CancellationToken cancellationToken = default) => await GetAsync<Track>(apiUri, name, cancellationToken: cancellationToken);
        public async Task<Track> CreateTrackAsync(Track track, CancellationToken cancellationToken = default) => await PostResponseAsync<Track, Track>(apiUri, track, cancellationToken);
        public async Task<Track> UpdateTrackAsync(Track track, CancellationToken cancellationToken = default) => await PutResponseAsync<Track, Track>(apiUri, track, cancellationToken);
        public async Task DeleteTrackAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(apiUri, name, cancellationToken: cancellationToken);

        public async Task<TrackKeyItemsContained> GetTrackKeyContainedAsync(CancellationToken cancellationToken = default) => await GetAsync<TrackKeyItemsContained>(keyContainedApiUri, cancellationToken);
        public async Task<TrackKeyItemsContained> UpdateTrackKeyContainedAsync(TrackKeyItemContainedRequest trackKeyRequest, CancellationToken cancellationToken = default) => await PutResponseAsync<TrackKeyItemContainedRequest, TrackKeyItemsContained>(keyContainedApiUri, trackKeyRequest, cancellationToken);
        public async Task DeleteTrackKeyContainedAsync(CancellationToken cancellationToken = default) => await DeleteAsync(keyContainedApiUri, cancellationToken);

        public async Task SwapTrackKeyContainedAsync(TrackKeyItemContainedSwap trackKeySwap, CancellationToken cancellationToken = default) => await PostAsync(keyContainedSwapApiUri, trackKeySwap, cancellationToken);

        public async Task<TrackKey> GetTrackKeyTypeAsync(CancellationToken cancellationToken = default) => await GetAsync<TrackKey>(keyTypeApiUri, cancellationToken);
        public async Task UpdateTrackKeyTypeAsync(TrackKey trackKeyRequest, CancellationToken cancellationToken = default) => await PutAsync(keyTypeApiUri, trackKeyRequest, cancellationToken);

        public async Task<PaginationResponse<ResourceCulture>> GetMasterResourceCulturesAsync(string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<ResourceCulture>(listMasterResourceCulturesApiUri, paginationToken: paginationToken, cancellationToken: cancellationToken);
        public async Task<PaginationResponse<ResourceName>> GetMasterResourceNamesAsync(string filterName, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<ResourceName>(listMasterResourceNamesApiUri, filterName, paginationToken: paginationToken, cancellationToken: cancellationToken);
        
        public async Task<PaginationResponse<ResourceName>> GetTrackOnlyResourceNamesAsync(string filterName, string paginationToken = null, CancellationToken cancellationToken = default) => await GetListAsync<ResourceName>(listTrackOnlyResourceNamesApiUri, filterName, paginationToken: paginationToken, cancellationToken: cancellationToken);
        public async Task<ResourceName> UpdateTrackOnlyResourceNameAsync(TrackResourceName trackResourceName, CancellationToken cancellationToken = default) => await PutResponseAsync<TrackResourceName, ResourceName>(trackOnlyResourceNameApiUri, trackResourceName, cancellationToken);
        public async Task DeleteTrackOnlyResourceNameAsync(string name, CancellationToken cancellationToken = default) => await DeleteAsync(trackOnlyResourceNameApiUri, name, cancellationToken: cancellationToken);

        public async Task<ResourceItem> GetTrackOnlyResourceAsync(int resourceId, CancellationToken cancellationToken = default) => await GetAsync<ResourceItem>(trackOnlyResourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId), cancellationToken: cancellationToken);
        public async Task<ResourceItem> UpdateTrackOnlyResourceAsync(TrackResourceItem trackResourceItem, CancellationToken cancellationToken = default) => await PutResponseAsync<ResourceItem, ResourceItem>(trackOnlyResourceApiUri, trackResourceItem, cancellationToken);
        public async Task DeleteTrackOnlyResourceAsync(int resourceId, CancellationToken cancellationToken = default) => await DeleteAsync(trackOnlyResourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId), cancellationToken: cancellationToken);

        public async Task<ResourceItem> GetTrackResourceAsync(int resourceId, CancellationToken cancellationToken = default) => await GetAsync<ResourceItem>(resourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId), cancellationToken: cancellationToken);
        public async Task<ResourceItem> UpdateTrackResourceAsync(TrackResourceItem trackResourceItem, CancellationToken cancellationToken = default) => await PutResponseAsync<ResourceItem, ResourceItem>(resourceApiUri, trackResourceItem, cancellationToken);
        public async Task DeleteTrackResourceAsync(int resourceId, CancellationToken cancellationToken = default) => await DeleteAsync(resourceApiUri, Convert.ToString(resourceId), parmName1: nameof(resourceId), cancellationToken: cancellationToken);

        public async Task<ResourceSettings> GetTrackResourceSettingAsync(CancellationToken cancellationToken = default) => await GetAsync<ResourceSettings>(resourceSettingApiUri, cancellationToken);
        public async Task SaveTrackResourceSettingAsync(ResourceSettings resourceSettings, CancellationToken cancellationToken = default) => await PostAsync(resourceSettingApiUri, resourceSettings, cancellationToken);

        public async Task<SendEmail> GetTrackSendEmailAsync(CancellationToken cancellationToken = default) => await GetAsync<SendEmail>(sendEmailApiUri, cancellationToken);
        public async Task UpdateTrackSendEmailAsync(SendEmail sendEmail, CancellationToken cancellationToken = default) => await PutAsync(sendEmailApiUri, sendEmail, cancellationToken);
        public async Task DeleteTrackSendEmailAsync(CancellationToken cancellationToken = default) => await DeleteAsync(sendEmailApiUri, cancellationToken);

        public async Task<SendSms> GetTrackSendSmsAsync(CancellationToken cancellationToken = default) => await GetAsync<SendSms>(sendSmsApiUri, cancellationToken);
        public async Task UpdateTrackSendSmsAsync(SendSms sendSms, CancellationToken cancellationToken = default) => await PutAsync(sendSmsApiUri, sendSms, cancellationToken);
        public async Task DeleteTrackSendSmsAsync(CancellationToken cancellationToken = default) => await DeleteAsync(sendSmsApiUri, cancellationToken);

        public async Task<List<ClaimMap>> GetTrackClaimMappingAsync(CancellationToken cancellationToken = default) => await GetAsync<List<ClaimMap>>(claimMappingApiUri, cancellationToken);
        public async Task SaveTrackClaimMappingAsync(List<ClaimMap> claimMappings, CancellationToken cancellationToken = default) => await PostAsync(claimMappingApiUri, claimMappings, cancellationToken);

        public async Task<UsageLogResponse> GetTrackUsageLogAsync(UsageLogRequest usageLogRequest, CancellationToken cancellationToken = default) => await GetAsync<UsageLogRequest, UsageLogResponse>(logUsageApiUri, usageLogRequest, cancellationToken);

        public async Task<LogResponse> GetTrackAuditLogAsync(AuditLogRequest auditLogRequest, CancellationToken cancellationToken = default) => await GetAsync<AuditLogRequest, LogResponse>(logAuditApiUri, auditLogRequest, cancellationToken);

        public async Task<LogResponse> GetLogAsync(LogRequest logRequest, CancellationToken cancellationToken = default) => await GetAsync<LogRequest, LogResponse>(logApiUri, logRequest, cancellationToken);

        public async Task<LogSettings> GetTrackLogSettingAsync(CancellationToken cancellationToken = default) => await GetAsync<LogSettings>(logSettingApiUri, cancellationToken);
        public async Task SaveTrackLogSettingAsync(LogSettings logSettings, CancellationToken cancellationToken = default) => await PostAsync(logSettingApiUri, logSettings, cancellationToken);

        public async Task<LogStreams> GetTrackLogStreamSettingsAsync(CancellationToken cancellationToken = default) => await GetAsync<LogStreams>(logStreamsSettingsApiUri, cancellationToken);
        public async Task SaveTrackLogStreamSettingsAsync(LogStreams logStreams, CancellationToken cancellationToken = default) => await PostAsync(logStreamsSettingsApiUri, logStreams, cancellationToken);
    }
}
