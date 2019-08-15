# FoxIDs

FoxIDs is an open source security service supporting login, OAuth 2.0, OpenID Connect 1.0, SAML 2.0 and convention between the standards.

FoxIDs is a cloud service which is deployed in you Azure tenant and repay on Azure resources. In the future is will also be possible to use FoxIDs on [https://FoxIDs.com](https://foxids.com) for at small transaktion fee.

> For [Getting started](https://github.com/ITfoxtec/FoxIDs/wiki/Getting-started) guide and more documentation please see the [Wiki](https://github.com/ITfoxtec/FoxIDs/wiki).

> FoxIDs is .NET Core 2.2 and the web sites is ASP.NET Core.

## Deployment

You can [deploy FoxIDs](#1-Azure-deployment) in your Azure tenant. Afterwords, FoxIDs is initialized with the [seed tool](#2-Seed), to create the master certificate and the first admin user.

### 1. Azure deployment

[![Deploy to Azure](https://azuredeploy.net/deploybutton.svg)](https://deploy.azure.com/?repository=https://github.com/ITfoxtec/FoxIDs/tree/release-current?ptmpl=parameters.azuredeploy.json)

The ARM deployment script deploys:

- Two App Services one for FoxIDs and one for the FoxIDs API. Both App Services is hosted in the same App Service plan. 
- FoxIDs is deployed to the two App Services from the `release-current` branch with Kudu. Thereafter, it is possible to manually initiate the Kudu update.
- Key vault. Secrets are placed in Key vault.
- Document DB.
- Redis cache.
- SendGrid.
- Application Insights.

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

> You can either download the seed tool (Win_x64) from [releases](https://github.com/ITfoxtec/FoxIDs/releases) or compile it from source code.

In the first initial seed step the seed tool saves documents directly in to the Cosmos DB. All subsequently seed steps is executed by calling the FoxIDs api.

The seed tool is configured in the `appsettings.json` file.

Add the FodIDs and FoxIDs api endpoints to the seed tool configured. They can be added by updating the instance names `foxidsxxxx` and `foxidsapixxxx` or by configuring custom domains.

```json
"SeedSettings": {
    "FoxIDsEndpoint": "https://foxidsxxxx.azurewebsites.net", 
    "FoxIDsApiEndpoint": "https://foxidsapixxxx.azurewebsites.net" 
}
```

#### 2.1 Create master tenant documents

The Cosmos DB instance is configured in the seed tool. In the `EndpointUri` the `foxidsxxx` is changed to the Cosmos DB instance name. And the Cosmos DB primary key is configured in the `PrimaryKey`. Both endpoint and primary key can be found in [Azure portal](https://portal.azure.com).

```json
"SeedSettings": {
  "CosmosDb": {
    "EndpointUri": "https://foxidsxxx.documents.azure.com:443/",
    "PrimaryKey": "xxx...xxx"
  }
}
```

Run the seed tool executable `SeedTool.exe`, select `M` for `Create master tenant documents`. When asked please write the first administrator users email.

> IMPORTANT: Remember password and secrets.

Add the seed client secret to the seed tool configured.

```json
"SeedSettings": {
  "ClientSecret": "xxx"
}
```

#### 2.2 Add text resources

The seed tool add generic text resources as a document in Cosmos DB. The resources can later be customized per track in the track configuration in Cosmos DB.

Run the seed tool executable `SeedTool.exe`, select `R` for `Add text resources`. 

#### 2.3 Create passwords risk list

The seed tool can add passwords risk list of insecure passwords to use in Cosmos DB documents as SHA-1 hashes. The insecure passwords (pwned passwords) is from [haveibeenpwned.com](https://haveibeenpwned.com)

Download the `SHA-1` pwned passwords `ordered by hash` from [haveibeenpwned.com/passwords](https://haveibeenpwned.com/Passwords).

Add the local pwned passwords file path to the seed tool configured.

```json
"SeedSettings": {
  "PwnedPasswordsPath": "c:\\... xxx ...\\pwned-passwords-sha1-ordered-by-count-v4.txt"
}
```

> Be aware that it takes long time to upload the entire password risk list. This step can be omitted and postponed to later.

Run the seed tool executable `SeedTool.exe`, select `P` for `Create passwords risk list`.

#### 2.4 Possible to add samples

It is possible to run the sample applications after they are configured in FoxIDs. The sample configuration can be added with the seed tool, see how in the [samples guid](https://github.com/ITfoxtec/FoxIDs/wiki/Samples).

## Support

Please ask your question on <a href="https://stackoverflow.com/">Stack Overflow</a> and email a link to <a href="mailto:support@itfoxtec.com?subject=FoxIDs">support@itfoxtec.com</a> for me to answer.<br />

## Development

FoxIDs
https://localhost:44330

FoxIDs API
https://localhost:44331

*FoxIDs Portal
https://localhost:44332 - not created yet*

*FoxIDs web
https://localhost:44333 - not created yet*
