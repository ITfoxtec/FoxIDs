using FoxIDs.Models;
using Api = FoxIDs.Models.Api;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using OpenSearch.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FoxIDs.Infrastructure.Hosting;

namespace FoxIDs.Logic
{
    public class AuditLogOpenSearchLogic : LogicBase
    {
        private readonly FoxIDsControlSettings settings;
        private readonly OpenSearchClient openSearchClient;

        public AuditLogOpenSearchLogic(FoxIDsControlSettings settings, OpenSearchClientQueryLog openSearchClient, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.openSearchClient = openSearchClient;
        }

        public async Task<Api.LogResponse> QueryLogsAsync(Api.AuditLogRequest logRequest, string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange, int maxResponseLogItems)
        {
            var documents = (await LoadLogsAsync(logRequest, tenantName, trackName, queryTimeRange, maxResponseLogItems)).ToList();
            var response = new Api.LogResponse
            {
                Items = documents.Select(ToApiLogItem).ToList(),
                ResponseTruncated = documents.Count >= maxResponseLogItems
            };
            return response;
        }

        private Api.LogItem ToApiLogItem(OpenSearchEventLogItem item)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AddValue(values, Constants.Logs.Results.Name, item.Message);

            AddValue(values, nameof(item.MachineName), item.MachineName);
            AddValue(values, nameof(item.ClientIP), item.ClientIP);
            AddValue(values, nameof(item.SessionId), item.SessionId);
            AddValue(values, nameof(item.UserAgent), item.UserAgent);
            AddValue(values, nameof(item.UpPartyId), item.UpPartyId);
            AddValue(values, nameof(item.TenantName), item.TenantName);
            AddValue(values, nameof(item.TrackName), item.TrackName);
            AddValue(values, nameof(item.UserId), item.UserId);
            AddValue(values, nameof(item.Email), item.Email);
            AddValue(values, nameof(item.AuditType), item.AuditType);
            AddValue(values, nameof(item.AuditAction), item.AuditAction);
            AddValue(values, nameof(item.AuditDataAction), item.AuditDataAction);
            AddValue(values, nameof(item.DocumentId), item.DocumentId);
            AddValue(values, nameof(item.Data), item.Data, false);

            return new Api.LogItem
            {
                Type = Api.LogItemTypes.Event,
                Timestamp = item.Timestamp.ToUnixTimeSeconds(),
                SequenceId = item.SequenceId,
                OperationId = item.OperationId,
                Values = values
            };
        }

        private async Task<IEnumerable<OpenSearchEventLogItem>> LoadLogsAsync(Api.AuditLogRequest logRequest, string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange, int maxResponseLogItems)
        {
            var response = await openSearchClient.SearchAsync<OpenSearchEventLogItem>(s => s
                .Index(Indices.Index(GetIndexNames()))
                    .Size(maxResponseLogItems)
                    .Sort(sort => sort.Descending(f => f.Timestamp))
                    .Query(q => q.Bool(b => GetQuery(b, logRequest, tenantName, trackName, queryTimeRange)))
            );

            return response.Documents;
        }

        private IEnumerable<string> GetIndexNames()
        {
            foreach (var name in GetIndexBaseNames())
            {
                yield return name;
            }

            if (settings.OpenSearchQuery != null && !string.IsNullOrWhiteSpace(settings.OpenSearchQuery?.CrossClusterSearchClusterName))
            {
                foreach (var name in GetIndexBaseNames())
                {
                    yield return $"{settings.OpenSearchQuery.CrossClusterSearchClusterName}:{name}";
                }
            }
        }

        private IEnumerable<string> GetIndexBaseNames()
        {
            yield return $"{settings.OpenSearch.LogName}*";
        }

        private IBoolQuery GetQuery(BoolQueryDescriptor<OpenSearchEventLogItem> boolQuery, Api.AuditLogRequest logRequest, string tenantName, string trackName, (DateTime start, DateTime end) queryTimeRange)
        {
            boolQuery = boolQuery.Filter(f => f.DateRange(dt => dt.Field(field => field.Timestamp)
                                     .GreaterThanOrEquals(queryTimeRange.start)
                                     .LessThanOrEquals(queryTimeRange.end)));

            boolQuery = boolQuery.Must(m =>
                m.Exists(e => e.Field(f => f.AuditType)) && 
                m.Term(t => t.TenantName, tenantName) &&
                m.Term(t => t.TrackName, trackName) &&
                m.Term(t => t.LogType, LogTypes.Event.ToString()) &&
                m.MultiMatch(ma => ma
                    .Fields(fs => fs
                        .Field(f => f.Message)
                        .Field(f => f.AuditType)
                        .Field(f => f.AuditAction)
                        .Field(f => f.AuditDataAction)
                        .Field(f => f.DocumentId)
                        .Field(f => f.UserId)
                        .Field(f => f.Email)
                        .Field(f => f.TenantName)
                        .Field(f => f.TrackName)
                        .Field(f => f.Data)
                    )
                    .Query(MapSearchText(logRequest))));

            return boolQuery;
        }

        private static string MapSearchText(Api.AuditLogRequest logRequest)
        {
            var filter = logRequest.Filter;
            if (filter.IsNullOrWhiteSpace())
            {
                return filter;
            }

            if (filter.Contains("Change password", StringComparison.OrdinalIgnoreCase) && !filter.Contains("ChangePassword", StringComparison.Ordinal))
            {
                filter = string.Concat(filter, " ChangePassword");
            }

            if (filter.Contains("Create user", StringComparison.OrdinalIgnoreCase) && !filter.Contains("CreateUser", StringComparison.Ordinal))
            {
                filter = string.Concat(filter, " CreateUser");
            }

            return filter;
        }

        private void AddValue(IDictionary<string, string> values, string key, string value, bool truncate = true)
        {
            if (value.IsNullOrWhiteSpace())
            {
                return;
            }

            var finalValue = truncate && value.Length > Constants.Logs.Results.PropertiesValueMaxLength
                ? $"{value.Substring(0, Constants.Logs.Results.PropertiesValueMaxLength)}..."
                : value;

            values[key] = finalValue;
        }
    }
}
