using Schemas = ITfoxtec.Identity.Saml2.Schemas;
using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using ITfoxtec.Identity;
using FoxIDs.Models.Config;
using System.Threading.Tasks;
using FoxIDs.Logic;

namespace FoxIDs.Infrastructure.Saml2
{
    public class FoxIdsSaml2AuthnResponse : Saml2AuthnResponse
    {
        private readonly FoxIDsSettings settings;

        public FoxIdsSaml2AuthnResponse(FoxIDsSettings settings, Saml2Configuration config) : base(config)
        {
            this.settings = settings;
        }

        public Task CreateSecurityTokenAsync(SecurityTokenDescriptor tokenDescriptor, Saml2AuthenticationStatement authenticationStatement, Saml2SubjectConfirmation subjectConfirmation)
        {
            return Task.FromResult(CreateSecurityToken(tokenDescriptor, authenticationStatement, subjectConfirmation));
        }

        public SecurityTokenDescriptor CreateTokenDescriptor(IEnumerable<Claim> claims, string appliesToAddress, DateTimeOffset tokenIssueTime, int issuedTokenLifetime)
        {
            if (Issuer.IsNullOrEmpty()) throw new ArgumentNullException("Issuer property");

            var subjectClaims = claims.Where(c => c.Type != ClaimTypes.AuthenticationMethod && c.Type != ClaimTypes.AuthenticationInstant && 
                c.Type != ClaimTypes.NameIdentifier && c.Type != Saml2ClaimTypes.NameIdFormat && 
                c.Type != Saml2ClaimTypes.SessionIndex);

            if(subjectClaims.Count() < 1)
            {
                subjectClaims = AddNameIdAsNameClaim(subjectClaims);
            }

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(subjectClaims),
                Expires = tokenIssueTime.AddSeconds(issuedTokenLifetime).UtcDateTime,
                Audience = appliesToAddress,
                Issuer = Issuer
            };

            return tokenDescriptor;
        }

        private IEnumerable<Claim> AddNameIdAsNameClaim(IEnumerable<Claim> claims)
        {
            var newClaims = new List<Claim>(claims);
            if(NameId != null)
            {
                newClaims.AddClaim(ClaimTypes.Name, NameId.Value);
            }
            else
            {
                throw new SamlResponseException("Either NameID or another claim is required. Eg. UPN, Email og Name claim");
            }
            return newClaims;
        }

        public Saml2AuthenticationStatement CreateAuthenticationStatement(string authnContext, DateTimeOffset authenticationInstant)
        {
            var authenticationStatement = new Saml2AuthenticationStatement(new Saml2AuthenticationContext(new Uri(authnContext)), authenticationInstant.UtcDateTime);
            authenticationStatement.SessionIndex = SessionIndex;
            return authenticationStatement;
        }

        public Saml2SubjectConfirmation CreateSubjectConfirmation(DateTimeOffset tokenIssueTime, int subjectConfirmationLifetime)
        {
            if (Destination == null) throw new ArgumentNullException("Destination property");

            var subjectConfirmationData = new Saml2SubjectConfirmationData
            {
                Recipient = Destination,
                NotBefore = tokenIssueTime.AddSeconds(settings.SamlTokenAddNotBeforeTime).UtcDateTime,
                NotOnOrAfter = tokenIssueTime.AddSeconds(subjectConfirmationLifetime).UtcDateTime,
            };

            if (InResponseTo != null)
            {
                subjectConfirmationData.InResponseTo = InResponseTo;
            }

            return new Saml2SubjectConfirmation(Schemas.Saml2Constants.Saml2BearerToken, subjectConfirmationData);
        }
    }
}
