# Connect Signicat as authentication method

FoxIDs can be connected to Signicat with OpenID Connect and thereby authenticating end users with MitID and all other credentials supported by Signicat.

> You can test the Signicat Express login with the [online web app sample](https://aspnetcoreoidcallupsample.itfoxtec.com) ([sample docs](samples.md#aspnetcoreoidcauthcodealluppartiessample)) by clicking `Log in` and then `Signicat TEST`.  
> Take a look at the Signicat Express sample configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!` then select the `Production` environment and the `Authentication methods` tab.

You can create a [free account](https://www.signicat.com/sign-up/express-api-onboarding) on [Signicat Express](https://developer.signicat.com/express/docs/) and get access to the [dashbord](https://dashboard-test.signicat.io/dashboard). 
Her you have access to the test environment.

This guide describes how to connect a FoxIDs authentication method to the Signicat Express test environment.

## Configuring Signicat as OpenID Provider (OP)

This connection use OpenID Connect Authorization Code flow with PKCE, which is the recommended OpenID Connect flow.

**1 - Start by creating an API client in [Signicat Express dashbord](https://dashboard-test.signicat.io/dashboard)**

 1. Navigate to Account and then API Clients
 2. Add the Client name
 3. In Auth Flow / Grant Type select Authorization code
 4. Copy the Secret
 5. Click Create
 6. Copy the Client ID

**2 - Then create an OpenID Connect authentication method in [FoxIDs Control Client](control.md#foxids-control-client)**

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
 4. Add the three URLs from the FoxIDs authentication method client: `Redirect URL`, `Post logout redirect URL` and `Front channel logout URL` in the respectively fields 
 5. Click Save

That's it, you are done. 

> The new authentication method can now be selected as an allowed authentication method in a application registration.  
> The application registration can read the claims from the authentication method. You can optionally add a `*` in the application registration Issue claims list to issue all the claims to your application.
