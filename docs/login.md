# Login
FoxIDs support handling user login in an up-party login interface. There can be configured a number of up-party login's per track with different configurations.   
A track contains one [user repository](#user-repository) and all up-party login's configured in a track authenticate users with the same user repository.

When a user authenticates the user’s session is connected to the particular up-party login. Therefor, a user can authenticate in multiple configured up-party login’s and have multiple separated user sessions. A up-party login will create a user session after the user is authenticated if the session lifetime is configured to more than 0 seconds.

The up-party login interface authenticates user’s in a one-step user interface with the username and password on the same page. In the future, a two-step login user interface will be added hawing the username and password input on two different pages.

In the future, support for Two-factor authentication will be added.

## Configuration
A default up-party login is created in each track. 

> The default login can be changed or deleted but be careful as you may lose access.

It can be configured if the user should be allowed to cancel login. Likewise, it can be configured it new user’s are allowed to create a user in the login interface. New users can alternatively be created through FoxIDs Control or be provisioned through FoxIDs Control API.

The following shows how to create an up-party login which has user sessions of 10 hours (sliding session).

![Configure Login](images/configure-login.png)

In the advanced settings it is possible to configure absolute session lifetime. And if the persistent session is configured the session is preserved if the browser is closed.

The up-party login interface can be customized with CSS.

![Configure Login](images/configure-login-advanced.png)


## User repository 
Each track contains a user repository where the users is saved in Cosmos DB. The users id, email and other claims are saved as text. The password is never saved needer in logs or in Cosmos DB. Instead a hash of the password is saved in Cosmos DB along with the rest of the user information.

### Password hash
FoxIDs is designed to support a growing number of algorithms with different iterations by saving information about the hash algorithm used alongside the actually hash in Cosmos DB. Thereby FoxIDs can validate an old hash algorithm and at the same time save new hashes with a new hash algorithm.

Currently FoxIDs support and use hash algorithm `P2HS512:10` which is defined as:

- The HMAC algorithm (RFC 2104) using the SHA-512 hash function (FIPS 180-4).
- With 10 iterations.
- Salt is generated from 64 bytes.
- Derived key length is 80 bytes.

Standard .NET liberals are used to calculate the hash.
