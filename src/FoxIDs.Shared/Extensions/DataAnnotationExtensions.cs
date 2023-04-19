using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Reflection;
using FoxIDs.Infrastructure.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using ITfoxtec.Identity;

namespace FoxIDs
{
    public static class DataAnnotationExtensions
    {        
        public static bool TryValidateRequiredParameter(this ModelStateDictionary modelState, string value, string parameterName)
        {
            modelState.Clear();

            if (value.IsNullOrWhiteSpace())
            {
                modelState.TryAddModelError(parameterName, $"The {parameterName} parameter is required.");
                return false;
            }
            return true;
        }

        public static async Task<bool> TryValidateObjectAsync(this ModelStateDictionary modelState, object data)
        {
            modelState.Clear();

            (var isValid, var results) = await ValidateObjectResultsAsync(data);
            if (!isValid)
            {
                foreach (var result in results)
                {
                    foreach (var memberName in result.MemberNames)
                    {
                        modelState.TryAddModelError(memberName.ToCamelCase(), result.ErrorMessage);
                    }
                }
            }
            return isValid;
        }     

        public static async Task ValidateObjectAsync(this object data)
        {
            if (data == null) return;

            (var isValid, var results) = await ValidateObjectResultsAsync(data);
            if (!isValid)
            {
                throw new ValidationResultException(data, results);
            }
        }

        public static Task<(bool isValid, List<ValidationResult> results)> ValidateObjectResultsAsync(this object data)
        {
            var results = new List<ValidationResult>();
            var isValid = TryValidateObjectRecursive(data, results);

            return Task.FromResult((isValid, results));
        }

        private static bool TryValidateObject(object data, ICollection<ValidationResult> results)
        {
            return Validator.TryValidateObject(data, new ValidationContext(data), results, true);
        }

        private static bool TryValidateObjectRecursive(object data, List<ValidationResult> results)
        {
            return TryValidateObjectRecursive(data, results, new HashSet<object>());
        }

        private static bool TryValidateObjectRecursive(object data, List<ValidationResult> results, ISet<object> validatedObjects)
        {
            // stop infinite loops
            if (validatedObjects.Contains(data))
            {
                return true;
            }

            validatedObjects.Add(data);
            bool isValid = TryValidateObject(data, results);

            var properties = data.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead &&
                p.GetCustomAttributes(typeof(ValidationAttribute), false).Any() &&
                p.GetIndexParameters().Length == 0).ToList();

            foreach (var property in properties)
            {
                if (property.PropertyType == typeof(string) || property.PropertyType.IsValueType)
                {
                    continue;
                }

                var value = property.GetValue(data, null);
                if (value == null)
                {
                    continue;
                }
                else if (value is IEnumerable)
                {
                    foreach (var dataItem in value as IEnumerable)
                    {
                        if (dataItem != null)
                        {
                            var nestedResults = new List<ValidationResult>();
                            if (!TryValidateObjectRecursive(dataItem, nestedResults, validatedObjects))
                            {
                                isValid = false;
                                foreach (var validationResult in nestedResults)
                                {
                                    results.Add(new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(mn => property.Name + '.' + mn)));
                                }
                            };
                        }
                    }
                }
                else
                {
                    var nestedResults = new List<ValidationResult>();
                    if (!TryValidateObjectRecursive(value, nestedResults, validatedObjects))
                    {
                        isValid = false;
                        foreach (var validationResult in nestedResults)
                        {
                            results.Add(new ValidationResult(validationResult.ErrorMessage, validationResult.MemberNames.Select(mn => property.Name + '.' + mn)));
                        }
                    };
                }
            }

            return isValid;
        }
    }
}
