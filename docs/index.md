**FoxIDs is a free and open source identity service supporting: login, OAuth 2.0, OpenID Connect 1.0 and SAML 2.0.**

FoxIDs can at the same time work as both an authentication platform and a security broker. Furthermore, FoxIDs support converting between SAML 2.0 and OpenID Connect.

> STATUS: I'm currently working on the documentation and the first FoxIDs [release](https://github.com/ITfoxtec/FoxIDs/milestone/1).

FoxIDs consist of two services:

- The identity service which in short is called FoxIDs. The service handles user login and all other security traffic.
- The UI configuration client and API is called FoxIDs Control. The control service is used to configure FoxIDs in a user interface ([FoxIDs Control Client](control.md#foxids-control-client)) or by calling an API ([FoxIDs Control API](control.md#foxids-control-api)).

FoxIDs support Cloud and Private Cloud deployment:

- FoxIDs is available at [FoxIDs.com](https://foxids.com) as an Identity as a Service (IDaaS).
- Install FoxIDs for free in Microsoft Azure as your own [private cloud](getting-started.md#foxids-private-cloud).

> FoxIDs is build on .NET 5.0 and the FoxIDs Control Client is build on Blazor .NET Standard 2.1.

## Free and Open Source

FoxIDs is free and the open source - [GitHub repository](https://github.com/ITfoxtec/FoxIDs).  
The [license](https://github.com/ITfoxtec/FoxIDs/blob/master/LICENSE) grant all (individuals, companies etc.) the right to use FoxIDs for free. The license only restricts reselling FoxIDs as a IDaaS to third parties, without a supplementary agreement.
You are free to use FoxIDs as a IDaaS for you own products.

## Support

If you have questions please ask them on [Stack Overflow](https://stackoverflow.com/questions/tagged/foxids). Tag your questions with `foxids` and I will answer as soon as possible.

You are otherwise welcome to use [support@itfoxtec.com](mailto:support@itfoxtec.com?subject=FoxIDs) e.g., for topics not suitable for Stack Overflow.

## How FoxIDs works

FoxIDs is a multi-tenant system designed to be deployed in the Azure cloud. FoxIDs support being deployed as a service used by many companies, organizations etc. each with its one tenant. Or to be deployed in a company's Azure subscription where only one tenant is configured in FoxIDs holding the company's entire security service.

FoxIDs is deployed in two App Services which expose:

- FoxIDs, the identity service which handles all the security requests and user authentication
- [FoxIDs Control](control.md), the administration application and API in which FoxIDs is configured

Both is exposed as websites where the [domains can be customized](development.md#customized-domains). FoxIDs also relay on a number of backend service, please see [development](development.md) for details.

### Structure

FoxIDs is divided into logical elements.

- **Tenant** contain the company, organization, individual etc. security service. A tenant contains the tracks.
- **Track** is a production, QA, test etc. environment. Each track contains a [user repository](login.md#user-repository), a unique [certificate](certificates.md) and a track contains the up parties and down parties.
- **Up-party** is a upwards trust / federation or login configuration. Currently support: login (one view with both username and password) and SAML 2.0. Future support: OpenID Connect and two step login (two views separating the username and password input). 
- **Down-party** is a downward application configuration. Currently support: OpenID Connect (secret or PKCE), OAuth 2.0 API and SAML 2.0.

![FoxIDs structure](images/structure.svg)

**FoxIDs support unlimited tenants. Unlimited tracks in a tenant. Unlimited users, up parties and down parties in a track.**

### Separation
The structure is used to separate the different tenants, tracks and parties. 

If the FoxIDs is hosted on `https://foxidsxxxx.com/` the tenants are separated in the first folder of the URL `https://foxidsxxxx.com/tenant-x/`. The tracks are separated in the second folder of the URL `https://foxidsxxxx.com/tenant-x/track-y/` under each tenant.

A down-party is call by adding the down-party name as the third folder in the URL `https://foxidsxxxx.com/tenant-x/track-y/down-party-z/`.  
A up-party is call by adding the up-party name insight round brackets as the third folder in the URL `https://foxidsxxxx.com/tenant-x/track-y/(up-party-v)/`. If FoxIDs handles a up-party sequence like e.g. user authentication the same URL notation is used thus locking the session cookie to the URL. 

A client (application) starting an OAuth 2.0, OpenID Connect or SAML 2.0 login sequence would like to specify in which up-party the user should authenticate. The resulting up-party is specified by adding the up-party name in round brackets in the URL after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(up-party-v)/`.  

> The allowed up parties for a down-party is configured for each down-party in FoxIDs Control.

Selecting multiple up parties *(future support)*:

- Select all up parties allowed for a down-party by adding a star in round brackets in the URL after the down-party name `https://foxidsxxxx.com/tenant-x/track-y/down-party-z(*)/`
- Select a maximum of 5 up parties allowed for a down-party by adding the up parties as a comma separated list in round brackets in the URL after the down-party name `https://foxidsxxxx.com/*tenant-x*/*track-y*/*down-party-z*(up-party-v1*,up-party-v2*,up-party-v3,up-party-v4,up-party-v5)/`

A client which use client credentials as authorization grant would not specify the up-party. It is likewise optional to specify the up-party when calling an OpenID Connect discovery document or a SAML 2.0 metadata endpoint.