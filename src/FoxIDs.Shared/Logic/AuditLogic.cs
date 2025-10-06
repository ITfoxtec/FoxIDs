using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Config;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace FoxIDs.Logic
{
    public class AuditLogic : LogicBase
    {
        private const string sensitiveValueMask = "****";
        private static readonly string[] sensitivePropertyMatches = ["secret", "client_secret", "hash", "hash_salt", "code_verifier", "nonce", "key"];
        private static JsonSerializerSettings newtonsoftJsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
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

        private JToken ConvertToJsonNode<T>(T data) where T : IDataDocument
        {
            if (data == null)
            {
                return null;
            }

            var json = JsonConvert.SerializeObject(data, newtonsoftJsonSettings);
            var token = JToken.Parse(json);
            return NormalizeToken(token);
        }

        private JObject GetDiffNodeResult<T>(T before, T after) where T : IDataDocument
        {
            var beforeNode = ConvertToJsonNode(before);
            var afterNode = ConvertToJsonNode(after);
            var diff = GetDiffNode(beforeNode, afterNode);
            MaskSensitiveValues(diff);
            return diff;    
        }

        private JObject GetDiffNode(JToken beforeNode, JToken afterNode)
        {
            if (beforeNode == null && afterNode == null)
            {
                return null;
            }

            if (beforeNode == null || afterNode == null)
            {
                var diff = new JObject();
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

            if (beforeNode.Type == JTokenType.Object && afterNode.Type == JTokenType.Object)
            {
                var beforeObj = (JObject)beforeNode;
                var afterObj = (JObject)afterNode;
                var diff = new JObject();
                var propertyNames = beforeObj.Properties().Select(p => p.Name)
                    .Union(afterObj.Properties().Select(p => p.Name));
                foreach (var name in propertyNames)
                {
                    var childDiff = GetDiffNode(beforeObj[name], afterObj[name]);
                    if (childDiff != null)
                    {
                        diff[name] = childDiff;
                    }
                }
                return diff.HasValues ? diff : null;
            }

            if (beforeNode.Type == JTokenType.Array && afterNode.Type == JTokenType.Array)
            {
                if (JToken.DeepEquals(beforeNode, afterNode))
                {
                    return null;
                }

                return new JObject
                {
                    ["before"] = beforeNode.DeepClone(),
                    ["after"] = afterNode.DeepClone()
                };
            }

            if (JToken.DeepEquals(beforeNode, afterNode))
            {
                return null;
            }

            return new JObject
            {
                ["before"] = beforeNode.DeepClone(),
                ["after"] = afterNode.DeepClone()
            };
        }

        private static JToken NormalizeToken(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            return token.Type switch
            {
                JTokenType.Object => NormalizeObject((JObject)token),
                JTokenType.Array => NormalizeArray((JArray)token),
                _ => token.DeepClone()
            };
        }

        private static JObject NormalizeObject(JObject obj)
        {
            var normalized = new JObject();

            foreach (var property in obj.Properties().OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                normalized[property.Name] = NormalizeToken(property.Value);
            }

            return normalized;
        }

        private static JArray NormalizeArray(JArray array)
        {
            var normalized = new JArray();

            foreach (var item in array)
            {
                normalized.Add(NormalizeToken(item));
            }

            return normalized;
        }

        private IDictionary<string, string> BuildDataLogProperties<T>(AuditTypes auditType, AuditDataActions dataAction, JObject diff, string documentId)
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
            if (diff != null && diff.HasValues)
            {
                properties[Constants.Logs.Data] = diff.ToString(Formatting.None);
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

        private void MaskSensitiveValues(JToken token)
        {
            if (token == null)
            {
                return;
            }

            switch (token.Type)
            {
                case JTokenType.Object:
                    var obj = (JObject)token;
                    foreach (var property in obj.Properties().ToList())
                    {
                        if (IsSensitiveProperty(property.Name))
                        {
                            property.Value = JValue.CreateString(sensitiveValueMask);
                        }
                        else
                        {
                            MaskSensitiveValues(property.Value);
                        }
                    }
                    break;
                case JTokenType.Array:
                    foreach (var item in token.Children())
                    {
                        MaskSensitiveValues(item);
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
                if (propertyName.Equals(match, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}