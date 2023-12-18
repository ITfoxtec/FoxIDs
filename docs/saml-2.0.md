# SAML 2.0

FoxIDs support SAML 2.0 as both up-party and down-party.

![FoxIDs SAML 2.0](images/parties-saml.svg)

## Up-party

Configure [SAML 2.0 up-party](up-party-saml-2.0.md) which trust an external SAML 2.0 Identity Provider (IdP).

How to guides:

- Connect [AD FS](up-party-howto-saml-2.0-adfs.md)
- Connect [PingIdentity / PingOne](up-party-howto-saml-2.0-pingone.md)
- Connect [NemLog-in (Danish IdP)](up-party-howto-saml-2.0-nemlogin.md)
- Connect [Context Handler (Danish IdP)](howto-saml-2.0-context-handler.md#up-party---connect-to-context-handler)

## Down-party

Configure your application as a [SAML 2.0 down-party](down-party-saml-2.0.md).

How to guides:

- Connect [AD FS](down-party-howto-saml-2.0-adfs.md)
- Connect [Context Handler (Danish IdP)](howto-saml-2.0-context-handler.md#down-party---connect-to-context-handler)

## Claim mappings
Claim mapping between SAML 2.0 claim types and JWT claim types can be configured in the setting menu in [FoxIDs Control](control.md). The claim mappings is global for the track.

> SAML 2.0 claims are internally [converted to JWT claims](parties.md#jwt-and-saml) between up-party and down-party.

![Configure JWT and SAML 2.0 mappings](images/configure-jwt-saml-mappings.png)

If no claim mapping exists for a particular claim. The long SAML 2.0 claim name is kept from claims revived in a SAML 2.0 token instead of a shorter equivalent JWT claim name. The same goes in the opposite direction.
