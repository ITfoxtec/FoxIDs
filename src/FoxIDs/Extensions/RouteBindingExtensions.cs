using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using UrlCombineLib;

namespace FoxIDs
{
    public static class RouteBindingExtensions
    {
        public static string GetRouteSequenceString(this HttpContext httpContext)
        {
            return httpContext.Items[Constants.Routes.SequenceStringKey] as string;
        }

        public static string GetUpPartyUrl(this HttpContext httpContext, string upPartyName, string controller, string action = null, bool includeSequence = false, PartyBindingPatterns partyBindingPattern = PartyBindingPatterns.Brackets)
        {
            var routeBinding = httpContext.GetRouteBinding();
            var elements = new List<string> { routeBinding.TenantName, routeBinding.TrackName, upPartyName.ToUpPartyBinding(partyBindingPattern), controller };
            if (!action.IsNullOrEmpty())
            {
                elements.Add(action);
            }
            if (includeSequence)
            {
                elements.Add($"_{httpContext.GetSequenceString()}");
            }
            return UrlCombine.Combine(httpContext.GetHost(), elements.ToArray());
        }

        public static string ToUpPartyBinding(this string upPartyName, PartyBindingPatterns partyBindingPattern)
        {
            return partyBindingPattern switch
            {
                PartyBindingPatterns.Brackets => $"({upPartyName})",
                PartyBindingPatterns.Tildes => $"~{upPartyName}~",
                PartyBindingPatterns.Dot => $".{upPartyName}.",
                _ => throw new NotImplementedException($"Party binding pattern '{partyBindingPattern}' not implemented.")
            };
        }

        public static string GetDownPartyUrl(this HttpContext httpContext, string downPartyName, string upPartyName, string controller, string action = null, bool includeSequence = false)
        {
            var routeBinding = httpContext.GetRouteBinding();
            var elements = new List<string> { routeBinding.TenantName, routeBinding.TrackName, $"{downPartyName}({upPartyName})", controller };
            if (!action.IsNullOrEmpty())
            {
                elements.Add(action);
            }
            if (includeSequence)
            {
                elements.Add($"_{httpContext.GetSequenceString()}");
            }
            return UrlCombine.Combine(httpContext.GetHost(), elements.ToArray());
        }
    }
}
