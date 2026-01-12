using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Linq;
using ITfoxtec.Identity;
using System.Collections.Generic;
using FoxIDs.Repository;
using FoxIDs.Models;
using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using FoxIDs.Models.Config;

namespace FoxIDs.Logic
{
    public class UsageLogLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly IServiceProvider serviceProvider;
        private readonly ITenantDataRepository tenantDataRepository;

        public UsageLogLogic(FoxIDsControlSettings settings, IServiceProvider serviceProvider, ITenantDataRepository tenantDataRepository, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.serviceProvider = serviceProvider;
            this.tenantDataRepository = tenantDataRepository;
        }

        public async Task<Api.UsageLogResponse> GetTrackUsageLogAsync(Api.UsageLogRequest logRequest, string tenantName, string trackName, bool isMasterTenant = false, bool isMasterTrack = false)
        {
            var items = await QueryDbAsync(logRequest, tenantName, trackName, isMasterTenant, isMasterTrack);
            var excludeTrackLinkLogins = isMasterTrack;

            if (!logRequest.OnlyDbQuery)
            {
                switch (settings.Options.Log)
                {
                    case LogOptions.OpenSearchAndStdoutErrors:
                        items = await serviceProvider.GetService<UsageLogOpenSearchLogic>().QueryLogsAsync(logRequest, tenantName, trackName, excludeTrackLinkLogins, items);
                        break;
                    case LogOptions.ApplicationInsights:
                        items = await serviceProvider.GetService<UsageLogApplicationInsightsLogic>().QueryLogsAsync(logRequest, tenantName, trackName, isMasterTenant, excludeTrackLinkLogins, items);
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }

            return new Api.UsageLogResponse { SummarizeLevel = logRequest.SummarizeLevel, Items = SortUsageTypes(items) };
        }

        private async Task<List<Api.UsageLogItem>> QueryDbAsync(Api.UsageLogRequest logRequest, string tenantName, string trackName, bool isMasterTenant, bool isMasterTrack)
        {
            var items = new List<Api.UsageLogItem>();
            if (logRequest.TimeScope == Api.UsageLogTimeScopes.ThisMonth && logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Month)
            {
                if (isMasterTenant)
                {
                    if (logRequest.IncludeTenants)
                    {
                        var tenants = await GetTenantCountAsync(tenantName);
                        if (tenants != null)
                        {
                            items.Add(tenants);
                        }
                    }
                }
                if (isMasterTenant || isMasterTrack)
                {
                    if (logRequest.IncludeTracks)
                    {
                        var tracks = await GetTrackCountAsync(tenantName, trackName);
                        if (tracks != null)
                        {
                            items.Add(tracks);
                        }
                    }
                }
                if (logRequest.IncludeUsers)
                {
                    var users = await GetUserCountAsync(tenantName, trackName);
                    if (users != null)
                    {
                        items.Add(users);
                    }
                }
            }

            return items;
        }

        private async Task<Api.UsageLogItem> GetTenantCountAsync(string tenantName)
        {
            var usePartitionId = tenantName.IsNullOrWhiteSpace();

            var count = await tenantDataRepository.CountAsync(whereQuery: await GetTenantCountWhereQueryAsync(tenantName, usePartitionId), usePartitionId: usePartitionId);
            if (count > 0)
            {
                return new Api.UsageLogItem
                {
                    Type = Api.UsageLogTypes.Tenant,
                    Value = count
                };
            }
            else
            {
                return null;
            }
        }

        private async Task<Expression<Func<Tenant, bool>>> GetTenantCountWhereQueryAsync(string tenantName, bool usePartitionId)
        {
            if (usePartitionId)
            {
                return null;
            }
            else
            {
                var id = await Tenant.IdFormatAsync(tenantName);
                return p => p.Id.Equals(id);
            }
        }

        private async Task<Api.UsageLogItem> GetTrackCountAsync(string tenantName, string trackName)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            var usePartitionId = !idKey.TenantName.IsNullOrWhiteSpace() && idKey.TrackName.IsNullOrWhiteSpace();

            var count = await tenantDataRepository.CountAsync(idKey, whereQuery: await GetTrackCountWhereQueryAsync(idKey, usePartitionId), usePartitionId: usePartitionId);
            if (count > 0)
            {
                return new Api.UsageLogItem
                {
                    Type = Api.UsageLogTypes.Track,
                    Value = count
                };
            }
            else
            {
                return null;
            }
        }

        private async Task<Expression<Func<Track, bool>>> GetTrackCountWhereQueryAsync(Track.IdKey idKey, bool usePartitionId)
        {
            if (usePartitionId)
            {
                return p => !p.Id.EndsWith(Constants.Routes.MasterTrackName);
            }
            else
            {
                if (idKey.TenantName.IsNullOrWhiteSpace())
                {
                    return p => p.DataType.Equals(Constants.Models.DataType.Track) && !p.Id.EndsWith(Constants.Routes.MasterTrackName);
                }

                var id = await Track.IdFormatAsync(idKey);
                return p => p.Id.Equals(id);
            }
        }

        private async Task<Api.UsageLogItem> GetUserCountAsync(string tenantName, string trackName)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            var usePartitionId = !idKey.TenantName.IsNullOrWhiteSpace() && !idKey.TrackName.IsNullOrWhiteSpace();

            var count = await tenantDataRepository.CountAsync(idKey, whereQuery: GetUserCountWhereQuery(idKey, usePartitionId), usePartitionId: usePartitionId);
            if (count > 0)
            {
                return new Api.UsageLogItem
                {
                    Type = Api.UsageLogTypes.User,
                    Value = count
                };
            }
            else
            {
                return null;
            }
        }

        private Expression<Func<User, bool>> GetUserCountWhereQuery(Track.IdKey idKey, bool usePartitionId)
        {
            Expression<Func<User, bool>> baseFilter = p => p.DataType == Constants.Models.DataType.User || p.DataType == Constants.Models.DataType.ExternalUser;

            if (!usePartitionId && !idKey.TenantName.IsNullOrWhiteSpace())
            {
                var tenantPartitionPrefix = string.Concat(idKey.TenantName, ":");
                return p => (p.DataType == Constants.Models.DataType.User || p.DataType == Constants.Models.DataType.ExternalUser) && p.PartitionId.StartsWith(tenantPartitionPrefix);
            }

            return baseFilter;
        }

        private IEnumerable<Api.UsageLogItem> SortUsageTypes(IEnumerable<Api.UsageLogItem> items)
        {
            return items.Select(i => new Api.UsageLogItem { Type = i.Type, Value = i.Value, SubItems = i.SubItems != null ? SortUsageTypes(i.SubItems) : null }).OrderBy(i => (i.Type == Api.UsageLogTypes.ControlApiGet || i.Type == Api.UsageLogTypes.ControlApiUpdate) ? (int)i.Type * 1000 : (int)i.Type);
        }
    }
}
