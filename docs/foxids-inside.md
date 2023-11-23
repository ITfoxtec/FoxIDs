# FoxIDs inside

## Structure

FoxIDs is divided into logical elements.

- **Tenant** contain the company, organization, individual etc. security service. A tenant contains tracks.
- **Track** is a production, QA, test etc. environment. Each track is an Identity Provider with a [user repository](users.md), a unique [certificate](certificates.md) and a track contains the up-parties and down-parties.  
In some cases, it can be an advantage to place external connections in a separate tracks to configure connections specific certificates or log levels or just generalize the connections.
- **Up-party** is a upwards trust / federation with [OpenID Connect 1.0](up-party-oidc.md) and [SAML 2.0](up-party-saml-2.0.md) or [login](login.md) configuration.
- **Down-party** is a downward application configuration with [OAuth 2.0](down-party-oauth-2.0.md), [OpenID Connect 1.0](down-party-oidc.md) and [SAML 2.0](down-party-saml-2.0.md).

![FoxIDs structure](images/structure.svg)

> FoxIDs support unlimited tenants. Unlimited tracks in a tenant. Unlimited users and unlimited up-parties and down-parties in a track.

## Limitations

Basically, all strings handled in FoxIDs is limited in one way or the other for performance and security reasons. Strings is either truncated or an exception is thrown if they exceed the maximum allowed length. 

The most important limitations are listed below.

**URL**  
The URLs maximum allowed length is 10k (10,240) characters. The subsequently query strings maximum allowed length is also 10k (10,240) characters.

**Claim**  
A claim has both at type and a value. The claim types maximum allowed length is 80 characters for JWT (access tokens and ID tokens) and 300 characters for SAML 2.0. 
When a token and thereby claim values is processed by FoxIDs the maximum length per value and combined length is 50,000 characters.

**Tokens**   
A JWT (access tokens, ID tokens and refresh token) revived by FoxIDs is a allowed to have a maximum length of 50,000 characters. Claims revived is truncated if they exceed the maximum allowed lengths.  
FoxIDs can create larger tokens, where each claim is capped instead of the entire token.

If a JWT is included as a claim it is truncated if it exceeds the maximum allowed claim value length. 

A SAML 2.0 request / response is allowed to have a maximum length of 100,000 characters. The request is indirectly limited if it is send using a redirect binding in the URL query string. 
Claims revived in a SAML 2.0 authn response (SAML 2.0 token) is truncated if they exceed the maximum allowed lengths.