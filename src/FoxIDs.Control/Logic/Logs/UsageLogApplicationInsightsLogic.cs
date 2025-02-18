using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using Azure.Monitor.Query;
using System;
using System.Threading.Tasks;
using System.Linq;
using ITfoxtec.Identity;
using Azure.Monitor.Query.Models;
using Azure;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using FoxIDs.Models;
using System.Linq.Expressions;

namespace FoxIDs.Logic
{
    public class UsageLogApplicationInsightsLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly LogAnalyticsWorkspaceProvider logAnalyticsWorkspaceProvider;

        public UsageLogApplicationInsightsLogic(FoxIDsControlSettings settings, LogAnalyticsWorkspaceProvider logAnalyticsWorkspaceProvider, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.logAnalyticsWorkspaceProvider = logAnalyticsWorkspaceProvider;
        }

        public async Task<List<Api.UsageLogItem>> QueryLogsAsync(Api.UsageLogRequest logRequest, string tenantName, string trackName, bool isMasterTenant, List<Api.UsageLogItem> items)
        {
            var dayPointer = 0;
            var hourPointer = 0;
            List<Api.UsageLogItem> dayItemsPointer = items;
            List<Api.UsageLogItem> itemsPointer = items;
            var rows = await LoadUsageEventsAsync(tenantName, trackName, GetQueryTimeRange(logRequest.TimeScope, logRequest.TimeOffset), logRequest, isMasterTenant);
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
                };

                switch (logType)
                {
                    case Api.UsageLogTypes.Login:
                    case Api.UsageLogTypes.TokenRequest:
                        (var totalCount, var realCount, var extraCount) = GetCountAndRealEstra(row);
                        item.Value = totalCount;
                        item.SubItems = [new Api.UsageLogItem { Type = Api.UsageLogTypes.RealCount, Value = realCount }, new Api.UsageLogItem { Type = Api.UsageLogTypes.ExtraCount, Value = extraCount }];
                        break;
                    case Api.UsageLogTypes.Confirmation:
                    case Api.UsageLogTypes.ResetPassword:
                    case Api.UsageLogTypes.Mfa:
                        (var itemCount, var smsCount, var smsPrice, var emailCount) = GetCountAndSmsEmail(row);
                        item.Value = itemCount;
                        item.SubItems = [new Api.UsageLogItem { Type = Api.UsageLogTypes.Sms, Value = smsCount, SubItems = [new Api.UsageLogItem { Type = Api.UsageLogTypes.SmsPrice, Value = smsPrice }] }, new Api.UsageLogItem { Type = Api.UsageLogTypes.Email, Value = emailCount }];
                        break;
                    default:
                        item.Value = GetCount(row);
                        break;
                }

                itemsPointer.Add(item);
            }

            return items;
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

        private decimal GetCount(LogsTableRow row)
        {
            var count = row.GetDouble("UsageCount");
            return count.HasValue ? Math.Round(Convert.ToDecimal(count.Value), 1) : 0.0M;
        }

        private (decimal totalCount, decimal realCount, decimal extraCount) GetCountAndRealEstra(LogsTableRow row)
        {
            var item = row.GetDouble("UsageCount");
            double realCount = item.HasValue ? item.Value : 0.0;
            
            double extraCount = 0.0;
            var addItem = row.GetDouble("UsageAddRating");
            extraCount = addItem.HasValue ? addItem.Value : 0.0;

            return (Math.Round(Convert.ToDecimal(realCount + extraCount), 1), Math.Round(Convert.ToDecimal(realCount), 0), Math.Round(Convert.ToDecimal(extraCount), 1));
        }

        private (decimal realCount, decimal smsCount, decimal smsPrice, decimal emailCount) GetCountAndSmsEmail(LogsTableRow row)
        {
            var item = row.GetDouble("UsageCount");
            double itemCount = item.HasValue ? item.Value : 0.0;
            
            double smsCount = 0.0;
            var smsItem = row.GetDouble("UsageSms");
            smsCount = smsItem.HasValue ? smsItem.Value : 0.0;

            double smsPrice = 0.0;
            if (smsCount > 0)
            {
                var smsPriceItem = row.GetDouble("UsageSmsPrice");
                smsPrice = smsPriceItem.HasValue ? smsPriceItem.Value / smsCount : 0.0;
            }

            double emailCount = 0.0;
            var emailItem = row.GetDouble("UsageEmail");
            emailCount = emailItem.HasValue ? smsItem.Value : 0.0;

            return (Math.Round(Convert.ToDecimal(itemCount), 0), Math.Round(Convert.ToDecimal(smsCount), 0), Math.Round(Convert.ToDecimal(smsPrice), 4), Math.Round(Convert.ToDecimal(emailCount), 0));
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

        private string GetLogAnalyticsWorkspaceId()
        {
            return settings.ApplicationInsights.WorkspaceId;
        }

        private async Task<IReadOnlyList<LogsTableRow>> LoadUsageEventsAsync(string tenantName, string trackName, QueryTimeRange queryTimeRange, Api.UsageLogRequest logRequest, bool isMasterTenant)
        {
            if(!logRequest.IncludeLogins && !logRequest.IncludeTokenRequests && !logRequest.IncludeControlApi && !logRequest.IncludeAdditional)
            {
                logRequest.IncludeLogins = true;
                logRequest.IncludeTokenRequests = true;
            }
          
            var where = $"{string.Join(" or ", GetIncludes(logRequest).Select(i => $"{Constants.Logs.UsageType} == '{i}'"))}";

            var preOrderSummarizeBy = logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Month ? string.Empty : $"bin(TimeGenerated, 1{(logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Day ? "d" : "h")}), ";
            var preSortBy = logRequest.SummarizeLevel == Api.UsageLogSummarizeLevels.Month ? string.Empty : "TimeGenerated asc";

            var eventsQuery = GetQuery("AppEvents", GetWhereDataSlice(tenantName, trackName), where, preOrderSummarizeBy, preSortBy, isMasterTenant);
            Response<LogsQueryResult> response = await logAnalyticsWorkspaceProvider.QueryWorkspaceAsync(GetLogAnalyticsWorkspaceId(), eventsQuery, queryTimeRange);
            var table = response.Value.Table;
            return table.Rows;
        }

        private IEnumerable<string> GetIncludes(Api.UsageLogRequest logRequest)
        {
            if (logRequest.IncludeLogins)
            {
                yield return UsageLogTypes.Login.ToString();
            }
            if (logRequest.IncludeTokenRequests)
            {
                yield return UsageLogTypes.TokenRequest.ToString();
            }
            if (logRequest.IncludeAdditional)
            {
                yield return UsageLogTypes.Confirmation.ToString();
                yield return UsageLogTypes.ResetPassword.ToString();
                yield return UsageLogTypes.Mfa.ToString();
            }
            if (logRequest.IncludeControlApi)
            {
                yield return UsageLogTypes.ControlApiGet.ToString();
                yield return UsageLogTypes.ControlApiUpdate.ToString();
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
| extend {Constants.Logs.UsageSms} = Properties.{Constants.Logs.UsageSms}
| extend {Constants.Logs.UsageSmsPrice} = Properties.{Constants.Logs.UsageSmsPrice}
| extend {Constants.Logs.UsageEmail} = Properties.{Constants.Logs.UsageEmail}
{(whereDataSlice.IsNullOrEmpty() ? string.Empty : $"| where {whereDataSlice} ")}| where {where}
| summarize UsageCount = count(), UsageAddRating = sum(todouble({Constants.Logs.UsageAddRating})), UsageSms = sum(todouble({Constants.Logs.UsageSms})), UsageSmsPrice = sum(todouble({Constants.Logs.UsageSmsPrice})), UsageEmail = sum(todouble({Constants.Logs.UsageEmail})) by {preOrderSummarizeBy}tostring({Constants.Logs.UsageType})
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