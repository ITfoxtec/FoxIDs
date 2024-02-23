# Claims

Claims are processed first in the [up-party](#up-party) and then the [application registration](#application-registration), where it is possible to decide, which claims are transferred to the next step and to do [claim transforms](claim-transform.md).

> All claim comparisons are case-sensitive.

The claims process starts in the [up-party](parties.md#up-party) when a user authenticates. There it is possible to do [claim transforms](claim-transform.md) and configure which claims have to be carried forward to the next step.
Then the claims process continues in the [application registration](parties.md#application-registration) where it is also possible do claim transforms and configure which claims have to be issued to the application / API.

In a [Client Credentials Grant](app-reg-oauth-2.0.md#client-credentials-grant) scenario, the claims process is only done in the application registration. The same goes for the claim transforms and the configuration of which claims have to be issued to the application / API.

## Authentication method
In both an [OpenID Connect](up-party-oidc.md) and [SAML 2.0](up-party-saml-2.0.md) authentication method claims are carried forward by adding them to the `Forward claims` list. All claims are carried forward if a wildcard `*` is added to the `Forward claims` list.

An authentication method issues two claims which can be read in the application registration and used in [claim transforms](claim-transform.md). The claims always apply to the last authentication method.  
The authentication method issued claims (default forward):

- `up_party` contain the the authentication method name, the name is unique in a track.
- `up_party_type` contain the the authentication method type: `login`, `oidc` or `saml`.

A `sub` claim and an access token revived from an external Identity Provider is nested with a pipe symbol (|) after the up_party name.  
Examples: 

 - An external `sub` with the value `afeda2a3-c08b-4bbb-ab77-35138dd2ef2d` gets the nested value `the-up-party|afeda2a3-c08b-4bbb-ab77-35138dd2ef2d`
 - An external access token with the value `eyJhG.cRwczov...nNjb3B.lIjoi` is added in the `access_token` claim with the nested value `the-up-party|eyJhG.cRwczov...nNjb3B.lIjoi`

## Application registration
In both an [OpenID Connect](app-reg-oidc.md), [OAuth 2.0](app-reg-oauth-2.0.md) and [SAML 2.0](app-reg-saml-2.0.md) application registration claims are issued to the application / API by adding them to the `Issue claims` list. All claims are issued to the application / API if a wildcard `*` is added to the `Issue claims` list.

An OpenID Connect application registration can differentiate if a claim is only issued in the access token or also in the ID token.   


An OpenID Connect and OAuth 2.0 application registration can carry claims forward by a scope as well. This is done by adding the claim or claims to a scope's `Voluntary claims` list. And the claims are then issued if the client application request for the scope.  
An OpenID Connect application registration can also in the voluntary scope claims differentiate if a claim is only issued in the access token or also in the ID token.
