using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Models.ViewModels
{
    public class SearchTenantViewModel
    {
        /// <summary>
        /// Search by tenant name.
        /// </summary>
        [MaxLength(Constants.Models.TenantNameLength)]
        [RegularExpression(Constants.Models.TenantNameRegExPattern)]
        [Display(Name = "Search tenant")]
        public string Name { get; set; }
    }
}
