**FoxIDs is a free and open-source Identity Services (IDS) with support for [OAuth 2.0](oauth-2.0.md), [OpenID Connect 1.0](oidc.md) and [SAML 2.0](saml-2.0.md).**

> Developed in Denmark and hosted in Netherlands, ownership and data is kept in Europe.

FoxIDs is both an [authentication](login.md) platform and a security broker where FoxIDs support converting from [OpenID Connect 1.0](oidc.md) to [SAML 2.0](saml-2.0.md).

FoxIDs is designed as a container with multi-tenant support. Your tenant holds your tracks which correspond to your environments (prod, QA, test, dev) and other elements. 
Each track is an Identity Provider with a [user repository](users.md), a unique [certificate](certificates.md) and connections.
Connections to external Identity Provider is configured as [OpenID Connect 1.0](up-party-oidc.md) or [SAML 2.0](up-party-saml-2.0.md) up-parties where applications and APIs is configured as [OAuth 2.0](down-party-oauth-2.0.md), [OpenID Connect 1.0](down-party-oidc.md) or [SAML 2.0](down-party-saml-2.0.md) down-parties.  
The users [login](login.md) experience is configured as an up-party.

FoxIDs consist of two services:

- The identity service which in short is called FoxIDs. The service handles user login and all other security traffic.
- The configuration service FoxIDs Control is used to configure FoxIDs in a user interface ([FoxIDs Control Client](control.md#foxids-control-client)) or by calling an API ([FoxIDs Control API](control.md#foxids-control-api)).

FoxIDs can be deployed and used by a single company or deployed as a shared cloud container and used by multiple organisations. 
You can select to use a shared cloud or a private cloud setup.

- FoxIDs is available at [FoxIDs.com](https://foxids.com) as an Identity Services (IDS) also called Identity as a Service (IDaaS).  
FoxIDs.com is hosted in Europe and mainly in Microsoft Azure Holland, Netherlands.
- You are free to [deploy](deployment.md) FoxIDs as your own private cloud on Microsoft Azure.

> For more information please see the [get started](get-started.md) guide.

## Free and Open-Source

FoxIDs is free and open-source, see the [GitHub repository](https://github.com/ITfoxtec/FoxIDs).  
The [license](https://github.com/ITfoxtec/FoxIDs/blob/master/LICENSE) grant all (individuals, companies etc.) the right to use FoxIDs for free. The license only restricts reselling FoxIDs as a IDaaS to third parties, without a supplementary agreement.
You are free to use FoxIDs as a IDaaS for you own products.

## Selection by URL
The [structure](foxids-inside.md#structure) of FoxIDs separates the different tenants, tracks and [parties](parties.md) which is selected with URL elements. 

If FoxIDs is hosted on e.g., `https://foxidsxxxx.com/` the tenants are separated in the first path element of the URL `https://foxidsxxxx.com/tenant-x/`. 
The tracks are separated under each tenant in the second path element of the URL `https://foxidsxxxx.com/tenant-x/track-y/`.

A down-party is call by adding the down-party name as the third path element in the URL `https://foxidsxxxx.com/tenant-x/track-y/down-party-z/`.  
A up-party is call by adding the up-party name insight round brackets as the third path element in the URL `https://foxidsxxxx.com/tenant-x/track-y/(up-party-v)/`. 
If FoxIDs handles a up-party sequence resulting in a session cookie the same URL notation is used to lock the cookie to the URL.

When a client (application) starts an OpenID Connect or SAML 2.0 login sequence it needs to specify by which up-party the user should authenticate. 
The up-party is selected by adding the up-party name in round brackets in the URLs third path element after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(up-party-v)/`.  

Selecting multiple up-parties:

- Select all allowed up-parties for a down-party by adding a star in round brackets in the URL after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(*)/`
- Select a maximum of 4 allowed up-parties for a down-party by adding the up-parties as a comma separated list in round brackets 
  in the URL after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(up-party-v1,up-party-v2,up-party-v3,up-party-v4)/`

> The allowed up-parties is configured in each [down-party](parties.md#down-party).

A client using client credentials as authorization grant would not specify the up-party. 
It is likewise optional to specify the up-party when calling an OpenID Connect discovery document or a SAML 2.0 metadata endpoint.

## Support

If you have questions, please ask them on [Stack Overflow](https://stackoverflow.com/questions/tagged/foxids). Tag your questions with `foxids` and I will answer as soon as possible.

You are otherwise welcome to use [support@itfoxtec.com](mailto:support@itfoxtec.com?subject=FoxIDs) e.g., for topics not suitable for Stack Overflow.