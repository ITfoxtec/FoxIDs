using Api = FoxIDs.Models.Api;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;
using System.Linq;
using ITfoxtec.Identity;
using FoxIDs.Models.Config;
using System.Collections.Generic;
using FoxIDs.Models;
using OpenSearch.Client;
using FoxIDs.Infrastructure.Hosting;

namespace FoxIDs.Logic
{
    public class UsageLogOpenSearchLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly OpenSearchClient openSearchClient;

        public UsageLogOpenSearchLogic(FoxIDsControlSettings settings, OpenSearchClientQueryLog openSearchClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.openSearchClient = openSearchClient;
        }

        public async Task<List<Api.UsageLogItem>> QueryLogsAsync(Api.UsageLogRequest logRequest, string tenantName, string trackName, List<Api.UsageLogItem> items)
        {
            var dayPointer = -1;
            var hourPointer = -1;
            List<Api.UsageLogItem> dayItemsPointer = items;
            List<Api.UsageLogItem> itemsPointer = items;
            var aggregations = await LoadUsageEventsAsync(tenantName, trackName, GetQueryTimeRange(logRequest.TimeScope, logRequest.TimeOffset), logRequest);

            if (aggregations != null)
            {
                var userTypesAggregations = GetAggregations(aggregations, logRequest).OrderBy(a => a.aggregation.Date).ToList();

                foreach ((var usageType, var bucketItem) in userTypesAggregations)
                {
                    if (bucketItem.DocCount.HasValue && bucketItem.DocCount.Value > 0)
                    {
                        if (logRequest.SummarizeLevel != Api.UsageLogSummarizeLevels.Month)
                        {
                            var date = bucketItem.Date.AddHours(logRequest.TimeOffset);
                            if (date.Day != dayPointer)
                            {
                                dayPointer = date.Day;
                                hourPointer = -1;
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
                                var hour = date.Hour;
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

                        var logType = GetLogType(usageType);
                        var item = new Api.UsageLogItem
                        {
                            Type = logType,
                        };

                        switch (logType)
                        {
                            case Api.UsageLogTypes.Login:
                            case Api.UsageLogTypes.TokenRequest:
                                (var totalCount, var realCount, var extraCount) = GetCountAndRealEstra(bucketItem);
                                item.Value = totalCount;
                                item.SubItems = [new Api.UsageLogItem { Type = Api.UsageLogTypes.RealCount, Value = realCount }, new Api.UsageLogItem { Type = Api.UsageLogTypes.ExtraCount, Value = extraCount }];
                                break;
                            case Api.UsageLogTypes.Confirmation:
                            case Api.UsageLogTypes.ResetPassword:
                            case Api.UsageLogTypes.Mfa:
                                (var itemCount, var smsCount, var smsPrice, var emailCount) = GetCountAndSmsEmail(bucketItem);
                                item.Value = itemCount;
                                item.SubItems = [new Api.UsageLogItem { Type = Api.UsageLogTypes.Sms, Value = smsCount, SubItems = [new Api.UsageLogItem { Type = Api.UsageLogTypes.SmsPrice, Value = smsPrice }] }, new Api.UsageLogItem { Type = Api.UsageLogTypes.Email, Value = emailCount }];
                                break;
                            default:
                                item.Value = GetCount(bucketItem);
                                break;
                        } 

                        itemsPointer.Add(item);
                    }
                }
            }

            return items;
        }

        private IEnumerable<(string usageType, DateHistogramBucket aggregation)> GetAggregations(FiltersAggregate aggregations, Api.UsageLogRequest logRequest)
        {
            if (logRequest.IncludeLogins)
            {
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.Login.ToString()))
                {
                    yield return bucketItem;
                }
            }
            if (logRequest.IncludeTokenRequests)
            {
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.TokenRequest.ToString()))
                {
                    yield return bucketItem;
                }
            }
            if (logRequest.IncludeAdditional)
            {
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.Confirmation.ToString()))
                {
                    yield return bucketItem;
                }
           
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.ResetPassword.ToString()))
                {
                    yield return bucketItem;
                }
           
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.Mfa.ToString()))
                {
                    yield return bucketItem;
                }
            }
            if (logRequest.IncludeControlApi)
            {
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.ControlApiGet.ToString()))
                {
                    yield return bucketItem;
                }

                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.ControlApiUpdate.ToString()))
                {
                    yield return bucketItem;
                }
            }
        }

        private IEnumerable<(string usageType, DateHistogramBucket aggregation)> GetAggregationItems(FiltersAggregate aggregations, string usageType)
        {
            var singleBucketAggregate = aggregations[usageType] as SingleBucketAggregate;
            var bucketAggregate = singleBucketAggregate.Values.First() as BucketAggregate;
            foreach (DateHistogramBucket bucketItem in bucketAggregate.Items)
            {
                yield return (usageType, bucketItem);
            }
        }

        private Api.UsageLogTypes GetLogType(string usageType)
        {
            Api.UsageLogTypes logType;
            if (!Enum.TryParse(usageType, out logType))
            {
                throw new Exception($"Value '{usageType}' cannot be converted to enum type '{nameof(Api.UsageLogTypes)}'.");
            }
            return logType;
        }

        private decimal GetCount(DateHistogramBucket bucketItem)
        {
            double itemCount = bucketItem.DocCount.HasValue ? bucketItem.DocCount.Value : 0.0;           
            return Math.Round(Convert.ToDecimal(itemCount), 1);
        }

        private (decimal totalCount, decimal realCount, decimal extraCount) GetCountAndRealEstra(DateHistogramBucket bucketItem)
        {
            double realCount = bucketItem.DocCount.HasValue ? bucketItem.DocCount.Value : 0.0;
            double extraCount = 0.0;
            var valueAggregate = bucketItem[nameof(OpenSearchLogItem.UsageAddRating).ToLower()] as ValueAggregate;
            if (valueAggregate?.Value != null)
            {
                extraCount = valueAggregate.Value.Value;
            }
            return (Math.Round(Convert.ToDecimal(realCount + extraCount), 1), Math.Round(Convert.ToDecimal(realCount), 0), Math.Round(Convert.ToDecimal(extraCount), 1));
        }

        private (decimal realCount, decimal smsCount, decimal smsPrice, decimal emailCount) GetCountAndSmsEmail(DateHistogramBucket bucketItem)
        {
            double itemCount = bucketItem.DocCount.HasValue ? bucketItem.DocCount.Value : 0.0;
            double smsCount = 0.0;
            var smsValueAggregate = bucketItem[nameof(OpenSearchLogItem.UsageSms).ToLower()] as ValueAggregate;
            if (smsValueAggregate?.Value != null)
            {
                smsCount = smsValueAggregate.Value.Value;
            }

            double smsPrice = 0.0;
            if (smsCount > 0)
            {
                var smsPriceValueAggregate = bucketItem[nameof(OpenSearchLogItem.UsageSmsPrice).ToLower()] as ValueAggregate;
                if (smsPriceValueAggregate?.Value != null)
                {
                    smsPrice = smsPriceValueAggregate.Value.Value / smsCount;
                }
            }

            double emailCount = 0.0;
            var emailValueAggregate = bucketItem[nameof(OpenSearchLogItem.UsageEmail).ToLower()] as ValueAggregate;
            if (emailValueAggregate?.Value != null)
            {
                emailCount = emailValueAggregate.Value.Value;
            }
            return (Math.Round(Convert.ToDecimal(itemCount), 0), Math.Round(Convert.ToDecimal(smsCount), 0), Math.Round(Convert.ToDecimal(smsPrice), 4), Math.Round(Convert.ToDecimal(emailCount), 0));
        }

        private (DateTime start, DateTime end) GetQueryTimeRange(Api.UsageLogTimeScopes timeScope, int timeOffset)
        {
            var timePointer = DateTimeOffset.UtcNow;
            if (timeScope == Api.UsageLogTimeScopes.LastMonth)
            {
                timePointer = timePointer.AddMonths(-1);
            }
            var start = new DateTimeOffset(timePointer.Year, timePointer.Month, 1, 0, 0, 0, TimeSpan.FromHours(timeOffset));
            var end = start.AddMonths(1);
            return (start.DateTime, end.DateTime);
        }

        private async Task<FiltersAggregate> LoadUsageEventsAsync(string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange, Api.UsageLogRequest logRequest)
        {
            if (!logRequest.IncludeLogins && !logRequest.IncludeTokenRequests && !logRequest.IncludeControlApi && !logRequest.IncludeAdditional)
            {
                logRequest.IncludeLogins = true;
                logRequest.IncludeTokenRequests = true;
            }

            var response = await openSearchClient.SearchAsync<OpenSearchLogItem>(s => s
                .Index(Indices.Index(GetIndexName()))
                    .Size(0)
                    .Query(q => q
                         .Bool(b => GetQuery(b, tenantName, trackName, queryTimeRange)))
                    .Aggregations(a => a
                        .Filters("usagetype", agg => agg
                            .NamedFilters(filters => GetFilters(filters, logRequest))
                            .Aggregations(childAggs => childAggs
                                .DateHistogram("per_interval", dh => dh
                                    .Field(f => f.Timestamp).CalendarInterval(GetDateInterval(logRequest.SummarizeLevel))
                                    .Aggregations(sumAggs => sumAggs
                                  
                                        .Sum(nameof(OpenSearchLogItem.UsageSms).ToLower(), sa => sa
                                            .Field(p => p.UsageSms)
                                        )
                                        .Sum(nameof(OpenSearchLogItem.UsageSmsPrice).ToLower(), sa => sa
                                            .Field(p => p.UsageSmsPrice)
                                        )
                                        .Sum(nameof(OpenSearchLogItem.UsageEmail).ToLower(), sa => sa
                                            .Field(p => p.UsageEmail)
                                        )
                                        .Sum(nameof(OpenSearchLogItem.UsageAddRating).ToLower(), sa => sa
                                            .Field(p => p.UsageAddRating)
                                        )
                                    )
                             ))
                    ))
                );

            return response.Aggregations.Values.FirstOrDefault() as FiltersAggregate;
        }

        private IEnumerable<string> GetIndexName()
        {
            foreach (var name in GetIndexBaseName()) { yield return name; }

            if (settings.OpenSearchQuery != null && !string.IsNullOrWhiteSpace(settings.OpenSearchQuery?.CrossClusterSearchClusterName))
            {
                foreach (var name in GetIndexBaseName()) { yield return $"{settings.OpenSearchQuery.CrossClusterSearchClusterName}:{name}"; }
            }
        }

        private IEnumerable<string> GetIndexBaseName()
        {
            yield return $"{settings.OpenSearch.LogName}*";
            // Remove in about 8 month (support logtype changed to keyword) from now 2025.01.17
            yield return $"{settings.OpenSearch.LogName}-r*";
        }

        private IBoolQuery GetQuery(BoolQueryDescriptor<OpenSearchLogItem> boolQuery, string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange)
        {
            boolQuery = boolQuery.Filter(f => f.DateRange(dt => dt.Field(field => field.Timestamp)
                                     .GreaterThanOrEquals(queryTimeRange.start)
                                     .LessThanOrEquals(queryTimeRange.end)));

            if (!tenantName.IsNullOrWhiteSpace() && !trackName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.TenantName, tenantName) && m.Term(t => t.TrackName, trackName) && QueryEventLogType(m));
            }
            else if (!tenantName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.TenantName, tenantName) && QueryEventLogType(m));
            }
            else
            {
                boolQuery = boolQuery.Must(QueryEventLogType);
            }

            return boolQuery;
        }

        private static QueryContainer QueryEventLogType(QueryContainerDescriptor<OpenSearchLogItem> m)
        {
            return m.Term(t => t.LogType, LogTypes.Event.ToString()) ||
                // Remove in about 8 month (support logtype changed to keyword) from now 2025.01.17
                m.Match(ma => ma.Field(f => f.LogType).Query(LogTypes.Event.ToString())); 
        }

        private IPromise<INamedFiltersContainer> GetFilters(NamedFiltersContainerDescriptor<OpenSearchLogItem> filters, Api.UsageLogRequest logRequest)
        {
            if (logRequest.IncludeLogins)
            {
                AddFilter(filters, UsageLogTypes.Login.ToString());
            }
            if (logRequest.IncludeTokenRequests)
            {
                AddFilter(filters, UsageLogTypes.TokenRequest.ToString());
            }
            if (logRequest.IncludeAdditional)
            {
                AddFilter(filters, UsageLogTypes.Confirmation.ToString());
                AddFilter(filters, UsageLogTypes.ResetPassword.ToString());
                AddFilter(filters, UsageLogTypes.Mfa.ToString());
            }
            if (logRequest.IncludeControlApi)
            {
                AddFilter(filters, UsageLogTypes.ControlApiGet.ToString());
                AddFilter(filters, UsageLogTypes.ControlApiUpdate.ToString());
            }
            return filters;
        }

        private void AddFilter(NamedFiltersContainerDescriptor<OpenSearchLogItem> filters, string usageType)
        {
            filters.Filter(usageType, q => q.Term(p => p.UsageType, usageType.ToLower()));
        }

        private DateInterval GetDateInterval(Api.UsageLogSummarizeLevels usageLogSummarizeLevels)
        {
            switch (usageLogSummarizeLevels)
            {
                case Api.UsageLogSummarizeLevels.Hour:
                    return DateInterval.Hour;
                case Api.UsageLogSummarizeLevels.Day:
                    return DateInterval.Day;
                case Api.UsageLogSummarizeLevels.Month:
                    return DateInterval.Month;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}