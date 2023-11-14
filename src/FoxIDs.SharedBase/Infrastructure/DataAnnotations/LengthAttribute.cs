using System;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using ITfoxtec.Identity;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false)]
    public class LengthAttribute : RangeAttribute
    {
        private const string fieldNameKey = "[field_name]";
        private readonly int? maxStringLenght;
        private readonly int? totalMaxStringLenght;
        private readonly string regExPattern;
        private string tempFormatErrorMessage;

        /// <summary>
        /// Validates string list.
        /// </summary>
        /// <param name="minListLength">Min number of items in the list.</param>
        /// <param name="maxListLangth">Max number of items in the list.</param>
        public LengthAttribute(int minListLength, int maxListLangth) : base(minListLength, maxListLangth)
        { }

        /// <summary>
        /// Validates string list.
        /// </summary>
        /// <param name="minListLength">Min number of items in the list.</param>
        /// <param name="maxListLangth">Max number of items in the list.</param>
        /// <param name="maxStringLenght">Max string length per item.</param>
        public LengthAttribute(int minListLength, int maxListLangth, int maxStringLenght) : base(minListLength, maxListLangth)
        {
            this.maxStringLenght = maxStringLenght;
        }

        /// <summary>
        /// Validates string list.
        /// </summary>
        /// <param name="minListLength">Min number of items in the list.</param>
        /// <param name="maxListLangth">Max number of items in the list.</param>
        /// <param name="maxStringLenght">Max string length per item.</param>
        /// <param name="totalMaxStringLenght">Max string length for all items combined.</param>
        public LengthAttribute(int minListLength, int maxListLangth, int maxStringLenght, int totalMaxStringLenght) : this(minListLength, maxListLangth, maxStringLenght)
        {
            this.totalMaxStringLenght = totalMaxStringLenght;
        }

        /// <summary>
        /// Validates string list.
        /// </summary>
        /// <param name="minListLength">Min number of items in the list.</param>
        /// <param name="maxListLangth">Max number of items in the list.</param>
        /// <param name="maxStringLenght">Max string length per item.</param>
        /// <param name="regExPattern">RegEx validation of each item in the list.</param>
        public LengthAttribute(int minListLength, int maxListLangth, int maxStringLenght, string regExPattern) : this(minListLength, maxListLangth, maxStringLenght)
        {
            this.regExPattern = regExPattern;
        }

        public override bool IsValid(object value)
        {
            tempFormatErrorMessage = null;
            var totalStringLenght = 0;
            var count = 0;
            try
            {
                if (value is IEnumerable)
                {
                    var enumerator = (value as IEnumerable).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        if (enumerator.Current == null || enumerator.Current.ToString().IsNullOrWhiteSpace())
                        {
                            throw new ValidationException($"{fieldNameKey}.item[{count + 1}] is null or contain only white spaces.");
                        }

                        if (enumerator.Current is string currentStringItem)
                        {
                            if (maxStringLenght.HasValue)
                            {
                                new MaxLengthAttribute(maxStringLenght.Value).Validate(currentStringItem, $"{fieldNameKey}.item[{count}]");
                            }

                            if (totalMaxStringLenght.HasValue && currentStringItem != null)
                            {
                                totalStringLenght += currentStringItem.Length;
                            }

                            if (!regExPattern.IsNullOrEmpty())
                            {
                                new RegularExpressionAttribute(regExPattern).Validate(currentStringItem, $"{fieldNameKey}.item[{count}]");
                            }
                        }
                        count++;
                    }
                }

                if (totalMaxStringLenght.HasValue && totalStringLenght > totalMaxStringLenght.Value)
                {
                    throw new ValidationException($"The total length of all items combined in {fieldNameKey} exceeds the maximum allowed length '{totalMaxStringLenght.Value}'.");
                }
            }
            catch (ValidationException ex)
            {
                tempFormatErrorMessage = ex.Message;
                return false;
            }

            return base.IsValid(count);
        }

        public override string FormatErrorMessage(string name)
        {
            if(tempFormatErrorMessage.IsNullOrEmpty())
            {
                return base.FormatErrorMessage(name);
            }
            else
            {
                return tempFormatErrorMessage.Replace(fieldNameKey, name);
            }
        }
    }
}
