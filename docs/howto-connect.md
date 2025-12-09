<!--
{
    "title":  "How to connect",
    "description":  "FoxIDs becomes an IdP by registering an application you connect to applications and APIs. External IdPs are connected with authentication methods.",
    "ogTitle":  "How to connect",
    "ogDescription":  "FoxIDs becomes an IdP by registering an application you connect to applications and APIs. External IdPs are connected with authentication methods.",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "howto connect, FoxIDs docs"
                       }
}
-->

# How to connect

FoxIDs becomes an IdP by [registering an application](connections.md#application-registration) that you connect to your applications and APIs. External IdPs are connected with [authentication methods](connections.md#authentication-method).

By configuring a [SAML 2.0 authentication method](auth-method-saml-2.0.md) and an [OpenID Connect application](app-reg-oidc.md), FoxIDs becomes a [bridge](bridge.md) between SAML 2.0 and OpenID Connect and automatically converts SAML 2.0 claims to JWT (OAuth 2.0) claims.  
FoxIDs handles the SAML 2.0 connection so your application only needs to care about OpenID Connect. You can select multiple authentication methods for the same OpenID Connect application to offer users different sign-in options.

![How to connect with applications and authentication methods](images/how-to-connect.svg)

If needed you can [connect two FoxIDs environments](#connect-foxids-environments).

> Take a look at the FoxIDs test connections in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `gEh#V6kSw`

## How to connect OpenID Provider / Identity Provider

An external OpenID Provider (OP) / Identity Provider (IdP) can be connected with an OpenID Connect or SAML 2.0 authentication method.

All IdPs supporting either OpenID Connect or SAML 2.0 can be connected to FoxIDs. The following are how-to guides for common IdPs; more guides will be added over time.

### OpenID Connect

Configure [OpenID Connect](auth-method-oidc.md) to trust an external OpenID Provider (OP) - *an Identity Provider (IdP) is called an OpenID Provider (OP) if configured with OpenID Connect*.

> Always request the `sub` claim, even if you only plan to use the `email` claim or another custom user ID claim.

How to guides:

- Connect [IdentityServer](auth-method-howto-oidc-identityserver.md)
- Connect [Microsoft Entra ID (Azure AD)](auth-method-howto-oidc-microsoft-entra-id.md) 
- Connect [Azure AD B2C](auth-method-howto-oidc-azure-ad-b2c.md) 
- Connect [Amazon Cognito](auth-method-howto-oidc-amazon-cognito.md)
- Connect [Google](auth-method-howto-oidc-google.md)
- Connect [Facebook](auth-method-howto-oidc-facebook.md)
- Connect [Signicat](auth-method-howto-oidc-signicat.md)
- Connect [Nets eID Broker](auth-method-howto-oidc-nets-eid-broker.md)



### SAML 2.0

Configure [SAML 2.0](auth-method-saml-2.0.md) to trust an external Identity Provider (IdP).

> Always request the `NameID` claim, even if you primarily use the email (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`) claim or another custom user ID claim. SAML 2.0 logout requires `NameID`.  
> Prefer metadata-driven configuration so the customer's IdP can automatically download certificate(s). When possible, ask the customer for a live IdP metadata endpoint.

How to guides:

- Connect [PingIdentity / PingOne](auth-method-howto-saml-2.0-pingone.md)
- Connect [Google Workspace](auth-method-howto-saml-2.0-google-workspace.md)
- Connect [Microsoft AD FS](auth-method-howto-saml-2.0-adfs.md)
- Connect [NemLog-in (Danish IdP)](auth-method-howto-saml-2.0-nemlogin.md)
- Connect [Context Handler (Danish identity broker)](howto-saml-2.0-context-handler.md)


### Verified platforms

List of customer-verified platforms.

- [MobilityGuard](https://www.mobilityguard.com/)
- [F5 BIG-IP](https://www.f5.com/products/big-ip)
- [Swedish E-Identitet](https://e-identitet.se/)
- [PhenixID](https://www.phenixid.se/)
- [Nexus Group](https://www.nexusgroup.com/)

## How to connect applications
When you register an application with either OpenID Connect or SAML 2.0, FoxIDs becomes an OpenID Provider (OP) / Identity Provider (IdP). 
You most often connect applications and APIs, but an application registration can also issue tokens to an external system where that system is the relaying party (RP). 

### OpenID Connect and OAuth 2.0
It is recommended to secure applications and APIs with [OpenID Connect](app-reg-oidc.md) and [OAuth 2.0](app-reg-oauth-2.0.md). Please see the [samples](samples.md).

How to guides:

- Connect [Tailscale](app-reg-howto-oidc-tailscale.md)

### SAML 2.0
Configure [SAML 2.0](app-reg-saml-2.0.md) to be an Identity Provider (IdP).

How to guides:

- Connect [Amazon IAM Identity Center](app-reg-howto-saml-amazon-iam-identity-center.md)
- Connect [Google Workspace](app-reg-howto-saml-google-workspace.md)
- Connect [Microsoft Entra ID](app-reg-howto-saml-microsoft-entra-id.md)
- Connect [Context Handler test IdP (Danish identity broker)](howto-saml-2.0-context-handler#configuring-test-identity-provider-for-context-handler)

## Connect FoxIDs environments

It is possible to interconnect FoxIDs environments with an [Environment Link](howto-environmentlink-foxids.md) or [OpenID Connect](howto-oidc-foxids.md).

You can connect two environments in the same tenant with an [Environment Link](howto-environmentlink-foxids.md).
Environment Links are fast and secure, but they can only be used to connect within a tenant.  
*Use Environment Link if you need to connect environments in the same tenant.*

You can connect two environments in the same or different tenants with [OpenID Connect](howto-oidc-foxids.md). The configuration is more complex than using an Environment Link. 
OpenID Connect is secure and can connect all environments regardless of tenant. There is essentially no difference between external OpenID Connect connections and internal connections used between environments.

