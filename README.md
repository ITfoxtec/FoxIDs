# [FoxIDs](https://www.foxids.com)

FoxIDs is an open-source Identity Services (IDS) supporting [login](https://www.foxids.com/docs/login), [OAuth 2.0](https://www.foxids.com/docs/oauth-2.0), [OpenID Connect 1.0](https://www.foxids.com/docs/oidc), [SAML 2.0](https://www.foxids.com/docs/saml-2.0) and convention between [OpenID Connect and SAML 2.0](https://www.foxids.com/docs/parties).  
FoxIDs handles multi-factor authentication (MFA) / two-factor authentication (2FA) with support for two-factor authenticator app.

> For [Getting started](https://www.foxids.com/docs/getting-started) guide and more documentation please see the [documentation](https://www.foxids.com/docs).

FoxIDs is designed as a container with multi-tenant support. FoxIDs can be deployed and use by e.g. a single company or deployed as a shared cloud container and used by multiple organisations, companies or everyone with the need.  
Separation is ensured at the tenant level and in each tenant separated by tracks. The tracks in a tenant segmentate environments, e.g. test, QA and production and e.g. trusts to external or internal IdPs.

FoxIDs consist of two services:

- Identity service called FoxIDs handling user login and all other security traffic.
- Client and API called FoxIDs Control. The FoxIDs Control Client is used to configure FoxIDs, or alternatively by calling the FoxIDs Control API directly.

Deployment or as a service:

- FoxIDs is a cloud service ready to be [deployed](https://www.foxids.com/docs/deployment) in you Azure tenant.
- Or you can use FoxIDs as an Identity as a Service (IDaaS) at [FoxIDs.com](https://foxids.com).

> FoxIDs is .NET 7.0 and the FoxIDs Control Client is Blazor .NET 7.0.

## Deployment

Deploy FoxIDs in your Azure tenant. FoxIDs is deployed in a resource group e.g., named `FoxIDs` where you need to be `Owner` or `Contributor` and `User Access Administrator` on either subscription level or resource group level.

> Please see more [deployment details](https://www.foxids.com/docs/deployment)

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FITfoxtec%2FFoxIDs%2Fmaster%2Fazuredeploy.json)

The Azure deployment include:

- Two App Services one for FoxIDs and one for the FoxIDs Control (Client and API). Both App Services is hosted in the same App Service plan and the App Services has both a production and test slot. 
- FoxIDs is deployed to the two App Services test slots from the `master` branch with Kudu. [Updates](https://www.foxids.com/docs/update) is initiated manually in the App Services test slots. Deployment updates is automatically promoted from the test slots to the production slots. It is possible to change the automatically promoted to manually initiated.
- Key Vault. Certificates and secrets are saved and handled in Key Vault.
- Cosmos DB. Contain all data including tenants, tracks and users. Cosmos DB is a NoSQL database and data is saved in JSON documents.
- Redis cache. Holds sequence (e.g., login and logout sequences) data, data cache to improve performance and handle counters to secure authentication against various attacks.
- Application Insights and Log Analytics workspace. Logs are send to Application Insights and queries in Log Analytics workspace.
- VLAN with subnets.
  - Subnet for App services, Cosmos DB and Key Vault. 
  - Subnet with Private Link to Redis.
  - Subnet with Azure Monitor Private Link Scope (AMPLS) to Application Insights and Log Analytics workspace. To see logs in the Azure Portal, change the setting to accept public networks.

> There is only Internet access to App services, every thing else is encapsulated.

### Send emails with Sendgrid or SMTP
FoxIDs supports sending emails with SendGrid and SMTP as [email provider](https://www.foxids.com/docs/email).

### Send emails with Sendgrid
FoxIDs relay on Sendgrid to send emails to the users for account verification and password reset.  
You can optionally configure a Sendgrid from email address and Sendgrid API key in the Azure deployment configuration. You can either [create Sendgrid in Azure](https://docs.microsoft.com/en-us/azure/sendgrid-dotnet-how-to-send-email) or directly on [Sendgrid](https://Sendgrid.com), there are more free emails in an Azure manage Sendgrid.

> Remember to setup up [domain authentication](https://sendgrid.com/docs/ui/account-and-settings/how-to-set-up-domain-authentication/) in Sendgrid for the from email.

A Sendgrid from email address and API Key can at a later time be configure per track.

### First login and admin users
After successfully deployment open [FoxIDs Control Client](control.md#foxids-control-client) on `https://foxidscontrolxxxxxxxxxx.azurewebsites.net` (the app service starting with foxidscontrol...) which brings you to the master tenant.

> The default admin user is: `admin@foxids.com` with password: `FirstAccess!` (you are required to change the password on first login)
> *Please wait a few minutes before logging in after the deployment is complete to allow the initial seed to finish.*

![FoxIDs Control Client - Master tenant](docs/images/master-tenant2.png)

Create more admin users with a valid email addresses and grant the users the admin `role` with the value `foxids:tenant.admin`.

![FoxIDs Control Client - Master tenant admin user](docs/images/master-tenant-admin-user.png)

#### Add sample configuration to a track

It is possible to run the sample applications after they are configured in a FoxIDs track. The sample configuration can be added with the [sample seed tool](docs/samples.md#configure-samples-in-foxids-track).

## Support

If you have questions please ask them on [Stack Overflow](https://stackoverflow.com/questions/tagged/foxids). Tag your questions with 'foxids' and I will answer as soon as possible.

Otherwise you can use [support@itfoxtec.com](mailto:support@itfoxtec.com?subject=FoxIDs) for topics not suitable for Stack Overflow.

## Development

FoxIDs  
`https://localhost:44330`

FoxIDs Control (Blazor WebAssembly Client and API)  
`https://localhost:44331`
