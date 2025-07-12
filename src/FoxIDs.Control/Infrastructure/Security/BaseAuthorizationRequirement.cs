using ITfoxtec.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxIDs.Infrastructure.Security
{
    public abstract class BaseAuthorizationRequirement<Tar, Tsc> : AuthorizationHandler<Tar>, IAuthorizationRequirement where Tar : IAuthorizationRequirement where Tsc : BaseScopeAuthorizeAttribute
    {
        private static Regex accessToTracksRegex = new Regex(@"((?::track\[(?<track>[\w-]+)\])|(?::track\[(?<trackget>[\w-]+)\].read)|(?::track\[(?<trackpost>[\w-]+)\].create)|(?::track\[(?<trackput>[\w-]+)\].update)|(?::track\[(?<trackdelete>[\w-]+)\].delete))$", RegexOptions.Compiled);
        protected bool supportAccessToTracks = false;

        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, Tar requirement)
        {
            if (context.User?.Identity?.IsAuthenticated == true && context.Resource is HttpContext httpContext)
            {
                var scopedLogger = httpContext.RequestServices.GetService<TelemetryScopedLogger>();
                try
                {
                    var executingEnpoint = httpContext.GetEndpoint();
                    var scopeAuthorizeAttribute = executingEnpoint.Metadata.OfType<Tsc>().FirstOrDefault();
                    if (scopeAuthorizeAttribute == null)
                    {
                        throw new Exception($"Scope authorize attribute '{typeof(Tsc)}' is null");
                    }
                    else
                    {
                        var userScopes = context.User.Claims.Where(c => string.Equals(c.Type, JwtClaimTypes.Scope, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).FirstOrDefault().ToSpaceList();
                        var userRoles = context.User.Claims.Where(c => string.Equals(c.Type, JwtClaimTypes.Role, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).ToList();
                        if (userScopes?.Count() > 0 && userRoles?.Count > 0)
                        {
                            var accessToTrackNames = GetAccessToTracks(userScopes, userRoles, httpContext.Request?.Method);
                            (var acceptedScopes, var acceptedRoles) = GetAcceptedScopesAndRoles(scopeAuthorizeAttribute.Segments, httpContext.GetRouteBinding().TrackName, httpContext.Request?.Method, accessToTrackNames);

                            if (userScopes.Where(us => acceptedScopes.Any(s => s.Equals(us, StringComparison.Ordinal))).Any() && userRoles.Where(ur => acceptedRoles.Any(r => r.Equals(ur, StringComparison.Ordinal))).Any())
                            {
                                AddAccessToTracksRequestItems(httpContext, userScopes.Where(us => acceptedScopes.Any(s => s.Equals(us, StringComparison.Ordinal))), userRoles.Where(ur => acceptedRoles.Any(r => r.Equals(ur, StringComparison.Ordinal))), httpContext.Request?.Method);
                                context.Succeed(requirement);
                            }
                            else
                            {
                                scopedLogger.ScopeTrace(() => $"Control API, Users scope '{(userScopes != null ? string.Join(", ", userScopes) : string.Empty)}' and role '{(userRoles != null ? string.Join(", ", userRoles) : string.Empty)}'.");
                                scopedLogger.ScopeTrace(() => $"Control API, Accepted scope '{(acceptedScopes != null ? string.Join(", ", acceptedScopes) : string.Empty)}'.");
                                scopedLogger.ScopeTrace(() => $"Control API, Accepted role '{(acceptedRoles != null ? string.Join(", ", acceptedRoles) : string.Empty)}'.");
                                throw new Exception("Users scope and role not accepted.");
                            }
                        }
                        else
                        {
                            throw new Exception("Users scope or role is empty.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    scopedLogger.Error(ex, "Control API access denied.");
                }
            }

            return Task.CompletedTask;
        }

        protected abstract (List<string> acceptedScopes, List<string> acceptedRoles) GetAcceptedScopesAndRoles(IEnumerable<string> segments, string trackName, string httpMethod, IEnumerable<string> accessToTrackNames = null);

        protected void AddScopeAndRole(List<string> acceptedScopes, List<string> acceptedRoles, string httpMethod, string scope, string role, string segment = "")
        {
            acceptedScopes.Add(scope);
            acceptedRoles.Add(role);

            if (segment != Constants.ControlApi.Segment.Usage)
            {
                if (httpMethod == HttpMethod.Get.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Read}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Read}");
                }
                else if (httpMethod == HttpMethod.Post.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Create}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Create}");

                }
                else if (httpMethod == HttpMethod.Put.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Update}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Update}");

                }
                else if (httpMethod == HttpMethod.Delete.Method)
                {
                    acceptedScopes.Add($"{scope}{Constants.ControlApi.AccessElement.Delete}");
                    acceptedRoles.Add($"{role}{Constants.ControlApi.AccessElement.Delete}");
                }
            }
        }

        private IEnumerable<string> GetAccessToTracks(IEnumerable<string> userScopes, IEnumerable<string> userRoles, string httpMethod)
        {
            if (!supportAccessToTracks)
            {
                return null;
            }

            var accessToTrackNames = new List<string>();
            accessToTrackNames.AddRange(GetAccessToTracks(userScopes, httpMethod));
            accessToTrackNames.ConcatOnce(GetAccessToTracks(userRoles, httpMethod));
            return accessToTrackNames;
        }

        private IEnumerable<string> GetAccessToTracks(IEnumerable<string> userScopesOrRole, string httpMethod)
        {
            foreach (var item in userScopesOrRole)
            {
                var itemMatch = accessToTracksRegex.Match(item);
                if (itemMatch.Success)
                {
                    if (itemMatch.Groups["track"].Success)
                    {
                        yield return itemMatch.Groups["track"].Value;
                    }

                    if (httpMethod == HttpMethod.Get.Method)
                    {
                        if (itemMatch.Groups["trackget"].Success)
                        {
                            yield return itemMatch.Groups["trackget"].Value;
                        }
                    }
                    else if (httpMethod == HttpMethod.Post.Method)
                    {
                        if (itemMatch.Groups["trackpost"].Success)
                        {
                            yield return itemMatch.Groups["trackpost"].Value;
                        }

                    }
                    else if (httpMethod == HttpMethod.Put.Method)
                    {
                        if (itemMatch.Groups["trackput"].Success)
                        {
                            yield return itemMatch.Groups["trackput"].Value;
                        }

                    }
                    else if (httpMethod == HttpMethod.Delete.Method)
                    {
                        if (itemMatch.Groups["trackdelete"].Success)
                        {
                            yield return itemMatch.Groups["trackdelete"].Value;
                        }
                    }
                }
            }
        }

        private void AddAccessToTracksRequestItems(HttpContext httpContext, IEnumerable<string> accessUserScopes, IEnumerable<string> accessUserRoles, string httpMethod)
        {
            if (!supportAccessToTracks)
            {
                return;
            }

            var scopesGrantAccessToAnyTrack = !accessToTracksRegex.Match(accessUserScopes.First()).Success;
            var rolesGrantAccessToAnyTrack = !accessToTracksRegex.Match(accessUserRoles.First()).Success;

            if (scopesGrantAccessToAnyTrack && rolesGrantAccessToAnyTrack)
            {
                httpContext.Items[Constants.ControlApi.AccessToAnyTrackKey] = true;
                return;
            }

            var scopesGrantAccessToTrackNames = GetAccessToTracks(accessUserScopes, httpMethod);
            var rolesGrantAccessToTrackNames = GetAccessToTracks(accessUserRoles, httpMethod);

            var accessToTracks = GetLimitedGrantedAccessToTracks(scopesGrantAccessToAnyTrack, rolesGrantAccessToAnyTrack, scopesGrantAccessToTrackNames, rolesGrantAccessToTrackNames);
            if (accessToTracks.Count() > 0)
            {
                httpContext.Items[Constants.ControlApi.AccessToTrackNamesKey] = accessToTracks;
            }
        }

        private IEnumerable<string> GetLimitedGrantedAccessToTracks(bool scopesGrantAccessToAnyTrack, bool rolesGrantAccessToAnyTrack, IEnumerable<string> scopesGrantAccessToTrackNames, IEnumerable<string> rolesGrantAccessToTrackNames)
        {
            if (scopesGrantAccessToAnyTrack)
            {
                return rolesGrantAccessToTrackNames;
            }
            else if (rolesGrantAccessToAnyTrack)
            {
                return scopesGrantAccessToTrackNames;
            }
            else
            {
                return scopesGrantAccessToTrackNames.Where(us => rolesGrantAccessToTrackNames.Any(s => s.Equals(us, StringComparison.Ordinal)));
            }
        }
    }
}
