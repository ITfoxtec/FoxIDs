using FoxIDs.Models;
using Microsoft.AspNetCore.Http;
using System;

namespace FoxIDs
{
    public static class RouteBindingExtensions
    {
        public static RouteBinding GetRouteBinding(this HttpContext httpContext)
        {
            return httpContext.Items[Constants.Routes.RouteBindingKey] as RouteBinding;
        }
        public static RouteBinding TryGetRouteBinding(this HttpContext httpContext)
        {
            try
            {
                return httpContext.GetRouteBinding();
            }
            catch { }

            return null;
        }

        public static string GetRouteSequenceString(this HttpContext httpContext)
        {
            return httpContext.Items[Constants.Routes.SequenceStringKey] as string;
        }

        public static string ToUpPartyBinding(this string upPartyName, PartyBindingPatterns partyBindingPattern)
        {
            return partyBindingPattern switch
            {
                PartyBindingPatterns.Brackets => $"({upPartyName})",
                PartyBindingPatterns.Tildes => $"~{upPartyName}~",
                _ => throw new NotImplementedException($"Party binding pattern '{partyBindingPattern}' not implemented.")
            };
        }
    }
}
