# Reverse proxy
It is recommended to place both the FoxIDs site and the FoxIDs Control site behind a reverse proxy. 

## Reverse proxies
FoxIDs generally support all reverse proxies, the following reverse proxies has been tested.
 
### Azure Front Door
Azure Front Door can be configured as a reverse proxy. Azure Front Door rewrite domains by default. 

> Do NOT enable caching. The `Accept-Language` header is not forwarded if caching is enabled. The header is required by FoxIDs to support cultures.

Add a Azure Front Door endpoint for both the FoxIDs site and the FoxIDs Control site. [Restrict access](#restrict-access) by requiring the `X-FoxIDs-Secret` HTTP header.  
Disable Session affinity and optionally configure WAF policies.

### Cloudflare
Cloudflare can be configured as a reverse proxy. But Cloudflare require a Enterprise plan to rewrite domains (host headers). [Restrict access](#restrict-access) by requiring the `X-FoxIDs-Secret` HTTP header.

### Azure Application Gateway
Azure Application Gateway can rewrite all domains if configured. 
The `X-FoxIDs-Secret` HTTP header can optionally be added to [restrict access](#restrict-access) (recommended depended on the infrastructure).

Optionally configure a rewrite rule to both requiring a secret and sending a secret in a `X-FoxIDs-Secret` HTTP header. You could require a `X-FoxIDs-Secret` HTTP header if you have a reverse proxy in front of the Azure Application Gateway.  
If requiring a secret, add a custom HTTPS health probe with the `X-FoxIDs-Secret` query parameter `/?x-foxids-secret=xxx` and the secret.

### IIS ARR Proxy
Internet Information Services (IIS) Application Request Routing (ARR) Proxy require a Windows server. ARR Proxy rewrite domains with a rewrite rule. 
The `X-FoxIDs-Secret` HTTP header can optionally be added to [restrict access](#restrict-access) (recommended depended on the infrastructure).

An accept all external domains rule can be configured. This example is a global rule, rules can also be added to websites.  
Optionally both requiring (`secret1`) and sending (`secret2`) in a `X-FoxIDs-Secret` HTTP header. You could require a `X-FoxIDs-Secret` HTTP header if you have a reverse proxy in front of the ARR Proxy.

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

## Read HTTP headers
The FoxIDs site support reading the client IP address in the following HTTP headers in order of priority:

 1. `CF-Connecting-IP`
 2. `X-Azure-ClientIP`
 3. `X-Forwarded-For`

The FoxIDs site support reading the [custom domain](custom-domain.md) (host name) from the revers proxy in the following HTTP headers in order of priority:

 1. `X-ORIGINAL-HOST`
 2. `X-Forwarded-Host`

> The host header is only read if access is restricted by the `X-FoxIDs-Secret` HTTP header or the `Settings__TrustProxyHeaders` setting is set to `true`.

The FoxIDs site support to read the HTTP/HTTPS scheme if the `Settings__TrustProxySchemeHeader` setting is set to `true`. In the following HTTP headers in order of priority:

 1. `X-Forwarded-Scheme`
 2. `X-Forwarded-Proto`

 ## Restrict access
Both the FoxIDs site and FoxIDs Control sites can restrict access based on the `X-FoxIDs-Secret` HTTP header.  
The access restriction is activated by adding the `Settings__ProxySecret` setting with the secret.