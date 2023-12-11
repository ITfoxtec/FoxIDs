# How to connect

An external IdP is connected with an [up-party](parties.md#up-party) and FoxIDs become an IdP by using a [down-party](parties.md#down-party) where you can connect applications and APIs.

By configuring an [SAML 2.0 up-party](up-party-saml-2.0.md) and a [OpenID Connect down-party](down-party-oidc.md) FoxIDs become a bridge between SAML 2.0 and OpenID Connect. 
FoxIDs will then handle the SAML 2.0 connection and you only need to care about OpenID Connect in your application. You can possibly connect to multiple up-parties from the same OpenID Connect down-party.

![How to connect with up-parties and down-parties](images/how-to-connect.svg)

If needed you can [connect two FoxIDs tracks](#connect-foxids-tracks).

## How to connect Identity Provider (IdP)

An external OpenID Provider (OP) / Identity Provider (IdP) can be connected with a OpenID Connect up-party or a SAML 2.0 up-party.

All IdPs supporting either OpenID Connect or SAML 2.0 can be connected to FoxIDs. The following is how to guides for some IdPs; more guides will be added over time.

### OpenID Connect up-party

Configure [OpenID Connect up-party](up-party-oidc.md) which trust an external OpenID Provider (OP) - *an Identity Provider (IdP) is called an OpenID Provider (OP) if configured with OpenID Connect*.

How to guides:

- Connect [IdentityServer](up-party-howto-oidc-identityserver.md)
- Connect [Microsoft Entra ID (Azure AD)](up-party-howto-oidc-azure-ad.md) 
- Connect [Azure AD B2C](up-party-howto-oidc-azure-ad-b2c.md) 
- Connect [Signicat](up-party-howto-oidc-signicat.md)
- Connect [Nets eID Broker](up-party-howto-oidc-nets-eid-broker.md)

### SAML 2.0 up-party

Configure [SAML 2.0 up-party](up-party-saml-2.0.md) which trust an external Identity Provider (IdP).

How to guides:

- Connect [PingIdentity / PingOne](up-party-howto-saml-2.0-pingone.md)
- Connect [Microsoft AD FS](up-party-howto-saml-2.0-adfs.md)
- Connect [NemLog-in (Danish IdP)](up-party-howto-saml-2.0-nemlogin.md)
- Connect [Context Handler (Danish identity broker)](howto-saml-2.0-context-handler.md)

## How to become an Identity Provider (IdP)
When you configure a down-party with either OpenID Connect or SAML 2.0, FoxIDs become an OpenID Provider (OP) / Identity Provider (IdP). 
You would most often use down-parties to connect applications and APIs. But a down-party can also be used as a OP / IdP for an external system where the external system is the relaying party (RP). 

### OpenID Connect and OAuth 2.0 down-party
It is recommended to secure applications and APIs with [OpenID Connect](down-party-oidc.md) and [OAuth 2.0](down-party-oauth-2.0.md). Please see the [samples](samples.md).

### SAML 2.0 down-party

Configure [SAML 2.0 down-party](down-party-saml-2.0.md) to be an Identity Provider (IdP).

How to guides:

- Connect test IdP on [Context Handler (Danish identity broker)](howto-saml-2.0-context-handler.md)

## Connect FoxIDs tracks

It is possible to interconnect FoxIDs tracks with a [track link](howto-tracklink-foxids.md) or [OpenID connect](howto-oidc-foxids.md).

You can connect two FoxIDs tracks in the same tenant with a [track link](howto-tracklink-foxids.md). The configuration is easily done by coping track and party names. 
Track links is fast and secure, but they can only be used in to connect tracks in the same tenant.  
*It is recommended to use track links if you need to connect tracks in the same tenant.*

You can connect two FoxIDs tracks in the same or different tenants with [OpenID connect](howto-oidc-foxids.md). The configuration is more complex than if you use a track link. 
OpenID connect is secure and you can connect all tracks regardless of which tenant they are in. There is basically not different in external OpenID Connect connections and internal connections used between tracks.