﻿using System;
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
        private readonly string regExPattern;
        private string tempFormatErrorMessage;

        public LengthAttribute(int minListLength, int maxListLangth) : base(minListLength, maxListLangth)
        { }

        public LengthAttribute(int minListLength, int maxListLangth, int maxStringLenght) : base(minListLength, maxListLangth)
        {
            this.maxStringLenght = maxStringLenght;
        }

        public LengthAttribute(int minListLength, int maxListLangth, int maxStringLenght, string regExPattern) : this(minListLength, maxListLangth, maxStringLenght)
        {
            this.regExPattern = regExPattern;
        }

        public override bool IsValid(object value)
        {
            tempFormatErrorMessage = null;
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

                        if (enumerator.Current is string)
                        {
                            if (maxStringLenght.HasValue)
                            {
                                new MaxLengthAttribute(maxStringLenght.Value).Validate(enumerator.Current as string, $"{fieldNameKey}.item[{count}]");
                            }

                            if (!regExPattern.IsNullOrEmpty())
                            {
                                new RegularExpressionAttribute(regExPattern).Validate(enumerator.Current as string, $"{fieldNameKey}.item[{count}]");
                            }
                        }
                        count++;
                    }
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
