# OAuth 2.0

FoxIDs support OAuth 2.0 as a down-party. OAuth 2.0 is not supported as an up-party.

![FoxIDs OAuth 2.0](images/parties-oauth.svg)

FoxIDs support down-party OAuth 2.0 Client Credentials Authorization Grant and not the remaining Authorization Grants. Instead, OpenID Connect is used because it is more secure.  

FoxIDs support OAuth 2.0 resource (API) as a down-party.

## Down-party

FoxIDs [down-party Client Credentials Grant](down-party-oauth-2.0.md#client-credentials-grant).

The client can request an access token for multiple APIs defined as [down-party OAuth 2.0 resources](down-party-oauth-2.0.md#oauth-20-resource).

## Client secrets
It is important to store client secrets securely, therefor client secrets are hashed with the same [hash algorithm](login.md#password-hash) as passwords. If the secret is more than 20 character (which it should be) the first 3 characters is saved as information and is shown for each secret in FoxIDs Control. 


