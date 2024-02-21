# How to connect

Foxids become an IdP by [registering an application](parties.md#down-party) where you can connect applications and APIs. An external IdP is connected with an [authentication method](parties.md#up-party)

By configuring a [SAML 2.0 authentication method](up-party-saml-2.0.md) and a [OpenID Connect application](down-party-oidc.md) Foxids become a [bridge](bridge.md) between SAML 2.0 and OpenID Connect. 
Foxids will then handle the SAML 2.0 connection and you only need to care about OpenID Connect in your application. You can possibly select multiple authentication methods from the same OpenID Connect application.

![How to connect with applications and authentication methods](images/how-to-connect.svg)

If needed you can [connect two Foxids configurations](#connect-foxids-configurations).

> Take a look at the Foxids test connections in Foxids Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!`

## How to connect applications
When you register an application with either OpenID Connect or SAML 2.0, Foxids become an OpenID Provider (OP) / Identity Provider (IdP). 
You would most often connect applications and APIs. But a application registration can also be used as a OP / IdP for an external system where the external system is the relaying party (RP). 

### OpenID Connect and OAuth 2.0
It is recommended to secure applications and APIs with [OpenID Connect](down-party-oidc.md) and [OAuth 2.0](down-party-oauth-2.0.md). Please see the [samples](samples.md).

### SAML 2.0
Configure [SAML 2.0](down-party-saml-2.0.md) to be an Identity Provider (IdP).

How to guides:

- Connect test IdP on [Context Handler (Danish identity broker)](howto-saml-2.0-context-handler.md)

## How to connect authentication method

An external OpenID Provider (OP) / Identity Provider (IdP) can be connected with a OpenID Connect or SAML 2.0 authentication method.

All IdPs supporting either OpenID Connect or SAML 2.0 can be connected to Foxids. The following is how to guides for some IdPs; more guides will be added over time.

### OpenID Connect

Configure [OpenID Connect](up-party-oidc.md) which trust an external OpenID Provider (OP) - *an Identity Provider (IdP) is called an OpenID Provider (OP) if configured with OpenID Connect*.

How to guides:

- Connect [IdentityServer](up-party-howto-oidc-identityserver.md)
- Connect [Microsoft Entra ID (Azure AD)](up-party-howto-oidc-azure-ad.md) 
- Connect [Azure AD B2C](up-party-howto-oidc-azure-ad-b2c.md) 
- Connect [Signicat](up-party-howto-oidc-signicat.md)
- Connect [Nets eID Broker](up-party-howto-oidc-nets-eid-broker.md)

### SAML 2.0

Configure [SAML 2.0](up-party-saml-2.0.md) which trust an external Identity Provider (IdP).

How to guides:

- Connect [PingIdentity / PingOne](up-party-howto-saml-2.0-pingone.md)
- Connect [Microsoft AD FS](up-party-howto-saml-2.0-adfs.md)
- Connect [NemLog-in (Danish IdP)](up-party-howto-saml-2.0-nemlogin.md)
- Connect [Context Handler (Danish identity broker)](howto-saml-2.0-context-handler.md)

## Connect Foxids configurations

It is possible to interconnect Foxids configurations with a [configuration link](howto-tracklink-foxids.md) or [OpenID connect](howto-oidc-foxids.md).

You can connect two Foxids configurations in the same tenant with a [configuration link](howto-tracklink-foxids.md).
Configuration links is fast and secure, but they can only be used in to connect within a tenant.  
*It is recommended to use configuration links if you need to connect configurations in the same tenant.*

You can connect two Foxids configurations in the same or different tenants with [OpenID connect](howto-oidc-foxids.md). The configuration is more complex than if you use a configuration link. 
OpenID connect is secure and you can connect all configurations regardless of which tenant they are in. There is basically not different in external OpenID Connect connections and internal connections used between configurations.