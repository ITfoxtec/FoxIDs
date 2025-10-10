using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [Serializable]
    public class ValidationResultException : Exception
    {
        public ValidationResultException() { }
        public ValidationResultException(object validationData, List<ValidationResult> validationResults) : base(DataTypeName(validationData))
        {
            ValidationData = validationData;
            ValidationResults = validationResults ?? new List<ValidationResult>();
        }

        public ValidationResultException(object validationData, List<ValidationResult> validationResults, Exception inner) : base(DataTypeName(validationData), inner)
        {
            ValidationData = validationData;
            ValidationResults = validationResults ?? new List<ValidationResult>();
        }

        public List<ValidationResult> ValidationResults { get; }

        private object ValidationData { get; }

        public override string Message => $"{base.Message}: [{string.Join(", ", ValidationResultStrings)}]";

        private List<string> ValidationResultStrings => ValidationResults?.Select(FormatValidationResult).ToList() ?? new List<string>();

        private string FormatValidationResult(ValidationResult result)
        {
            var memberNames = result.MemberNames?.ToList();
            if (memberNames == null || !memberNames.Any())
            {
                return result.ErrorMessage;
            }

            var formattedMembers = memberNames.Select(FormatMember).ToList();
            return $"{string.Join(", ", formattedMembers)}: {result.ErrorMessage}";
        }

        private string FormatMember(string memberName)
        {
            if (TryGetMemberValue(ValidationData, memberName, out var value))
            {
                return $"{memberName}='{FormatValue(value)}'";
            }

            return memberName;
        }

        private static string FormatValue(object value)
        {
            if (value == null)
            {
                return "null";
            }

            var stringValue = value switch
            {
                string s => s,
                _ => value.ToString()
            };

            return stringValue?.Replace("'", "''");
        }

        private static bool TryGetMemberValue(object data, string memberPath, out object value)
        {
            value = null;
            if (data == null || string.IsNullOrWhiteSpace(memberPath))
            {
                return false;
            }

            object current = data;
            foreach (var segment in memberPath.Split('.'))
            {
                if (current == null)
                {
                    return false;
                }

                var (name, index) = ParseSegment(segment);
                var property = current.GetType().GetProperty(name);
                if (property == null)
                {
                    return false;
                }

                current = property.GetValue(current);
                if (index.HasValue)
                {
                    if (!TryGetCollectionElement(ref current, index.Value))
                    {
                        return false;
                    }
                }
            }

            value = current;
            return true;
        }

        private static (string name, int? index) ParseSegment(string segment)
        {
            var start = segment.IndexOf('[');
            if (start >= 0)
            {
                var end = segment.IndexOf(']', start);
                if (end > start)
                {
                    var name = segment.Substring(0, start);
                    var indexSegment = segment.Substring(start + 1, end - start - 1);
                    if (int.TryParse(indexSegment, out var index))
                    {
                        return (name, index);
                    }
                }

                return (segment.Substring(0, start), null);
            }

            return (segment, null);
        }

        private static bool TryGetCollectionElement(ref object current, int index)
        {
            if (current == null || index < 0)
            {
                return false;
            }

            if (current is IList list)
            {
                if (index >= list.Count)
                {
                    return false;
                }

                current = list[index];
                return true;
            }

            if (current is IEnumerable enumerable)
            {
                var enumerator = enumerable.GetEnumerator();
                try
                {
                    var position = 0;
                    while (enumerator.MoveNext())
                    {
                        if (position == index)
                        {
                            current = enumerator.Current;
                            return true;
                        }

                        position++;
                    }
                }
                finally
                {
                    (enumerator as IDisposable)?.Dispose();
                }
            }

            return false;
        }

        private static string DataTypeName(object data)
        {
            return data?.GetType()?.Name;
        }
    }
}
