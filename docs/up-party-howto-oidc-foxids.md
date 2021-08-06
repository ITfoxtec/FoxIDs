# Up-party - connect FoxIDs track

FoxIDs can be connected to another FoxIDs with OpenID Connect and thereby authenticating an end user in another FoxIDs track.  
The integration can be used in the same FoxIDs tenant to connect to different tracks or e.g. between different FoxIDs deployments.

The integration between two FoxIDs tracks support [OpenID Connect authentication](https://openid.net/specs/openid-connect-core-1_0.html#Authentication) (login), [RP-initiated logout](https://openid.net/specs/openid-connect-rpinitiated-1_0.html) and [front-channel logout](https://openid.net/specs/openid-connect-frontchannel-1_0.html). A session is established when the user authenticates and the session is invalidated on logout.

> A sample integration to a parallel FoxIDs track is configured in the FoxIDs `test-corp` with the up-party name `foxids_oidcpkce`.  
> You can test parallel FoxIDs login with the `AspNetCoreOidcAuthorizationCodeSample` [sample](samples.md) application by clicking `OIDC parallel FoxIDs Log in`.

The following describes how to first configure a OpenID Connect Relaying Party (RP) / client in a parallel FoxIDs track and then create a trust relation ship to your FoxIDs track. By configuring the parallel FoxIDs track as OpenID Provider (OP) / authority in your FoxIDs track.

