# Login
FoxIDs support user login in an up-party login interface. There can be configured a number of up-party login's per track with different configurations.   
A track contains one [user repository](#user-repository) and all up-party login's configured in a track authenticate user's with the same user repository.

When a user authenticates the user's session is connected to the particular up-party login. Therefore, a user can authenticate in multiple configured up-party login's and have multiple separate user sessions. A up-party login will create a user session after the user is authenticated if the session lifetime is configured to more than 0 seconds.

A [down-party OpenID Connect](down-party-oauth-2.0-oidc.md) or [down-party SAML 2.0](down-party-saml-2.0.md) can authenticate users by selecting an up-party login.

![FoxIDs login](images/parties-login.svg)

The up-party login interface authenticates user's in a one-step user interface with the username and password on the same page. In the future, a two-step login interface will be added hawing the username and password input on two separate pages.

In the future, support for Two-factor authentication will be added.

## Configuration
A default up-party login is created in each track. 

> The default login can be changed or deleted but be careful as you may lose access.

It can be configured if the user should be allowed to cancel login. Likewise, it is configurable rather users are allowed to create a new user in the login interface. New users can alternatively be created through [FoxIDs Control Client](control.md#foxids-control-client) or be provisioned through [FoxIDs Control API](control.md#foxids-control-api).

The following screen shut show how to create an up-party login with user sessions set to having a 10 hours lifetime (sliding session).

![Configure Login](images/configure-login.png)

It is possible to configure an absolute session lifetime in the advanced settings. And if the persistent session is configured the session is also preserved after the browser has been closed.

The up-party login interface can be [customized with custom title, icon and CSS](title-icon-css).

![Configure Login](images/configure-login-advanced.png)

> Change the claims the up-party pass on with [claim transforms](claim-transform.md).

## User repository 
Each track contains a user repository supporting an unlimited number of users because they are saved in Cosmos DB. The users id, email and other claims are saved as text.  
The password is never saved needer in logs or in Cosmos DB. Instead a hash of the password is saved along with the rest of the user information.

### Password hash
FoxIDs is designed to support a growing number of algorithms with different iterations by saving information about the hash algorithm used alongside the actually hash. Therefore, FoxIDs can validate an old hash algorithm and at the same time save new hashes with a new hash algorithm.

Currently FoxIDs support and use hash algorithm `P2HS512:10` which is defined as:

- The HMAC algorithm (RFC 2104) using the SHA-512 hash function (FIPS 180-4).
- With 10 iterations.
- Salt is generated from 64 bytes.
- Derived key length is 80 bytes.

Standard .NET liberals are used to calculate the hash.
