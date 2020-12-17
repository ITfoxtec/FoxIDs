# FoxIDs Control
FoxIDs is configured with FoxIDs Control which consists of [FoxIDs Control Client](#foxids-control-client) and [FoxIDs Control API](#foxids-control-api). FoxIDs Control Client and API is secured by FoxIDs and the client call FoxIDs Control API to update the configuration. 

FoxIDs Control API contain all the configuration functionality. Therefore, it is possible to automate the configuration by integrating with FoxIDs Control API.

## FoxIDs Control Client 
FoxIDs Control Client is a Blazor WebAssembly web client.

### Tenant and master track
If FoxIDs is [deployed](development.md) in your one Azure tenant you get access to the master tenant. You firstly need to create a tenant which will contain your entire security configuration. You probably only need one, but it is possible to configure an unlimited number of tenants.  

![Configure tenants](images/configure-tenant.png)

In the future if you use FoxIDs at [https://FoxIDs.com](https://foxids.com). Your one tenant will be pre created on registration.

A tenant contains a master track, from where the entire tenant is configured. The master track contains a user repository and on creation only one administrator user.

Select and open the tenant you just created. At first the tenant only contains a master track, normally you should not change the master track configuration or add new up- parties or down-parties.  
It is possible to create more user in the master track. A user become an administrator by adding the administrator role `foxids:tenant.admin` like shown below.

![Configure administrator user](images/configure-tenant-adminuser.png)

FoxIDs support translating the interfaces elements into the languages that are configured. English is default (FoxIDs Control Client only support English). It is possible to add text translations to all text elements used in the FoxIDs interface.  
The text translations in the master track is used in all the other tracks. It is furthermore possible to add track specific translations to each track.  
This is an example of a text element translated into two languages.

![Configure text](images/configure-tenant-text.png)

### Tracks
Configure a number of tracks, one for each of your environments e.g. dev, qa and prod.

> Create one or more tracks, do not place configuration in the master track.

![Configure tracks](images/configure-track.png)

A track contains a user repository and a default created up-party [login](login.md). It is possible to create users and add track specific text translations. 

Add [OAuth 2.0, OpenID Connect](oauth-2.0-oidc.md) and [SAML 2.0](saml-2.0.md) down-parties and down-parties.

![Configure down-parties and down-parties](images/configure-parties.png)

Each track contains a primary certificate and possible a secondary certificate. It is possible to swap between the primary and secondary certificate if both is configured, depending on the [certificate](index.md#certificates) container type.

![Configure certificates](images/configure-certificate.png)

The track properties can be configured by clicking the top right setting icon. 

- Sequence lifetime is the max lifetime of a user's login flow from start to end.
- FoxIDs protect against password guess. Configured in max failing logins, failing login count lifetime and observation period.
- Password requirements are configured regarding length, complexity and [password risk](https://haveibeenpwned.com/Passwords).
- It is possible to host the FoxIDs login interface in an iframe from a allowed domain.

![Configure track settings](images/configure-track-setting.png)

## FoxIDs Control API
FoxIDs Control API is a REST API. The API expose a Swagger (OpenApi) interface document.

FoxIDs Control API require that the client calling the API is granted the `foxids:master` scope to access master data or the `foxids:tenant` scope access tenant API. Normally only tenant data is accessed.
The client can be an OAuth 2.0 client with the administrator role `foxids:tenant.admin` acting as the client itself. Or an OAuth 2.0 / OIDC client with an authenticated user having the administrator role `foxids:tenant.admin`. 

This shows the FoxIDs Control API configuration in the master track with a scope configuration that grants access to tenant data.

![Configure foxids_control_api](images/configure-foxids_control_api.png)

FoxIDs Control API is called with an access token as described in the OAuth 2.0 Bearer Token standard.

The Swagger (OpenApi) interface document is exposed on `.../api/swagger/v1/swagger.json`.  
In the future you can also find the Swagger (OpenApi) [interface document](https://control.foxids.com/api/swagger/v1/swagger.json) online.

