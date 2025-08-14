# .NET Samples
The FoxIDs .NET samples show:

- User login and logout with OpenID Connect 1.0 and SAML 2.0
- Client credential grant with secret and certificate
- API calls secured with OAuth 2.0 
- Token exchange from access token to access token and SAML 2.0 to access token

Find the sample source in the [FoxIDs.Samples](https://github.com/ITfoxtec/FoxIDs.Samples) repository.

The samples are pre-configured in the FoxIDs online test tenant `test-corp` and can immediately run locally in Visual Studio on the pre-configured localhost ports.   
 
> Take a look at the FoxIDs test configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!`

You can alternatively configure the samples in [your one FoxIDs environment](#configure-samples-in-foxids-environment).

**Sample index**

- ASP.NET Core - OpenID Connect - application registration
    -  [AspNetCoreOidcAuthCodeAllUpPartiesSample](#aspnetcoreoidcauthcodealluppartiessample) ([online](https://aspnetoidcsample.itfoxtec.com)) <- *Good starting point!*
    -  [AspNetCoreOidcAuthorizationCodeSample](#AspNetCoreOidcAuthorizationCodeSample)
    -  [AspNetCoreOidcImplicitSample](#aspnetcoreoidcimplicitsample)

- ASP.NET Core - SAML 2.0 - application registration

    -  [AspNetCoreSamlSample](#aspnetcoresamlsample)

- Blazor - OpenID Connect - application registration

    -  [BlazorBFFAspNetCoreOidcSample](#blazorbffaspnetcoreoidcsample)
    -  [BlazorOidcPkceSample](#blazoroidcpkcesample)
    -  [BlazorServerOidcSample](#blazorserveroidcsample)

- Console app - OAuth 2.0 Client credential grant - application registration

    -  [NetCoreClientCredentialGrantConsoleSample](#netcoreclientcredentialgrantconsolesample)
    -  [NetCoreClientCredentialGrantAssertionConsoleSample](#netcoreclientcredentialgrantassertionconsolesample)
    -  [NetFramework4.7ClientCredentialGrantAssertionConsoleSample](#netframework47clientcredentialgrantassertionconsolesample)

- ASP.NET Core API - OAuth 2.0 - application registration

    -  [AspNetCoreApi1Sample](#aspnetcoreapi1sample) ([online](https://aspnetapi1sample.itfoxtec.com))
    -  [AspNetCoreApi2Sample](#aspnetcoreapi2sample) ([online](https://aspnetapi2sample.itfoxtec.com))
    -  [AspNetCoreApiOAuthTwoIdPsSample](#aspnetcoreapioauthtwoidpssample)

- ASP.NET Core - OpenID Connect - authentication method

    -  [IdentityServerOidcOpSample](#identityserveroidcopsample)

- ASP.NET Core - SAML 2.0 - authentication method
  
    -  [AspNetCoreSamlIdPSample](#aspnetcoresamlidpsample) ([online](https://aspnetsamlidpsample.itfoxtec.com))

- ASP.NET Core - External APIs
  
    -  [ExternalLoginApiSample](#externalloginapisample) ([online](https://externalloginsample.itfoxtec.com))
    -  [ExternalClaimsApiSample](#externalclaimsapisample)
    -  [ExternalPasswordApiSample](#externalpasswordapisample)

- Console app - FoxIDs Control API
  
    -  [FoxIDsControlApiSample](#foxidscontrolapisample)

> You can use the [JWT tool](https://www.foxids.com/tools/Jwt) and [SAML 2.0 tool](https://www.foxids.com/tools/Saml) to decode tokens and create self-signed certificates with the [certificate tool](https://www.foxids.com/tools/Certificate).

## Sample applications

The samples show different applications which trust FoxIDs as an IdP.

### AspNetCoreOidcAuthCodeAllUpPartiesSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreOidcAuthCodeAllUpPartiesSample))  application showing user login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider and requesting login by all authentication methods.  

You can test this [sample online](https://aspnetoidcsample.itfoxtec.com).

> This sample is a good starting point!

The possible authentication methods is configured in the application registration as allowed authentication methods. There can be configured one to many allowed authentication methods. 
All the configured authentication methods is selected with a star instead of an authentication method name.

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and all other authentication methods.

API calls:

 - Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 
 - Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token which again call [API2 sample](#aspnetcoreapi2sample) using token exchange insight API1. 
 - Show how to obtained an access token by token exchange, the client use client authentication method client_secret_post. And then call the [API2 sample](#aspnetcoreapi2sample) secured with the obtained access token.

Local development domain and port: `https://localhost:44349`


### AspNetCoreOidcAuthorizationCodeSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreOidcAuthorizationCodeSample)) application showing user login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider.  

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44340`

### AspNetCoreOidcImplicitSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreOidcImplicitSample)) application showing user login and logout with OpenID Connect (OIDC) using implicit flow as a service provider.

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44341`

### AspNetCoreSamlSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreSamlSample)) application showing user login and logout with SAML 2.0 as a relying party.

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Show how to obtained an access token from an SAML 2.0 bearer token with token exchange, the client use client authentication method private_key_jwt (certificate). And then call the [API1 sample](#aspnetcoreapi1sample) secured with the obtained access token.

Local development domain and port: `https://localhost:44343`

### NetCoreClientCredentialGrantConsoleSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/NetCoreClientCredentialGrantConsoleSample)) console application (backend) showing client authentication with OAuth 2.0 Client Credentials Grant using a secret (client authentication method client_secret_post).

Show how to call the [API1 sample](#aspnetcoreapi1sample) and [API with two IdPs sample](#AspNetCoreApiOAuthTwoIdPsSample) secured with an access token. 

### NetCoreClientCredentialGrantAssertionConsoleSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/NetCoreClientCredentialGrantAssertionConsoleSample)) console application (backend) showing client authentication with OAuth 2.0 Client Credentials Grant using a certificate (client authentication method private_key_jwt).

Show how to call the [API1 sample](#aspnetcoreapi1sample) and [API with two IdPs sample](#AspNetCoreApiOAuthTwoIdPsSample) secured with an access token. 

### NetFramework4.7ClientCredentialGrantAssertionConsoleSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/NetFramework4.7ClientCredentialGrantAssertionConsoleSample)) .NET Framework 4.7 console application (backend) showing client authentication with OAuth 2.0 Client Credentials Grant using a certificate (client authentication method private_key_jwt).

Show how to call the [API1 sample](#aspnetcoreapi1sample) and [API with two IdPs sample](#AspNetCoreApiOAuthTwoIdPsSample) secured with an access token. 

### BlazorBFFAspNetCoreOidcSample

Sample (code link [client](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/BlazorBFFAspNetOidcSample.Client) and [server](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/BlazorBFFAspNetOidcSample.Server)) application showing user login and logout with OpenID Connect (OIDC) using authorization code flow in a Blazor BFF (Backend For Frontend) application with a ASP.NET Core backend.  
In a BFF architecture the backend handles OIDC, the tokens are never shared with the Blazor client. Instead a session based on an identity cookie secure the application after successfully user authentication.

The sample show how to call the [API1 sample](#aspnetcoreapi1sample) from both the Blazor client through a backend API proxy which add the access token to the outgoing API call. 

Local development domain and port: `https://localhost:44348`

### BlazorOidcPkceSample

Blazor sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/BlazorOidcPkceSample)) application showing user login and logout with OpenID Connect (OIDC) using authorization code flow and PKCE as a service provider.

Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44345`

### BlazorServerOidcSample

Blazor server sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/BlazorServerOidcSample)) application showing user login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider.

Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44347`

### AspNetCoreApi1Sample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreApi1Sample)) API showing how to secure an API with an access token and how to restrict access by scopes.

You can call this [sample online](https://aspnetapi1sample.itfoxtec.com).

The API calls [API2 sample](#aspnetcoreapi2sample) secured with an access token obtained by token exchange where the client use client authentication method private_key_jwt (certificate).

Local development domain and port: `https://localhost:44344`

### AspNetCoreApi2Sample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreApi2Sample)) API showing how to secure an API with an access token and how to restrict access by a scope.

You can call this [sample online](https://aspnetapi2sample.itfoxtec.com).

Local development domain and port: `https://localhost:44351`

### AspNetCoreApiOAuthTwoIdPsSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreApiOAuthTwoIdPsSample)) API showing how to create an API which can accept access tokens from two different IdPs. Each IdP - API relation can be configured with individual resource IDs and scopes.

This scenario occurs most often in a transitional period moving from one IdP to another IdP. Having APIs with dual IdP support the clients can be moved from one IdP to another IdP independent of the APIs.

The sample API can be called by changing comment out code in the [NetCoreClientCredentialGrantConsoleSample](#netcoreclientcredentialgrantconsolesample) or [NetCoreClientCredentialGrantAssertionConsoleSample](#netcoreclientcredentialgrantassertionconsolesample) samples.

Local development domain and port: `https://localhost:44350`

## Authentication methods samples

The authentication methods samples show different IdPs connected to FoxIDs, where FoxIDs trust the IdP samples.

### AspNetCoreSamlIdPSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/AspNetCoreSamlIdPSample)) application implementing a SAML 2.0 identity provider (IdP) connected as a SAML 2.0 authentication method in FoxIDs.
The sample also show how to do IdP-Initiated login.

You can test this [sample online](https://aspnetsamlidpsample.itfoxtec.com).

Local development domain and port: `https://localhost:44342`

### IdentityServerOidcOpSample

Identity Server ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/IdentityServerOidcOpSample)) implementing OpenID Connect (OIDC) exposing a OpenID Provider (OP) / identity provider (IdP) connected as a OpenID Connect authentication method in FoxIDs.

Local development domain and port: `https://localhost:44346`

### ExternalLoginApiSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalLoginApiSample)) application implementing an external login API which is connected as a external login authentication method in FoxIDs.

Local development domain and port: `https://localhost:44352`

### ExternalClaimsApiSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalClaimsApiSample)) application implementing an external claims API which can be configured and called from a claims transform. The external claims API can then add external claims to the issued claims.

Local development domain and port: `https://localhost:44353`

### ExternalPasswordApiSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalPasswordApiSample)) application implementing an external password API which is connected as an external password API in a FoxIDs environment.

Local development domain and port: `https://localhost:44354`

### FoxIDsControlApiSample

Sample ([code link](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/FoxIDs.ControlApiSample)) console application showing how to call FoxIDs Control API.

## Configure samples in FoxIDs environment

The samples can be configured in a FoxIDs environment with the sample seed tool or manually through the FoxIDs Control Client.  

> The sample seed tool is found in the sample solution: tools/SampleSeedTool.

### Configure the sample seed tool

> The sample seed tool is configured in the `appsettings.json` file.

Add the FoxIDs and FoxIDs Control API endpoints to the sample seed tool configuration. They can be added by updating the instance names `https://foxids.com` and `https://control.foxids.com/api`. If you are running FoxIDs locally in Visual Studio the endpoints is configured to FoxIDs localhost `https://localhost:44330/` and FoxIDs Control API localhost `https://localhost:44331/`.

```json
"SeedSettings": {
    "FoxIDsEndpoint": "https://foxids.com", 
    "FoxIDsConsolApiEndpoint": "https://control.foxids.com/api"
}
```

> Access to create the sample configuration in a environment is granted in the `master` environment. The sample configuration should not be added to the `master` environment.

Create a sample seed tool OAuth 2.0 client in the [FoxIDs Control Client](control.md#foxids-control-client):

1. Select the `master` environment and create a OAuth 2.0 application registration, click `OAuth 2.0 - Client Credentials Grant`.
2. Set the client id to `sample_seed`.
3. Remember the client secret.
4. In the resource and scopes section. Grant the sample seed client access to the FoxIDs Control API resource `foxids_control_api` with the scope `foxids:tenant`.
5. Click show advanced. 
6. In the issue claims section. Add a claim with the name `role` and the value `foxids:tenant.admin`. This will granted the client the administrator role. 

The sample seed tool client is thereby granted access to update to the tenant.

![FoxIDs Control Client - sample_seed client](images/sample_seed-client.png)

Create a new FoxIDs environment for the sample applications or select an existing environment.

Change the tenant, the environment and the sample seed tool client secret in the sample seed tool configured. 

```json
"SeedSettings": {
  "Tenant": "xxx",
  "Track": "xxx",
  "DownParty": "sample_seed",
  "ClientSecret": "xxx"
}
```

> Change the tenant and the environment configuration for all the samples. 

### Run the sample seed tool

Run the sample seed tool executable SampleSeedTool.exe or run the seed tool directly from Visual Studio. 

* Click 'c' to create the sample configuration 
* Click 'd' to delete the sample configuration

The sample seed tool will create and delete configurations for all samples.

The sample applications require a login authentication method with the name `login` (handles user login). It is created by the sample seed tool if it do not exists. The login authentication method is not deleted if the sample configuration is deleted.
