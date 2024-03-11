# Connect two environments with Environment Link

FoxIDs environments in the same tenant can be connected with environment links. A Environment Link acts mostly like OpenID Connect but it is simpler to configure and the steps it goes through is faster. 

![Environment Link](images/how-to-environment-link.svg)

Environment links is fast and secure but can only be used in the same tenant. A link based on [OpenID Connect connection](howto-oidc-foxids.md) is required if you need to jump between environments located in different tenants.

> Take a look at the sample environment links configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!` then e.g., take a look at the `nemlogin` and `- (dash is production)` environments.

Environment links support login, RP-initiated logout and front-channel logout. Furthermore, it is possible to configure [claim and claim transforms](claim.md), logout session and home realm discovery (HRD) like all other connecting authentication methods and application registrations.

## Configure integration

The following describes how to connect two environments called `track_x` and `track_y` where `track_y` become an authentication method on `track_x`.

**1 - Start in the `track_x` environment by creating a Environment Link in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Select the Authentication methods tab
2. Click Create authentication method and then Environment Link
3. Add the name e.g., `track_y-connection` 
4. Add the `track_y` environment name
5. Add the application registration name in the `track_y` environment e.g., `track_x-connection` 
6. Click Create

![Create Environment Link authentication method](images/howto-environmentlink-foxids-auth-method.png)

**2 - Then go to the `track_y` environment and create a Environment Link in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Select the Applications tab
2. Click Create application registration and then Environment Link
3. Add the name e.g., `track_x-connection` 
4. Add the `track_x` environment name
5. Add the authentication method name in the `track_x` environment e.g., `track_y-connection` 
6. Select which authentication methods in the `track_y` environment the user is allowed to use for authentication
6. Click Create

![Create Environment Link application registration](images/howto-environmentlink-foxids-app-reg.png)

That's it, you are done. 

> Your new authentication method `track_y-connection` can now be selected as an allowed authentication method in the application registrations in you `track_x` environment.  
> The application registrations in you `track_x` environment can read the claims from your `track_y-connection` authentication method. 