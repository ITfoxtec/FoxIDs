using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class MinAttribute : ValidationAttribute
    {
        private readonly int min;

        public MinAttribute(int min)
        {
            this.min = min;
        }

        public override bool IsValid(object value)
        {
            if (value == null)
            {
                return true;
            }

            var isNumber = double.TryParse(Convert.ToString(value), out var numberValue);
            return isNumber && numberValue >= min;

        }

        public override string FormatErrorMessage(string name)
        {
            return $"The field {name} must be greater than or equal to {min}.";
        }
    }
}
