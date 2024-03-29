﻿using ITfoxtec.Identity.Saml2;
using System;
using System.Xml;

namespace FoxIDs.Infrastructure.Saml2
{
    public class FoxIdsSaml2TokenExchangeRequest : Saml2AuthnResponse 
    {
        public FoxIdsSaml2TokenExchangeRequest(Saml2Configuration config) : base(config)
        { }

        public override string ElementName => throw new NotImplementedException();

        public override XmlDocument ToXml()
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

        }

        protected override void ValidateProtocol()
        { }
        protected override void ValidateElementName()
        { }
        protected override void ValidateStatus()
        { }
        protected override void ValidateSubjectConfirmationExpiration(XmlElement subjectElement)
        { }

        protected override XmlElement GetAssertionElement()
        {
            return XmlDocument.DocumentElement;
        }
    }
}
