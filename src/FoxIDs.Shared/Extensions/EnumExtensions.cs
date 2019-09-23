//using ITfoxtec.Identity;
//using Newtonsoft.Json;
//using System;
//using System.Linq;

//namespace FoxIDs
//{
//    public static class EnumExtensions
//    {
//        /// <summary>
//        /// Convert json value to enum. Defaults to enum.ToString() to enum.
//        /// </summary>
//        public static T ToEnum<T>(this string value) where T : struct, Enum
//        {
//            if (value.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(value));
//            if (!typeof(T).IsEnum) throw new ArgumentException("Generic type must be an enum.");

//            foreach (T ev in System.Enum.GetValues(typeof(T)))
//            {
//                if (ev.ToValue<T>() == value)
//                {
//                    return ev;
//                }
//            }

//            if (Enum.TryParse<T>(value, true, out T enumValue))
//            {
//                return enumValue;
//            }

//            throw new InvalidCastException($"'{value}' not converted to enum {typeof(T).Name}.");
//        }

//        /// <summary>
//        /// Convert enum to json property name. Defaults to enum.ToString().
//        /// </summary>
//        public static string ToValue<T>(this T enumValue) where T : struct, Enum
//        {
//            if (!typeof(T).IsEnum) throw new ArgumentException("Generic type must be an enum.");

//            return enumValue.GetType().GetMember(enumValue.ToString())
//                    .SelectMany(mi => mi.GetCustomAttributes(typeof(JsonPropertyAttribute), false),
//                        (mi, jpa) => (jpa as JsonPropertyAttribute).PropertyName)
//                    .FirstOrDefault() ?? enumValue.ToString();
//        }

//        //public static T ToValue<T>(this T value) where T : struct, Enum
//        //{
//        //    if (!typeof(T).IsEnum)
//        //    {
//        //        throw new ArgumentException("Generic type must be an enum.");
//        //    }

//        //    T enumValue;
//        //    if (Enum.TryParse<T>(value, true, out enumValue))
//        //    {
//        //        return enumValue;
//        //    }
//        //    else
//        //    {
//        //        throw new InvalidCastException($"'{value}' not converted to enum {typeof(T).Name}.");
//        //    }
//        //}

//        //private static string GetJsonPropertyName<T>(this T enumerationValue) where T : struct, Enum
//        //{
//        //    return enumerationValue.GetType().GetMember(enumerationValue.ToString())
//        //            .SelectMany(mi => mi.GetCustomAttributes(typeof(JsonPropertyAttribute), false),
//        //                (mi, jpa) => (jpa as JsonPropertyAttribute).PropertyName)
//        //            .FirstOrDefault() ?? enumerationValue.ToString();
//        //}
//    }
//}
