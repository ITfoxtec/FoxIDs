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
            AddValue(values, Constants.Logs.Message, item.Message);
            AddValue(values, Constants.Logs.AuditType, item.AuditType);
            AddValue(values, Constants.Logs.AuditDataType, item.AuditDataType);
            AddValue(values, Constants.Logs.AuditDataAction, item.AuditDataAction);
            AddValue(values, Constants.Logs.DocumentId, item.DocumentId);
            AddValue(values, Constants.Logs.PartitionId, item.PartitionId);
            AddValue(values, Constants.Logs.Data, item.Data);
            AddValue(values, Constants.Logs.UserId, item.UserId);
            AddValue(values, Constants.Logs.Email, item.Email);
            AddValue(values, Constants.Logs.RequestPath, item.RequestPath);
            AddValue(values, Constants.Logs.RequestMethod, item.RequestMethod);
            AddValue(values, Constants.Logs.TenantName, item.TenantName);
            AddValue(values, Constants.Logs.TrackName, item.TrackName);

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
            foreach (var name in GetIndexBaseNames()) { yield return name; }

            if (settings.OpenSearchQuery != null && !string.IsNullOrWhiteSpace(settings.OpenSearchQuery?.CrossClusterSearchClusterName))
            {
                foreach (var name in GetIndexBaseNames()) { yield return $"{settings.OpenSearchQuery.CrossClusterSearchClusterName}:{name}"; }
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

            boolQuery = boolQuery.Filter(f => f.Exists(e => e.Field(fld => fld.AuditDataAction)));
            boolQuery = boolQuery.Must(m => m.Term(t => t.LogType, LogTypes.Event.ToString()));

            if (!tenantName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.TenantName, tenantName));
            }

            if (!trackName.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.TrackName, trackName));
            }

            if (!logRequest.AuditType.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.AuditType, logRequest.AuditType));
            }

            if (!logRequest.AuditDataType.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.AuditDataType, logRequest.AuditDataType));
            }

            if (!logRequest.AuditDataAction.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.AuditDataAction, logRequest.AuditDataAction));
            }

            if (!logRequest.DocumentId.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.DocumentId, logRequest.DocumentId));
            }

            if (!logRequest.PartitionId.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Term(t => t.PartitionId, logRequest.PartitionId));
            }

            if (!logRequest.Data.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.Match(match => match.Field(f => f.Data).Query(logRequest.Data)));
            }

            if (!logRequest.Filter.IsNullOrWhiteSpace())
            {
                boolQuery = boolQuery.Must(m => m.MultiMatch(ma => ma
                    .Fields(fs => fs
                        .Field(f => f.Message)
                        .Field(f => f.AuditType)
                        .Field(f => f.AuditDataType)
                        .Field(f => f.AuditDataAction)
                        .Field(f => f.DocumentId)
                        .Field(f => f.PartitionId)
                        .Field(f => f.UserId)
                        .Field(f => f.Email)
                        .Field(f => f.RequestPath)
                        .Field(f => f.RequestMethod)
                        .Field(f => f.TenantName)
                        .Field(f => f.TrackName)
                        .Field(f => f.Data)
                    )
                    .Query(logRequest.Filter)));
            }

            return boolQuery;
        }

        private void AddValue(IDictionary<string, string> values, string key, string value)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                value = value.Length > Constants.Logs.Results.PropertiesValueMaxLength ? $"{value.Substring(0, Constants.Logs.Results.PropertiesValueMaxLength)}..." : value;
                values[key] = value;
            }
        }
    }
}
