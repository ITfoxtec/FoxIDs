using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs
{
    public static class EnumExtensions
    {
        public static T ToEnum<T>(this string value) where T : struct, IConvertible
        {
            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("Generic type must be an enum.");
            }

            T enumValue;
            if (Enum.TryParse<T>(value, true, out enumValue))
            {
                return enumValue;
            }
            else
            {
                throw new InvalidCastException($"'{value}' not converted to enum {typeof(T).Name}.");
            }
        }



    }
}
