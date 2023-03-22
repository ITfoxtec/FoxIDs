using FoxIDs.Models.Api;
using System;

namespace FoxIDs.Client
{
    public static class RouteBindingExtensions
    {
        public static string ToDownPartyBinding(this string downPartyName, bool addUpParty, PartyBindingPatterns partyBindingPattern)
        {
            return partyBindingPattern switch
            {
                PartyBindingPatterns.Brackets => $"{downPartyName}{(addUpParty ? $"(*)" : string.Empty)}",
                PartyBindingPatterns.Tildes => $"{downPartyName}{(addUpParty ? $"~*~" : string.Empty)}",
                PartyBindingPatterns.Dot => $"{downPartyName}{(addUpParty ? $".*." : string.Empty)}",
                _ => throw new NotImplementedException($"Party binding pattern '{partyBindingPattern}' not implemented.")
            };
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
    }
}
