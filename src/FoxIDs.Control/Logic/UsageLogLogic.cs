using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Azure.Core;
using Azure.Monitor.Query;
using System;
using System.Threading.Tasks;
using System.Linq;
using ITfoxtec.Identity;
using Azure.Monitor.Query.Models;
using Azure;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using FoxIDs.Infrastructure;
using FoxIDs.Repository;
using FoxIDs.Models;
using System.Linq.Expressions;

namespace FoxIDs.Logic
{
    public class UsageLogLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly ITenantRepository tenantRepository;
        private readonly TokenCredential tokenCredential;

        public UsageLogLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, ITenantRepository tenantRepository, TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.tenantRepository = tenantRepository;
            this.tokenCredential = tokenCredential;
        }

        public async Task<Api.UsageLogResponse> GetTrackUsageLog(Api.UsageLogRequest logRequest, string tenantName, string trackName, bool isMasterTenant = false, bool isMasterTrack = false)
        {
            var items = new List<Api.UsageLogItem>();

            #region DB
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
                    if (logRequest.IncludeKeyVaultManagedCertificates)
                    {
                        var tracks = await GetKeyVaultManagedCertificateCountAsync(tenantName, trackName);
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
            #endregion

            #region logs
            var dayPointer = 0;
            var hourPointer = 0;
            List<Api.UsageLogItem> dayItemsPointer = items;
            List<Api.UsageLogItem> itemsPointer = items;
            var client = new LogsQueryClient(tokenCredential);
            var rows = await LoadUsageEventsAsync(client, tenantName, trackName, GetQueryTimeRange(logRequest.TimeScope, logRequest.TimeOffset), logRequest, isMasterTenant);
            foreach (var row in rows)
            {
                if (logRequest.SummarizeLevel != Api.UsageLogSummarizeLevels.Month)
                {
                    var date = GetDate(row);
                    if (date.Day != dayPointer)
                    {
                        dayPointer = date.Day;
                        hourPointer = 0;
                        var dayItem = new Api.UsageLogItem
                        {
                            Type = Api.UsageLogTypes.Day,
                            Value = date.Day
                        };
                        dayItem.SubItems = itemsPointer = dayItemsPointer = new List<Api.UsageLogItem>();
                        items.Add(dayItem);
                    }

                    if (logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Hour)
                    {
                        var hour = date.Hour + logRequest.TimeOffset;
                        if (hour != hourPointer)
                        {
                            hourPointer = hour;
                            var hourItem = new Api.UsageLogItem
                            {
                                Type = Api.UsageLogTypes.Hour,
                                Value = hour
                            };
                            hourItem.SubItems = itemsPointer = new List<Api.UsageLogItem>();
                            dayItemsPointer.Add(hourItem);
                        }
                    }
                }

                var logType = GetLogType(row);
                var item = new Api.UsageLogItem
                {
                    Type = logType,
                    Value = GetCount(row, logType),
                };
                itemsPointer.Add(item);
            }
            #endregion

            return new Api.UsageLogResponse { SummarizeLevel = logRequest.SummarizeLevel, Items = SortUsageTypes(items) };
        }

        private async Task<Api.UsageLogItem> GetTenantCountAsync(string tenantName)
        {
            var usePartitionId = tenantName.IsNullOrWhiteSpace();

            var count = await tenantRepository.CountAsync(whereQuery: await GetTenantCountWhereQueryAsync(tenantName, usePartitionId), usePartitionId: usePartitionId);
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

            var count = await tenantRepository.CountAsync(idKey, whereQuery: await GetTrackCountWhereQueryAsync(idKey, usePartitionId), usePartitionId: usePartitionId);
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
                return null;
            }
            else
            {
                if (idKey.TenantName.IsNullOrWhiteSpace())
                {
                    return p => p.DataType.Equals("track");
                }

                var id = await Track.IdFormatAsync(idKey);
                return p => p.Id.Equals(id);
            }
        }

        private async Task<Api.UsageLogItem> GetKeyVaultManagedCertificateCountAsync(string tenantName, string trackName)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            var usePartitionId = !idKey.TenantName.IsNullOrWhiteSpace() && idKey.TrackName.IsNullOrWhiteSpace();

            var count = await tenantRepository.CountAsync(idKey, whereQuery: await GetKeyVaultManagedCertificateCountWhereQueryAsync(idKey, usePartitionId), usePartitionId: usePartitionId);
            if (count > 0)
            {
                return new Api.UsageLogItem
                {
                    Type = Api.UsageLogTypes.KeyVaultManagedCertificate,
                    Value = count
                };
            }
            else
            {
                return null;
            }
        }
        private async Task<Expression<Func<Track, bool>>> GetKeyVaultManagedCertificateCountWhereQueryAsync(Track.IdKey idKey, bool usePartitionId)
        {
            if (usePartitionId)
            {
                return p => p.Key.Type == TrackKeyTypes.KeyVaultRenewSelfSigned;
            }
            else
            {
                if (idKey.TenantName.IsNullOrWhiteSpace())
                {
                    return p => p.DataType.Equals("track") && p.Key.Type == TrackKeyTypes.KeyVaultRenewSelfSigned;
                }

                var id = await Track.IdFormatAsync(idKey);
                return p => p.Id.Equals(id) && p.Key.Type == TrackKeyTypes.KeyVaultRenewSelfSigned;
            }
        }

        private async Task<Api.UsageLogItem> GetUserCountAsync(string tenantName, string trackName)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            var usePartitionId = !idKey.TenantName.IsNullOrWhiteSpace() && !idKey.TrackName.IsNullOrWhiteSpace();

            var count = await tenantRepository.CountAsync(idKey, whereQuery: GetUserCountWhereQuery(idKey, usePartitionId), usePartitionId: usePartitionId);
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
            if (!usePartitionId && !idKey.TenantName.IsNullOrWhiteSpace())
            {
                return p => p.DataType.Equals("user") && p.PartitionId.StartsWith($"{idKey.TenantName}:");
            }

            return p => p.DataType.Equals("user");
        }

        private IEnumerable<Api.UsageLogItem> SortUsageTypes(IEnumerable<Api.UsageLogItem> items)
        {
            return items.Select(i => new Api.UsageLogItem { Type = i.Type, Value = i.Value, SubItems = i.SubItems != null ? SortUsageTypes(i.SubItems) : null }).OrderBy(i => i.Type);
        }

        private Api.UsageLogTypes GetLogType(LogsTableRow row)
        {
            Api.UsageLogTypes logType;
            var typeValue = row.GetString(Constants.Logs.UsageType);
            if(!Enum.TryParse(typeValue, out logType))
            {
                throw new Exception($"Value '{typeValue}' cannot be converted to enum type '{nameof(Api.UsageLogTypes)}'.");
            }
            return logType;
        }

        private double GetCount(LogsTableRow row, Api.UsageLogTypes logType)
        {
            var count = row.GetDouble("UsageCount");
            if (logType == Api.UsageLogTypes.Login || logType == Api.UsageLogTypes.TokenRequest)
            {
                count += row.GetDouble("UsageAddRating");
            }
            return Math.Round(count.HasValue ? count.Value : 0, 1);
        }

        private DateTime GetDate(LogsTableRow row)
        {
            var timeGeneratedValue = row.GetString("TimeGenerated");
            if (timeGeneratedValue.IsNullOrEmpty())
            {
                throw new InvalidOperationException("Usage log TimeGenerated is empty.");
            }

            DateTime timeGenerated;
            if (DateTime.TryParse(timeGeneratedValue, out timeGenerated))
            {
                return timeGenerated;
            }
            else
            {
                throw new InvalidOperationException("Usage log TimeGenerated is invalid.");
            }
        }

        private QueryTimeRange GetQueryTimeRange(Api.UsageLogTimeScopes timeScope, int timeOffset)
        {
            var timePointer = DateTimeOffset.Now;
            if (timeScope == Api.UsageLogTimeScopes.LastMonth)
            {
                timePointer = timePointer.AddMonths(-1);
            }
            var startDate = new DateTimeOffset(timePointer.Year, timePointer.Month, 1, 0, 0, 0, TimeSpan.FromHours(timeOffset));
            var endDate = startDate.AddMonths(1);
            return new QueryTimeRange(startDate, endDate);
        }

        private string GetLogAnalyticsWorkspaceId(bool isMasterTenant)
        {
            if (!isMasterTenant && !string.IsNullOrWhiteSpace(RouteBinding?.LogAnalyticsWorkspaceId))
            {
                return RouteBinding.LogAnalyticsWorkspaceId;
            }
            else
            {
                return settings.ApplicationInsights.WorkspaceId;
            }
        }

        private async Task<IReadOnlyList<LogsTableRow>> LoadUsageEventsAsync(LogsQueryClient client, string tenantName, string trackName, QueryTimeRange queryTimeRange, Api.UsageLogRequest logRequest, bool isMasterTenant)
        {
            if(!logRequest.IncludeLogins && !logRequest.IncludeTokenRequests && !logRequest.IncludeControlApiGets && !logRequest.IncludeControlApiUpdates)
            {
                logRequest.IncludeLogins = true;
                logRequest.IncludeTokenRequests = true;
            }
            var includeAll = logRequest.IncludeLogins && logRequest.IncludeTokenRequests && logRequest.IncludeControlApiGets && logRequest.IncludeControlApiUpdates;

            var where = includeAll ? $"isnotempty(f_UsageType)" : $"{string.Join(" or ", GetIncludes(logRequest).Select(i => $"f_UsageType == '{i}'"))}";

            var preOrderSummarizeBy = logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Month ? string.Empty : $"bin(TimeGenerated, 1{(logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Day ? "d" : "h")}), ";
            var preSortBy = logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Month ? string.Empty : "TimeGenerated asc";

            var eventsQuery = GetQuery("AppEvents", GetWhereDataSlice(tenantName, trackName), where, preOrderSummarizeBy, preSortBy, isMasterTenant);
            Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(isMasterTenant), eventsQuery, queryTimeRange);
            var table = response.Value.Table;
            return table.Rows;
        }

        private IEnumerable<string> GetIncludes(Api.UsageLogRequest logRequest)
        {
            if (logRequest.IncludeLogins)
            {
                yield return Api.UsageLogTypes.Login.ToString();
            }
            if (logRequest.IncludeTokenRequests)
            {
                yield return Api.UsageLogTypes.TokenRequest.ToString();
            }
            if (logRequest.IncludeControlApiGets)
            {
                yield return Api.UsageLogTypes.ControlApiGet.ToString();
            }
            if (logRequest.IncludeControlApiUpdates)
            {
                yield return Api.UsageLogTypes.ControlApiUpdate.ToString();
            }
        }

        private string GetWhereDataSlice(string tenantName, string trackName)
        {
            var whereDataSlice = new List<string>();
            if (!tenantName.IsNullOrWhiteSpace())
            {
                whereDataSlice.Add($"{Constants.Logs.TenantName} == '{tenantName}'");
            }
            if (!trackName.IsNullOrWhiteSpace())
            {
                whereDataSlice.Add($"{Constants.Logs.TrackName} == '{trackName}'");
            }
            return string.Join(" and ", whereDataSlice);
        }

        private string GetQuery(string fromType, string whereDataSlice, string where, string preOrderSummarizeBy, string preSortBy, bool isMasterTenant)
        {
            return
@$"{GetFromTypeAndUnion(fromType, isMasterTenant)}
| extend {Constants.Logs.TenantName} = Properties.{Constants.Logs.TenantName}
| extend {Constants.Logs.TrackName} = Properties.{Constants.Logs.TrackName}
| extend {Constants.Logs.UsageType} = Properties.{Constants.Logs.UsageType}
| extend {Constants.Logs.UsageAddRating} = Properties.{Constants.Logs.UsageAddRating}
{(whereDataSlice.IsNullOrEmpty() ? string.Empty : $"| where {whereDataSlice} ")}| where {where}
| summarize UsageCount = count(), UsageAddRating = sum(todouble({Constants.Logs.UsageAddRating})) by {preOrderSummarizeBy}tostring({Constants.Logs.UsageType})
{(preSortBy.IsNullOrEmpty() ? string.Empty : $"| sort by {preSortBy}")}";
        }

        private string GetFromTypeAndUnion(string fromType, bool isMasterTenant)
        {
            if (!isMasterTenant || !(settings.ApplicationInsights.PlanWorkspaceIds?.Count() > 0))
            {
                return fromType;
            }
            else
            {
                return $"union {fromType}, {string.Join(", ", settings.ApplicationInsights.PlanWorkspaceIds.Select(w => $"workspace(\"{w}\").{fromType}").ToArray())}";
            }
        }
    }
}