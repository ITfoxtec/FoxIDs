# Interconnect FoxIDs with OpenID Connect

FoxIDs can be connected to another FoxIDs with OpenID Connect and thereby authenticating end users in another FoxIDs track or an external Identity Provider (IdP) configured as an up-party.  
FoxIDs tracks can be interconnect in the same FoxIDs tenant or in different FoxIDs tenants. Interconnections can also be configured between FoxIDs tracks in different FoxIDs deployments.

> You can easily connect two tracks in the same tenant with a [track link](howto-tracklink-foxids.md).

The integration between two FoxIDs tracks support [OpenID Connect authentication](https://openid.net/specs/openid-connect-core-1_0.html#Authentication) (login), [RP-initiated logout](https://openid.net/specs/openid-connect-rpinitiated-1_0.html) and [front-channel logout](https://openid.net/specs/openid-connect-frontchannel-1_0.html). A session is established when the user authenticates and the session is invalidated on logout.

> You can test OpenID Connect track connections with the [online web app sample](https://aspnetcoreoidcallupsample.itfoxtec.com) ([sample docs](samples.md#aspnetcoreoidcauthcodealluppartiessample)) by clicking `Log in` and then `Parallel FoxIDs track`.  
> Take a look at the track connection configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!` then take a look at the `parallel` and `- (dash is production)` tracks.

The following describes how to configure a OpenID Connect up-party in your FoxIDs track and trust a parallel FoxIDs track where a OpenID Connect application registration is configured. This will make your FoxIDs track trust the parallel FoxIDs track to authenticate users.

## Configure integration

**1 - Start in your FoxIDs track by creating an OpenID Connect up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the name

![Read the redirect URLs](images/howto-oidc-foxids-up-party-readredirect.png)

It is now possible to read the `Redirect URL`, `Post logout redirect URL` and `Front channel logout URL`.

**2 - Then go to the parallel FoxIDs track and create the application registration client**

The client is a confidential client using Authorization Code Flow and PKCE.

1. Specify client name in application registration name.
2. Select allowed up-parties. E.g. `login` or some other up-party.
3. Select show advanced settings.
4. Specify redirect URI read in your up-party.
5. Specify post logout redirect URI read in your up-party.
6. Specify front channel logout URI read in your up-party.
7. Specify a secret (remember the secret to the next step).
8. Remove the `offline_access`.
9. Remove / edit the scopes depending on your needs.
10. Click create.

![Parallel FoxIDs application registration client](images/howto-oidc-foxids-parallel-app-reg.png)

**3 - Go back to your FoxIDs up-party client in [FoxIDs Control Client](control.md#foxids-control-client)**

 1. Add the parallel FoxIDs track application registration client authority.  
     > Default the parallel track use the `login` up-party to authenticate users with the `https://localhost:44330/testcorp/dev2/foxids_oidcpkce(login)/` authority.  
     > It is possible to select another up-party in the parallel track. E.g. `azure_ad` with the `https://localhost:44330/testcorp/dev2/foxids_oidcpkce(azure_ad)/` authority.
 2. Add the profile and email scopes (possible other or more scopes).
 3. Add the parallel FoxIDs track application registration client's client secret.
 6. Add the claims which will be transferred from the up-party to the application registrations. E.g., email, email_verified, name, given_name, family_name, role and possible the access_token claim to transfer the parallel FoxIDs tracks access token.
 7. Click create.

 ![Parallel FoxIDs application registration client](images/howto-oidc-foxids-up-party.png)

That's it, you are done. 

> Your new up-party can now be selected as an allowed up-party in the application registrations in you track.  
> The application registrations in you track can read the claims from your up-party. It is possible to add the access_token claim to include the parallel FoxIDs tracks access token as a claim in the issued access token.