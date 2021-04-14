using FoxIDs.Models.Api;
using System;

namespace FoxIDs.Client
{
    public static class RouteBindingExtensions
    {
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
    }
}
