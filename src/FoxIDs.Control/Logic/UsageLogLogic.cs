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

        public async Task<Api.UsageLogResponse> GetTrackUsageLog(Api.UsageLogRequest logRequest, string tenantName, string trackName)
        {
            var client = new LogsQueryClient(tokenCredential);  
            var rows = await LoadUsageEventsAsync(client, tenantName, trackName, GetQueryTimeRange(logRequest.TimeScope), logRequest);

            var items = new List<Api.UsageLogItem>();
            if(logRequest.IncludeUsers && logRequest.TimeScope == Api.UsageLogTimeScopes.ThisMonth && logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Month)
            {
                var users = await GetUserCountAsync(tenantName, trackName);
                if (users != null)
                {
                    items.Add(users);
                }
            }
            var dayPointer = 0;
            var hourPointer = 0;
            List<Api.UsageLogItem> dayItemsPointer = items;
            List<Api.UsageLogItem> itemsPointer = items;
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
                        if (date.Hour != hourPointer)
                        {
                            hourPointer = date.Hour;
                            var hourItem = new Api.UsageLogItem
                            {
                                Type = Api.UsageLogTypes.Hour,
                                Value = date.Hour
                            };
                            hourItem.SubItems = itemsPointer = new List<Api.UsageLogItem>();
                            dayItemsPointer.Add(hourItem);
                        }
                    }
                }

                var item = new Api.UsageLogItem
                {
                    Type = GetLogType(row),
                    Value = GetCount(row),
                };
                itemsPointer.Add(item);
            }

            return new Api.UsageLogResponse { SummarizeLevel = logRequest.SummarizeLevel, Items = SortUsageTypes(items) };
        }

        private async Task<Api.UsageLogItem> GetUserCountAsync(string tenantName, string trackName)
        {
            var idKey = new Track.IdKey { TenantName = tenantName, TrackName = trackName };
            var usePartitionId = !idKey.TenantName.IsNullOrEmpty() && !idKey.TrackName.IsNullOrEmpty();
            Expression<Func<User, bool>> whereQuery = usePartitionId ? p => p.DataType.Equals("user") : p => p.DataType.Equals("user") && p.PartitionId.StartsWith($"{idKey.TenantName}:");

            var count = await tenantRepository.CountAsync<User>(idKey, whereQuery: GetUserCountWhereQuery(idKey, usePartitionId), usePartitionId: usePartitionId);
            if (count > 0)
            {
                return new Api.UsageLogItem
                {
                    Type = Api.UsageLogTypes.user,
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
            if (!usePartitionId && !idKey.TenantName.IsNullOrEmpty())
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

        private long GetCount(LogsTableRow row)
        {
            var count = row.GetInt64("UsageCount");
            return count.HasValue ? count.Value : 0;
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

        private QueryTimeRange GetQueryTimeRange(Api.UsageLogTimeScopes timeScope)
        {
            var timePointer = DateTimeOffset.Now;
            if (timeScope == Api.UsageLogTimeScopes.LastMonth)
            {
                timePointer = timePointer.AddMonths(-1);
            }
            var startDate = new DateTime(timePointer.Year, timePointer.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            return new QueryTimeRange(startDate, endDate);
        }

        private async Task<IReadOnlyList<LogsTableRow>> LoadUsageEventsAsync(LogsQueryClient client, string tenantName, string trackName, QueryTimeRange queryTimeRange, Api.UsageLogRequest logRequest)
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

            var eventsQuery = GetQuery("AppEvents", GetWhereDataSlice(tenantName, trackName), where, preOrderSummarizeBy, preSortBy);
            Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(settings.ApplicationInsights.WorkspaceId, eventsQuery, queryTimeRange);
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
                whereDataSlice.Add($"f_TenantName == '{tenantName}'");
            }
            if (!trackName.IsNullOrWhiteSpace())
            {
                whereDataSlice.Add($"f_TrackName == '{trackName}'");
            }
            return string.Join(" and ", whereDataSlice);
        }

        private string GetQuery(string fromType, string whereDataSlice, string where, string preOrderSummarizeBy, string preSortBy)
        {
            return
@$"{fromType}
| extend f_TenantName = Properties.f_TenantName
| extend f_TrackName = Properties.f_TrackName
| extend f_UsageType = Properties.f_UsageType
{(whereDataSlice.IsNullOrEmpty() ? string.Empty : $"| where {whereDataSlice} ")}| where {where}
| summarize UsageCount = count() by {preOrderSummarizeBy}tostring(f_UsageType)
{(preSortBy.IsNullOrEmpty() ? string.Empty : $"| sort by {preSortBy}")}";
        }
    }
}