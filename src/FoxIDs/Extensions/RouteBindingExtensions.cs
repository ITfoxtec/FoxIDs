using FoxIDs.Models;
using ITfoxtec.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.Collections.Generic;
using ITfoxtec.Identity.Util;

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
            var elements = new List<string> { upPartyName.ToUpPartyBinding(partyBindingPattern), controller };
            if (!action.IsNullOrEmpty())
            {
                elements.Add(action);
            }
            if (includeSequence)
            {
                elements.Add($"_{httpContext.GetSequenceString()}");
            }
            return UrlCombine.Combine(httpContext.GetHostWithTenantAndTrack(), elements.ToArray());
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

        public static string GetDownPartyUrl(this HttpContext httpContext, string downPartyName, string upPartyName, string controller, string action = null, bool includeSequence = false, PartyBindingPatterns partyBindingPattern = PartyBindingPatterns.Brackets)
        {
            var elements = new List<string> { downPartyName.ToDownPartyBinding(upPartyName, partyBindingPattern), controller };
            if (!action.IsNullOrEmpty())
            {
                elements.Add(action);
            }
            if (includeSequence)
            {
                elements.Add($"_{httpContext.GetSequenceString()}");
            }
            return UrlCombine.Combine(httpContext.GetHostWithTenantAndTrack(), elements.ToArray());
        }
        
        public static string ToDownPartyBinding(this string downPartyName, string upPartyName, PartyBindingPatterns partyBindingPattern)
        {
            return partyBindingPattern switch
            {
                PartyBindingPatterns.Brackets => $"{downPartyName}({upPartyName})",
                PartyBindingPatterns.Tildes => $"{downPartyName}~{upPartyName}~",
                PartyBindingPatterns.Dot => $"{downPartyName}.{upPartyName}.",
                _ => throw new NotImplementedException($"Party binding pattern '{partyBindingPattern}' not implemented.")
            };
        }        

        public static string GetTrackUpPartyUrl(this HttpContext httpContext, string trackName, string upPartyName, string controller, string action = null, bool includeKeySequence = false)
        {
            var elements = new List<string> { upPartyName.ToUpPartyBinding(PartyBindingPatterns.Brackets), controller, action };
            var url = UrlCombine.Combine(httpContext.GetHostWithTenantAndTrack(trackName), elements.ToArray());
            if (includeKeySequence)
            {
                url = QueryHelpers.AddQueryString(url, new Dictionary<string, string> { { Constants.Routes.KeySequenceKey, httpContext.GetSequenceString() } });
            }
            return url;
        }

        public static string GetTrackDownPartyUrl(this HttpContext httpContext, string trackName, string downPartyName, List<string> upPartyNames, string controller, string action, bool includeKeySequence = false)
        {
            if (upPartyNames?.Count > Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax)
            {
                throw new ArgumentException($"More then {Constants.Models.TrackLinkDownParty.SelectedUpPartiesMax} to up-party names.", nameof(upPartyNames));
            }

            var elements = new List<string> { $"{downPartyName}({string.Join(',', upPartyNames)})", controller, action };
            var url = UrlCombine.Combine(httpContext.GetHostWithTenantAndTrack(trackName), elements.ToArray());
            if (includeKeySequence)
            {
                url = QueryHelpers.AddQueryString(url, new Dictionary<string, string> { { Constants.Routes.KeySequenceKey, httpContext.GetSequenceString() } });
            }
            return url;
        }
    }
}
