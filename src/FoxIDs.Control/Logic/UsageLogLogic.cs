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
using FoxIDs.Models.Api;
using FoxIDs.Infrastructure;

namespace FoxIDs.Logic
{
    public class UsageLogLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly TelemetryScopedLogger logger;
        private readonly TokenCredential tokenCredential;

        public UsageLogLogic(FoxIDsControlSettings settings, TelemetryScopedLogger logger, TokenCredential tokenCredential, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logger = logger;
            this.tokenCredential = tokenCredential;
        }

        public async Task<Api.UsageLogResponse> GetTrackUsageLog(Api.UsageLogRequest logRequest)
        {
            var client = new LogsQueryClient(tokenCredential);  
            var rows = await LoadUsageEventsAsync(client, GetQueryTimeRange(logRequest.TimeScope), logRequest);

            var items = new List<Api.UsageLogItem>();
            var dayPointer = 0;
            var hourPointer = 0;
            List<Api.UsageLogItem> dayItemsPointer = items;
            List<Api.UsageLogItem> itemsPointer = items;
            foreach (var row in rows)
            {
                if (logRequest.SummarizeLevel != UsageLogSummarizeLevels.Month)
                {
                    var date = GetDate(row);
                    if (date.Day != dayPointer)
                    {
                        dayPointer = date.Day;
                        hourPointer = 0;
                        var dayItem = new UsageLogItem
                        {
                            Type = UsageLogTypes.Day,
                            Value = date.Day
                        };
                        dayItem.SubItems = itemsPointer = dayItemsPointer = new List<UsageLogItem>();
                        items.Add(dayItem);
                    }

                    if (logRequest.SummarizeLevel == UsageLogSummarizeLevels.Hour)
                    {
                        if (date.Hour != hourPointer)
                        {
                            hourPointer = date.Hour;
                            var hourItem = new UsageLogItem
                            {
                                Type = UsageLogTypes.Hour,
                                Value = date.Hour
                            };
                            hourItem.SubItems = itemsPointer = new List<UsageLogItem>();
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

        private IEnumerable<UsageLogItem> SortUsageTypes(IEnumerable<UsageLogItem> items)
        {
            return items.Select(i => new UsageLogItem { Type = i.Type, Value = i.Value, SubItems = i.SubItems != null ? SortUsageTypes(i.SubItems) : null }).OrderBy(i => i.Type);
        }

        private UsageLogTypes GetLogType(LogsTableRow row)
        {
            UsageLogTypes logType;
            var typeValue = row.GetString(Constants.Logs.UsageType);
            if(!Enum.TryParse(typeValue, out logType))
            {
                throw new Exception($"Value '{typeValue}' cannot be converted to enum type '{nameof(UsageLogTypes)}'.");
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

        private QueryTimeRange GetQueryTimeRange(UsageLogTimeScopes timeScope)
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

        private async Task<IReadOnlyList<LogsTableRow>> LoadUsageEventsAsync(LogsQueryClient client, QueryTimeRange queryTimeRange, Api.UsageLogRequest logRequest)
        {
            if(!logRequest.IncludeLogins && !logRequest.IncludeTokenRequests && !logRequest.IncludeControlApiGets && !logRequest.IncludeControlApiUpdates)
            {
                logRequest.IncludeLogins = true;
                logRequest.IncludeTokenRequests = true;
            }
            var includeAll = logRequest.IncludeLogins && logRequest.IncludeTokenRequests && logRequest.IncludeControlApiGets && logRequest.IncludeControlApiUpdates;

            var where = includeAll ? $"| where isnotempty(f_UsageType)" : $"| where {string.Join(" or ", GetIncludes(logRequest).Select(i => $"f_UsageType == '{i}'"))}";

            var preOrderSummarizeBy = logRequest.SummarizeLevel == UsageLogSummarizeLevels.Month ? string.Empty : $"bin(TimeGenerated, 1{(logRequest.SummarizeLevel == UsageLogSummarizeLevels.Day ? "d" : "h")}), ";
            var preSortBy = logRequest.SummarizeLevel == UsageLogSummarizeLevels.Month ? string.Empty : "TimeGenerated asc, ";

            var eventsQuery = GetQuery("AppEvents", where, preOrderSummarizeBy, preSortBy);
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

        private string GetQuery(string fromType, string where, string preOrderSummarizeBy, string preSortBy)
        {
            return
@$"{fromType}
| extend f_TenantName = Properties.f_TenantName
| extend f_TrackName = Properties.f_TrackName
| extend f_UsageType = Properties.f_UsageType
| where f_TenantName == '{RouteBinding.TenantName}' and f_TrackName == '{RouteBinding.TrackName}' {where}
| summarize UsageCount = count() by {preOrderSummarizeBy}tostring(f_UsageType)
| sort by {(preSortBy.IsNullOrEmpty() ? string.Empty : preSortBy)}f_UsageType desc";
        }
    }
}