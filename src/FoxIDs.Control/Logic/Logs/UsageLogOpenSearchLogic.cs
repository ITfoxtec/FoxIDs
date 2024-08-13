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

namespace FoxIDs.Logic
{
    public class UsageLogOpenSearchLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly OpenSearchClient openSearchClient;

        public UsageLogOpenSearchLogic(FoxIDsControlSettings settings, OpenSearchClient openSearchClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.openSearchClient = openSearchClient;
        }

        public async Task<List<Api.UsageLogItem>> QueryLogs(Api.UsageLogRequest logRequest, string tenantName, string trackName, bool isMasterTenant, List<Api.UsageLogItem> items)
        {
            var dayPointer = 0;
            var hourPointer = 0;
            List<Api.UsageLogItem> dayItemsPointer = items;
            List<Api.UsageLogItem> itemsPointer = items;
            var aggregations = await LoadUsageEventsAsync(tenantName, trackName, GetQueryTimeRange(logRequest.TimeScope, logRequest.TimeOffset), logRequest, isMasterTenant);

            var userTypesAggregations = GetAggregations(aggregations, logRequest).OrderBy(a => a.aggregation.Date).ToList();

            foreach ((var usageType, var bucketItem) in userTypesAggregations)
            {
                if (logRequest.SummarizeLevel != Api.UsageLogSummarizeLevels.Month)
                {
                    var date = bucketItem.Date;
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

                var logType = GetLogType(usageType);
                var item = new Api.UsageLogItem
                {
                    Type = logType,
                    Value = GetCount(bucketItem, logType),
                };
                itemsPointer.Add(item);
            }

            return items;
        }

        private IEnumerable<(string usageType, DateHistogramBucket aggregation)> GetAggregations(FiltersAggregate aggregations, Api.UsageLogRequest logRequest)
        {
            if (logRequest.IncludeLogins)
            {
                foreach(var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.Login.ToString()))
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
            if (logRequest.IncludeControlApiGets)
            {
                foreach (var bucketItem in GetAggregationItems(aggregations, Api.UsageLogTypes.ControlApiGet.ToString()))
                {
                    yield return bucketItem;
                }
            }
            if (logRequest.IncludeControlApiUpdates)
            {
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
            foreach(DateHistogramBucket bucketItem in bucketAggregate.Items)
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

        private double GetCount(DateHistogramBucket bucketItem, Api.UsageLogTypes logType)
        {
            var count = bucketItem.DocCount.HasValue ? Convert.ToDouble(bucketItem.DocCount.Value) : 0.0;
            if (logType == Api.UsageLogTypes.Login || logType == Api.UsageLogTypes.TokenRequest)
            {
                var valueAggregate = bucketItem.Values.First() as ValueAggregate;
                if(valueAggregate?.Value != null)
                {
                    count += valueAggregate.Value.Value;
                }
            }
            return Math.Round(count, 1);
        }

        private (DateTime start, DateTime end) GetQueryTimeRange(Api.UsageLogTimeScopes timeScope, int timeOffset)
        {
            var timePointer = DateTimeOffset.Now;
            if (timeScope == Api.UsageLogTimeScopes.LastMonth)
            {
                timePointer = timePointer.AddMonths(-1);
            }
            var start = new DateTimeOffset(timePointer.Year, timePointer.Month, 1, 0, 0, 0, TimeSpan.FromHours(timeOffset));
            var end = start.AddMonths(1);
            return (start.DateTime, end.DateTime);
        }

        private async Task<FiltersAggregate> LoadUsageEventsAsync(string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange, Api.UsageLogRequest logRequest, bool isMasterTenant)
        {
            if (!logRequest.IncludeLogins && !logRequest.IncludeTokenRequests && !logRequest.IncludeControlApiGets && !logRequest.IncludeControlApiUpdates)
            {
                logRequest.IncludeLogins = true;
                logRequest.IncludeTokenRequests = true;
            }

            var response = await openSearchClient.SearchAsync<OpenSearchLogItem>(s => s
                .Index(GetIndexName(Constants.Logs.IndexName.Events))
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
                                        .Sum("usageaddrating", sa => sa
                                            .Field(p => p.UsageAddRating)
                                        )
                                    )
                             ))
                    ))
                );

            return response.Aggregations.Values.First() as FiltersAggregate;
        }

        private IPromise<INamedFiltersContainer> GetFilters(NamedFiltersContainerDescriptor<OpenSearchLogItem> filters, Api.UsageLogRequest logRequest)
        {
            if (logRequest.IncludeLogins)
            {
                AddFilter(filters, Api.UsageLogTypes.Login.ToString());
            }
            if (logRequest.IncludeTokenRequests)
            {
                AddFilter(filters, Api.UsageLogTypes.TokenRequest.ToString());
            }
            if (logRequest.IncludeControlApiGets)
            {
                AddFilter(filters, Api.UsageLogTypes.ControlApiGet.ToString());
            }
            if (logRequest.IncludeControlApiUpdates)
            {
                AddFilter(filters, Api.UsageLogTypes.ControlApiUpdate.ToString());
            }
            return filters;
        }

        private void AddFilter(NamedFiltersContainerDescriptor<OpenSearchLogItem> filters, string usageType)
        {
            filters.Filter(usageType, q => q.Term(p => p.UsageType, usageType.ToLower()));
        }

        private IBoolQuery GetQuery(BoolQueryDescriptor<OpenSearchLogItem> boolQuery, string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange)
        {
            boolQuery = boolQuery.Filter(f => f.DateRange(dt => dt.Field(field => field.Timestamp)
                                     .GreaterThanOrEquals(queryTimeRange.start)
                                     .LessThanOrEquals(queryTimeRange.end)));

            if (!tenantName.IsNullOrWhiteSpace() && !trackName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.TenantName, tenantName) && m.Term(t => t.TrackName, trackName));
            }
            else if (!tenantName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.TenantName, tenantName) && m.Exists(e => e.Field(f => f.TrackName)));
            }
            else if (!trackName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Exists(e => e.Field(f => f.TenantName)) && m.Term(t => t.TrackName, trackName));
            }

            return boolQuery;
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

        private string GetIndexName(string logIndexName)
        {
            var lifetime = settings.OpenSearch.LogLifetime.GetLifetimeInDays();

            if (RouteBinding?.PlanLogLifetime != null)
            {
                lifetime = RouteBinding.PlanLogLifetime.Value.GetLifetimeInDays();
            }

            return $"log-{lifetime}d-{logIndexName}*";
        }
    }
}