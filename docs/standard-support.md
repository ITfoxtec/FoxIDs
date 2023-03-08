# Supported standards

- All tokens are JSON Web Token (JWT)
  - [RFC 7519](https://tools.ietf.org/html/rfc7519)
- OpenID Connect 1.0 supported in both down-parties and up-parties
   - [OpenID Connect Core 1.0](http://openid.net/specs/openid-connect-core-1_0.html)
   - [OpenID Connect Discovery 1.0](https://openid.net/specs/openid-connect-discovery-1_0.html)
   - [OpenID Connect Session Management 1.0 ](http://openid.net/specs/openid-connect-session-1_0.html)
   - [OpenID Connect Front-Channel Logout 1.0](http://openid.net/specs/openid-connect-frontchannel-1_0.html)
   - [OpenID Connect RP-Initiated Logout 1.0](https://openid.net/specs/openid-connect-rpinitiated-1_0.html)
- Proof Key for Code Exchange (PKCE) supported in OpenID Connect down-parties and up-parties
  - [RFC 7636](https://tools.ietf.org/html/rfc7636)
- SAML 2.0 supported in both down-parties and up-parties
  - [SAML 2.0 Core](https://docs.oasis-open.org/security/saml/v2.0/saml-core-2.0-os.pdf)
  - [SAML 2.0 bindings](https://docs.oasis-open.org/security/saml/v2.0/saml-bindings-2.0-os.pdf) limited to POST and redirect binding
  - [SAML 2.0 metadata](https://docs.oasis-open.org/security/saml/v2.0/saml-metadata-2.0-os.pdf)
- OAuth 2.0 limited to down-party [Client Credential Grant](https://datatracker.ietf.org/doc/html/rfc6749#section-4.4)
  - [RFC 6749](https://datatracker.ietf.org/doc/html/rfc6749)
- Two-factor authentication (2FA) with One-Time Password (OPT)
  - [RFC 6238](https://datatracker.ietf.org/doc/html/rfc6238)