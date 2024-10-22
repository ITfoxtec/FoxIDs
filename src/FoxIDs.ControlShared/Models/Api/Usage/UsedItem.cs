using FoxIDs.Infrastructure.DataAnnotations;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class UsedItem : IValidatableObject
    {
        [MaxLength(Constants.Models.Used.UsedItemTextLength)]
        public string Text { get; set; }

        [Range(Constants.Models.Used.DayMin, Constants.Models.Used.DayMax)]
        public int Day { get; set; }

        [Range(Constants.Models.Used.CountMin, Constants.Models.Used.CountMax)]
        public int Count { get; set; }

        [Min(Constants.Models.Used.PriceMin)]
        public double Price { get; set; }

        public UsedItemTypes Type { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();
            if (Type == UsedItemTypes.Text)
            {
                if (Count > 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Count)} field can not be used if the {nameof(Type)} field is '{Type}'.", [nameof(Count), nameof(Type)]));
                }
                if (Price == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Price)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(Price), nameof(Type)]));
                }
            }
            else if (Type == UsedItemTypes.Hours)
            {
                if (Day == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Day)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(Day), nameof(Type)]));
                }
                if (Count == 0)
                {
                    results.Add(new ValidationResult($"The {nameof(Count)} field is required if the {nameof(Type)} field is '{Type}'.", [nameof(Count), nameof(Type)]));
                }
            }
            return results;
        }
    }
}
