using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FoxIDs.Logic;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FoxIDs.Controllers
{
    public abstract class HealthControllerBase : Controller
    {
        private static readonly Dictionary<string, string> ComponentAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["database"] = HealthComponent.Database,
            ["db"] = HealthComponent.Database,
            ["data"] = HealthComponent.Database,
            ["log"] = HealthComponent.Log,
            ["logs"] = HealthComponent.Log,
            ["logging"] = HealthComponent.Log,
            ["cache"] = HealthComponent.Cache,
        };

        private static class HealthComponent
        {
            public const string Database = "database";
            public const string Log = "log";
            public const string Cache = "cache";
        }

        private static readonly string[] ComponentOrder =
        {
            HealthComponent.Database,
            HealthComponent.Log,
            HealthComponent.Cache
        };

        private readonly HealthCheckLogic healthCheckLogic;

        protected HealthControllerBase(HealthCheckLogic healthCheckLogic)
        {
            this.healthCheckLogic = healthCheckLogic;
        }

        protected async Task<IActionResult> HandleHealthAsync()
        {
            var (components, includeAll, invalidTokens) = ParseRequestedComponents();
            var supportedTargets = GetSupportedTargets().ToList();

            var invalid = invalidTokens.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (invalid.Length > 0)
            {
                return JsonContent(new
                {
                    status = "Invalid",
                    message = $"Unsupported health check target(s): {string.Join(", ", invalid)}",
                    unsupportedTargets = invalid,
                    supportedTargets
                }, StatusCodes.Status400BadRequest);
            }

            if (components.Count == 0 && !includeAll)
            {
                return JsonContent(new
                {
                    status = HealthCheckStatus.Healthy,
                    checkedAt = DateTimeOffset.UtcNow,
                    checks = Array.Empty<HealthCheckResult>()
                }, StatusCodes.Status200OK);
            }

            if (includeAll)
            {
                components.UnionWith(supportedTargets);
            }

            var unavailableComponents = components.Where(component => !IsComponentAvailable(component)).ToArray();
            if (unavailableComponents.Length > 0)
            {
                return JsonContent(new
                {
                    status = "Invalid",
                    message = $"Health check target{(unavailableComponents.Count() > 1 ? "s" : "")} '{string.Join(", ", unavailableComponents)}' not available in current configuration.",
                    unsupportedTargets = unavailableComponents,
                    supportedTargets
                }, StatusCodes.Status400BadRequest);
            }

            var checks = new List<HealthCheckResult>();
            var cancellationToken = HttpContext?.RequestAborted ?? CancellationToken.None;

            foreach (var component in components
                .OrderBy(GetOrderIndex)
                .ThenBy(component => component, StringComparer.OrdinalIgnoreCase))
            {
                HealthCheckResult result = component switch
                {
                    HealthComponent.Database => await healthCheckLogic.CheckDatabaseAsync(cancellationToken),
                    HealthComponent.Log => await healthCheckLogic.CheckLogAsync(cancellationToken),
                    HealthComponent.Cache => await healthCheckLogic.CheckCacheAsync(cancellationToken),
                    _ => HealthCheckResult.Skipped(component, "Component not recognized.")
                };

                checks.Add(result);
            }

            var unhealthy = checks.Any(r => r.Status == HealthCheckStatus.Unhealthy);
            var response = new
            {
                status = unhealthy ? HealthCheckStatus.Unhealthy : HealthCheckStatus.Healthy,
                checkedAt = DateTimeOffset.UtcNow,
                checks
            };

            if (unhealthy)
            {
                return JsonContent(response, StatusCodes.Status503ServiceUnavailable);
            }

            return JsonContent(response, StatusCodes.Status200OK);
        }

        private (HashSet<string> Components, bool IncludeAll, List<string> InvalidTokens) ParseRequestedComponents()
        {
            var components = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var invalidTokens = new List<string>();
            var includeAll = false;

            var query = Request?.Query;
            if (query == null || query.Count == 0)
            {
                return (components, includeAll, invalidTokens);
            }

            foreach (var kvp in query)
            {
                var key = kvp.Key;

                if (!string.IsNullOrWhiteSpace(key))
                {
                    if (string.Equals(key, "all", StringComparison.OrdinalIgnoreCase))
                    {
                        includeAll = true;
                    }
                    else if (TryResolveComponent(key, out var componentFromKey))
                    {
                        components.Add(componentFromKey);
                    }
                }
            }

            return (components, includeAll, invalidTokens);
        }

        private static bool TryResolveComponent(string token, out string component)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                component = null;
                return false;
            }

            return ComponentAliases.TryGetValue(token.Trim(), out component);
        }

        private static int GetOrderIndex(string component)
        {
            var index = Array.IndexOf(ComponentOrder, component);
            return index >= 0 ? index : int.MaxValue;
        }

        private IEnumerable<string> GetSupportedTargets()
        {
            yield return HealthComponent.Database;

            if (healthCheckLogic.CanCheckLog)
            {
                yield return HealthComponent.Log;
            }

            if (healthCheckLogic.CanCheckCache)
            {
                yield return HealthComponent.Cache;
            }
        }

        private bool IsComponentAvailable(string component) =>
            component switch
            {
                HealthComponent.Database => healthCheckLogic.CanCheckDatabase,
                HealthComponent.Log => healthCheckLogic.CanCheckLog,
                HealthComponent.Cache => healthCheckLogic.CanCheckCache,
                _ => false
            };

        private static ContentResult JsonContent(object payload, int statusCode) =>
            new()
            {
                StatusCode = statusCode,
                ContentType = "application/json",
                Content = JsonConvert.SerializeObject(payload, Formatting.Indented)
            };
    }
}
