using ITfoxtec.Identity.Saml2;
using ITfoxtec.Identity.Saml2.Tokens;
using Microsoft.IdentityModel.Tokens.Saml2;
using System;
using System.Security.Claims;
using System.Xml;

namespace FoxIDs.Infrastructure.Saml2
{
    public class FoxIdsSaml2TokenExchangeRequest : //  Saml2AuthnResponse?
    {
        /// <summary>
        /// Claims Identity.
        /// </summary>
        public ClaimsIdentity ClaimsIdentity { get; set; }

        /// <summary>
        /// Saml2 Security Token.
        /// </summary>
        public Saml2SecurityToken Saml2SecurityToken { get; protected set; }

        /// <summary>
        /// Saml2 Security Token Handler.
        /// </summary>
        public Saml2ResponseSecurityTokenHandler Saml2SecurityTokenHandler { get; protected set; }

        public FoxIdsSaml2TokenExchangeRequest(Saml2Configuration config) : base(config)
        { }

        public override string ElementName => throw new NotImplementedException();

        public override XmlDocument ToXml()
        {
            throw new NotImplementedException();
        }

        protected override void ValidateElementName()
        {
            throw new NotImplementedException();
        }

        internal void ReadInternal(string xml, bool validate, bool detectReplayedTokens)
        {
            Read(xml, validate, detectReplayedTokens);
        }

        protected override void Read(string xml, bool validate, bool detectReplayedTokens)
        {
            base.Read(xml, false, detectReplayedTokens);

            if (validate)
            {
                ValidateXmlSignature(XmlDocument.DocumentElement);
            }

            var tokenString = XmlDocument.DocumentElement.OuterXml;
            Saml2SecurityToken = ReadSecurityToken(tokenString);
            ClaimsIdentity = ReadClaimsIdentity(tokenString, detectReplayedTokens);
        }

        private Saml2SecurityToken ReadSecurityToken(string tokenString)
        {
            return Saml2SecurityTokenHandler.ReadSaml2Token(tokenString);
        }

        private ClaimsIdentity ReadClaimsIdentity(string tokenString, bool detectReplayedTokens)
        {
            return Saml2SecurityTokenHandler.ValidateToken(Saml2SecurityToken, tokenString, this, detectReplayedTokens).First();
        }
    }
}
