using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IDynamicElementsViewModel
    {
        [ListLength(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        public List<DynamicElementViewModel> Elements { get; set; }
    }
}
