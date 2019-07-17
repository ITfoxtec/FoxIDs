using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class MaxLengthMessageAttribute : MaxLengthAttribute
    {
        public MaxLengthMessageAttribute(int length) : base(length)
        {
            ErrorMessage = ErrorMessageString;
        }

        public override bool IsValid(object value)
        {
            return base.IsValid(value);
        }
    }
}
