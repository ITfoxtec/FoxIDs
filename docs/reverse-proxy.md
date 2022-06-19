# Reverse proxy
It is recommended to place FoxIDs behind a reverse proxy. FoxIDs service support [custom domain](custom-domain.md) rewrite (not [custom primary domain](deployment.md#custom-primary-domains)). FoxIDs Control only support custom primary domain and require to be called on the same domain as exposed on the reverse proxy.

## Restrict the access
Both FoxIDs service and FoxIDs Control sites can restrict access based on the `X-FoxIDs-Secret` HTTP header.  
The access restriction is activated by adding a secret with the name `Settings--ProxySecret` in Key Vault.

![Configure reverse proxy secret](images/configure-reverse-proxy-secret.png)

> The sites needs to be restarted to read the secret.

After the reverse proxy secret has been configured in Key Vault the reverse proxy needs to add the `X-FoxIDs-Secret` HTTP header in all backed calls to FoxIDs to get access.

## Read HTTP headers
FoxIDs service support reading the client IP address in the following prioritized HTTP headers:

 1. `CF-Connecting-IP`
 2. `X-Azure-ClientIP`
 3. `X-Forwarded-For`

FoxIDs service support reading the [custom domain](custom-domain.md) (hostname) exposed on the revers proxy in the following prioritized HTTP headers:

 1. `X-ORIGINAL-HOST`
 2. `X-Forwarded-Host`

## Tested reverse proxies
FoxIDs is tested with the following reverse proxies.
 
### Azure Front Door
Azure Front Door can be configured as a reverse proxy with close to the default setup. Azure Front Door rewrite domains by default. The `X-FoxIDs-Secret` HTTP header can optionally be added.

### Cloudflare
Cloudflare can be configured as a reverse proxy. But Cloudflare require a Enterprise plan to rewrite domains (host headers). The `X-FoxIDs-Secret` HTTP header can be added (recommended).

### IIS ARR Proxy
Internet Information Services (IIS) Application Request Routing (ARR) Proxy require a Windows server. ARR Proxy rewrite domains with a rewrite rule. The `X-FoxIDs-Secret` HTTP header can optionally be added (recommended depended on the infrastructure).

An accept all external exposed domains rule can be configured. This example is a global rule, rules can also be added to websites. Optionally both requiring (`secret1`) and sending (`secret2`) a `X-FoxIDs-Secret` HTTP header. You could require a `X-FoxIDs-Secret` HTTP header if you have a reverse proxy in front of the ARR Proxy.

    <globalRules>
        <rule name="my-rule-name" patternSyntax="Wildcard" stopProcessing="true">
            <match url="*" />
            <conditions>
                <add input="{HTTP_X-FoxIDs-Secret}" pattern="... secret1 ..." ignoreCase="false" />
            </conditions>                                                
            <action type="Rewrite" url="https://my-foxids-installation.com/{R:1}" />
            <serverVariables>
                <set name="HTTP_X-ORIGINAL-HOST" value="{HTTP_HOST}" />
                <set name="HTTP_X-FoxIDs-Secret" value="... secret2 ..." />
            </serverVariables>
        </rule>
    </globalRules>