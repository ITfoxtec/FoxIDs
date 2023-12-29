# Connect to Context Handler with FoxIDs

> By using FoxIDs it become straight forward and easy to connect to Context Handler / F&aelig;lleskommunal Adgangsstyring (Danish identity broker).

Configure a connection from [FoxIDs](https://www.foxids.com) to Context Handler by following the [step-by-step guide](howto-saml-2.0-context-handler.md) - FoxIDs handles the SAML 2.0 / OIOSAML3 traffic.  
Then connect your application to FoxIDs with [OpenID Connect](down-party-oidc.md) or [lightweight SAML 2.0](down-party-saml-2.0.md) .

![Connect to Context Handler](images/how-to-context-handler.svg)

By default, FoxIDs is a [bridge](bridge.md) between [SAML 2.0](saml-2.0.md) and [OpenID Connect](oidc.md) / [OAuth 2.0](oauth-2.0.md) without any additional configuration. 

## About FoxIDs
FoxIDs is developed in Denmark and hosted in Netherlands, ownership and data is kept in Europe.  
You can [get started](https://www.foxids.com/action/createtenant) for free and optionally continue to use a Free plan.

## Online test
Test Context Handler with the <a href="https://aspnetcoreoidcallupsample.itfoxtec.com/auth/login" target="_blank">online test app</a>, select `Danish Context Handler` or `Danish Context Handler TEST` for the test environment.  
The OpenID Connect test app call FoxIDs and FoxIDs call Context Handler to let the user authenticate.

## Context Handler details
FoxIDs support Context Handler including OIOSAML3, login, single logout, logging, issuer naming, OCES3 certificates and NSIS.

> Transform the [DK privilege XML claim](claim-transform-dk-privilege.md) to a JSON claim.

You are maybe required to save logs for 180 days. If you are on an Enterprise plan, logs are stored for 180 days. If you are using a Free or Pro plan, you can send logs to your own Application Insights with a [log stream](logging.md#log-stream) and thereby save the logs for 180 days.

It is safe to go into production with a Free or Pro plan, but you are guaranteed a better SLA with an Enterprise plan.