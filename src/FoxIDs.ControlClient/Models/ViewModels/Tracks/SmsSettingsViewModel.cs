using FoxIDs.Models.Api;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Client.Models.ViewModels
{
    public class SmsSettingsViewModel : SendSms
    {
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Type == SendSmsTypes.TeliaSmsGateway && Key == null)
            {
                results.Add(new ValidationResult($"The mTLS certificate is required.", [nameof(Key)]));
            }
            else
            {
                var baseResults = base.Validate(validationContext);
                if (baseResults.Count() > 0)
                {
                    results.AddRange(baseResults);
                }
            }

            return results;
        }
    }
}
