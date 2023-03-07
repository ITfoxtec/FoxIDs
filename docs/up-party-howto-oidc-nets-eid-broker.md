# Up-party - connect Nets eID Broker with OpenID Connect

FoxIDs can be connected to Nets eID Broker with OpenID Connect and thereby authenticating end users with MitID and other credentials supported by Nets eID Broker.

How to configure Nets eID Broker in
- [test environment](#configuring-nets-eid-broker-demotest-as-openid-provider-op) using Nets eID Broker demo
- [production environment](#configuring-nets-eid-broker-as-openid-provider-op) using Nets eID Broker admin portal

> A connection to Nets eID Broker demo can be tested with the [samples](samples.md). E.g., with the [AspNetCoreOidcAuthCodeAllUpPartiesSample](https://github.com/ITfoxtec/FoxIDs.Samples/tree/master/src/AspNetCoreOidcAuthCodeAllUpPartiesSample) in the [sample solution](https://github.com/ITfoxtec/FoxIDs.Samples). 

## Configuring Nets eID Broker demo/test as OpenID Provider (OP)

This guide describes how to connect a FoxIDs up-party to Nets eID Broker demo in the test environment.

Nets eID Broker has a [MitID demo](https://broker.signaturgruppen.dk/en/technical-documentation/open-oidc-clients) where all clients can connect without prior registration. All redirect URIs are accepted. 
Her you can find all needed to register a client with Nets eID Broker. 

This connection use OpenID Connect Authorization Code flow with PKCE, which is the recommended OpenID Connect flow.

**Create an OpenID Connect up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the name
2. Add the Nets eID Broker demo authority `https://pp.netseidbroker.dk/op` in the Authority field
3. In the scopes list add `mitid` (to support MitID) and optionally `nemid` (to support the old NemID)
4. Add the Nets eID Broker demo secret `rnlguc7CM/wmGSti4KCgCkWBQnfslYr0lMDZeIFsCJweROTROy2ajEigEaPQFl76Py6AVWnhYofl/0oiSAgdtg==` in the Client secret field
5. Select show advanced settings
6. Add the Nets eID Broker demo client id `0a775a87-878c-4b83-abe3-ee29c720c3e7` in the Optional customer SP client ID field
7. Select use claims from ID token
8. Click create

That's it, you are done. 

> The new up-party can now be selected as an allowed up-party in a down-party.  
> The down-party can read the claims from the up-party. You can optionally add a `*` in the down-party Issue claims list to issue all the claims to your application.

## Configuring Nets eID Broker as OpenID Provider (OP)

This guide describes how to connect a FoxIDs up-party to the Nets eID Broker in the production environment.

You are granted access to the [Nets eID Broker admin portal](https://netseidbroker.dk/admin) by Nets. The Nets eID Broker [documentation](https://broker.signaturgruppen.dk/en/technical-documentation).  

This connection use OpenID Connect Authorization Code flow with PKCE, which is the recommended OpenID Connect flow.

**1 - Start by creating an API client in [Nets eID Broker admin portal](https://netseidbroker.dk/admin)**

 1. Navigate to Services & Clients
 2. Select the Service Provider
 3. Create or select a Service
 4. Click Add new client
 5. Add a Client name
 6. Select Web
 7. Click Create
 8. Copy the Client ID
 9. Click Create new Client Secret
 10. Select Based on password
 11. Add a name for the new client secret
 12. Click Generate on server
 13. Copy the Secret
 14. Click the IDP tab
 15. Select MitID and click `Add to pre-selected login options`, optionally select others
 16. Click the Advanced tab
 17. Set PKCE to Active
  
**2 - Then create an OpenID Connect up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the name
2. Add the Nets eID Broker demo authority `https://netseidbroker.dk/op` in the Authority field
3. Copy the two URLs: `Redirect URL` and `Post logout redirect URL`
4. In the scopes list add `mitid` (to support MitID) and optionally other scopes like e.g, `nemid.pid` to request the NemID PID and/or `ssn` to request the CPR number
5. Add the Nets eID Broker secret in the Client secret field
6. Select show advanced settings
7. Add the Nets eID Broker client id in the Optional customer SP client ID field
8. Select use claims from ID token
9. Click create

 **3 - Go back to [Nets eID Broker admin portal](https://netseidbroker.dk/admin)**

 1. Click the Endpoints tab
 2. Add the two URLs from the FoxIDs up-party client: `Redirect URL` and `Post logout redirect URL` in the fields `Login redirects` and `Logout redirects`.

That's it, you are done. 

> The new up-party can now be selected as an allowed up-party in a down-party.  
> The down-party can read the claims from the up-party. You can optionally add a `*` in the down-party Issue claims list to issue all the claims to your application.