# Custom domain

Each FoxIDs tenant can be configured with a custom domain. A tenant connected to a custom domain does not include the tenant name in the URL like a tenant without a custom domain.

- A default tenant e.g., `my-tenant` without a custom domain would on FoxIDs.com result in a URL like this `https://foxids.com/my-tenant/some-environment/...`.
- If the same tenant is connected to a custom domain e.g., `my-domain.com` the URL on FoxIDs.com would be `https://my-domain.com/some-environment/...` without the tenant element.

The custom domain can be configured with [Control Client](control.md#foxids-control-client) in your tenants master environment under Settings --> Tenant settings. 

![Configure reverse proxy secret](images/configure-tenant-custom-domain-my-environment.png)

> When a new custom domain is added it needs to be verified. 
> After verification the domain is enabled in all environments in the tenant.

Custom domains is not supported in the master tenant and master environments.

> Please also take a look at [custom primary domains](deployment.md#custom-primary-domains).

## FoxIDs.com
Configuring a custom domain in your FoxIDs.com tenant.

> Only sub domains is supported as custom domains, like e.g., `id.some-domain.com`, `auth.some-domain.com`, `login.some-domain.com` or `id.zyx.some-domain.com`

Steps:

 1. In your DNS, add a CNAME with your custom domain and the target `custom-domains.foxids.com`    
 2. Configure your custom domain in your FoxIDs tenants master environment.
 3. Write an email to [FoxIDs support (support@foxids.com)](mailto:support@foxids.com) and ask for a custom domain verification.
 4. FoxIDs support will ask you to add one or two TXT records to your DNS for verification.
 5. After successfully verification your domain become active.

## Your own private cloud FoxIDs
Custom domains is supported if the FoxIDs service is behind a [reverse proxy](reverse-proxy.md) that can do domain rewrite.  
OR  
Alternatively without a reverse proxy, FoxIDs support custom domains by reading the HTTP request domain and using the domain as a custom domain if the `Setting:RequestDomainAsCustomDomain` setting is set to `true`. The FoxIDs App Service need to be configured with the custom domain in this case.


A domain is marked as verified in the master tenants master environment and is thereafter accepted by FoxIDs.

All custom domains on all tenants can be configured with [Control Client](control.md#foxids-control-client) and [Control API](control.md#foxids-control-api) in the master tenants master environment. 
Where also the domain can be marked as verified at the same time. 

![Configure reverse proxy secret](images/configure-tenant-custom-domain-environment.png)
