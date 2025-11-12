<!--
{
    "title":  "OAuth 2.0 application registration",
    "description":  "FoxIDs OAuth 2.0 application registration enable you to connect an APIs as OAuth 2.0 resources. And connect your backend service using Client Credentials Grant.",
    "ogTitle":  "OAuth 2.0 application registration",
    "ogDescription":  "FoxIDs OAuth 2.0 application registration enable you to connect an APIs as OAuth 2.0 resources. And connect your backend service using Client Credentials Grant.",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "app reg oauth 2.0, FoxIDs docs"
                       }
}
-->

# OAuth 2.0 application registration

FoxIDs OAuth 2.0 application registration enable you to connect an APIs as [OAuth 2.0 resources](#oauth-20-resource). And connect your backend service using [Client Credentials Grant](#client-credentials-grant).

![FoxIDs OAuth 2.0 application registration](images/connections-app-reg-oauth.svg)

## OAuth 2.0 Resource
An API is configured as a OAuth 2.0 application registration resource.

- Click New application and then OAuth 2.0 - Resource (API)
- Specify resource (API) name in application registration name.
- Specify one or more scopes.

![Resource with scopes](images/configure-oauth-resource.png)

A client can subsequently be given access by configuring [resource and scopes](app-reg-oidc.md#resource-and-scopes) in the client.

## Client Credentials Grant
An application using Client Credentials Grant could be a backend service secured by a client id and secret or key.

- Click New application and then OAuth 2.0 - Client Credentials Grant
- Specify client name in application registration name.
- Specify client authentication method, default `client secret post`
    - A secret is default generated
    - Optionally change to another client authentication method
      - Select show advanced
      - Select client authentication method: `client secret basic` or `private key JWT`
      - If `private key JWT` is selected, upload a client certificate (pfx file)
- Optionally grant the client access to call the `party-api2` resource (API) with the `read1` and `read2` scopes.

![Configure Client Credentials Grant](images/configure-client-credentials-grant.png)

Access tokens can be issued with a list of audiences and thereby be issued to multiple APIs defined in FoxIDs as OAuth 2.0 resources.

> You can change the claims and do claim tasks with [claim transforms and claim tasks](claim-transform-task.md).


### Authenticate with certificate as client credential

The client can authenticate with a certificate, if `private key JWT` is selected as client authentication method and a client certificate has been uploaded. 

Sample Client Credentials Grant with `private key JWT` POST request to the token endpoint:

```plaintext 
POST https://foxids.com/test-corp/-/my-backend-client(*)/oauth/token HTTP/1.1
Host: foxids.com
Content-Type: application/x-www-form-urlencoded

client_assertion_type=urn%3Aietf%3Aparams%3Aoauth%3Aclient-assertion-type%3Ajwt-bearer
&client_assertion=eyJhbGcrOiI...kyX3NhbXBsZS
&grant_type=client_credentials
&scope=party-api2%3Aread1
```


### Client secrets
It is important to store client secrets securely, therefor client secrets are hashed inside FoxIDs with the same [hash algorithm](login.md#password-hash) as passwords. If the secret is more than 20 character (which it should be) the first 3 characters is saved as information and is shown for each secret in FoxIDs Control. 


## Resource Owner Password Credentials Grant
Resource Owner Password Credentials Grant is not supported for security reasons because it is insecure and should not be used.


