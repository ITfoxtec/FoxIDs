# Claims

Claims are processed first in the [up-party](#up-party) and then the [down-party](#down-party), where it is possible to decide, which claims are transferred to the next step and to do [claim transforms](claim-transform.md).

> All claim comparisons are case-sensitive.

The claims process starts in the [up-party](parties.md#up-party) when a user authenticates. There it is possible to do [claim transforms](claim-transform.md) and configure which claims have to be carried forward to the next step.
Then the claims process continues in the [down-party](parties.md#down-party) where it is also possible do claim transforms and configure which claims have to be issued to the application / API.

In a [Client Credentials Grant](down-party-oauth-2.0.md#client-credentials-grant) scenario, the claims process is only done in the down-party. The same goes for the claim transforms and the configuration of which claims have to be issued to the application / API.

## Up-party
In both an [OpenID Connect](up-party-oidc.md) and [SAML 2.0](up-party-saml-2.0.md) up-party claims are carried forward by adding them to the `Forward claims` list. All claims are carried forward if a wildcard `*` is added to the `Forward claims` list.

An up-party issues two claims which can be read in the down-party and used in [claim transforms](claim-transform.md). The claims always apply to the last up-party.  
The up-party issued claims (default forward):

- `up_party` contain the the up-party name, the name is unique in a track.
- `up_party_type` contain the the up-party type: `login`, `oidc` or `saml`.

A `sub` claim and an access token revived from an external Identity Provider is nested with a pipe symbol (|) after the up_party name.  
Examples: 

 - An external `sub` with the value `afeda2a3-c08b-4bbb-ab77-35138dd2ef2d` gets the nested value `the-up-party|afeda2a3-c08b-4bbb-ab77-35138dd2ef2d`
 - An external access token with the value `eyJhG.cRwczov...nNjb3B.lIjoi` is added in the `access_token` claim with the nested value `the-up-party|eyJhG.cRwczov...nNjb3B.lIjoi`

## Down-party
In both an [OpenID Connect](down-party-oidc.md), [OAuth 2.0](down-party-oauth-2.0.md) and [SAML 2.0](down-party-saml-2.0.md) down-party claims are issued to the application / API by adding them to the `Issue claims` list. All claims are issued to the application / API if a wildcard `*` is added to the `Issue claims` list.

An OpenID Connect down-party can differentiate if a claim is only issued in the access token or also in the ID token.   


An OpenID Connect and OAuth 2.0 down-party can carry claims forward by a scope as well. This is done by adding the claim or claims to a scope's `Voluntary claims` list. And the claims are then issued if the client application request for the scope.  
An OpenID Connect down-party can also in the voluntary scope claims differentiate if a claim is only issued in the access token or also in the ID token.
