using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.Extensions.Localization;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.Localization
{
    public class LocalizedValidationAttributeAdapterProvider : IValidationAttributeAdapterProvider
    {
        private readonly ValidationAttributeAdapterProvider originalProvider = new ValidationAttributeAdapterProvider();

        public IAttributeAdapter GetAttributeAdapter(ValidationAttribute attribute, IStringLocalizer stringLocalizer)
        {
            if (!(attribute is DataTypeAttribute))
            {
                attribute.ErrorMessage = attribute.FormatErrorMessage("{0}");

                if(attribute is MaxLengthAttribute)
                {
                    attribute.ErrorMessage = attribute.ErrorMessage.Replace((attribute as MaxLengthAttribute).Length.ToString(), "{1}");
                }
                else if (attribute is MinLengthAttribute)
                {
                    attribute.ErrorMessage = attribute.ErrorMessage.Replace((attribute as MinLengthAttribute).Length.ToString(), "{1}");
                }
                else if (attribute is StringLengthAttribute)
                {
                    attribute.ErrorMessage = attribute.ErrorMessage.Replace((attribute as StringLengthAttribute).MaximumLength.ToString(), "{1}").Replace((attribute as StringLengthAttribute).MinimumLength.ToString(), "{2}");
                }
            }

            return originalProvider.GetAttributeAdapter(attribute, stringLocalizer);
        }
    }
}
