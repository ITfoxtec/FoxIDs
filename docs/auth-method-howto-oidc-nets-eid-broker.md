# Connect Nets eID Broker as authentication method

FoxIDs can be connected to Nets eID Broker with OpenID Connect and thereby authenticating end users with MitID and other credentials supported by Nets eID Broker.

How to configure Nets eID Broker in
- [test environment](#configuring-nets-eid-broker-demotest-as-openid-provider-op) using Nets eID Broker demo
- [production environment](#configuring-nets-eid-broker-as-openid-provider-op) using Nets eID Broker admin portal

> You can testNets eID Broker demo login with the [online web app sample](https://aspnetcoreoidcallupsample.itfoxtec.com) ([sample docs](samples.md#aspnetcoreoidcauthcodealluppartiessample)) by clicking `Log in` and then `Nets eID Broker TEST`.  
> Take a look at the Nets eID Broker sample configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!` then select the `Production` environment and the `Authentication methods` tab.

## Configuring Nets eID Broker demo/test as OpenID Provider (OP)

This guide describes how to connect a FoxIDs authentication method to Nets eID Broker demo in the test environment.

Nets eID Broker has a [MitID demo](https://broker.signaturgruppen.dk/en/technical-documentation/open-oidc-clients) where all clients can connect without prior registration. All redirect URIs are accepted. 
Her you can find all needed to register a client with Nets eID Broker. 

This connection use OpenID Connect Authorization Code flow with PKCE, which is the recommended OpenID Connect flow.

**Create an OpenID Connect authentication method in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the name
2. Add the Nets eID Broker demo authority `https://pp.netseidbroker.dk/op` in the Authority field
3. In the scopes list add `mitid` (to support MitID) and optionally `nemid` (to support the old NemID)
4. Select show advanced settings
5. Optionally add an additionally parameter with the name `idp_values` and e.g. the value `mitid` to show the MitID IdP or e.g. the value `mitid_erhverv` to show the MitID Erhverv IdP.
6. Add the Nets eID Broker demo secret `rnlguc7CM/wmGSti4KCgCkWBQnfslYr0lMDZeIFsCJweROTROy2ajEigEaPQFl76Py6AVWnhYofl/0oiSAgdtg==` in the Client secret field
7. Add the Nets eID Broker demo client id `0a775a87-878c-4b83-abe3-ee29c720c3e7` in the Optional customer SP client ID field
8. Select to read claims from the UserInfo Endpoint instead of the access token or ID token
9. Click create

That's it, you are done. 

> The new authentication method can now be selected as an allowed authentication method in a application registration.  
> The application registration can read the claims from the authentication method. You can optionally add a `*` in the application registration Issue claims list to issue all the claims to your application. Or optionally define a [scope to issue claims](#scope-and-claims).

## Configuring Nets eID Broker as OpenID Provider (OP)

This guide describes how to connect a FoxIDs authentication method to the Nets eID Broker in the production environment.

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
  
**2 - Then create an OpenID Connect authentication method in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the name
2. Add the Nets eID Broker demo authority `https://netseidbroker.dk/op` in the Authority field
3. Copy the two URLs: `Redirect URL` and `Post logout redirect URL`
4. In the scopes list add `mitid` (to support MitID) and optionally other scopes like e.g, `nemid.pid` to request the NemID PID and/or `ssn` to request the CPR number
5. Select show advanced settings
6. Optionally add an additionally parameter with the name `idp_values` and e.g. the value `mitid` to show the MitID IdP or e.g. the value `mitid_erhverv` to show the MitID Erhverv IdP.
7. Add the Nets eID Broker secret in the Client secret field
8. Add the Nets eID Broker client id in the Optional customer SP client ID field
9. Select to read claims from the UserInfo Endpoint instead of the access token or ID token
10. Click create

 **3 - Go back to [Nets eID Broker admin portal](https://netseidbroker.dk/admin)**

 1. Click the Endpoints tab
 2. Add the two URLs from the FoxIDs authentication method client: `Redirect URL` and `Post logout redirect URL` in the fields `Login redirects` and `Logout redirects`.

That's it, you are done. 

> The new authentication method can now be selected as an allowed authentication method in a application registration.  
> The application registration can read the claims from the authentication method. You can optionally add a `*` in the application registration Issue claims list to issue all the claims to your application. Or optionally define a [scope to issue claims](#scope-and-claims).

## Scope and claims
You can optionally create a scope on the application registration with the Nets eID Broker claims as voluntary claims. The scope can then be used by a OpenID Connect client or another FoxIDs authentication method acting as a OpenID Connect client.

The name of the scope can e.g, be `nets_eid_broker`

The most used Nets eID Broker claims:

- `idp`
- `idp_identity_id`
- `loa`
- `mitid.uuid`
- `mitid.has_cpr`
- `dk.cpr`
- `nemid.pid`
- `nemid.pid_status`
- `mitid.age`
- `mitid.date_of_birth`
- `mitid.identity_name`
- `mitid.transaction_id`
