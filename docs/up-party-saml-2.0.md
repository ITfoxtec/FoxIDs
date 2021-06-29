# Up-party - SAML 2.0

FoxIDs up-party [SAML 2.0 Identity Provider (IdP)](https://docs.oasis-open.org/security/saml/v2.0/saml-core-2.0-os.pdf) which trust an external SAML 2.0 Identity Provider (IdP).

![FoxIDs up-party SAML 2.0](images/parties-up-party-saml.svg)

It is possible to configure multiple SAML 2.0 IdP up-parties which then can be selected by [down-parties](parties.md#down-party).

FoxIDs support [redirect and post bindings](https://docs.oasis-open.org/security/saml/v2.0/saml-bindings-2.0-os.pdf).

A up-party expose [SAML 2.0 metadata](https://docs.oasis-open.org/security/saml/v2.0/saml-metadata-2.0-os.pdf) and can be configured with metadata or manually.

Both the login, logout and single logout [SAML 2.0 profiles](https://docs.oasis-open.org/security/saml/v2.0/saml-profiles-2.0-os.pdf) is supported. The Artifact profile is not supported.

> The FoxIDs SAML 2.0 metadata only include logout and single logout information if logout is configured in the SAML 2.0 up-party or down-party.

How to guides:

- Connect [AD SF](up-party-howto-saml-2.0-adfs.md)
- Connect yyy

## Configuration
//TODO

