# FoxIDs Control
FoxIDs is configured with FoxIDs Control which consists of [FoxIDs Control Client](#foxids-control-client) and [FoxIDs Control API](#foxids-control-api). FoxIDs Control Client and API is secured by FoxIDs and FoxIDs Control Client relay on FoxIDs Control API. 

FoxIDs Control API contain all the configuration functionality. Therefore, it is possible to automate the configuration by integrating with FoxIDs Control API.

## FoxIDs Control Client
FoxIDs Control Client is a Blazor WebAssembly (WASM) app.

### Tenant and master track
If you use FoxIDs at [FoxIDs.com](https://foxids.com). Your one tenant will be pre created on registration.

Otherwise if FoxIDs is [deployed](development.md) in your one Azure tenant you get access to the master tenant. In this case you firstly need to create a tenant which will contain your entire security configuration. You probably only need one, but it is possible to configure an unlimited number of tenants.  

![Configure tenants](images/configure-tenant.png)

A tenant contains a master track, from where the entire tenant is configured. The master track contains a user repository and on creation only one administrator user.

Normally you should not change the master track configuration or add new up-parties or down-parties, but it is possible. You can e.g., by adding an up-party gain single sign-on (SSO) to the master track. 

### Create administrator user(s)

It is possible to create more administrator users in the master track. A user become an administrator by adding the administrator role `foxids:tenant.admin` like shown below.

Create a user:

1. Open the master track
2. Select the Users tab
3. Click Create User
4. Add the user information and click Create.

![Configure administrator user](images/configure-tenant-adminuser.png)

### Tracks
Configure a number of tracks, one for each of your environments e.g. dev, qa and prod.

> Create one or more tracks, do not place configuration in the master track.

![Configure tracks](images/configure-track.png)

Each track contains a user repository and a default created [login](login.md) up-party.

You can add [OAuth 2.0, OpenID Connect](oauth-2.0-oidc.md) and [SAML 2.0](saml-2.0.md) down-parties and up-parties in the Parties tab. 

![Configure down-parties and down-parties](images/configure-parties.png)

A track contains a primary certificate and possible a secondary certificate in the Certificates tab. It is possible to swap between the primary and secondary certificate if both is configured, depending on the [certificate](index.md#certificates) container type.

![Configure certificates](images/configure-certificate.png)

The track properties can be configured by clicking the top right setting icon. 

- Sequence lifetime is the max lifetime of a user's login flow from start to end.
- FoxIDs protect against password guess. Configured in max failing logins, failing login count lifetime and observation period.
- Password requirements are configured regarding length, complexity and [password risk](https://haveibeenpwned.com/Passwords).
- It is possible to host FoxIDs in an iframe from allowed domains.
- You can sent emails with you one SendGrid tenant by adding a custom email address and SendGrid key.

![Configure track settings](images/configure-track-setting.png)

## FoxIDs Control API
FoxIDs Control API is a REST API. The API expose a Swagger (OpenApi) interface document.

FoxIDs Control API require that the client calling the API is granted the `foxids:master` scope to access master tenant data or the `foxids:tenant` scope access tenant data in a particular tenant. Normally only tenant data is accessed.

 - The client can be an OAuth 2.0 client. Where the client is granted the administrator role `foxids:tenant.admin` acting as the client itself using client credentials grant.  
 Her is how the [sample seed tool](samples.md#configure-the-sample-seed-tool) client is granted access.
 - Or a OpenID Connect client with an authenticated master track user. Where the user is granted the administrator role `foxids:tenant.admin`. 

This shows the FoxIDs Control API configuration in a tenants master track with a scope that grants access to tenant data.

![Configure foxids_control_api](images/configure-foxids_control_api.png)

FoxIDs Control API is called with an access token as described in the [OAuth 2.0 Bearer Token (RFC 6750)](https://datatracker.ietf.org/doc/html/rfc6750) standard.

The Swagger (OpenApi) interface document is exposed on `.../api/swagger/v1/swagger.json`.  

You can also find the FoxIDs.com Swagger (OpenApi) [interface document](https://control.foxids.com/api/swagger/v1/swagger.json) online.

