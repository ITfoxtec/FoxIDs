using ITfoxtec.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class EmailAddressEmptyStringAttribute : DataTypeAttribute
    {
        public EmailAddressEmptyStringAttribute() : base(DataType.EmailAddress)
        {
            ErrorMessage = "The {0} field is not a valid e-mail address.";
        }

        public override bool IsValid(object? value)
        {
            if (value == null)
            {
                return true;
            }

            if (value is string valueString && valueString.IsNullOrWhiteSpace())
            {
                return true;
            }

            return new EmailAddressAttribute().IsValid(value);
        }
    }
}
