# How to connect

An [IdP is connected](#up-party---how-to-connect-identity-provider-idp) with a [up-party](parties.md#up-party) and an [application or API is connected]() with a [down-party](parties.md#down-party).

It is possible to interconnect FoxIDs tracks either with a track link or OpenID Connect:

- Connect two FoxIDs tracks in a tenant with a [track link](howto-tracklink-foxids.md)
- Connect two FoxIDs tracks in the same or different tenants with [OpenID connect](howto-oidc-foxids.md)

## Up-party - How to connect Identity Provider (IdP)

An Identity Provider (IdP) can be connected with an [OpenID Connect up-party](#openid-connect-up-party) or an [SAML 2.0 up-party](#saml-20-up-party). An Identity Provider (IdP) is more precisely called an OpenID Provider (OP) if configured with OpenID Connect.

All IdPs supporting either OpenID Connect or SAML 2.0 can be connected to FoxIDs. The following is how to guides for some IdPs, more guides will be added over time.

### OpenID Connect up-party

Configure [OpenID Connect up-party](up-party-oidc.md) which trust an external OpenID Provider (OP) - *an Identity Provider (IdP) is called an OpenID Provider (OP) if configured with OpenID Connect*.

How to guides:

- Connect [Azure AD](up-party-howto-oidc-azure-ad.md) 
- Connect [Azure AD B2C](up-party-howto-oidc-azure-ad-b2c.md) 
- Connect [IdentityServer](up-party-howto-oidc-identityserver.md)
- Connect [Signicat](up-party-howto-oidc-signicat.md)
- Connect [Nets eID Broker](up-party-howto-oidc-nets-eid-broker.md)

### SAML 2.0 up-party

Configure [SAML 2.0 up-party](up-party-saml-2.0.md) which trust an external SAML 2.0 Identity Provider (IdP).

How to guides:

- Connect [AD FS](up-party-howto-saml-2.0-adfs.md)
- Connect [PingIdentity / PingOne](up-party-howto-saml-2.0-pingone.md)
- Connect [NemLog-in (Danish IdP)](up-party-howto-saml-2.0-nemlogin.md)
- Connect [Context Handler (Danish IdP)](howto-saml-2.0-context-handler.md#up-party---connect-to-context-handler)

## Up-party - How to connect relying party (RP)
// TODO
