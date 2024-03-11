# Connect to NemLog-in with FoxIDs

> By using FoxIDs it become straight forward and easy to connect to NemLog-in (Danish IdP).

Configure a connection from [FoxIDs](https://www.foxids.com) to NemLog-in by following the [step-by-step guide](auth-method-howto-saml-2.0-nemlogin.md) - FoxIDs handles the SAML 2.0 / OIOSAML3 traffic.  
Then connect your application to FoxIDs with [OpenID Connect](app-reg-oidc.md) or [lightweight SAML 2.0](app-reg-saml-2.0.md) .

![Connect to NemLog-in](images/how-to-nemlogin.svg)

By default, FoxIDs is a [bridge](bridge.md) between [SAML 2.0](saml-2.0.md) and [OpenID Connect](oidc.md) / [OAuth 2.0](oauth-2.0.md) without any additional configuration. 

## About FoxIDs
FoxIDs is developed in Denmark and hosted in Netherlands, ownership and data is kept in Europe.  
You can [get started](https://www.foxids.com/action/createtenant) for free and optionally continue to use a Free plan.

## Online test
Test NemLog-in with the <a href="https://aspnetcoreoidcallupsample.itfoxtec.com/auth/login" target="_blank">online test app</a>, select `Danish NemLog-in` or `Danish NemLog-in TEST` for the test environment.  
The OpenID Connect test app call FoxIDs and FoxIDs call NemLog-in to let the user authenticate with MitID.

## NemLog-in details
FoxIDs support NemLog-in including OIOSAML3, login, single logout, logging, issuer naming, OCES3 (RSASSA-PSS) certificates and NSIS.

> Transform the [DK privilege XML claim](claim-transform-dk-privilege.md) to a JSON claim.

NemLog-in require you to save logs for 180 days. If you are on an Enterprise plan, logs are stored for 180 days. If you are using a Free or Pro plan, you can send logs to your own Application Insights with a [log stream](logging.md#log-stream) and thereby save the logs for 180 days.

It is safe to go into production with a Free or Pro plan, but you are guaranteed a better SLA with an Enterprise plan.