# Samples
The samples for FoxIDs show login and logout with OpenID Connect 1.0 and SAML 2.0 and API call secured with OAuth 2.0. The samples is located in the [FoxIDs.Samples](https://github.com/ITfoxtec/FoxIDs.Samples) repository.

> The samples are pre-configured in the [FoxIDs.com test track](#foxidscom-test-tenant-for-samples) and can immediately run locally in Visual Studio on the pre-configured localhost ports.   
You can also configure the samples in [your one FoxIDs track](#configure-samples-in-foxids-track).

> You can use the [JWT tool](https://www.foxids.com/tools/Jwt) and [SAML 2.0 tool](https://www.foxids.com/tools/Saml) to decode tokens and create self-signed certificates with [certificate tool](https://www.foxids.com/tools/Certificate).

## Sample applications

The sample Visual Studio solution contain the following sample applications.

### AspNetCoreOidcAuthorizationCodeSample

Sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider.  
Show how to call the [API sample](#aspnetcoreapi1sample) secured with an access token. 

Support login/(logout) with FoxIDs login page, [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44340`

### AspNetCoreOidcImplicitSample

Sample application showing login and logout with OpenID Connect (OIDC) using implicit flow as a service provider.

Support login/(logout) with FoxIDs login page, [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44341`

### AspNetCoreSamlIdPSample

Sample application implementing a SAML 2.0 identity provider (IdP) making it possible to configure at sample SAML 2.0 IdP in the FoxIDs track.

Local development domain and port: `https://localhost:44342`

### AspNetCoreSamlSample

Sample application showing login and logout with SAML 2.0 as a relying party.

Support login/(logout) with FoxIDs login page, [SAML 2.0 IdP sample](#aspnetcoresamlidpsample) and if configured [AD FS using SAML 2.0](saml-2.0.md#connecting-ad-fs).

Local development domain and port: `https://localhost:44343`

### NetCoreClientGrantConsoleSample

Sample console application (backend) showing client login with OAuth 2.0 Client Credentials Grant.  
Show how to call the [API sample](#aspnetcoreapi1sample) secured with an access token. 

### AspNetCoreApi1Sample

Sample API showing how to secure an API with an access token and how to restrict access by a scope.

Local development domain and port: `https://localhost:44344`

### BlazorOidcPkceSample

Blazor sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow and PKCE as a service provider.  
Show how to call the [API sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44345`

### BlazorServerOidcSample

Blazor server sample application showing login and logout with OpenID Connect (OIDC) using authorization code flow as a service provider.  
Show how to call the [API sample](#aspnetcoreapi1sample) secured with an access token. 

Local development domain and port: `https://localhost:44347`

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

Create the sample seed OAuth 2.0 client in the FoxIDs Control Client:

1. Select the `master` track and create a OAuth 2.0 down-party.
2. Set the Client to on and the Resource to off.
3. Set the client id to `sample_seed`, redirect Uri to `uri:sample:seed:client` and response type to `token`. 
4. Set Require PKCE to off.
5. Add a client secret and Remember the secret.
6. In the resource and scopes section. Remove the default resource scope and give the sample seed client access to the FoxIDs Control API resource `foxids_control_api` with the scope `foxids:tenant`.
7. In the scopes section. Remove all scopes.
8. Click show advanced settings. 
9. In the claims section. Granted the client the administrator `role` with the value `foxids:tenant.admin`. 

The sample seed client is thereby granted access to update the tenant.

![FoxIDs Control Client - sample_seed client](images/sample_seed-client.png)

Create a new FoxIDs track for the sample applications or select an existing track.

Change the tenant, the track and the sample seed tool client secret in the sample seed tool configured. 

```json
"SeedSettings": {
  "Tenant": "xxx",
  "Track": "xxx",
  "DownParty": "sample_seed",
  "RedirectUri": "uri:sample:seed:client",
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
