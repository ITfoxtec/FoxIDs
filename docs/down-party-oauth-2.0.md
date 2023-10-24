# Down-party - OAuth 2.0 

FoxIDs down-party [OAuth 2.0](https://datatracker.ietf.org/doc/html/rfc6749) enable you to connect an APIs as [OAuth 2.0 resources](#oauth-20-resource). And connect your backend service using [Client Credentials Grant](#client-credentials-grant).

![FoxIDs down-party OAuth 2.0](images/parties-down-party-oauth.svg)

## OAuth 2.0 Resource
An API is configured as a down-party OAuth 2.0 resource.

- Click Create Down-party and then OAuth 2.0 - Resource (API)
- Specify resource (API) name in down-party name.
- Specify one or more scopes.

![Resource with scopes](images/configure-oauth-resource.png)

A client can subsequently be given access by configuring [resource and scopes](down-party-oidc.md#resource-and-scopes) in the client.

## Client Credentials Grant
An application using [Client Credentials Grant](https://datatracker.ietf.org/doc/html/rfc6749#section-4.4) could be a backend service secured by a client id and secret or key.

- Click Create Down-party and then OAuth 2.0 - Client Credentials Grant
- Specify client name in down-party name.
- Specify client authentication method, default `client secret post`
    - A secret is default generated
    - Optionally change to another client authentication method
      - Select show advanced settings
      - Select client authentication method: `client secret basic` or `private key JWT`
      - If `private key JWT` is selected, upload a client certificate (pfx file)
- Optionally grant the client access to call the `party-api2` resource (API) with the `read1` and `read2` scopes.

![Configure Client Credentials Grant](images/configure-client-credentials-grant.png)

Access tokens can be issued with a list of audiences and thereby be issued to multiple APIs defined in FoxIDs as OAuth 2.0 resources.

> Change the claims the down-party pass on with [claim transforms](claim-transform.md).

## Resource Owner Password Credentials Grant
[Resource Owner Password Credentials Grant](https://datatracker.ietf.org/doc/html/rfc6749#section-4.3) is not supported for security reasons because it is insecure and should not be used.

## Client secrets
It is important to store client secrets securely, therefor client secrets are hashed inside FoxIDs with the same [hash algorithm](login.md#password-hash) as passwords. If the secret is more than 20 character (which it should be) the first 3 characters is saved as information and is shown for each secret in FoxIDs Control. 
