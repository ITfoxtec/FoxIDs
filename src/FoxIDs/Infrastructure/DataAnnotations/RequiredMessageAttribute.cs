using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class RequiredMessageAttribute : RequiredAttribute
    {
        public RequiredMessageAttribute()
        {
            ErrorMessage = ErrorMessageString;
        }

        public override bool IsValid(object value)
        {
            return base.IsValid(value);
        }
    }
}
