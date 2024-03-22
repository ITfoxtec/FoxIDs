# Users
Users are saved in the environment's user repository. To achieve multiple user stores, you create additional environments and thus achieve more user stores.

There are two different types of users:
- [Internal users](#internal-users) which are authenticated using the [login](login.md) authentication method.
- [External users](#external-users) which are linked by an authenticated method to an external user/identity with a claim. The user is authenticated in an external Identity Provider using an authenticated method: OpenID Connect, SAML 2.0 or Environment Link.

## Internal users
Internal users can be authenticated in all [login](login.md) authentication methods in an environment, making is possible to [customize](customization.md) the login experience e.g., depending on different [application](connections.md#application-registration) requirements.

### Create user
Depending on the selected [login](login.md) authentication method's configuration, new users can create an account online.

The user can select to create a new user.

![Select create an account online](images/user-login.png)

And is then asked to fill out a form to create a user.

![New users create an account online](images/user-create-new-account.png)

The page is composed by dynamic elements which can be customized per [login](login.md) authentication method.  
In this example the create user page is composed by a Full name element and the default required Email and password element, ordered with the Full name element at the top.

This is the configuration in the [login](login.md) authentication method. Moreover, the claim `some_custom_claim` is added to each user as a constant in the claim transformation.

![Login configuration - create an account online](images/user-create-new-account-config.png)

### Provisioning
Internal users can be created, changed and deleted with the [Control Client](control.md#foxids-control-client) or be provisioned through the [Control API](control.md#foxids-control-api).

![Configure Login](images/configure-user.png)

### Multi-factor authentication (MFA)
Multi-factor authentication can be required per user. The user will then be required to authenticate with a two-factor authenticator app in a [login authentication method](login.md#two-factor-authentication-2famfa) and to configure the authenticator app if not already configured.

It is possible to see whether a two-factor authenticator app is configured for the user, and the administrator can deactivate the configured two-factor authenticator app.

![Configure Login](images/configure-user-mfa.png)

### Password hash
A hash of the password is saved in the database along with the rest of the user information.

The password hash functionality is designed to support a growing number of algorithms with different iterations by saving information about the hash algorithm used alongside the actually hash. Therefore, it is possible to validate an old hash algorithm and at the same time save new hashes with a new hash algorithm.

Current supported hash algorithm `P2HS512:10` which is defined as:

- The `HMAC` algorithm (`RFC 2104`) using the `SHA-512` hash function (`FIPS 180-4`).
- With 10 iterations.
- Salt is generated from 64 bytes.
- Derived key length is 80 bytes.

Standard .NET liberals are used to calculate the hash.

## External users
An external user is linked to one authentication method and can only be authenticated with that particular authentication method. External users can be linked to the authentication methods: OpenID Connect, SAML 2.0 or Environment Link.

All external user grouped under a authentication method is linked with the same claim type (e.g. the `sub` or `email` claim type) and the users are separated by unique claim values.

> With external users you can store claims on each user. E.g. store the your user ID claim representing the user in your system and thereby mapping the external user ID to your user ID. 

### Create external user
Depending on the selected authentication method's configuration, new users is asked to fill out a form to create a user.

![New external users create an account](images/user-external-create-new-account.png)

The page is composed by dynamic elements which can be customized per authentication method.  
In this example the create user page is composed by three elements; Email, Given name and Family name, ordered with the Email element at the top.

This is the configuration in a [OpenID Connect](auth-method-oidc.md) authentication method.

![OpenID Connect configuration - create an account online](images/user-external-create-new-account-config.png)

> If the login sequence is started base on a [login](login.md) authentication method, it provides the basis for the UI look and feel ([customize](customization.md)). Otherwise, the default [login](login.md) authentication method is selected as the base.

### Provisioning
External users can be created, changed and deleted with the [Control Client](control.md#foxids-control-client) or be provisioned through the [Control API](control.md#foxids-control-api).

![Configure Login](images/configure-user-external.png)

In this example the user is connected to Azure AD with an OpenID Connect authentication method and a `app_user_id` claim is added with the internal user ID.