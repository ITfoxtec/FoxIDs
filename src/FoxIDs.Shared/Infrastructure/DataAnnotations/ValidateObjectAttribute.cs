using System;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ValidateObjectAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            return true;
        }
    }
}
