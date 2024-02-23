**Foxids is a free and open-source Identity Services (IDS) with support for [OAuth 2.0](oauth-2.0.md), [OpenID Connect 1.0](oidc.md) and [SAML 2.0](saml-2.0.md).**

> Developed in Denmark and hosted in Netherlands, ownership and data is kept in Europe.

Foxids is both an [authentication](login.md) platform and a security broker where Foxids support converting from [OpenID Connect 1.0](oidc.md) to [SAML 2.0](saml-2.0.md).

Foxids is designed as a container with multi-tenant support. Your tenant holds your tracks which correspond to your environments (prod, QA, test, dev) and other elements. 
Each track is an Identity Provider with a [user repository](users.md), a unique [certificate](certificates.md) and connections.
Connections to external Identity Provider is configured as [OpenID Connect 1.0](up-party-oidc.md) or [SAML 2.0](up-party-saml-2.0.md) up-parties where applications and APIs is configured as [OAuth 2.0](app-reg-oauth-2.0.md), [OpenID Connect 1.0](app-reg-oidc.md) or [SAML 2.0](app-reg-saml-2.0.md) application registrations.  
The users [login](login.md) experience is configured as an up-party.

> Take a look at the Foxids test configuration in Foxids Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!`

Foxids consist of two services:

- The identity service which in short is called Foxids. The service handles user login and all other security traffic.
- The configuration service Foxids Control is used to configure Foxids in a user interface ([Foxids Control Client](control.md#foxids-control-client)) or by calling an API ([Foxids Control API](control.md#foxids-control-api)).

Foxids can be deployed and used by a single company or deployed as a shared cloud container and used by multiple organisations. 
You can select to use a shared cloud or a private cloud setup.

- Foxids SaaS is available at [Foxids.com](https://foxids.com) as an Identity Services (IDS) also called Identity as a Service (IDaaS).  
Foxids.com is hosted in Europe and mainly in Microsoft Azure Holland, Netherlands.
- You are free to [deploy](deployment.md) Foxids as your own private cloud on Microsoft Azure.

> For more information please see the [get started](get-started.md) guide.

## Free and Open-Source

Foxids is free and open-source, see the [GitHub repository](https://github.com/ITfoxtec/Foxids).  
The [license](https://github.com/ITfoxtec/Foxids/blob/master/LICENSE) grant all (individuals, companies etc.) the right to use Foxids for free. The license only restricts reselling Foxids as a IDaaS to third parties, without a supplementary agreement.
You are free to use Foxids as a IDaaS for you own products.

## Selection by URL
The [structure](foxids-inside.md#structure) of Foxids separates the different tenants, tracks and [parties](parties.md) which is selected with URL elements. 

If Foxids is hosted on e.g., `https://foxidsxxxx.com/` the tenants are separated in the first path element of the URL `https://foxidsxxxx.com/tenant-x/`. 
The tracks are separated under each tenant in the second path element of the URL `https://foxidsxxxx.com/tenant-x/track-y/`.

A application registration is call by adding the application registration name as the third path element in the URL `https://foxidsxxxx.com/tenant-x/track-y/app-reg-z/`.  
A up-party is call by adding the up-party name insight round brackets as the third path element in the URL `https://foxidsxxxx.com/tenant-x/track-y/(up-party-v)/`. 
If Foxids handles a up-party sequence resulting in a session cookie the same URL notation is used to lock the cookie to the URL.

When a client (application) starts an OpenID Connect or SAML 2.0 login sequence it needs to specify by which up-party the user should authenticate. 
The up-party is selected by adding the up-party name in round brackets in the URLs third path element after the application registration name `https://foxidsxxxx.com/tenant-x/track-y/app-reg-z(up-party-v)/`.  

Selecting multiple up-parties:

- Select all allowed up-parties for a application registration by adding a star in round brackets in the URL after the application registration name `https://foxidsxxxx.com/tenant-x/track-y/app-reg-z(*)/`
- Select a maximum of 4 allowed up-parties for a application registration by adding the up-parties as a comma separated list in round brackets 
  in the URL after the application registration name `https://foxidsxxxx.com/tenant-x/track-y/app-reg-z(up-party-v1,up-party-v2,up-party-v3,up-party-v4)/`

> The allowed up-parties is configured in each [application registration](parties.md#application-registration).

A client using client credentials as authorization grant would not specify the up-party. 
It is likewise optional to specify the up-party when calling an OpenID Connect discovery document or a SAML 2.0 metadata endpoint.