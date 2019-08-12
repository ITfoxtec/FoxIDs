# FoxIDs

FoxIDs is an open source security service supporting login, OAuth 2.0, OpenID Connect 1.0, SAML 2.0 and convention between the standards.

FoxIDs is a cloud service which is deployed in you Azure tenant and repay on Azure resources. In the future is will also be possible to use FoxIDs on [https://FoxIDs.com](https://foxids.com) for at small transaktion fee.

## Deployment

You can [deploy FoxIDs](#1-Azure-deployment) in your Azure tenant. Afterwords, FoxIDs is initialized with the [seed tool](#2-Seed), to create the master certificate and the first admin user.

### 1. Azure deployment

The ARM deployment script deploys:

- Two App Services one for FoxIDs and one for the FoxIDs API. Both App Services is hosted in the same App Service plan. 
- FoxIDs is deployed to the two App Services from the ´release-current´ branch with Kudu. Thereafter, it is possible to manually initiate the Kudu update.
- Key vault. Secrets are placed in Key vault.
- Document DB.
- Redis cache.
- SendGrid.
- Application Insights.

[![Deploy to Azure](https://azuredeploy.net/deploybutton.svg)](https://deploy.azure.com/?repository=https://github.com/ITfoxtec/FoxIDs/tree/release-current?ptmpl=parameters.azuredeploy.json)

**Troubleshooting deployent errors:**

> **Deployment timeout.** If you receive a deployment error like *"The gateway did not receive a response from 'Microsoft.DocumentDB' within the specified time period." or "The gateway did not receive a response from 'Microsoft.Web' within the specified time period."* 
>
> The deployment have probably succeed anyway, please verify in [Azure portal](https://portal.azure.com).

> **Sendgrid terms.** If you have not already accepted the Sendgrid legal terms for the selected plan in the subscription you will get the error *"User failed validation to purchase resources. Error message: 'Legal terms have not been accepted for this item on this subscription: 'XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX'. To accept legal terms using PowerShell, please use Get-AzureRmMarketplaceTerms and Set-AzureRmMarketplaceTerms API(https://go.microsoft.com/fwlink/?linkid=862451) or deploy via the Azure portal to accept the terms'"* 
>
> You need to accept the terms either by deploying a Sendgrid instance in [Azure portal](https://portal.azure.com) or with PowerShell. 
> The following PowerShell commands accept the Sendgrid terms for the free plan:
>
>     Connect-AzureRmAccount
>     $terms = Get-AzureRmMarketplaceTerms -Publisher 'SendGrid' -Product 'sendgrid_azure' -Name 'free'
>     Set-AzureRmMarketplaceTerms -Publisher 'SendGrid' -Product 'sendgrid_azure' -Name 'free' -Terms $terms -Accept
>
> Then delete the falling resource groups and redeploy.

### 2. Seed

...

## Support

Please ask your question on <a href="https://stackoverflow.com/">Stack Overflow</a> and email a link to <a href="mailto:support@itfoxtec.com?subject=FoxIDs">support@itfoxtec.com</a> for me to answer.<br />

## Development

FoxIDs
https://localhost:44330

FoxIDs API
https://localhost:44331

*FoxIDs Portal
https://localhost:44332*

*FoxIDs web
https://localhost:44333* 
