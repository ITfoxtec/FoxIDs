# Samples
The samples for FoxIDs show login and logout with OpenID Connect 1.0 and SAML 2.0 and API call secured with OAuth 2.0. The samples is located in the [FoxIDs.Samples](https://github.com/ITfoxtec/FoxIDs.Samples) repository.

> The samples are pre-configured in the [FoxIDs.com test track](#foxidscom-test-tenant-for-samples) and can immediately run locally in Visual Studio on the pre-configured localhost ports.   
You can also configure the samples in [your one FoxIDs track](#configure-samples-in-foxids-track).

> You can use the [JWT tool](https://www.foxids.com/tools/Jwt) and [SAML 2.0 tool](https://www.foxids.com/tools/Saml) to decode tokens and create self-signed certificates with the [certificate tool](https://www.foxids.com/tools/Certificate).

The sample Visual Studio solution contain the following down-party and up-party sample applications.

## Down-party OpenId Connect sample applications

### AspNetCoreOidcAuthCodeAllUpPartiesSample

Sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider and requesting login by all up-parties.  

Show API calls:

 - Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 
 - Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token which again call [API2 sample](#aspnetcoreapi2sample). 
 - Show how to obtained an access token by token exchange and call the [API2 sample](#aspnetcoreapi2sample) secured with the obtained access token, the client use client authentication method client_secret_post.

The possible up-parties is configured in the down-party as allowed up-parties. There can be configured one to many allowed up-parties. 
All the configured up-parties is selected with a star indited of an up-party name.

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and all other up-parties.

Local development domain and port: `https://localhost:44349`


### AspNetCoreOidcAuthorizationCodeSample

Sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider.  
Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44340`

### AspNetCoreOidcImplicitSample

Sample application showing login and logout with OpenID Connect (OIDC) using implicit flow as a service provider.

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44341`

### AspNetCoreSamlSample

Sample application showing login and logout with SAML 2.0 as a relying party.

Show how to obtained an access token by token exchange and call the [API1 sample](#aspnetcoreapi1sample) secured with the obtained access token, the client use client authentication method private_key_jwt (certificate).

Support login/logout with FoxIDs login page, [Identity Server](#identityserveroidcopsample), [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44343`

### NetCoreClientCredentialGrantConsoleSample

Sample console application (backend) showing client login with OAuth 2.0 Client Credentials Grant using a secret (client authentication method client_secret_post).  
Show how to call the [API1 sample](#aspnetcoreapi1sample) and [API with two IdPs sample](#AspNetCoreApiOAuthTwoIdPsSample) secured with an access token. 

### NetCoreClientCredentialGrantAssertionConsoleSample

Sample console application (backend) showing client login with OAuth 2.0 Client Credentials Grant using a certificate (client authentication method private_key_jwt).  
Show how to call the [API1 sample](#aspnetcoreapi1sample) and [API with two IdPs sample](#AspNetCoreApiOAuthTwoIdPsSample) secured with an access token. 

### BlazorBFFAspNetCoreOidcSample

Sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow in a Blazor BFF (Backend For Frontend) application with a ASP.NET Core backend.  
In a BFF architecture the backend handles OIDC, the tokens are never shared with the Blazor client. Instead a session based on an identity cookie secure the application after successfully user authentication.  
The sample show how to call the [API1 sample](#aspnetcoreapi1sample) from both the Blazor client and a ASP.NET page securing the call with an access token. 

Local development domain and port: `https://localhost:44348`

### BlazorOidcPkceSample

Blazor sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow and PKCE as a service provider.  
Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44345`

### BlazorServerOidcSample

Blazor server sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider.  
Show how to call the [API1 sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44347`

## Down-party OAuth 2.0 sample applications

### AspNetCoreApi1Sample

Sample API showing how to secure an API with an access token and how to restrict access by a scope.

The API calls [API2 sample](#aspnetcoreapi2sample) secured with an access token obtained by token exchange and the client use client authentication method private_key_jwt (certificate).

Local development domain and port: `https://localhost:44344`

### AspNetCoreApi2Sample

Sample API showing how to secure an API with an access token and how to restrict access by a scope.

Local development domain and port: `https://localhost:44351`

### AspNetCoreApiOAuthTwoIdPsSample

Sample API showing how to create an API which can accept access tokens from two different IdPs. Each IdP - API relation can be configured with individual resource ID and scopes.

This scenario occurs most often in a transitional period moving from one IdP to another IdP. Having APIs with dual IdP support the clients can be moved from one IdP to another IdP independent of the APIs.

The sample API can be called by changing comment out code in the [NetCoreClientGrantConsoleSample](#netcoreclientgrantconsolesample).

Local development domain and port: `https://localhost:44350`

## Up-party sample applications

### AspNetCoreSamlIdPSample

Sample application implementing a SAML 2.0 identity provider (IdP) making it possible to configure at sample SAML 2.0 IdP in the FoxIDs track.

Local development domain and port: `https://localhost:44342`

### IdentityServerOidcOpSample

Identity Server implementing OpenID Connect (OIDC) exposing a OpenID Provider (OP) / identity provider (IdP) making it possible to configure at sample OpenID Provider in the FoxIDs track.

Local development domain and port: `https://localhost:44346`

## FoxIDs.com test tenant for samples
//TODO

## Configure samples in FoxIDs track

The samples can be configured in a FoxIDs track with the sample seed tool or manually through the FoxIDs Control Client.  

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

> Access to create the sample configuration in a track is granted in the `master` track. The sample configuration should not be added to the `master` track.

Create a sample seed tool OAuth 2.0 client in the [FoxIDs Control Client](control.md#foxids-control-client):

1. Select the `master` track and create a OAuth 2.0 down-party, click `OAuth 2.0 - Client Credentials Grant`.
2. Set the client id to `sample_seed`.
3. Remember the client secret.
4. In the resource and scopes section. Grant the sample seed client access to the FoxIDs Control API resource `foxids_control_api` with the scope `foxids:tenant`.
5. Click show advanced settings. 
6. In the issue claims section. Add a claim with the name `role` and the value `foxids:tenant.admin`. This will granted the client the administrator role. 

The sample seed tool client is thereby granted access to update to the tenant.

![FoxIDs Control Client - sample_seed client](images/sample_seed-client.png)

Create a new FoxIDs track for the sample applications or select an existing track.

Change the tenant, the track and the sample seed tool client secret in the sample seed tool configured. 

```json
"SeedSettings": {
  "Tenant": "xxx",
  "Track": "xxx",
  "DownParty": "sample_seed",
  "ClientSecret": "xxx"
}
```

> Change the tenant and the track configuration for all the samples. 

### Run the sample seed tool

Run the sample seed tool executable SampleSeedTool.exe or run the seed tool directly from Visual Studio. 

* Click 'c' to create the sample configuration 
* Click 'd' to delete the sample configuration

The sample seed tool will create and delete configurations for all samples.

The sample applications require a login up-party with the name `login` (handles user login). It is created by the sample seed tool if it do not exists. The login up-party is not deleted if the sample configuration is deleted.
