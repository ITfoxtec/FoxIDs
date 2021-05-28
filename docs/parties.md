# FoxIDs parties

FoxIDs is configured with up-parties and down-parties. 

- Up-parties authenticate the users optionally by trust to an external identity provider (IdP). 
- Applications and APIs are connected to FoxIDs as down-parties.

![FoxIDs up-parties and down-parties](images/parties.svg)

There are four different party types:

- [Login](login.md)
- [OpenID Connect](oidc.md)
- [OAuth 2.0](oauth-2.0.md)
- [SAML 2.0](saml-2.0.md)

## Up-party

FoxIDs support tree different up-party types:

- [Login](login.md)
- [OpenID Connect](up-party-oidc.md)
- [SAML 2.0]( up-party-saml-2.0.md)

## Down-party

FoxIDs support tree different down-party types:

- [OpenID Connect](down-party-oidc.md)
- [OAuth 2.0](down-party-oauth-2.0.md)
- [SAML 2.0](down-party-saml-2.0.md)

## JWT and SAML 
OpenID Connect, OAuth 2.0, JWT and JWT claims are first class citizens in FoxIDs. Internally claims are always represented as JWT claims and request / response properties are described with OAuth 2.0 and OpenID Connect attributes. 

FoxIDs converts between standards where attributes are converted to the same internal representation using JWT claims and OAuth 2.0 / OpenID Connect attributes.  
Therefor, SAML 2.0 claims is internally converted to JWT claims between up-party and down-party.
