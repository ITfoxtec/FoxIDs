using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FoxIDs.Logic
{
    public class AuditLogic : LogicBase
    {
        private const string sensitiveValueMask = "****";
        private static readonly string[] sensitivePropertyMatches = ["password", "secret", "client_secret", "code_verifier", "nonce", "key", "cert", "certificate"];
        private static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReferenceHandler = ReferenceHandler.IgnoreCycles,
            WriteIndented = false
        };
        private readonly Settings settings;
        private readonly TelemetryLogger telemetryLogger;

        public AuditLogic(Settings settings, TelemetryLogger telemetryLogger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.settings = settings;
            this.telemetryLogger = telemetryLogger;
        }

        public bool ShouldLogAudit()
        {
            if(settings.Options.Log == LogOptions.OpenSearchAndStdoutErrors || settings.Options.Log == LogOptions.ApplicationInsights)
            {
                return true;
            }

            return false;
        }

        public bool ShouldLogAuditData()
        {
            if (!ShouldLogAudit())
            {
                return false;
            }

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

        public void LogDataEvent<T>(AuditDataActions dataAction, T before, T after, string documentId) where T : MasterDocument
        {
            if (!ShouldLogAuditData())
            {
                return;
            }

            try
            {
                var properties = BuildDataLogProperties<T>(AuditTypes.Data, dataAction, GetDiffNodeResult(before, after), documentId);
                telemetryLogger.Event($"System-Level {AuditTypes.Data} {dataAction}", properties);
            }
            catch (Exception ex)
            {
                telemetryLogger.Warning(ex, message: $"System-Level {AuditTypes.Data} {dataAction} master document logging failed.");
            }
        }

        public void LogDataEvent<T>(AuditDataActions dataAction, T before, T after, string documentId, TelemetryScopedLogger scopedLogger) where T : IDataDocument
        {
            if (!ShouldLogAuditData())
            {
                return;
            }

            try
            {
                var properties = BuildDataLogProperties<T>(AuditTypes.Data, dataAction, GetDiffNodeResult(before, after), documentId);
                scopedLogger.Event($"System-Level {AuditTypes.Data} {dataAction}", properties: properties);
            }
            catch (Exception ex)
            {
                scopedLogger.Warning(ex, message: $"System-Level {AuditTypes.Data} {dataAction} tenant document logging failed.");
            }
        }

        public void LogLoginEvent(PartyTypes partyType, string upPartyId, List<Claim> claims)
        {
            if (!ShouldLogAudit())
            {
                return;
            }

            try
            {
                var properties = BuildUserActionProperties(AuditTypes.Login, upPartyId, partyType, claims);
                telemetryLogger.Event($"{AuditTypes.Login} action in {partyType} up-party", properties: properties);
            }
            catch (Exception ex)
            {
                telemetryLogger.Warning(ex, message: $"{AuditTypes.Login} {partyType} event logging failed.");
            }
        }

        private JsonNode ConvertToJsonNode<T>(T data) where T : IDataDocument
        {
            if (data == null)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(data, jsonOptions);
            var node = JsonNode.Parse(json);
            return NormalizeNode(node);
        }

        private JsonObject GetDiffNodeResult<T>(T before, T after) where T : IDataDocument
        {
            var beforeNode = ConvertToJsonNode(before);
            var afterNode = ConvertToJsonNode(after);
            var diff = GetDiffNode(beforeNode, afterNode);
            MaskSensitiveValues(diff);
            return diff;    
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

        private static JsonNode NormalizeNode(JsonNode node)
        {
            if (node == null)
            {
                return null;
            }

            return node switch
            {
                JsonObject obj => NormalizeObject(obj),
                JsonArray array => NormalizeArray(array),
                _ => node.DeepClone()
            };
        }

        private static JsonObject NormalizeObject(JsonObject obj)
        {
            var normalized = new JsonObject();

            foreach (var property in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                normalized[property.Key] = NormalizeNode(property.Value);
            }

            return normalized;
        }

        private static JsonArray NormalizeArray(JsonArray array)
        {
            var normalized = new JsonArray();

            foreach (var item in array)
            {
                normalized.Add(NormalizeNode(item));
            }

            return normalized;
        }

        private IDictionary<string, string> BuildDataLogProperties<T>(AuditTypes auditType, AuditDataActions dataAction, JsonObject diff, string documentId)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Constants.Logs.AuditType, auditType.ToString() },
                { Constants.Logs.AuditDataAction, dataAction.ToString() },
                { Constants.Logs.AuditAction, typeof(T).Name }
            };

            AddProperty(properties, Constants.Logs.UserId, HttpContext.User.FindFirstValue(JwtClaimTypes.Subject));
            AddProperty(properties, Constants.Logs.Email, HttpContext.User.FindFirstValue(JwtClaimTypes.Email));

            AddProperty(properties, Constants.Logs.DocumentId, documentId);
            if (diff != null && diff.Count > 0)
            {
                properties[Constants.Logs.Data] = diff.ToJsonString(jsonOptions);
            }

            AddTenantTrackProperties(properties);
            return properties;
        }

        private IDictionary<string, string> BuildUserActionProperties(AuditTypes auditType, string upPartyId, PartyTypes partyType, List<Claim> claims)
        {
            var properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { Constants.Logs.AuditType, auditType.ToString() },
                { Constants.Logs.AuditAction, partyType.ToString() },
                { Constants.Logs.UpPartyId, upPartyId }
            };

            AddProperty(properties, Constants.Logs.UserId, claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Subject));
            AddProperty(properties, Constants.Logs.Email, claims.FindFirstOrDefaultValue(c => c.Type == JwtClaimTypes.Email));

            AddTenantTrackProperties(properties);
            return properties;
        }

        private void AddTenantTrackProperties(Dictionary<string, string> properties)
        {
            var routeBinding = RouteBinding;
            if (routeBinding != null)
            {
                AddProperty(properties, Constants.Logs.TenantName, routeBinding.TenantName);
                AddProperty(properties, Constants.Logs.TrackName, routeBinding.TrackName);
            }
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
                            obj[property.Key] = JsonValue.Create(sensitiveValueMask);
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
            if (propertyName.IsNullOrWhiteSpace())
            {
                return false;
            }

            foreach (var match in sensitivePropertyMatches)
            {
                if (propertyName.Contains(match, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}