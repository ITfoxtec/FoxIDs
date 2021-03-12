# Up-party connecting Azure AD with OpenID Connect

FoxIDs can be connecting to Azure AD with OpenID Connect and thereby authenticating by trust to an Azure AD App. 

It is possible to connect both a single tenant and multitenant Azure AD App as an up-party on FoxIDs using OpenID Connect.

## Configure single tenant

Start creating an OpenID Connect up-party in FoxIDs

 1. Add the name
 2. Select show advanced settings
 3. Select tildes URL binding pattern

![Read the redirect URLs](images/howto-oidc-azuread-readredirect.png)

It is now possible to read the `Redirect URL` and `Post logout redirect URL`.

Create the Azure AD App

 1. Add the name
 2. Select single tenant
 3. (It is a Web application) Add the `Redirect URL` 
 4. Click Register
 5. Copy the Application (client) ID
 6. Copy the Directory (tenant) ID
 7. Go to the Authentication tab and add the FoxIDs `Post logout redirect URL` as `Front-channel logout URL`, click save.
 8. Go to the Certificates & secrets tab and add a client secrets and copy the secret value.

Go back to the FoxIDs up-party

 1. Add the authority which is `https://login.microsoftonline.com/{Azure AD tenant ID}/v2.0`
 2. Add the profile and email scopes
 3. Add the Azure AD client ID as a custom SP client ID
 4. Add the Azure AD client secret value as the client secret
 5. Select use claims from ID token
 6. Add claims which is accepted by the up-party. E.g., preferred_username, email, name, given_name, family_name, oid, ipaddr
 7. Click create.

That is it, you are done. The new up-party can now be selected as a possible up-party in a down-party.

## Configure multitenant

The multitenant configuration differs slightly form the single tenant configuration.

In the Azure AD

 1. During the App creation select multitenant

In the FoxIDs up-party

 1. Add the authority `https://login.microsoftonline.com/common/v2.0`
 2. Select edit issuer
 3. Change the issuer to `https://login.microsoftonline.com/{Azure AD tenant ID}/v2.0`, you can possible add multiple issuers

## Read claims from access token

If you want to read claims from the access token you need to add one more Azure AD App acting as a resource (API). Expose a scope from the resource app and grant the other Azure AD App the resource app scope.
Then add the resource app scope as a scope in the FoxIDs up-party. 

By during this the access token is issued by the same OP (IdP) and is thereby accepted.




