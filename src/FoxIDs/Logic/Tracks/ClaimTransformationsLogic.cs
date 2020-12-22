using FoxIDs.Infrastructure;
using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ClaimTransformationsLogic : LogicBase
    {
        private readonly TelemetryScopedLogger logger;

        public ClaimTransformationsLogic(TelemetryScopedLogger logger, IHttpContextAccessor httpContextAccessor) : base(httpContextAccessor)
        {
            this.logger = logger;
        }

        public Task<List<Claim>> Transform(IEnumerable<ClaimTransformation> claimTransformations, IEnumerable<Claim> claims)
        {
            if(claimTransformations == null|| claims == null)
            {
                return Task.FromResult(new List<Claim>(claims));
            }

            logger.ScopeTrace("Transform claims.");
            var transformedClaims = new List<Claim>(claims);
            var orderedTransformations = claimTransformations.OrderBy(t => t.Type).ThenBy(t => t.Order);
            foreach(var transformation in orderedTransformations)
            {
                switch (transformation.Type)
                {
                    case ClaimTransformationTypes.Constant:
                        transformedClaims.Add(ConstantTransformation(transformation));
                        break;
                    case ClaimTransformationTypes.Match:
                        transformedClaims.AddRange(MatchTransformation(transformation, transformedClaims));
                        break;
                    case ClaimTransformationTypes.RegexMatch:
                        transformedClaims.AddRange(RegexMatchTransformation(transformation, transformedClaims));
                        break;
                    case ClaimTransformationTypes.Map:
                        transformedClaims.AddRange(MapTransformation(transformation, transformedClaims));
                        break;
                    case ClaimTransformationTypes.RegexMap:
                        transformedClaims.AddRange(RegexMapTransformation(transformation, transformedClaims));
                        break;
                    case ClaimTransformationTypes.Concatenate:
                        transformedClaims.AddRange(ConcatenateTransformation(transformation, transformedClaims));
                        break;
                    default:
                        throw new NotSupportedException($"Claim transformation type '{transformation.Type}' not supported.");
                }
            }
            return Task.FromResult(transformedClaims);
        }

        private Claim ConstantTransformation(ClaimTransformation claimTransformation)
        {
            return new Claim(claimTransformation.ClaimOut, claimTransformation.Transformation);
        }

        private IEnumerable<Claim> MatchTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            var transformedClaims = new List<Claim>();
            foreach (var claim in claims)
            {
                if(claim.Type.Equals(claimTransformation.ClaimsIn.Single(), StringComparison.OrdinalIgnoreCase) && claim.Value.Equals(claimTransformation.Transformation, StringComparison.OrdinalIgnoreCase))
                {
                    transformedClaims.Add(new Claim(claimTransformation.ClaimOut, claimTransformation.TransformationExtension));
                }
            }
            return transformedClaims;
        }

        private IEnumerable<Claim> RegexMatchTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            var transformedClaims = new List<Claim>();
            var regex = new Regex(claimTransformation.Transformation, RegexOptions.IgnoreCase);
            var claimIn = claimTransformation.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.OrdinalIgnoreCase))
                {
                    var match = regex.Match(claim.Value);
                    if(match.Success)
                    {
                        transformedClaims.Add(new Claim(claimTransformation.ClaimOut, claimTransformation.TransformationExtension));
                    }
                }
            }
            return transformedClaims;
        }

        private IEnumerable<Claim> MapTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            var transformedClaims = new List<Claim>();
            var claimIn = claimTransformation.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.OrdinalIgnoreCase))
                {
                    transformedClaims.Add(new Claim(claimTransformation.ClaimOut, claim.Value));
                }
            }
            return transformedClaims;
        }

        private IEnumerable<Claim> RegexMapTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            var transformedClaims = new List<Claim>();
            var regex = new Regex(claimTransformation.Transformation, RegexOptions.IgnoreCase);
            var claimIn = claimTransformation.ClaimsIn.Single();
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimIn, StringComparison.OrdinalIgnoreCase))
                {
                    var match = regex.Match(claim.Value);
                    if (match.Success && match.Groups.ContainsKey("map"))
                    {
                        transformedClaims.Add(new Claim(claimTransformation.ClaimOut, match.Groups["map"].Value));
                    }
                }
            }
            return transformedClaims;
        }

        private IEnumerable<Claim> ConcatenateTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            var transformedClaims = new List<Claim>();
            var addTransformationClaim = false;
            var values = new string[claimTransformation.ClaimsIn.Count()];
            int i = 0;
            foreach (var claimIn in claimTransformation.ClaimsIn)
            {
                var value = claims.Where(c => c.Type.Equals(claimIn, StringComparison.OrdinalIgnoreCase)).Select(c => c.Value).FirstOrDefault();
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
                var transformationValue = string.Format(claimTransformation.Transformation, values);
                transformedClaims.Add(new Claim(claimTransformation.ClaimOut, transformationValue));
            }
            return transformedClaims;
        }
    }
}
