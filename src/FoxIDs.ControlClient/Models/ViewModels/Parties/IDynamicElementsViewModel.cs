using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public interface IDynamicElementsViewModel
    {
        [Length(Constants.Models.DynamicElements.ElementsMin, Constants.Models.DynamicElements.ElementsMax)]
        [Display(Name = "Dynamic elements shown in order (use the move up and down arrows to change the order)")]
        public List<DynamicElementViewModel> Elements { get; set; }
    }
}
