﻿using FoxIDs.Infrastructure;
using FoxIDs.Models;
using FoxIDs.Models.Logic;
using ITfoxtec.Identity;
using ITfoxtec.Identity.Saml2;
using Microsoft.AspNetCore.Http;
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
        private readonly ClaimTransformValidationLogic claimTransformValidationLogic;
        private readonly ExternalClaimsConnectLogic externalClaimsConnectLogic;

        public ClaimTransformLogic(TelemetryScopedLogger logger, ClaimTransformValidationLogic claimTransformValidationLogic, ExternalClaimsConnectLogic externalClaimsConnectLogic, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
            this.claimTransformValidationLogic = claimTransformValidationLogic;
            this.externalClaimsConnectLogic = externalClaimsConnectLogic;
        }

        public Task<List<Claim>> Transform(IEnumerable<ClaimTransform> claimTransforms, IEnumerable<Claim> claims)
        {
            if(claimTransforms == null|| claims == null)
            {
                return Task.FromResult(new List<Claim>(claims));
            }

            claimTransformValidationLogic.ValidateAndPrepareClaimTransforms(claimTransforms);

            logger.ScopeTrace(() => "Transform claims.");
            var outputClaims = new List<Claim>(claims);
            var orderedClaimTransforms = claimTransforms.OrderBy(t => t.Order);
            foreach(var claimTransform in orderedClaimTransforms)
            {
                try
                {
                    switch (claimTransform.Type)
                    {
                        case ClaimTransformTypes.Constant:
                            ConstantTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.MatchClaim:
                            MatchClaimTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.Match:
                            MatchTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.RegexMatch:
                            RegexMatchTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.Map:
                            MapTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.RegexMap:
                            RegexMapTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.Concatenate:
                            ConcatenateTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.ExternalClaims:
                            ExternalClaimsTransformation(outputClaims, claimTransform);
                            break;
                        case ClaimTransformTypes.DkPrivilege:
                            DkPrivilegeTransformation(outputClaims, claimTransform);
                            break;
                        default:
                            throw new NotSupportedException($"Claim transform type '{claimTransform.Type}' not supported.");
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Claim transform type '{claimTransform.Type}' with output claim '{claimTransform.ClaimOut}' failed.", ex);
                }
            }
            return Task.FromResult(outputClaims);
        }

        private static void AddOrReplaceClaims(List<Claim> outputClaims, ClaimTransform claimTransform, Claim newClaim)
        {
            switch (claimTransform.Action)
            {
                case ClaimTransformActions.Add:
                case ClaimTransformActions.AddIfNot:
                    outputClaims.Add(newClaim);
                    break;
                case ClaimTransformActions.Replace:
                case ClaimTransformActions.ReplaceIfNot:
                    outputClaims.RemoveAll(c => claimTransform.ClaimOut.Equals(c.Type, StringComparison.Ordinal));
                    outputClaims.Add(newClaim);
                    break;
                default:
                    throw new NotSupportedException("Claim transform action is not supported in method.");
            }
        }

        private static void AddOrReplaceClaims(List<Claim> outputClaims, ClaimTransform claimTransform, List<Claim> newClaims)
        {
            if (newClaims.Count() > 0)
            {
                switch (claimTransform.Action)
                {
                    case ClaimTransformActions.Add:
                    case ClaimTransformActions.AddIfNot:
                    case ClaimTransformActions.AddIfNotOut:
                        outputClaims.AddRange(newClaims);
                        break;
                    case ClaimTransformActions.Replace:
                    case ClaimTransformActions.ReplaceIfNot:
                        outputClaims.RemoveAll(c => claimTransform.ClaimOut.Equals(c.Type, StringComparison.Ordinal));
                        outputClaims.AddRange(newClaims);
                        break;
                    default:
                        throw new NotSupportedException("Claim transform action is not supported in method.");
                }
            }
        }

        private void ConstantTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaim = new Claim(claimTransform.ClaimOut, claimTransform.Transformation);
            AddOrReplaceClaims(claims, claimTransform, newClaim);
        }

        private void MatchClaimTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            if (claimTransform.Action != ClaimTransformActions.Remove)
            {
                var newClaims = new List<Claim>();
                foreach (var claim in claims)
                {
                    if (claim.Type.Equals(claimTransform.ClaimsIn.Single(), StringComparison.Ordinal))
                    {
                        newClaims.Add(new Claim(claimTransform.ClaimOut, claimTransform.Transformation));
                    }
                }

                if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                {
                    AddOrReplaceClaims(claims, claimTransform, newClaims);
                }
                else if (newClaims.Count() <= 0 && (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot))
                {
                    AddOrReplaceClaims(claims, claimTransform, new Claim(claimTransform.ClaimOut, claimTransform.Transformation));
                }
            }
            else
            {
                claims.RemoveAll(c => c.Type.Equals(claimTransform.ClaimOut, StringComparison.Ordinal));
            }
        }

        private void MatchTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            if (claimTransform.Action != ClaimTransformActions.Remove)
            {
                var newClaims = new List<Claim>();
                foreach (var claim in claims)
                {
                    if (claim.Type.Equals(claimTransform.ClaimsIn.Single(), StringComparison.Ordinal))
                    {
                        if (claim.Value.Equals(claimTransform.Transformation, StringComparison.Ordinal))
                        {
                            newClaims.Add(new Claim(claimTransform.ClaimOut, claimTransform.TransformationExtension));
                        }
                    }
                }

                if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                {
                    AddOrReplaceClaims(claims, claimTransform, newClaims);
                }
                else if (newClaims.Count() <= 0 && (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot))
                {
                    AddOrReplaceClaims(claims, claimTransform, new Claim(claimTransform.ClaimOut, claimTransform.TransformationExtension));
                }
            }
            else
            {
                claims.RemoveAll(c => c.Type.Equals(claimTransform.ClaimOut, StringComparison.Ordinal) && c.Value.Equals(claimTransform.Transformation, StringComparison.Ordinal));
            }
        }

        private void RegexMatchTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var regex = new Regex(claimTransform.Transformation, RegexOptions.IgnoreCase);

            if (claimTransform.Action != ClaimTransformActions.Remove)
            {
                var newClaims = new List<Claim>();
                var claimIn = claimTransform.ClaimsIn.Single();
                foreach (var claim in claims)
                {
                    if (claim.Type.Equals(claimIn, StringComparison.Ordinal))
                    {
                        var match = regex.Match(claim.Value);
                        if (match.Success)
                        {
                            newClaims.Add(new Claim(claimTransform.ClaimOut, claimTransform.TransformationExtension));
                        }
                    }
                }

                if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
                {
                    AddOrReplaceClaims(claims, claimTransform, newClaims);
                }
                else if (newClaims.Count() <= 0 && (claimTransform.Action == ClaimTransformActions.AddIfNot || claimTransform.Action == ClaimTransformActions.ReplaceIfNot))
                {
                    AddOrReplaceClaims(claims, claimTransform, new Claim(claimTransform.ClaimOut, claimTransform.TransformationExtension));
                }
            }
            else
            {
                claims.RemoveAll(c => c.Type.Equals(claimTransform.ClaimOut, StringComparison.Ordinal) && regex.Match(c.Value).Success);
            }
        }

        private void MapTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var newClaims = new List<Claim>();
            var claimIn = claimTransform.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.Ordinal))
                {
                    newClaims.Add(new Claim(claimTransform.ClaimOut, claim.Value));
                }
            }

            if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
            {
                AddOrReplaceClaims(claims, claimTransform, newClaims);
            }
            else if (claimTransform.Action == ClaimTransformActions.AddIfNotOut)
            {
                if (!(claims.Where(c => c.Type.Equals(claimTransform.ClaimOut, StringComparison.Ordinal)).Count() > 0))
                {
                    AddOrReplaceClaims(claims, claimTransform, newClaims);
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
                        newClaims.Add(new Claim(claimTransform.ClaimOut, match.Groups["map"].Value));
                    }
                }
            }

            if (claimTransform.Action == ClaimTransformActions.Add || claimTransform.Action == ClaimTransformActions.Replace)
            {
                AddOrReplaceClaims(claims, claimTransform, newClaims);
            }
            else if (claimTransform.Action == ClaimTransformActions.AddIfNotOut)
            {
                if (!(claims.Where(c => c.Type.Equals(claimTransform.ClaimOut, StringComparison.Ordinal)).Count() > 0))
                {
                    AddOrReplaceClaims(claims, claimTransform, newClaims);
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
                newClaims.Add(new Claim(claimTransform.ClaimOut, transformationValue));
            }
            AddOrReplaceClaims(claims, claimTransform, newClaims);
        }

        private async void ExternalClaimsTransformation(List<Claim> claims, ClaimTransform claimTransform)
        {
            var selectedClaims = new List<Claim>();
            var claimsIn = claimTransform.ClaimsIn.ConcatOnce([JwtClaimTypes.Subject, Constants.JwtClaimTypes.AuthMethod, Constants.JwtClaimTypes.AuthMethodType, Constants.JwtClaimTypes.UpParty, Constants.JwtClaimTypes.UpPartyType]);
            foreach (var claimIn in claimsIn)
            {
                var claimsResult = claims.Where(c => c.Type.Equals(claimIn, StringComparison.Ordinal));
                if(claimsResult.Count() > 0)
                {
                    selectedClaims.AddRange(claimsResult);
                }
            }

            var newClaims = await externalClaimsConnectLogic.GetClaimsAsync(claimTransform, selectedClaims);
            AddOrReplaceClaims(claims, claimTransform, newClaims);
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

                        newClaims.Add(new Claim(claimTransform.ClaimOut, dkPrivilegeGroupResult.ToJson()));
                    }                    
                }
            }

            AddOrReplaceClaims(claims, claimTransform, newClaims);
        }
    }
}
