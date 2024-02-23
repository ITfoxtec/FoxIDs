# Foxids parties

Foxids is configured with authentication methods and application registrations. Authentication methods authenticate the internal users or optionally by trust to an external Identity Provider (IdP). Applications and APIs are connected to Foxids as application registrations.

![Foxids authentication methods and application registrations](images/parties.svg)

There are four different connection types:

- [Login](login.md)
- [OpenID Connect](oidc.md)
- [OAuth 2.0](oauth-2.0.md)
- [SAML 2.0](saml-2.0.md)

## Authentication method

Foxids support tree different authentication method types:

- [Login authentication method](login.md)
- [OpenID Connect authentication method](auth-met-oidc.md)
- [SAML 2.0 authentication method](auth-met-saml-2.0.md)


### Authentication method session
Each authentication method creates a session when a user is authenticated. All sessions are separately connected to an authentication method. There are two different kinds of sessions.
A login authentication method create a [user session](login.md#configure-user-session). An OpenID Connect authentication method and SAML 2.0 authentication method create an authentication method session which only holds information to enable logout. 

Both session types lifetime, absolute lifetime and persistence (if the session should be saved when the browser is closed) can be configured.


## Application registration

Foxids support tree different application registration types:

- [OpenID Connect application registration](app-reg-oidc.md)
- [OAuth 2.0 application registration](app-reg-oauth-2.0.md)
- [SAML 2.0 application registration](app-reg-saml-2.0.md)

## JWT and SAML 
OpenID Connect, OAuth 2.0, JWT and JWT claims are first class citizens in Foxids. Internally claims are always represented as JWT claims and request / response properties are described with OAuth 2.0 and OpenID Connect attributes. 

Foxids converts between standards where attributes are converted to the same internal representation using JWT claims and OAuth 2.0 / OpenID Connect attributes.  
Therefor, SAML 2.0 claims is internally converted to JWT claims between authentication method and application registration.
