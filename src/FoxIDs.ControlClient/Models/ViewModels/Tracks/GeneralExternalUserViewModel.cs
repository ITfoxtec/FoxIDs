using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;
using System;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralExternalUserViewModel : ExternalUser
    {
        public GeneralExternalUserViewModel()
        { }

        public GeneralExternalUserViewModel(ExternalUser externalUser)
        {
            UpPartyName = externalUser.UpPartyName;
            LinkClaimValue = externalUser.LinkClaimValue;
            RedemptionClaimValue = externalUser.RedemptionClaimValue;
            UserId = externalUser.UserId;
            DisableAccount = externalUser.DisableAccount;

            LoadName(externalUser.Claims);
        }

        public string Name { get; private set; }

        public string UpPartyDisplayName { get; set; }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<ExternalUserViewModel> Form { get; set; }

        public void LoadName(List<ClaimAndValues> claims)
        {
            Name = ResolveNameFromClaims(claims);
        }

        private string ResolveNameFromClaims(List<ClaimAndValues> claims)
        {
            if (claims == null)
            {
                return null;
            }

            var name = GetClaimValue(claims, JwtClaimTypes.Name);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            var givenName = GetClaimValue(claims, JwtClaimTypes.GivenName);
            var familyName = GetClaimValue(claims, JwtClaimTypes.FamilyName);
            if (!string.IsNullOrWhiteSpace(givenName) || !string.IsNullOrWhiteSpace(familyName))
            {
                return string.Join(" ", new[] { givenName, familyName }.Where(value => !string.IsNullOrWhiteSpace(value)));
            }

            return null;
        }

        private string GetClaimValue(List<ClaimAndValues> claims, string claimType)
        {
            return claims?
                .FirstOrDefault(c => string.Equals(c.Claim, claimType, StringComparison.OrdinalIgnoreCase))?
                .Values?
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
