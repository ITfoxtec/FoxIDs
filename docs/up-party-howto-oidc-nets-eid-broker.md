# Up-party - connect Nets eID Broker with OpenID Connect

FoxIDs can be connected to Nets eID Broker with OpenID Connect and thereby authenticating end users with MitID.

> A connection to Nets eID Broker demo can be tested with the [samples](samples.md). E.g., with the [AspNetCoreOidcAuthCodeAllUpPartiesSample](https://github.com/ITfoxtec/FoxIDs.Samples/tree/master/src/AspNetCoreOidcAuthCodeAllUpPartiesSample) in the [sample solution](https://github.com/ITfoxtec/FoxIDs.Samples). 

Nets eID Broker has a [MitID demo](https://broker.signaturgruppen.dk/en/technical-documentation/open-oidc-clients) where all clients can connect without prior registration. All redirect URIs are accepted. 
Her you can find all needed to register a client with Nets eID Broker. 

This guide describes how to connect a FoxIDs up-party to Nets eID Broker demo.

## Configuring Nets eID Broker as OpenID Provider (OP)

This connection use OpenID Connect Authorization Code flow with PKCE, which is the recommended OpenID Connect flow.

**Create an OpenID Connect up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the name
2. Add the Nets eID Broker demo authority `https://pp.netseidbroker.dk/op` in the Authority field
3. In the scopes list add `mitid` (to support MitID) and optionally `nemid` (to support the old NemID)
4. Add the Nets eID Broker demo secret `rnlguc7CM/wmGSti4KCgCkWBQnfslYr0lMDZeIFsCJweROTROy2ajEigEaPQFl76Py6AVWnhYofl/0oiSAgdtg==` in the Client secret field
5. Select show advanced settings
6. Add the Signicat Express client id `0a775a87-878c-4b83-abe3-ee29c720c3e7` in the Optional customer SP client ID field
7. Click create

That's it, you are done. 

> The new up-party can now be selected as an allowed up-party in a down-party.  
> The down-party can read the claims from the up-party. You can optionally add a `*` in the down-party Issue claims list to issue all the claims to your application.
