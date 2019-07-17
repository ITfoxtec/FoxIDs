using FoxIDs.Models.Sequences;
using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs
{
    public static class SequenceExtensions
    {
        public static Sequence GetSequence(this HttpContext httpContext)
        {
            if (!httpContext.Items.ContainsKey(Constants.Sequence.Object))
            {
                throw new InvalidOperationException("HttpContext Items do not contain a sequence object.");
            }

            return httpContext.Items[Constants.Sequence.Object] as Sequence;
        }

        public static string GetSequenceString(this HttpContext httpContext)
        {
            if (!httpContext.Items.ContainsKey(Constants.Sequence.String))
            {
                throw new InvalidOperationException("HttpContext Items do not contain a sequence string.");
            }

            return httpContext.Items[Constants.Sequence.String] as string;
        }
    }
}
