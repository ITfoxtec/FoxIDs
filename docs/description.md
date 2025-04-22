**FoxIDs is a Identity Service (IDS) with support for [OAuth 2.0](oauth-2.0.md), [OpenID Connect 1.0](oidc.md) and [SAML 2.0](saml-2.0.md).**

> Hosted in Europe / Ownership and data in Europe.

FoxIDs is both an [authentication](login.md) platform and a security broker where FoxIDs support [converting](bridge.md) between OpenID Connect 1.0 and SAML 2.0.

FoxIDs is designed as service with multi-tenant support. Your tenant holds your environments (prod, QA, test, dev or corporate, external-idp, app-a, app-b) and possible [interconnect](howto-environmentlink-foxids.md) the environments.  
Each environment is an Identity Provider with a [user repository](users.md) and a unique [certificate](certificates.md). 
An environment can be connected to external Identity Provider with [OpenID Connect 1.0](auth-method-oidc.md) or [SAML 2.0](auth-method-saml-2.0.md) authentication methods. 
The environment is configured as the IdP for applications and APIs with [OAuth 2.0](app-reg-oauth-2.0.md), [OpenID Connect 1.0](app-reg-oidc.md) or [SAML 2.0](app-reg-saml-2.0.md) application registrations.  
The user's [log in](login.md) experience is configured and optionally [customised](customisation.md).

> Take a look at the FoxIDs test configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!`

FoxIDs consist of two services:

- [FoxIDs](connections.md) - identity service, which handles user log in, OAuth 2.0, OpenID Connect 1.0 and SAML 2.0.
- [FoxIDs Control](control.md), which is used to configure FoxIDs in a user interface or by calling an API.

Hosting:

- FoxIDs SaaS is available at [FoxIDs Cloud](https://www.foxids.com/action/signup) as an Identity Service (IDS).  
- You can [deploy](deployment.md) FoxIDs anywhere using Docker or Kubernetes (K8s).

> For more information please see the [get started](get-started.md) guide.

## Source code available 

The FoxIDs source code is available at the [GitHub repository](https://github.com/ITfoxtec/FoxIDs). 
The [license](https://github.com/ITfoxtec/FoxIDs/blob/main/LICENSE) grant all the right to install and use FoxIDs for non-production. The license grant small companies including, personal projects and non-profit educational institutions the right to install and use FoxIDs in production.

## Selection by URL
The [structure](foxids-inside.md#structure) of FoxIDs separates the different tenants, environments and [connections](connections.md) which is selected with URL elements. 

If FoxIDs is hosted on e.g., `https://foxidsxxxx.com/` the tenants are separated in the first path element of the URL `https://foxidsxxxx.com/tenant-x/`. 
The environments are separated under each tenant in the second path element of the URL `https://foxidsxxxx.com/tenant-x/environment-y/`.

An application registration is call by adding the application registration name as the third path element in the URL `https://foxidsxxxx.com/tenant-x/environment-y/application-z/`.  
An authentication method is call by adding the authentication method name insight round brackets as the third path element in the URL `https://foxidsxxxx.com/tenant-x/environment-y/(auth-method-s)/`. 
If FoxIDs handles a authentication method sequence resulting in a session cookie the same URL notation is used to lock the cookie to the URL.

When a client (application) starts an OpenID Connect or SAML 2.0 login sequence it needs to specify by which authentication method the user should authenticate. 
The authentication method is selected by adding the authentication method name in round brackets in the URLs third path element after the application registration name `https://foxidsxxxx.com/tenant-x/environment-y/application-z(auth-method-s)/`.  

Selecting multiple authentication methods:

- **Default** - Select all allowed authentication methods for an application registration by adding a star `*` in round brackets in the URL after the application registration name `https://foxidsxxxx.com/tenant-x/environment-y/application-z(*)/`
- Select a maximum of 4 allowed authentication methods for an application registration by adding the authentication methods as a comma separated list in round brackets 
  in the URL after the application registration name `https://foxidsxxxx.com/tenant-x/environment-y/application-z(auth-method-s1,auth-method-s2,auth-method-s3,auth-method-s4)/`
- Select an authentication methods profile by adding the authentication method `+` profile instead of just the authentication method in the URL `https://foxidsxxxx.com/tenant-x/environment-y/application-z(auth-method-s+profile-u)/`

> The allowed authentication methods is configured in each [application registration](connections.md#application-registration).

A client using client credentials as authorization grant would not specify the authentication method. 
It is likewise optional to specify the authentication method when calling an OpenID Connect discovery document or a SAML 2.0 metadata endpoint.