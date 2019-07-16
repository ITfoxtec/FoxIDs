using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class MaxAttribute : ValidationAttribute
    {
        private readonly int max;

        public MaxAttribute(int max)
        {
            this.max = max;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var isNumber = double.TryParse(Convert.ToString(value), out var numberValue);
            return isNumber && numberValue <= max;

        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be less than or equal to {max}.";
        }
    }
}
