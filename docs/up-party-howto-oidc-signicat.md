# Up-party - connect Signicat with OpenID Connect

FoxIDs can be connected to Signicat with OpenID Connect and thereby authenticating end users with MitID and all other credentials supported by Signicat.

> A connection to Signicat Express can be tested with the [samples](samples.md). E.g., with the [AspNetCoreOidcAuthCodeAllUpPartiesSample](https://github.com/ITfoxtec/FoxIDs.Samples/tree/master/src/AspNetCoreOidcAuthCodeAllUpPartiesSample) in the [sample solution](https://github.com/ITfoxtec/FoxIDs.Samples). 

You can create a [free account](https://www.signicat.com/sign-up/express-api-onboarding) on [Signicat Express](https://developer.signicat.com/express/docs/) and get access to the [dashbord](https://dashboard-test.signicat.io/dashboard). 
Her you have access to the test environment.

This guide describes how to connect a FoxIDs up-party to the Signicat Express test environment.

## Configuring Signicat as OpenID Provider (OP)

This connection use OpenID Connect Authorization Code flow with PKCE, which is the recommended OpenID Connect flow.

**1 - Start by creating an API client in [Signicat Express dashbord](https://dashboard-test.signicat.io/dashboard)**

 1. Navigate to Account and then API Clients
 2. Add the Client name
 3. In Auth Flow / Grant Type select Authorization code
 4. Copy the Secret
 5. Click Create
 6. Copy the Client ID

**2 - Then create an OpenID Connect up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

 1. Add the name
 2. Add the Signicat Express test authority `https://login-test.signicat.io` in the Authority field
 3. Copy the three URLs: `Redirect URL`, `Post logout redirect URL` and `Front channel logout URL`
 4. In the scopes list add `profile`
 5. Add the Signicat Express secret in the Client secret field
 6. Select show advanced settings
 7. Add the Signicat Express client id in the Optional customer SP client ID field
 8. Click create

 **3 - Go back to [Signicat Express dashbord](https://dashboard-test.signicat.io/dashboard)**

 1. Click OAuth / OpenID
 2. Click Edit
 3. Find the App URIs section
 4. Add the three URLs from the FoxIDs up-party client: `Redirect URL`, `Post logout redirect URL` and `Front channel logout URL` in the respectively fields 
 5. Click Save

That's it, you are done. 

> The new up-party can now be selected as an allowed up-party in a down-party.  
> The down-party can read the claims from the up-party. You can optionally add a `*` in the down-party Issue claims list to issue all the claims to your application.
