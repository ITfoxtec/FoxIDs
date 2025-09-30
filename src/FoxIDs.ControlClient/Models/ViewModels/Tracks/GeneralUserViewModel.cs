using System;
using System.Collections.Generic;
using System.Linq;
using ITfoxtec.Identity;
using FoxIDs.Client.Shared.Components;
using FoxIDs.Models.Api;

namespace FoxIDs.Client.Models.ViewModels
{
    public class GeneralUserViewModel : User
    {
        public GeneralUserViewModel()
        { }

        public GeneralUserViewModel(User user)
        {
            Email = user.Email;
            Phone = user.Phone;
            Username = user.Username;

            LoadName(user.Claims);
        }

        public string Name { get; private set; }

        public bool Edit { get; set; }

        public bool ShowAdvanced { get; set; }

        public bool CreateMode { get; set; }

        public bool DeleteAcknowledge { get; set; }

        public string Error { get; set; }

        public PageEditForm<UserViewModel> Form { get; set; }

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
