using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace FoxIDs.Client.Models.ViewModels
{
    public class OAuthDownPartyViewModel : IValidatableObject
    {
        [Display(Name = "Up Party name")]
        public string Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            //if (Client != null && AllowUpPartyNames?.Count <= 0)
            //{
            //    results.Add(new ValidationResult($"At least one in the field {nameof(AllowUpPartyNames)} is required if the field {nameof(Resource)} is defined.", new[] { nameof(Client), nameof(AllowUpPartyNames) }));
            //}
            //if (Client == null && Resource == null)
            //{
            //    results.Add(new ValidationResult($"Either the field {nameof(Client)} or the field {nameof(Resource)} is required.", new[] { nameof(Client), nameof(Resource) }));
            //}
            return results;
        }
    }
}
