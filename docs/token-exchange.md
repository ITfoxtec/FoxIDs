# Token exchange

FoxIDs support two different sceneries of token exchange: [token exchange in the same track](#token-exchange-in-the-same-track) or [token exchange by trust to external IdP/OP](#token-exchange-by-trust). 

In the same track it is possible to do token exchange of [access tokens](#access-token-to-access-token-in-the-same-track) between resources.

By external trust it is possible to do token exchange of external [access tokens](#access-token-to-access-token-by-trust) and [SAML 2.0 tokens](#saml-20-to-access-token-by-trust) to internal access tokens.

## Samples  

- TODO
- AspNetCoreApi1Sample
- AspNetCoreOidcAuthCodeAllUpPartiesSample
- AspNetCoreSamlSample


## Token exchange in the same track

It is possible to token exchange an [access token](#access-token-to-access-token-in-the-same-track) issued to a resource and thereby obtain an access token for another resource in the track.  
A down-party client is configured to handle the token exchange and to whitelist for which resources in the track, it is allowed to do a token exchange.

### Access token to Access token in the same track

Token exchange JWT access token to JWT access token' in the same track.

In this scenario an OpenID Connect client has obtained an access token after user authentication. The client could also be a OAuth 2.0 client using [client credentials grant](#client-credentials-grant).

The track include two resources and the OpenID Connect client is allowed to call the first resource directly. On the first resource a client is configured allowing calling access tokens to be exchange to a access token' valid for the second resource.  
During the token exchange sequence the claims transformations and limitations is executed on the down-party. 

The OpenID Connect client call the first resource with the obtained access token. The resources client do a token exchange call to FoxIDs authenticating the client and passing the access token while asking for a access token' to the second resource. 
If success, the resources client get back a access token' and can now call the second resource with the access token'.

![Token exchange, Access token to Access token in the same track](images/token-exchange-access-token-same-track.svg)

## Token exchange by trust

By external trust to IdP/OP it is possible to token exchange an [access token](#access-token-to-access-token-by-trust) or [SAML 2.0 token](#saml-20-to-access-token-by-trust) issued by an external party (or another FoxIDs track) and thereby obtain an access token for a resource in the track.  
A up-party trust is configured and a down-party client is configured to allow token exchange based on the up-party trust(s). The down-party client furthermore whitelist for which resources in the track, it is allowed to do a token exchange.

### Access token to Access token by trust

Token exchange external JWT access token to internal JWT access token by external trust.

In this scenario an OpenID Connect client trust an external OpenID Provider (OP) / Identity Provider (IdP) and has obtained a access token after user authentication. The client could also be a OAuth 2.0 client using [client credentials grant](#client-credentials-grant) to obtain the external access token.

There is a resource in the track but the OpenID Connect client is NOT allowed to call the resource directly. 
A OAuth 2.0 or OpenID Connect up-party is configured to trust the external OpenID Provider (OP) / Identity Provider (IdP) and the SP issuer is configured to match the OpenID Connect clients audience. 
A OAuth 2.0 down-party client is configured to accept external access tokens by the up-party trust. It is possible to have one or more up-party trusts. It is thereby possible to token exchange a external access token to a internal access token valid for the resource.  
During the token exchange sequence both the claims transformations and limitations is executed firs on the up-party and then on the down-party. 

The OpenID Connect client backend application do a token exchange call to FoxIDs. Authenticating the internal OAuth 2.0 client and passing the external access token while asking for a access token to the second resource. 
If success, the OpenID Connect client backend application get back a access token and can now call the resource.

![Token exchange, Access token to Access token by trust](images/token-exchange-access-token-by-trust.svg)


### SAML 2.0 to Access token by trust

Token exchange external SAML 2.0 token to internal JWT access token by external trust.

In this scenario an SAML 2.0 application trust an external Identity Provider (IdP) and has obtained a SAML 2.0 token after user authentication. 

There is a resource in the track but the SAML 2.0 application is NOT allowed to call the resource directly. 
A SAML 2.0 up-party is configured to trust the Identity Provider (IdP) and the SP issuer is configured to match the SAML 2.0 applications audience. 
A OAuth 2.0 down-party client is configured to accept external SAML 2.0 tokens by the up-party trust. It is possible to have one or more up-party trusts. It is thereby possible to token exchange a external SAML 2.0 token to a internal access token valid for the resource.  
During the token exchange sequence both the claims transformations and limitations is executed firs on the up-party and then on the down-party. 

The SAML 2.0 backend application do a token exchange call to FoxIDs. Authenticating the internal OAuth 2.0 client and passing the external SAML 2.0 token while asking for a access token to the second resource. 
If success, the SAML 2.0 backend application get back a access token and can now call the resource.

![Token exchange, SAML 2.0 to Access token by trust](images/token-exchange-saml-by-trust.svg)
