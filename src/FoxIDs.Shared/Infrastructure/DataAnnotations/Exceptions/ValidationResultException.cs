using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;

namespace FoxIDs.Infrastructure.DataAnnotations
{
    [Serializable]
    public class ValidationResultException : Exception
    {
        public ValidationResultException() { }
        public ValidationResultException(object validationData, List<ValidationResult> validationResults) : base(DataTypeName(validationData))
        {
            ValidationResults = validationResults;
        }

        public ValidationResultException(object validationData, List<ValidationResult> validationResults, Exception inner) : base(DataTypeName(validationData), inner)
        {
            ValidationResults = validationResults;
        }
        protected ValidationResultException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public List<ValidationResult> ValidationResults { get; }

        public override string Message => $"{base.Message}: [{string.Join(", ", ValidationResultStrings)}]";

        private List<string> ValidationResultStrings => ValidationResults.Select(r => $"{string.Join(", ", r.MemberNames)}: {r.ErrorMessage}").ToList();

        private static string DataTypeName(object data)
        {
            return data?.GetType()?.Name;
        }

    }
}
