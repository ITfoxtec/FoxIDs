# OpenID Connect

FoxIDs support OpenID Connect as both up-party and down-party.

![FoxIDs OpenID Connect](images/parties-oidc.svg)

## Up-party

Configure [up-party OpenID Connect](up-party-oidc.md) which trust an external OpenID Provider (OP).

How to guides:

- Connect [FoxIDs track](up-party-howto-oidc-foxids.md) 
- Connect [Azure AD](up-party-howto-oidc-azure-ad.md) 
- Connect [IdentityServer](up-party-howto-oidc-identityserver.md)

## Down-party

Configure your application as a [down-party OpenID Connect](down-party-oidc.md).

Besides receiving an ID token the client can request an access token for multiple APIs defined as [down-party OAuth 2.0 resources](down-party-oauth-2.0.md#oauth-20-resource).  
An OAuth 2.0 resource can optionally be defined in a down-party OpenID Connect or a down-party OAuth 2.0.

