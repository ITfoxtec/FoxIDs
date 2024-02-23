# OpenID Connect authentication method

Foxids [OpenID Connect](https://openid.net/specs/openid-connect-core-1_0.html) authentication method which trust an external OpenID Provider (OP) / Identity Provider (IdP).

![Foxids OpenID Connect authentication method](images/connections-auth-method-oidc.svg)

It is possible to configure multiple OpenID Connect authentication methods which then can be selected by [application registrations](connections.md#application-registration).

How to guides:

- Connect two Foxids environments in the same tenant with a [Environment Link](howto-environmentlink-foxids.md)
- Connect two Foxids environments in the same or different tenants with [OpenID Connect](howto-oidc-foxids.md)
- Connect [Microsoft Entra ID (Azure AD)](auth-method-howto-oidc-azure-ad.md) 
- Connect [Azure AD B2C](auth-method-howto-oidc-azure-ad-b2c.md) 
- Connect [IdentityServer](auth-method-howto-oidc-identityserver.md)
- Connect [Signicat](auth-method-howto-oidc-signicat.md)
- Connect [Nets eID Broker](auth-method-howto-oidc-nets-eid-broker.md)

> It is recommended to use OpenID Connect Authorization Code flow with PKCE, because it is considered a secure flow.

## Configuration
How to configure external OpenID Provider (OP) as an authority.

The following screen shot show the basic Foxids OpenID Connect authentication method configuration available in [Foxids Control Client](control.md#foxids-control-client).

> More configuration options become available by clicking `Show advanced settings`.

![Configure OpenID Connect](images/configure-oidc-auth-method.png)

Foxids automatically calls the OpenID Configuration endpoint (`.well-known/openid-configuration`) on create. You can see the added configuration by opening the authentication method again.

Foxids automatically read future updates. If the endpoint become unavailable for a period of time Foxids will stop the automated update process. It can be restarted by doing an authentication method update in [Foxids Control Client](control.md#foxids-control-client) or [API](control.md#foxids-control-api).

> Foxids Control Client only support creating automatic updated authentication methods using the OpenID Configuration endpoint. [Foxids Control API](control.md#foxids-control-api) support both automatic and manually updated authentication methods. In manual you can specify all values and the OpenID Configuration endpoint (`.well-known/openid-configuration`) will not be called.

Default the authentication method is configured for Authorization Code Flow, to use PKCE and read claim from the external access token. These settings can be changed.

The default client authentication method is `client secret post` and can be changed to `client secret basic` or `private key JWT`. Client authentication method none is supported with PKCE.

The scopes the Foxids authentication method should send in the request to the external OP can be configured. E.g, `profile` or `email`.

The authentication method only transfer default claims and configured claim to the application registrations. 

Default transferred claims are `sub`, `sid`, `acr` and `amr`.

> Change the claims the authentication method pass on with [claim transforms](claim-transform.md).

Foxids default use the brackets connection pattern `.../(auth-method)/...`. If not supported by the external OP (e.g., like Microsoft Entra ID), the pattern can be changed to the tildes connection pattern `.../~auth-method~/...` or dot connection pattern `.../.auth-method./...`.

If necessary, a custom client ID can be configured, otherwise the authentication method name is used as the client ID.

Optionally the issuer can be changed. Otherwise read from the OpenID Configuration endpoint. Furthermore, multiple issuers can be configured to trust tokens form multiple issuers signed with the same key (often used with Microsoft Entra ID).
