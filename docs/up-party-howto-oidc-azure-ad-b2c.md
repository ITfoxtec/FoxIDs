# Up-party - connect Azure AD B2C with OpenID Connect

FoxIDs can be connected to Azure AD B2C with OpenID Connect and thereby authenticating end users in an Azure AD B2C tenant.

## Configure integration

This chapter describes how to configure a connection with OpenID Connect Authorization Code flow and PKCE, which is the recommended OpenID Connect flow.

**1 - Start by creating an OpenID Connect up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

 1. Add the name
 2. Select show advanced settings
 3. Select tildes URL binding pattern

![Read the redirect URLs](images/howto-oidc-azuread-readredirect.png)

It is now possible to read the `Redirect URL` and `Post logout redirect URL`.

**2 - Then go to Azure AD B2C and create the app profile**

1. Create app profile
2. The profile will result in an authority like this `https://some-domain.b2clogin.com/some-domain.onmicrosoft.com/B2C_1A_SOME_SIGNIN_PROFILE/v2.0/`, including the profile name

When the authority is registered in FoxIDs as an up-party. FoxIDs will call the discovery endpoint on the authority which in this case will be `https://some-domain.b2clogin.com/some-domain.onmicrosoft.com/B2C_1A_SOME_SIGNIN_PROFILE/v2.0/.well-known/openid-configuration`

> If you receive a discovery endpoint URL formatted with the Azure AD B2C profile name in the query string like this `...?p=B2C_1A_SOME_SIGNIN_PROFILE` you have to change the URL structure.  
> The full URL would look like this `https://some-domain.b2clogin.com/some-domain.onmicrosoft.com/v2.0/.well-known/openid-configuration?p=B2C_1A_SOME_SIGNIN_PROFILE` and 
> the authority is then `https://some-domain.b2clogin.com/some-domain.onmicrosoft.com/B2C_1A_SOME_SIGNIN_PROFILE/v2.0/` where the Azure AD B2C profile name is moved to be a path element in the URL.

**3 - Go back to the FoxIDs up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

> Azure AD B2C is not by default return an access token in the token response and is thereby not OpenID Connect Authorization Code flow compliant. You need to add a Azure AD B2C client ID as a scope to get an access token returned.

 1. Add the authority, which is `https://some-domain.b2clogin.com/some-domain.onmicrosoft.com/B2C_1A_SOME_SIGNIN_PROFILE/v2.0/`
 2. Add the profile and email scopes (possible other or more scopes)
 3. Add the Azure AD B2C client ID as a custom SP client ID
 3. Add the Azure AD B2C client ID as a scope
 4. Add the Azure AD B2C client secret value as the client secret
 5. You probably / maybe need to select use claims from ID token
 6. Add the claims which will be transferred from the up-party to the down-parties. E.g., preferred_username, email, name, given_name, family_name, oid, ipaddr and possible the access_token claim to transfer the Azure AD B2C access token to down-parties
 7. Click create

That's it, you are done. 

> The new up-party can now be selected as an allowed up-party in a down-party.  
> The down-party can read the claims from the up-party. It is possible to add the access_token claim to include the Azure AD B2C access token as a claim in the issued access token.