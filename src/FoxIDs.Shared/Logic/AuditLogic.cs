using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;

namespace FoxIDs.Logic
{
    public class AuditLogic : LogicBase
    {
        private const string Mask = "****";

        private readonly TelemetryLogger telemetryLogger;
        private readonly JsonSerializerOptions jsonOptions;

        public AuditLogic(TelemetryLogger telemetryLogger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.telemetryLogger = telemetryLogger;
            jsonOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            };
        }

        public bool ShouldAudit()
        {
            var httpContext = HttpContext;
            if (httpContext?.Items == null)
            {
                return false;
            }

            if (httpContext.User?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            return httpContext.Items.ContainsKey(Constants.ControlApi.AuditLogEnabledKey);
        }

        public Task LogAsync<T>(AuditAction action, T before, T after, string partitionId, string documentId) where T : MasterDocument
        {
            if (!ShouldAudit())
            {
                return Task.CompletedTask;
            }

            try
            {
                var properties = BuildProperties(typeof(T).Name, action, GetDiffNode(before, after), partitionId, documentId);
                telemetryLogger.Event($"Audit '{action}'", properties);
            }
            catch (Exception ex)
            {
                telemetryLogger.Warning(ex, message: $"Audit '{action}' master document logging failed.");
            }

            return Task.CompletedTask;
        }

        public Task LogAsync<T>(AuditAction action, T before, T after, string partitionId, string documentId, TelemetryScopedLogger scopedLogger) where T : IDataDocument
        {
            if (!ShouldAudit())
            {
                return Task.CompletedTask;
            }

            try
            {
                var properties = BuildProperties(typeof(T).Name, action, GetDiffNode(before, after), partitionId, documentId);
                scopedLogger.Event($"Audit '{action}'", properties: properties);
            }
            catch (Exception ex)
            {
                scopedLogger.Warning(ex, message: $"Audit '{action}' tenant document logging failed.");
            }

            return Task.CompletedTask;
        }

        private JsonNode ConvertToJsonNode<T>(T data) where T : IDataDocument
        {
            if (data == null)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(data, jsonOptions);
            var node = JsonNode.Parse(json);
            MaskSensitiveValues(node);
            return node;
        }

        private JsonObject GetDiffNode<T>(T before, T after) where T : IDataDocument
        {
            var beforeNode = ConvertToJsonNode(before);
            var afterNode = ConvertToJsonNode(after);
            return GetDiffNode(beforeNode, afterNode);
        }
        
        private JsonObject GetDiffNode(JsonNode beforeNode, JsonNode afterNode)
        {
            if (beforeNode == null && afterNode == null)
            {
                return null;
            }

            if (beforeNode == null || afterNode == null)
            {
                var diff = new JsonObject();
                if (beforeNode != null)
                {
                    diff["before"] = beforeNode.DeepClone();
                }
                if (afterNode != null)
                {
                    diff["after"] = afterNode.DeepClone();
                }
                return diff;
            }

            if (beforeNode is JsonObject beforeObj && afterNode is JsonObject afterObj)
            {
                var diff = new JsonObject();
                var propertyNames = beforeObj.Select(p => p.Key).Union(afterObj.Select(p => p.Key));
                foreach (var name in propertyNames)
                {
                    var childDiff = GetDiffNode(beforeObj[name], afterObj[name]);
                    if (childDiff != null)
                    {
                        diff[name] = childDiff;
                    }
                }
                return diff.Count > 0 ? diff : null;
            }

            if (beforeNode is JsonArray beforeArray && afterNode is JsonArray afterArray)
            {
                if (JsonNode.DeepEquals(beforeArray, afterArray))
                {
                    return null;
                }

                return new JsonObject
                {
                    ["before"] = beforeArray.DeepClone(),
                    ["after"] = afterArray.DeepClone()
                };
            }

            if (JsonNode.DeepEquals(beforeNode, afterNode))
            {
                return null;
            }

            return new JsonObject
            {
                ["before"] = beforeNode.DeepClone(),
                ["after"] = afterNode.DeepClone()
            };
        }

        private IDictionary<string, string> BuildProperties(string auditTypeName, AuditAction action, JsonObject diff, string partitionId, string documentId)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Constants.Logs.AuditType, auditTypeName },
                { Constants.Logs.AuditAction, action.ToString() }
            };

            var user = HttpContext.User;
            AddProperty(properties,  Constants.Logs.Results.UserId, user.FindFirstValue(JwtClaimTypes.Subject));
            AddProperty(properties,  Constants.Logs.Results.Email, user.FindFirstValue(JwtClaimTypes.Email));

            AddProperty(properties,  Constants.Logs.Results.DocumentId, documentId);
            AddProperty(properties, Constants.Logs.Results.PartitionId, partitionId);
            if (diff != null && diff.Count > 0)
            {
                properties[Constants.Logs.Results.Changes] = diff.ToJsonString(jsonOptions);
            }

            var routeBinding = RouteBinding;
            if (routeBinding != null)
            {
                AddProperty(properties, Constants.Logs.TenantName, routeBinding.TenantName);
                AddProperty(properties, Constants.Logs.TrackName, routeBinding.TrackName);
            }

            return properties;
        }

        private static void AddProperty(IDictionary<string, string> properties, string key, string value)
        {
            if (!value.IsNullOrWhiteSpace())
            {
                properties[key] = value;
            }
        }

        private void MaskSensitiveValues(JsonNode node)
        {
            if (node == null)
            {
                return;
            }

            switch (node)
            {
                case JsonObject obj:
                    foreach (var property in obj.ToList())
                    {
                        if (IsSensitiveProperty(property.Key))
                        {
                            obj[property.Key] = JsonValue.Create(Mask);
                        }
                        else
                        {
                            MaskSensitiveValues(property.Value);
                        }
                    }
                    break;
                case JsonArray array:
                    for (var i = 0; i < array.Count; i++)
                    {
                        MaskSensitiveValues(array[i]);
                    }
                    break;
            }
        }

        private static bool IsSensitiveProperty(string propertyName)
        {
            return !propertyName.IsNullOrWhiteSpace() && (propertyName.Contains("password", StringComparison.OrdinalIgnoreCase) || propertyName.Contains("secret", StringComparison.OrdinalIgnoreCase));
        }
    }

    public enum AuditAction
    {
        Create,
        Update,
        Save,
        Delete
    }
}
