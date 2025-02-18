using FoxIDs.Infrastructure.DataAnnotations;
using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class CreateUserViewModel : CreateUser, IDynamicElementsViewModel
    {
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public new List<DynamicElementViewModel> Elements { get; set; }

        /// <summary>
        /// Claim transforms.
        /// </summary>
        [ValidateComplexType]
        [ListLength(Constants.Models.Claim.TransformsMin, Constants.Models.Claim.TransformsMax)]
        public new List<ClaimTransformViewModel> ClaimTransforms { get; set; } = new List<ClaimTransformViewModel>();
    }
}
