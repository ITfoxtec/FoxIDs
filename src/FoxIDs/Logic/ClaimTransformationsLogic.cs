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

            var transformedClaims = new List<Claim>(claims);
            var orderedTransformations = claimTransformations.OrderBy(t => t.Type).ThenBy(t => t.Order);
            foreach(var transformation in orderedTransformations)
            {
                switch (transformation.Type)
                {
                    case ClaimTransformationTypes.Constant:
                        transformedClaims.Add(ConstantTransformation(transformation));
                        break;
                    case ClaimTransformationTypes.Map:
                        transformedClaims.AddRange(MapTransformation(transformation, transformedClaims));
                        break;
                    case ClaimTransformationTypes.RegEx:
                        transformedClaims.AddRange(RegExTransformation(transformation, transformedClaims));
                        break;
                    case ClaimTransformationTypes.Concatenate:
                        transformedClaims.Add(ConcatenateTransformation(transformation, transformedClaims));
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

        private IEnumerable<Claim> MapTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            foreach(var claim in claims)
            {
                if(claim.Type.Equals(claimTransformation.ClaimsIn.Single(), StringComparison.OrdinalIgnoreCase))
                {
                    yield return new Claim(claimTransformation.ClaimOut, claim.Value);
                }
            }
        }

        private IEnumerable<Claim> RegExTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            foreach (var claim in claims)
            {
                if (claim.Type.Equals(claimTransformation.ClaimsIn.Single(), StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(claim.Value, claimTransformation.Transformation);
                    if(match.Success && match.Groups?.Count >= 1)
                    {
                        yield return new Claim(claimTransformation.ClaimOut, match.Groups[0].Value);
                    }
                }
            }
        }

        private Claim ConcatenateTransformation(ClaimTransformation claimTransformation, List<Claim> claims)
        {
            var values = claims.Where(c => claimTransformation.ClaimsIn.Any(ci => c.Type.Equals(ci, StringComparison.OrdinalIgnoreCase))).Select(c => c.Value);

            var transformationValue = string.Format(claimTransformation.Transformation, values);
            return new Claim(claimTransformation.ClaimOut, transformationValue);
        }
    }
}
