using AspNetCoreGeneratedDocument;
using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using FoxIDs.Repository;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Schemas;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace FoxIDs.Logic
{
    public class ClaimTransformLogic : LogicSequenceBase
    {
        private readonly TelemetryScopedLogger logger;
        private readonly IServiceProvider serviceProvider;
        private readonly ClaimTransformValidationLogic claimTransformValidationLogic;
        private readonly ExternalClaimsConnectLogic externalClaimsConnectLogic;

        public ClaimTransformLogic(TelemetryScopedLogger logger, IServiceProvider serviceProvider, ClaimTransformValidationLogic claimTransformValidationLogic, ExternalClaimsConnectLogic externalClaimsConnectLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.claimTransformValidationLogic = claimTransformValidationLogic;
            this.externalClaimsConnectLogic = externalClaimsConnectLogic;
        }

        public async Task<List<Claim>> TransformAsync(IEnumerable<ClaimTransform> claimTransforms, IEnumerable<Claim> claims)
        {
            (var outputClaims, _) = await TransformAsync(claimTransforms, claims, null);
            return outputClaims;
        }

        public async Task<(List<Claim> claims, IActionResult actionResult)> TransformAsync(IEnumerable<ClaimTransform> claimTransforms, IEnumerable<Claim> claims, ILoginRequest loginRequest)
        {
            if (claimTransforms == null || !(claimTransforms?.Count() > 0))
            {
                return (new List<Claim>(claims), null);
            }

            claimTransformValidationLogic.ValidateAndPrepareClaimTransforms(claimTransforms);

            // Too much logging logger.ScopeTrace(() => $"Claims transformation, input claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            if (loginRequest == null)
            {
                claimTransforms = claimTransforms.Where(t => t.Task != ClaimTransformTasks.UpPartyAction);
            }
            var orderedClaimTransforms = claimTransforms.OrderBy(t => t.Order);

            (var outputClaims, var actionResult) = await HandleTransformAsync(claimTransforms, AddLocalClaims(claims, loginRequest), loginRequest);

            outputClaims = outputClaims.Where(c => !c.Type.StartsWith(Constants.ClaimTransformClaimTypes.Namespace, StringComparison.Ordinal)).ToList();
            // Too much logging logger.ScopeTrace(() => $"Claims transformation, output claims '{outputClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return (outputClaims, actionResult);
        }

        private List<Claim> AddLocalClaims(IEnumerable<Claim> claims, ILoginRequest loginRequest)
        {
            var localClaims = new List<Claim>(claims);
            if(loginRequest != null)
            {
                localClaims.AddClaim(Constants.ClaimTransformClaimTypes.LoginAction, loginRequest.LoginAction.ToString().ToCamelCase());
                if (!loginRequest.UserId.IsNullOrWhiteSpace())
                {
                    localClaims.AddClaim(Constants.ClaimTransformClaimTypes.UserId, loginRequest.UserId);
                }
                if (loginRequest.MaxAge != null && loginRequest.MaxAge.Value > 0)
                {
                    localClaims.AddClaim(Constants.ClaimTransformClaimTypes.MaxAge, loginRequest.MaxAge.Value.ToString());
                }
                if (!loginRequest.LoginHint.IsNullOrWhiteSpace())
                {
                    localClaims.AddClaim(Constants.ClaimTransformClaimTypes.LoginHint, loginRequest.LoginHint);
                }
                if (loginRequest.Acr != null && loginRequest.Acr.Count() > 0)
                {
                    localClaims.AddClaim(Constants.ClaimTransformClaimTypes.Acr, loginRequest.Acr.ToSpaceList());
                }
            }

            logger.ScopeTrace(() => $"Claims transformation, Local claims '{localClaims.ToFormattedString()}'", traceType: TraceTypes.Claim);

            var claimsWithLocals = new List<Claim>(claims);
            if (localClaims.Count > 0)
            {
                AddOrReplaceClaims(claimsWithLocals, ClaimTransformActions.Replace, localClaims);
            }
            return claimsWithLocals;
        }

        private async Task<(List<Claim> outputClaims, IActionResult actionResult)> HandleTransformAsync(IEnumerable<ClaimTransform> claimTransforms, List<Claim> claims, ILoginRequest loginRequest)
        {
            IActionResult actionResult = null;
            foreach (var claimTransform in claimTransforms)
            {
                try
                {
                    switch (claimTransform.Type)
                    {
                        case ClaimTransformTypes.Constant:
                            ConstantTransformation(claims, claimTransform);
                            break;
                        case ClaimTransformTypes.MatchClaim:
                            actionResult = await MatchClaimTransformationAsync(claims, claimTransform, loginRequest);
                            break;
                        case ClaimTransformTypes.Match:
                            actionResult = await MatchTransformationAsync(claims, claimTransform, loginRequest);
                            break;
                        case ClaimTransformTypes.RegexMatch:
                            actionResult = await RegexMatchTransformationAsync(claims, claimTransform, loginRequest);
                            break;
                        case ClaimTransformTypes.Map:
                            MapTransformation(claims, claimTransform);
                            break;
                        case ClaimTransformTypes.RegexMap:
                            RegexMapTransformation(claims, claimTransform);
                            break;
                        case ClaimTransformTypes.Concatenate:
                            ConcatenateTransformation(claims, claimTransform);
                            break;
                        case ClaimTransformTypes.ExternalClaims:
                            await ExternalClaimsTransformationAsync(claims, claimTransform);
                            break;
                        case ClaimTransformTypes.DkPrivilege:
                            DkPrivilegeTransformation(claims, claimTransform);
                            break;
                        default:
                            throw new NotSupportedException($"Claim transform type '{claimTransform.Type}' not supported.");
                    }
                }
                catch (EndpointException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (claimTransform.Task != null)
                    {
                        throw new Exception($"Claim transform type '{claimTransform.Type}' and task '{claimTransform.Task}' failed.", ex);
                    }
                    else
                    {
                        throw new Exception($"Claim transform type '{claimTransform.Type}' with output claim '{claimTransform.ClaimsOut.First()}' failed.", ex);
                    }
                }

                if (actionResult != null)
                {
                    break;
                }
            }

            return await Task.FromResult((claims, actionResult));
        }

        private async Task<IActionResult> MatchClaimTransformationAsync(List<Claim> claims, ClaimTransform claimTransform, ILoginRequest loginRequest)
        {
            if (claimTransform.Action != ClaimTransformActions.Remove)
            {
                var claimIn = claimTransform.ClaimsIn.Single();
                if (claimTransform.Action == ClaimTransformActions.If || claimTransform.Action == ClaimTransformActions.IfNot)
                {
                    var existClaim = claims.Where(c => c.Type.Equals(claimIn, StringComparison.Ordinal));
                    var exist = existClaim.Any();
                    if (claimTransform.Task == ClaimTransformTasks.LogEvent && exist && claimTransform.Action == ClaimTransformActions.If)
                    {
                        HandleLogEventTask(existClaim.Select(c => c.Value));
                    }
                    else
                    {
                        var doAction = (exist && claimTransform.Action == ClaimTransformActions.If) || (!exist && claimTransform.Action == ClaimTransformActions.IfNot);
                        if (doAction)
                        {
                            return await HandleIfAndIfNotTaskAsync(claims, claimTransform, loginRequest);
                        }
                    }
                }
                else
                {
                    if (claimTransform.Task != null)
                    {
                        var selectUserClaimValue = claims.FindFirstOrDefaultValue(c => c.Type.Equals(claimIn, StringComparison.Ordinal));
                        if (!selectUserClaimValue.IsNullOrWhiteSpace())
                        {
                            await HandleAddReplaceTaskAsync(claims, claimTransform, claimIn, selectUserClaimValue);
                        }
                    }
                    else
                    {
                        var newClaims = new List<Claim>();
                        foreach (var claim in claims)
                        {
                            if (claim.Type.Equals(claimIn, StringComparison.Ordinal))
                            {
                                newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), claimTransform.Transformation));
                            }
                        }

                        if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                        {
                            AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
                        }
                        else if (newClaims.Count() <= 0 && (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot))
                        {
                            AddOrReplaceClaims(claims, claimTransform.Action, new Claim(claimTransform.ClaimsOut.First(), claimTransform.Transformation));
                        }
                    }
                }
            }
            else
            {
                claims.RemoveAll(c => c.Type.Equals(claimTransform.ClaimsOut.First(), StringComparison.Ordinal));
            }
            return null;
        }

        private async Task<IActionResult> MatchTransformationAsync(List<Claim> claims, ClaimTransform claimTransform, ILoginRequest loginRequest)
        {
            if (claimTransform.Action != ClaimTransformActions.Remove)
            {
                var claimIn = claimTransform.ClaimsIn.Single();
                if (claimTransform.Action == ClaimTransformActions.If || claimTransform.Action == ClaimTransformActions.IfNot)
                {
                    var exist = claims.Where(c => c.Type.Equals(claimIn, StringComparison.Ordinal) && c.Value.Equals(claimTransform.Transformation, StringComparison.Ordinal)).Any();
                    var doAction = (exist && claimTransform.Action == ClaimTransformActions.If) || (!exist && claimTransform.Action == ClaimTransformActions.IfNot);
                    if (doAction)
                    {
                        return await HandleIfAndIfNotTaskAsync(claims, claimTransform, loginRequest);
                    }
                }
                else
                {
                    var newClaims = new List<Claim>();
                    foreach (var claim in claims)
                    {
                        if (claim.Type.Equals(claimIn, StringComparison.Ordinal) && claim.Value.Equals(claimTransform.Transformation, StringComparison.Ordinal))
                        {
                            newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), claimTransform.TransformationExtension));
                        }
                    }

                    if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                    {
                        AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
                    }
                    else if (newClaims.Count() <= 0 && (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot))
                    {
                        AddOrReplaceClaims(claims, claimTransform.Action, new Claim(claimTransform.ClaimsOut.First(), claimTransform.TransformationExtension));
                    }
                }
            }
            else
            {
                claims.RemoveAll(c => c.Type.Equals(claimTransform.ClaimsOut.First(), StringComparison.Ordinal) && c.Value.Equals(claimTransform.Transformation, StringComparison.Ordinal));
            }
            return null;
        }

        private async Task<IActionResult> RegexMatchTransformationAsync(List<Claim> claims, ClaimTransform claimTransform, ILoginRequest loginRequest)
        {
            var regex = new Regex(claimTransform.Transformation, RegexOptions.IgnoreCase);

            if (claimTransform.Action != ClaimTransformActions.Remove)
            {
                var claimIn = claimTransform.ClaimsIn.Single();
                if (claimTransform.Action == ClaimTransformActions.If || claimTransform.Action == ClaimTransformActions.IfNot)
                {
                    var exist = claims.Where(c => c.Type.Equals(claimIn, StringComparison.Ordinal) && regex.Match(c.Value).Success).Any();
                    var doAction = (exist && claimTransform.Action == ClaimTransformActions.If) || (!exist && claimTransform.Action == ClaimTransformActions.IfNot);
                    if (doAction)
                    {
                        return await HandleIfAndIfNotTaskAsync(claims, claimTransform, loginRequest);
                    }
                }
                else
                {
                    var newClaims = new List<Claim>();
                    foreach (var claim in claims)
                    {
                        if (claim.Type.Equals(claimIn, StringComparison.Ordinal) && regex.Match(claim.Value).Success)
                        {
                            newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), claimTransform.TransformationExtension));
                        }
                    }

                    if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                    {
                        AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
                    }
                    else if (newClaims.Count() <= 0 && (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot))
                    {
                        AddOrReplaceClaims(claims, claimTransform.Action, new Claim(claimTransform.ClaimsOut.First(), claimTransform.TransformationExtension));
                    }
                }
            }
            else
            {
                claims.RemoveAll(c => c.Type.Equals(claimTransform.ClaimsOut.First(), StringComparison.Ordinal) && regex.Match(c.Value).Success);
            }
            return null;
        }

        private void MapTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaims = new List<Claim>();
            var claimIn = claimTransform.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.Ordinal))
                {
                    newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), claim.Value));
                }
            }

            if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
            {
                AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
            }
            else if (claimTransform.Action == ClaimTransformActions.AddIfNotOut)
            {
                if (!(claims.Where(c => c.Type.Equals(claimTransform.ClaimsOut.First(), StringComparison.Ordinal)).Count() > 0))
                {
                    AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
                }                
            }
        }

        private void RegexMapTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaims = new List<Claim>();
            var regex = new Regex(claimTransform.Transformation, RegexOptions.IgnoreCase);
            var claimIn = claimTransform.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.Ordinal))
                {
                    var match = regex.Match(claim.Value);
                    if (match.Success && match.Groups.ContainsKey("map"))
                    {
                        newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), match.Groups["map"].Value));
                    }
                }
            }

            if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
            {
                AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
            }
            else if (claimTransform.Action == ClaimTransformActions.AddIfNotOut)
            {
                if (!(claims.Where(c => c.Type.Equals(claimTransform.ClaimsOut.First(), StringComparison.Ordinal)).Count() > 0))
                {
                    AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
                }
            }
        }

        private void ConcatenateTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaims = new List<Claim>();
            var addTransformationClaim = false;
            var values = new string[claimTransform.ClaimsIn.Count()];
            int i = 0;
            foreach (var claimIn in claimTransform.ClaimsIn)
            {
                var value = claims.Where(c => c.Type.Equals(claimIn, StringComparison.Ordinal)).Select(c => c.Value).FirstOrDefault();
                if(value != null)
                {
                    addTransformationClaim = true;
                    values[i++] = value;
                }
                else
                {
                    values[i++] = string.Empty;
                }
            }

            if(addTransformationClaim)
            {
                var transformationValue = string.Format(claimTransform.Transformation, values);
                newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), transformationValue));
            }
            AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
        }

        private async Task ExternalClaimsTransformationAsync(List<Claim> claims, ClaimTransform claimTransform)
        {
            var selectedClaims = new List<Claim>();
            if (claimTransform.ClaimsIn.Where(c => c == "*").Any())
            {
                selectedClaims.AddRange(claims);
            }
            else
            {
                foreach (var claimIn in claimTransform.ClaimsIn)
                {
                    var claimsResult = claims.Where(c => c.Type.Equals(claimIn, StringComparison.Ordinal));
                    if (claimsResult.Count() > 0)
                    {
                        selectedClaims.AddRange(claimsResult);
                    }
                }
            }

            var newClaims = selectedClaims.Count() > 0 ? await externalClaimsConnectLogic.GetClaimsAsync(claimTransform, selectedClaims) : new List<Claim>();
            AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
        }

        private void DkPrivilegeTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaims = new List<Claim>();
            var claimIn = claimTransform.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.Ordinal))
                {
                    var privilegesAsString = Encoding.UTF8.GetString(Convert.FromBase64String(claim.Value));
                    logger.ScopeTrace(() => $"Transform claims, DK privilege base64-decoded XML '{privilegesAsString}'", traceType: TraceTypes.Claim);
                    var privilegesXmlDocument = privilegesAsString.ToXmlDocument();

                    var privilegeGroupXmlNodes = privilegesXmlDocument.DocumentElement.SelectNodes("PrivilegeGroup");
                    foreach (XmlNode privilegeGroupXmlNode in privilegeGroupXmlNodes)
                    {
                        var dkPrivilegeGroupResult = new DkPrivilegeGroup();

                        var scope = privilegeGroupXmlNode.Attributes["Scope"]?.Value;
                        if (string.IsNullOrWhiteSpace(scope) || !scope.Contains(':')) 
                        {
                            throw new Exception("DK privilege, invalid / empty XML PrivilegeGroup scope.");
                        }
                        var scopeDataSplitIndex = scope.LastIndexOf(':');
                        var scopeNamespace = scope.Substring(0, scopeDataSplitIndex);
                        var scopeData = scope.Substring(scopeDataSplitIndex + 1);
                        switch (scopeNamespace)
                        {
                            case "urn:dk:gov:saml:cvrNumberIdentifier":
                                dkPrivilegeGroupResult.CvrNumber = scopeData;
                                break;
                            case "urn:dk:gov:saml:productionUnitIdentifier":
                                dkPrivilegeGroupResult.ProductionUnit = scopeData;
                                break;
                            case "urn:dk:gov:saml:seNumberIdentifier":
                                dkPrivilegeGroupResult.SeNumber = scopeData;
                                break;
                            case "urn:dk:gov:saml:cprNumberIdentifier":
                                dkPrivilegeGroupResult.CprNumber = scopeData;
                                break;
                            default:
                                throw new NotSupportedException($"DK privilege, scope namespace '{scopeNamespace}' not supported.");
                        }

                        var constraintXmlNodes = privilegeGroupXmlNode.SelectNodes("Constraint");
                        if (constraintXmlNodes != null && constraintXmlNodes.Count > 0)
                        {
                            dkPrivilegeGroupResult.Constraint = new Dictionary<string, string>();
                            foreach (XmlNode constraintXmlNode in constraintXmlNodes)
                            {
                                var constraintName = constraintXmlNode.Attributes["Name"]?.Value;
                                if (string.IsNullOrWhiteSpace(constraintName))
                                {
                                    throw new Exception("DK privilege, invalid / empty XML Constraint name.");
                                }
                                dkPrivilegeGroupResult.Constraint.Add(constraintName, constraintXmlNode.InnerText);
                            }
                        }

                        var privilegeXmlNodes = privilegeGroupXmlNode.SelectNodes("Privilege");
                        if (privilegeXmlNodes == null || privilegeXmlNodes.Count < 1)
                        {
                            throw new Exception("DK privilege, invalid / empty XML Privilege.");
                        }
                        foreach(XmlNode privilegeXmlNode in privilegeXmlNodes)
                        {
                            dkPrivilegeGroupResult.Privilege.Add(privilegeXmlNode.InnerText);
                        }

                        newClaims.Add(new Claim(claimTransform.ClaimsOut.First(), dkPrivilegeGroupResult.ToJson()));
                    }                    
                }
            }

            AddOrReplaceClaims(claims, claimTransform.Action, newClaims);
        }

        private void HandleLogEventTask(IEnumerable<string> eventMessages) 
        {
            var cleanEventMessages = eventMessages
                .Where(em => !em.IsNullOrWhiteSpace())
                .Distinct()
                .Take(10);

            foreach (var eventMessage in cleanEventMessages)
            {
                logger.Event(eventMessage);
            }   
        }

        private async Task<IActionResult> HandleIfAndIfNotTaskAsync(List<Claim> claims, ClaimTransform claimTransform, ILoginRequest loginRequest)
        {
            switch (claimTransform.Task)
            {
                case ClaimTransformTasks.RequestException:
                    if (claimTransform is SamlClaimTransform)
                    {
                        logger.ScopeTrace(() => $"Claims transformation, Throw '{nameof(SamlRequestException)}' with error '{GetSaml2Status(claimTransform.Error)}' and error message '{claimTransform.ErrorMessage}'.");
                        throw new SamlRequestException(claimTransform.ErrorMessage) { RouteBinding = RouteBinding, Status = GetSaml2Status(claimTransform.Error) };
                    }
                    else
                    {
                        var error = claimTransform.Error.IsNullOrEmpty() ? IdentityConstants.ResponseErrors.InvalidRequest : claimTransform.Error;
                        logger.ScopeTrace(() => $"Claims transformation, Throw '{nameof(OAuthRequestException)}' with error '{error}' and error message '{claimTransform.ErrorMessage}'.");
                        throw new OAuthRequestException(claimTransform.ErrorMessage) { RouteBinding = RouteBinding, Error = error};
                    }
                case ClaimTransformTasks.UpPartyAction:
                    var upPartyLink = new UpPartyLink
                    {
                        Name = claimTransform.UpPartyName,
                        ProfileName = claimTransform.UpPartyProfileName
                    };
                    logger.ScopeTrace(() => $"Claims transformation, Start authentication type '{claimTransform.UpPartyType}' and method '{claimTransform.UpPartyName}'{(claimTransform.UpPartyProfileName.IsNullOrWhiteSpace() ? $" and profile '{claimTransform.UpPartyProfileName}'" : string.Empty)}.");
                    switch (claimTransform.UpPartyType)
                    {
                        case PartyTypes.Login:
                            return await serviceProvider.GetService<LoginUpLogic>().LoginRedirectAsync(upPartyLink, GetLoginRequestWithLocalClaims(claims, loginRequest));
                        case PartyTypes.OAuth2:
                            throw new NotImplementedException();
                        case PartyTypes.Oidc:
                            return await serviceProvider.GetService<OidcAuthUpLogic<OidcUpParty, OidcUpClient>>().AuthenticationRequestRedirectAsync(upPartyLink, GetLoginRequestWithLocalClaims(claims, loginRequest));
                        case PartyTypes.Saml2:
                            return await serviceProvider.GetService<SamlAuthnUpLogic>().AuthnRequestRedirectAsync(upPartyLink, GetLoginRequestWithLocalClaims(claims, loginRequest));
                        case PartyTypes.TrackLink:
                            return await serviceProvider.GetService<TrackLinkAuthUpLogic>().AuthRequestAsync(upPartyLink, GetLoginRequestWithLocalClaims(claims, loginRequest));
                        case PartyTypes.ExternalLogin:
                            return await serviceProvider.GetService<ExternalLoginUpLogic>().LoginRedirectAsync(upPartyLink, GetLoginRequestWithLocalClaims(claims, loginRequest));
                        default:
                            throw new NotSupportedException($"Connection type '{claimTransform.UpPartyType}' not supported.");
                    }

                default:
                    throw new NotSupportedException();
            }
        }

        private Saml2StatusCodes GetSaml2Status(string error)
        {
            if (!error.IsNullOrWhiteSpace() && Enum.TryParse(error, true, out Saml2StatusCodes statusCode)) 
            {
                return statusCode;
            }
            return Saml2StatusCodes.Responder;
        }

        private LoginRequest GetLoginRequestWithLocalClaims(List<Claim> claims, ILoginRequest loginRequest)
        {
            var outputLoginRequest = new LoginRequest(loginRequest);

            var loginActionValue = claims.FindFirstOrDefaultValue(c => c.Type == Constants.ClaimTransformClaimTypes.LoginAction);
            if (!loginActionValue.IsNullOrWhiteSpace() && Enum.TryParse(loginActionValue, true, out LoginAction loginAction))
            {
                outputLoginRequest.LoginAction = loginAction;
            }

            var maxAgeValue = claims.FindFirstOrDefaultValue(c => c.Type == Constants.ClaimTransformClaimTypes.MaxAge);
            if (!maxAgeValue.IsNullOrWhiteSpace() && int.TryParse(maxAgeValue, out int maxAge))
            {
                outputLoginRequest.MaxAge = maxAge;
            }

            var loginHint = claims.FindFirstOrDefaultValue(c => c.Type == Constants.ClaimTransformClaimTypes.LoginHint);
            if (!loginHint.IsNullOrWhiteSpace())
            {
                outputLoginRequest.LoginHint = loginHint.Trim().ToLower();
            }

            var acrValue = claims.FindFirstOrDefaultValue(c => c.Type == Constants.ClaimTransformClaimTypes.Acr);
            if (!acrValue.IsNullOrWhiteSpace())
            {
                outputLoginRequest.Acr = acrValue.ToSpaceList();
            }

            return outputLoginRequest;
        }

        private async Task HandleAddReplaceTaskAsync(List<Claim> claims, ClaimTransform claimTransform, string claimIn, string lookupUserClaimValue)
        {
            var tenantDataRepository = serviceProvider.GetService<ITenantDataRepository>();
            var idKey = new Track.IdKey { TenantName = RouteBinding.TenantName, TrackName = RouteBinding.TrackName };

            switch (claimTransform.Task)
            {
                case ClaimTransformTasks.QueryInternalUser:
                    var user = await FindInternalUserAsync(tenantDataRepository, idKey, claimTransform.Transformation, lookupUserClaimValue);
                    if (user != null)
                    {
                        AddOrReplaceClaims(claims, claimTransform.Action, GetClaims(user, claimIn, claimTransform.Transformation, lookupUserClaimValue, claimTransform.ClaimsOut));
                        return;
                    }
                    break;
                case ClaimTransformTasks.QueryExternalUser:
                    var externalUser = await FindExternalUserAsync(tenantDataRepository, idKey, claimTransform.Transformation, lookupUserClaimValue, claimTransform.UpPartyName);
                    if (externalUser != null)
                    {
                        AddOrReplaceClaims(claims, claimTransform.Action, GetClaims(externalUser, claimIn, claimTransform.Transformation, lookupUserClaimValue, claimTransform.ClaimsOut));
                        return;
                    }
                    break;
                case ClaimTransformTasks.SaveClaimInternalUser:
                    var userUpdateSourceValues = GetUpdateSourceValues(claims, claimTransform.TransformationExtension, isInternalUser: true);
                    if (userUpdateSourceValues != null)
                    {
                        var userSaveClaims = await FindInternalUserAsync(tenantDataRepository, idKey, claimTransform.Transformation, lookupUserClaimValue);
                        if (userSaveClaims != null)
                        {
                            (userSaveClaims.Claims, var isChanged) = UpdateUsersClaimValues(userSaveClaims.Claims, claimTransform.ClaimsOut.First(), userUpdateSourceValues, claimTransform.Action);
                            if (isChanged)
                            {
                                await tenantDataRepository.UpdateAsync(userSaveClaims);
                                logger.ScopeTrace(() => $"Claims transformation, Internal user '{userSaveClaims.UserId}' claim '{claimTransform.ClaimsOut.First()}' was not updated.");
                            }
                        }
                    }
                    break;
                case ClaimTransformTasks.SaveClaimExternalUser:
                    var externalUserUpdateSourceValues = GetUpdateSourceValues(claims, claimTransform.TransformationExtension, isInternalUser: false);
                    if (externalUserUpdateSourceValues != null)
                    {
                        var externalUserSaveClaims = await FindExternalUserAsync(tenantDataRepository, idKey, claimTransform.Transformation, lookupUserClaimValue, claimTransform.UpPartyName);
                        if (externalUserSaveClaims != null)
                        {
                            (externalUserSaveClaims.Claims, var isChanged) = UpdateUsersClaimValues(externalUserSaveClaims.Claims, claimTransform.ClaimsOut.First(), externalUserUpdateSourceValues, claimTransform.Action);
                            if (isChanged)
                            {
                                await tenantDataRepository.UpdateAsync(externalUserSaveClaims);
                                logger.ScopeTrace(() => $"Claims transformation, External user '{externalUserSaveClaims.UserId}' claim '{claimTransform.ClaimsOut.First()}' was not updated.");
                            }
                        }
                    }
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private List<Claim> GetClaims(User user, string claimIn, string selectUserClaim, string selectUserClaimValue, IEnumerable<string> claimTypesOutLimit)
        {
            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, user.UserId);
            if (!user.Email.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.Email, user.Email);
                claims.AddClaim(JwtClaimTypes.EmailVerified, user.EmailVerified.ToString().ToLower());
            }
            if (!user.Phone.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.PhoneNumber, user.Phone);
                claims.AddClaim(JwtClaimTypes.PhoneNumberVerified, user.PhoneVerified.ToString().ToLower());
            }
            if (!user.Username.IsNullOrEmpty() || !user.Email.IsNullOrEmpty())
            {
                claims.AddClaim(JwtClaimTypes.PreferredUsername, !user.Username.IsNullOrEmpty() ? user.Username : user.Email);
            }
            if (user.Claims?.Count() > 0)
            {
                claims.AddRange(user.Claims.ToClaimList());
            }

            if(claimIn.Equals(selectUserClaim, StringComparison.CurrentCulture))
            {
                claims = claims.Where(c => !(c.Type.Equals(selectUserClaim, StringComparison.CurrentCulture) && c.Value.Equals(selectUserClaimValue, StringComparison.CurrentCulture))).ToList();
            }
            claims = LimitClaimsOut(claims, claimTypesOutLimit);
            logger.ScopeTrace(() => $"Claims transformation, Internal users JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return claims;
        }

        private List<Claim> GetClaims(ExternalUser externalUser, string claimIn, string selectUserClaim, string selectUserClaimValue, IEnumerable<string> claimTypesOutLimit)
        {
            var claims = new List<Claim>();
            claims.AddClaim(JwtClaimTypes.Subject, externalUser.UserId);
            if (externalUser.Claims?.Count() > 0)
            {
                claims.AddRange(externalUser.Claims.ToClaimList());
            }

            if (claimIn.Equals(selectUserClaim, StringComparison.CurrentCulture))
            {
                claims = claims.Where(c => !(c.Type.Equals(selectUserClaim, StringComparison.CurrentCulture) && c.Value.Equals(selectUserClaimValue, StringComparison.CurrentCulture))).ToList();
            }
            claims = LimitClaimsOut(claims, claimTypesOutLimit);
            logger.ScopeTrace(() => $"Claims transformation, External users JWT claims '{claims.ToFormattedString()}'", traceType: TraceTypes.Claim);
            return claims;
        }

        private List<Claim> LimitClaimsOut(List<Claim> claims, IEnumerable<string> claimTypesOutLimit)
        {
            if (claimTypesOutLimit == null)
            {
                return claims;
            }

            var filteredClaimTypes = claimTypesOutLimit
                .Where(c => !c.IsNullOrWhiteSpace())
                .ToHashSet();

            if (filteredClaimTypes.Count == 0 || filteredClaimTypes.Any(c => c == "*"))
            {
                return claims;
            }

            return claims.Where(c => filteredClaimTypes.Contains(c.Type)).ToList();
        }

        private async Task<User> FindInternalUserAsync(ITenantDataRepository tenantDataRepository, Track.IdKey idKey, string lookupClaimTypeOnUser, string lookupUserClaimValue)
        {
            if (lookupClaimTypeOnUser == JwtClaimTypes.Email || lookupClaimTypeOnUser == JwtClaimTypes.PhoneNumber || lookupClaimTypeOnUser == JwtClaimTypes.PreferredUsername)
            {
                var user = await tenantDataRepository.GetAsync<User>(await User.IdFormatAsync(RouteBinding, new User.IdKey { UserIdentifier = lookupUserClaimValue }), required: false, queryAdditionalIds: true);
                if (user != null && !user.DisableAccount)
                {
                    logger.ScopeTrace(() => $"Claims transformation, Internal user '{user.UserId}' found in user identifiers by claim '{lookupClaimTypeOnUser}' and claim value '{lookupUserClaimValue}'.");
                    return user;
                }
            }

            (var users, _) = await tenantDataRepository.GetManyAsync<User>(idKey, whereQuery: u => u.DataType.Equals(Constants.Models.DataType.User) && !u.DisableAccount &&
                u.Claims.Where(c => c.Claim == lookupClaimTypeOnUser && c.Values.Any(v => v == lookupUserClaimValue)).Any());

            if (users?.Count() == 1)
            {
                logger.ScopeTrace(() => $"Claims transformation, Internal user '{users.First().UserId}' found in claims by claim '{lookupClaimTypeOnUser}' and claim value '{lookupUserClaimValue}'.");
                return users.First();
            }

            if (users?.Count() > 1)
            {
                logger.ScopeTrace(() => $"Claims transformation, More then one internal users found by claim '{lookupClaimTypeOnUser}' and claim value '{lookupUserClaimValue}'.");
            }
            else
            {
                logger.ScopeTrace(() => $"Claims transformation, No internal user found by claim '{lookupClaimTypeOnUser}' and claim value '{lookupUserClaimValue}'.");
            }
            return null;
        }

        private async Task<ExternalUser> FindExternalUserAsync(ITenantDataRepository tenantDataRepository, Track.IdKey idKey, string lookupClaimOnUser, string selectUserClaimValue, string upPartyName)
        {
            if (lookupClaimOnUser == Constants.ClaimTransformClaimTypes.ExternalUserLink)
            {
                var externalUserByLink = await tenantDataRepository.GetAsync<ExternalUser>(
                    await ExternalUser.IdFormatAsync(RouteBinding, upPartyName, await selectUserClaimValue.HashIdStringAsync()),
                    required: false);

                if (externalUserByLink != null)
                {
                    if (!externalUserByLink.DisableAccount)
                    {
                        logger.ScopeTrace(() => $"Claims transformation, External user '{externalUserByLink.UserId}' found by authentication method '{upPartyName}' and link claim value '{selectUserClaimValue}'.");
                        return externalUserByLink;
                    }

                    logger.ScopeTrace(() => $"Claims transformation, External user '{externalUserByLink.UserId}' found by authentication method '{upPartyName}' and link claim value '{selectUserClaimValue}' is disabled.");
                }
            }

            (var externalUsers, _) = await tenantDataRepository.GetManyAsync<ExternalUser>(idKey, whereQuery: u =>
                u.DataType.Equals(Constants.Models.DataType.ExternalUser) &&
                !u.DisableAccount &&
                u.UpPartyName.Equals(upPartyName) &&
                u.Claims.Where(c => c.Claim == lookupClaimOnUser && c.Values.Any(v => v == selectUserClaimValue)).Any());

            if (externalUsers?.Count() == 1)
            {
                logger.ScopeTrace(() => $"Claims transformation, External user '{externalUsers.First().UserId}' found in claims by authentication method '{upPartyName}' and claim '{lookupClaimOnUser}' with claim value '{selectUserClaimValue}'.");
                return externalUsers.First();
            }

            if (externalUsers?.Count() > 1)
            {
                logger.ScopeTrace(() => $"Claims transformation, More then one external users found by authentication method '{upPartyName}' and claim '{lookupClaimOnUser}' with claim value '{selectUserClaimValue}'.");
            }
            else
            {
                logger.ScopeTrace(() => $"Claims transformation, No external user found by authentication method '{upPartyName}' and claim '{lookupClaimOnUser}' with claim value '{selectUserClaimValue}'.");
            }
            return null;
        }

        private List<string> GetUpdateSourceValues(IEnumerable<Claim> claims, string updateSourceClaimType, bool isInternalUser)
        {
            var updateSourceValues = claims
                .Where(c => c.Type.Equals(updateSourceClaimType, StringComparison.Ordinal))
                .Select(c => c.Value)
                .Where(v => !v.IsNullOrWhiteSpace())
                .Distinct()
                .ToList();

            if (updateSourceValues.Count() > 0)
            {
                return updateSourceValues;
            }
            else
            {
                logger.ScopeTrace(() => $"Claims transformation, Unable to save {(isInternalUser ? "internal" : "external")} user claim because the claim to update is missing.");
                return null;
            }
        }

        private (List<ClaimAndValues>, bool isChanged) UpdateUsersClaimValues(List<ClaimAndValues> claims, string targetClaimType, List<string> targetValues, ClaimTransformActions action)
        {
            claims ??= new List<ClaimAndValues>();
            var targetClaim = claims.FirstOrDefault(c => c.Claim == targetClaimType);
            var isChanged = false;

            switch (action)
            {
                case ClaimTransformActions.Add:
                    if (targetClaim == null)
                    {
                        claims.Add(new ClaimAndValues
                        {
                            Claim = targetClaimType,
                            Values = targetValues
                        });
                        isChanged = true;
                    }
                    else
                    {
                        targetClaim.Values ??= new List<string>();
                        foreach (var value in targetValues)
                        {
                            if (!targetClaim.Values.Contains(value))
                            {
                                targetClaim.Values.Add(value);
                                isChanged = true;
                            }
                        }
                    }
                    break;

                case ClaimTransformActions.Replace:
                    if (targetClaim == null)
                    {
                        claims.Add(new ClaimAndValues
                        {
                            Claim = targetClaimType,
                            Values = targetValues
                        });
                        isChanged = true;
                    }
                    else
                    {
                        var existingValues = targetClaim.Values ?? new List<string>();
                        if (!existingValues.SequenceEqual(targetValues, StringComparer.Ordinal))
                        {
                            targetClaim.Values = targetValues;
                            isChanged = true;
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException("Claim transform action is not supported for updating user claims.");
            }

            return (claims, isChanged);
        }

        private static void AddOrReplaceClaims(List<Claim> outputClaims, ClaimTransformActions claimTransformAction, Claim newClaim)
        {
            switch (claimTransformAction)
            {
                case ClaimTransformActions.Add:
                case ClaimTransformActions.AddIfNot:
                    outputClaims.Add(newClaim);
                    break;
                case ClaimTransformActions.Replace:
                case ClaimTransformActions.ReplaceIfNot:
                    outputClaims.RemoveAll(c => newClaim.Type.Equals(c.Type, StringComparison.Ordinal));
                    outputClaims.Add(newClaim);
                    break;
                default:
                    throw new NotSupportedException("Claim transform action is not supported in method.");
            }
        }

        private static void AddOrReplaceClaims(List<Claim> outputClaims, ClaimTransformActions claimTransformAction, List<Claim> newClaims)
        {
            if (newClaims.Count() > 0)
            {
                switch (claimTransformAction)
                {
                    case ClaimTransformActions.Add:
                    case ClaimTransformActions.AddIfNot:
                    case ClaimTransformActions.AddIfNotOut:
                        outputClaims.AddRange(newClaims);
                        break;
                    case ClaimTransformActions.Replace:
                    case ClaimTransformActions.ReplaceIfNot:
                        outputClaims.RemoveAll(c => newClaims.Any(nc => nc.Type.Equals(c.Type, StringComparison.Ordinal)));
                        outputClaims.AddRange(newClaims);
                        break;
                    default:
                        throw new NotSupportedException("Claim transform action is not supported in method.");
                }
            }
        }

        private void ConstantTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaim = new Claim(claimTransform.ClaimsOut.First(), claimTransform.Transformation);
            AddOrReplaceClaims(claims, claimTransform.Action, newClaim);
        }
    }
}
