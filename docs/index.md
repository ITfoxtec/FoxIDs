**FoxIDs is a free and open-source Identity Services (IDS) supporting: [login](login.md), [OAuth 2.0](oauth-2.0.md), [OpenID Connect 1.0](oidc.md) and [SAML 2.0](saml-2.0.md).**

FoxIDs can at the same time work as an authentication platform and a security broker where FoxIDs support converting between [OpenID Connect 1.0 and SAML 2.0](parties.md).

> FoxIDs version 1.x, see [releases](https://github.com/ITfoxtec/FoxIDs/releases)

FoxIDs is designed as a container with multi-tenant support. FoxIDs can be deployed and use by e.g. a single company or deployed as a shared cloud container and used by multiple organisations, companies or everyone with the need.  
Separation is ensured at the tenant level and in each tenant separated by tracks. The tracks in a tenant segmentate environments, e.g. test, QA and production and e.g. trusts to external or internal IdPs.

FoxIDs consist of two services:

- The identity service which in short is called FoxIDs. The service handles user login and all other security traffic.
- The UI configuration client and API is called FoxIDs Control. FoxIDs Control is used to configure FoxIDs in a user interface ([FoxIDs Control Client](control.md#foxids-control-client)) or by calling an API ([FoxIDs Control API](control.md#foxids-control-api)).


FoxIDs support Cloud and Private Cloud deployment:

- FoxIDs is available at [FoxIDs.com](https://foxids.com) as an Identity Services (IDS) also called Identity as a Service (IDaaS).  
[FoxIDs.com](https://foxids.com) is deployed in Europe in Microsoft Azure Holland.
- You are free to [deploy](deployment.md) FoxIDs as your own private cloud in a Microsoft Azure tenant.

> FoxIDs is build on .NET 7.0 and the FoxIDs Control Client is Blazor.

## Free and Open Source

FoxIDs is free and open-source, see the [GitHub repository](https://github.com/ITfoxtec/FoxIDs).  
The [license](https://github.com/ITfoxtec/FoxIDs/blob/master/LICENSE) grant all (individuals, companies etc.) the right to use FoxIDs for free. The license only restricts reselling FoxIDs as a IDaaS to third parties, without a supplementary agreement.
You are free to use FoxIDs as a IDaaS for you own products.

## Support

If you have questions please ask them on [Stack Overflow](https://stackoverflow.com/questions/tagged/foxids). Tag your questions with `foxids` and I will answer as soon as possible.

You are otherwise welcome to use [support@itfoxtec.com](mailto:support@itfoxtec.com?subject=FoxIDs) e.g., for topics not suitable for Stack Overflow.

## How FoxIDs works

FoxIDs is a multi-tenant system designed to be deployed in Azure. FoxIDs support being deployed as a service used by many companies, organizations etc. each with its one tenant.  
Or to be deployed in a Azure subscription where usually only one tenant is configured in FoxIDs holding the organizations entire security service.  
In some cases, it can be an advantage to configure several tenants to e.g., separate a large number of external connections.

### Structure

FoxIDs is divided into logical elements.

- **Tenant** contain the company, organization, individual etc. security service. A tenant contains tracks.
- **Track** is a production, QA, test etc. environment. Each track contains a [user repository](login.md#user-repository), a unique [certificate](certificates.md) and a track contains the up-parties and down-parties.  
In some cases, it can be an advantage to place external connections in a separate tracks to configure connections specific certificates or log levels.
- **Up-party** is a upwards trust / federation or login configuration. Currently support: [login](login.md), [OpenID Connect 1.0](oidc.md#up-party) and [SAML 2.0](saml-2.0.md#up-party).
- **Down-party** is a downward application configuration. Currently support: [OAuth 2.0](oauth-2.0.md#down-party), [OpenID Connect 1.0](oidc.md#down-party) and [SAML 2.0](saml-2.0.md#down-party).

![FoxIDs structure](images/structure.svg)

> **FoxIDs support unlimited tenants. Unlimited tracks in a tenant. Unlimited users and unlimited up-parties and down-parties in a track.**

### Separation
The structure is used to separate the different tenants, tracks and [parties](parties.md). 

If the FoxIDs is hosted on `https://foxidsxxxx.com/` the tenants are separated in the first path element of the URL `https://foxidsxxxx.com/tenant-x/`. 
The tracks are separated under each tenant in the second path element of the URL `https://foxidsxxxx.com/tenant-x/track-y/`.

A down-party is call by adding the down-party name as the third path element in the URL `https://foxidsxxxx.com/tenant-x/track-y/down-party-z/`.  
A up-party is call by adding the up-party name insight round brackets as the third path element in the URL `https://foxidsxxxx.com/tenant-x/track-y/(up-party-v)/`. 
If FoxIDs handles a up-party sequence resulting in a session cookie the same URL notation is used to lock the cookie to the URL.

A client (application) starting an OpenID Connect or SAML 2.0 login sequence would like to specify in which up-party the user should authenticate. 
The up-party is selected by adding the up-party name in round brackets in the URLs third path element after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(up-party-v)/`.  

> The allowed up-parties for a [down-party](parties.md#down-party) is configured for each down-party in [FoxIDs Control Client](control.md#foxids-control-client).

Selecting multiple up-parties:

- Select all up-parties allowed for a down-party by adding a star in round brackets in the URL after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(*)/`
- Select a maximum of 4 up-parties allowed for a down-party by adding the up-parties as a comma separated list in round brackets 
  in the URL after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(up-party-v1,up-party-v2,up-party-v3,up-party-v4)/`

A client which use client credentials as authorization grant would not specify the up-party. It is likewise optional to specify the up-party when calling an OpenID Connect discovery document or a SAML 2.0 metadata endpoint.