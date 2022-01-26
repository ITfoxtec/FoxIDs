# Deployment

Deploy FoxIDs in your Azure tenant. 

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FITfoxtec%2FFoxIDs%2Fmaster%2Fazuredeploy.json)

The Azure deployment include:

- Two App Services one for FoxIDs and one for the FoxIDs Control (Client and API). Both App Services is hosted in the same App Service plan and the App Services has both a production and test slot. 
- FoxIDs is deployed to the two App Services test slots from the `master` branch with Kudu. Updates is initiated manually in the App Services test slots. Deployment updates is automatically promoted from the test slots to the production slots. It is possible to change the automatically promoted to manually initiated.
- Key vault. Secrets are placed in Key vault.
- Cosmos DB.
- Redis cache.
- Application Insights.

### Send emails with Sendgrid or SMTP
FoxIDs supports sending emails with SendGrid and SMTP as [email provider](email).

### First login and admin users
After successfully deployment open [FoxIDs Control Client](control.md#foxids-control-client) on `https://foxidscontrolxxxxxxxxxx.azurewebsites.net` (the app service starting with foxidscontrol...) which brings you to the master tenant.

> The default admin user is: `admin@foxids.com` with password: `FirstAccess!` (you are required to change the password on first login)

![FoxIDs Control Client - Master tenant](images/master-tenant2.png)

Create more admin users with a valid email addresses and grant the users the admin `role` with the value `foxids:tenant.admin`.

![FoxIDs Control Client - Master tenant admin user](images/master-tenant-admin-user.png)

> You should generally not change the parties configuration or add applications in the master tenant, unless you are sure about what you are doing.

### Troubleshooting deployent errors

**Key Vault soft deleted**
If you have deleted a previous deployment the Key Vault is only soft deleted and sill exist with the same name for some months. 
In this case you can experience getting a 'ConflictError' with the error message 'Exist soft deleted vault with the same name.'.

The solution is to delete (purge) the old Key Vault, which will release the name.

## Seed

### Upload risk passwords

You can upload risk passwrods in FoxIDs Control Client master tenant on the Risk Passwords tap. 

![FoxIDs Control Client - Upload risk passwrods](images/upload-risk-passwords.png)

Download the `SHA-1` pwned passwords `ordered by prevalence` from [haveibeenpwned.com/passwords](https://haveibeenpwned.com/Passwords).

> Be aware that it takes some time to upload all risk passwords. This step can be omitted and postponed to later.  
> The risk passwords are uploaded as bulk which has a higher consumption. Please make sure to adjust the Cosmos DB provisioned throughput (e.g. to 20000 RU/s) temporarily.

### Add sample configuration to a track

It is possible to run the sample applications after they are configured in a FoxIDs track. The sample configuration can be added with the [sample seed tool](samples.md#configure-samples-in-foxids-track).

## Custom domains

The FoxIDs and FoxIDs Control domains can be customized.  

> Important: change the primary domain before adding tenants.

FoxIDs default domain is `https://foxidsxxxx.azurewebsites.net` which can be changed to a custom a domain like e.g., `https://foxidsxxxx.com` or `https://foxids.xxxx.com`  
FoxIDs Control default domain is `https://foxidscontrolxxxx.azurewebsites.net` which can be changed to a domain like e.g., `https://control.foxidsxxxx.com` or `https://foxidscontrol.xxxx.com`

Custom domains are configured in Azure portal on the FoxIDs App Service and the FoxIDs Control App Service production slot under the `Custom domains` tab and by clicking the `Add custom domain` link. The FoxIDs site support one primary domain and multiple secondary domains, where the FoxIDs Control only support one primary domain.

Additionally primary custom domain configuration:

1) First login to the FoxIDs Control client using the default/old primary domain. Select the `Parties` menu and under `Down-parties` select click `OpenID Connect - foxids_control_client` and click `Show advanced settings`.

- Add the FoxIDs Control sites new primary custom domain to the `Allow CORS origins` list without a trailing slash.
- Add the FoxIDs Control Client sites new primary custom domain login and logout redirect URIs to the `Redirect URIs` list including the trailing `/master/authentication/login_callback` and `/master/authentication/logout_callback`.

> If you have added tenants before changing the primary domain, the `OpenID Connect - foxids_control_client` configuration have to be done in each tenant.

2) Then configure the FoxIDs and FoxIDs Control sites new primary custom domains in the FoxIDs Control App Service under the `Configuration` tab and `Applications settings` sub tab: 

- The setting `Settings:FoxIDsEndpoint` is changed to the FoxIDs sites primary custom domains.
- The setting `Settings:FoxIDsControlEndpoint` is changed to the FoxIDs Control sites primary custom domains.

## Specify default page

An alternative default page can be configured for the FoxIDs site using the `Settings:WebsiteUrl` setting. If configured a full URL is required like e.g., `https://www.foxidsxxxx.com`.
