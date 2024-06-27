using FoxIDs.Models;
using ITfoxtec.Identity;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FoxIDs.Logic
{
    public class ExternalAccountLogic
    {
        public async Task<List<Claim>> ValidateUser(string username, string password)
        {
            //var claims = new List<Claim>();
            //claims.AddClaim(JwtClaimTypes.Subject, user.UserId);
            //claims.AddClaim(JwtClaimTypes.PreferredUsername, user.Email);
            //claims.AddClaim(JwtClaimTypes.Email, user.Email);
            //claims.AddClaim(JwtClaimTypes.EmailVerified, user.EmailVerified.ToString().ToLower());

            throw new NotImplementedException();
        }
    }
}
