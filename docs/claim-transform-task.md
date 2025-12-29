<!--
{
    "title":  "Claim transforms and claim tasks",
    "description":  "Each FoxIDs authentication method and application registration handles claims and supports claim transformations and claim tasks. This means that multiple sets of claim transforms and claim tasks can be executed for each user authentic...",
    "ogTitle":  "Claim transforms and claim tasks",
    "ogDescription":  "Each FoxIDs authentication method and application registration handles claims and supports claim transformations and claim tasks. This means that multiple sets of claim transforms and claim tasks can be executed for each user authentic...",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "claim transform task, FoxIDs docs"
                       }
}
-->

# Claim transforms and claim tasks

Each FoxIDs authentication method and application registration handles [claims](claim.md) and supports claim transformations and claim tasks.
This means that multiple sets of claim transforms and claim tasks can be executed for each user authentication.
First, claim transforms and claim tasks are executed on the authentication method, and then on the application registration.

Additional subsets of claim transforms and claim tasks can be performed if a user or an external user is created.

![Claim transform flow diagram](images/claim-transform.svg)

If you create a new claim with a first-level claim transform or claim task, the claim is local to the authentication method other then a Login authentication method.  
In an authentication method, the claim is forwarded if the claim type is added to the `Forward claims` list, or if `*` (default) is in the list.  

If you create a new claim with a claim transform or claim task, the claim is local to the application registration.  
In an application registration, you need to add the claim or `*` to the `Issue claims` list. Alternatively, for OpenID Connect, add the claim to a scope's `Voluntary claims` list and request the scope from your application.

Please see the [claim transform examples](#claim-transform-examples).

> Enable `Log claim trace` in the [log settings](logging.md#log-settings) to see the claims before and after transformation in the [logs](logging.md). 

Claim transforms can be configured in a login authentication method.

![FoxIDs authentication method claim transform](images/configure-claim-transform-auth-method.png)

And claim tasks.

![FoxIDs authentication method claim task](images/configure-claim-task-auth-method.png)

Similarly, claim transforms and claim tasks can be configured as first-level and second-level in an OpenID Connect authentication method.

![FoxIDs application registration claim transform](images/configure-claim-transform-app-reg.png)

> Claims are by default represented as JWT claims. If the authentication method is SAML 2.0, the first-level claims are represented as SAML 2.0 claims.
> If the application registration is SAML 2.0, the claims are represented as SAML 2.0 claims.

A claim transform and claim task will do one of up to seven different actions depending on the particular claim transform or claim task type.

Claim transform and claim task actions:

- `Add claim` - add a new claim
- `Add claim, if not match` - do the add action if the condition does not match
- `Replace claim` - add a new claim and remove existing claims if one or more exist
- `Replace claim, if not match` - do the replace action if the condition does not match
- `Remove claim` - remove the claims if one or more exist
- `If match` - do the action if the condition matches
- `If not match` - do the action if the condition does not match

Claim transforms and claim tasks are executed in order, and the actions are therefore executed in order. This means that it is possible to create a local variable by adding a claim and later in the sequence make decisions based on the claim.
A claim is local in the claim transforms and claim tasks set if it starts with `_local:`.

With the `Add claim, if not match` action it is possible to add a claim (local variable) if another claim or a claim value does not exist.

Claim transform types that support all actions:

- `Match claim` - do the action if the claim type matches
- `Match claim and value` - do the action if the claim type and claim value match
- `Regex match` - do the action if the claim type matches and the claim value matches the regular expression

Claim transform types that support `Add claim`, `Replace claim` and `Add claim, if new claim does not exist` actions:

- `Map` - do the action if the claim type matches, then map the claim value to a new claim
- `Regex map` - do the action if the claim type matches and the claim value matches the regular expression group, then map the group value to a new claim

Claim transform types that support `Add claim` and `Replace claim` actions:

- `Constant` - always do the action (add/replace a claim with a constant value)
- `Concatenate` - do the action if one or more of the claim types match, then concatenate the claim values to a new claim
- `External claims API` - call an [external API](#external-claims---api) with the selected claims to add/replace claims with external claims
- `DK XML privilege to JSON` - convert the [DK privilege to JSON](claim-transform-dk-privilege).

Claim task types that support `Add claim` and `Replace claim` actions:

- `Query internal user` - match the claim and find exactly one internal user based on the value of the claim. The request will fail if more than one user is found. Then add/replace the user's claims.
- `Query external user` - match the claim and find exactly one external user based on the value of the claim. The request will fail if more than one user is found. Then add/replace the user's claims.

Claim task types that support `If match` and `If not match` actions:

- `Match claim and return error` - return an error if the claim type matches/does not match.
- `Match claim and value and return error` - return an error if the claim type and value match/do not match.
- `Regex match and return error` - return an error if the claim type and claim value match/do not match the regular expression.
- `Match claim and start authentication` - start a new login flow by initiating an authentication method if the claim type matches/does not match.
- `Match claim and value and start authentication` - start a new login flow by initiating an authentication method if the claim type and value match/do not match.
- `Regex match and start authentication` - start a new login flow by initiating an authentication method if the claim type and claim value match/do not match the regular expression.

> The start authentication claim tasks can be used for step-up when the user is logged in with one factor and another factor is required, or if additional information (claims) is required.

## External claims - API
You can [call your own API](#implement-api) from FoxIDs with a claim transformation. The API is called with claims and the claims returned from the API can be added with an add or replace action.
The API is only called if at least one selected claim exists. You can use `*` to select and send all claims to your API.

Use case scenarios:
- Call your API from an authentication method each time a user is authenticated either in FoxIDs or with an external identity provider. 
  You can then find the user in your database and return a user ID and maybe a customer ID or basically anything of relevance. For example, you can also create the user in your database.
- Call your API from an application registration with the user ID (`sub`) and query the user's roles in your database. Your API would then either return an empty list or a list of role claims or maybe a more complex rights structure.

### Implement API

You need to implement a simple API that FoxIDs calls when the claim transformation is executed.  
Please have a look at the [sample code](#api-sample).

The API has a base URL, and the functionality is divided into folders. Currently, only the `claims` folder (functionality) for requesting a list of claims is supported.  

If the base URL for the API is `https://somewhere.org/myclaimsstore` the URL for the `claims` folder will be `https://somewhere.org/myclaimsstore/claims`.

> FoxIDs Cloud calls your API from the IP address `57.128.60.142`.  
  *The outgoing IP address can be changed and more can be added over time.*

#### Request
Secured with [HTTP Basic auth](https://datatracker.ietf.org/doc/html/rfc6749#section-2.3.1): username `external_claims`, password = configured secret.

The API is called with HTTP POST and a JSON body.

This is a request JSON body with two input claims:
```JSON
{
  "claims": [
    { "type": "sub", "value": "1b1ac05e-5937-4939-a49c-0e84a89662df" },
    { "type": "email", "value": "some@test.org" }
  ]
}
```

#### Response - Success
On success the API should return HTTP code 200 and a list of `claims` (the list can be empty).

For example, the user's sub (user ID / username), customer ID and roles:
```JSON
{
    "claims": [
        { "type": "sub", "value": "somewhere/external-some@test.org" },
        { "type": "customer_id", "value": "1234abcd" },
        { "type": "role", "value": "admin_access" },
        { "type": "role", "value": "read_access" },
        { "type": "role", "value": "write_access" }
    ]
}
```

#### Response - Error 
The API must return HTTP code 401 (Unauthorized) and an `error` (required) if the Basic auth is rejected. Optionally add an error description in `ErrorMessage`.
```JSON
{
    "error": "invalid_api_id_secret",
    "ErrorMessage": "Invalid API ID or secret"
}
```

If other errors occur, the API should return HTTP code 500 or another appropriate error code.  
It is recommended to add a technical error message `ErrorMessage` for diagnostics (it is only logged; never shown to the end user).

> Error messages returned from the API in `ErrorMessage` are NOT displayed to the user; they are only logged.

### API Sample
The sample [ExternalClaimsApiSample](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalClaimsApiSample) shows how to implement the API in ASP.NET Core.

You can use this [Postman collection](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalClaimsApiSample/external-claims-api.postman_collection.json) to call and test your API with [Postman](https://www.postman.com/downloads/).

### Configure 
Configure FoxIDs to call your API from a claim transformation in [FoxIDs Control Client](control.md#foxids-control-client).

 1. Navigate to the **Claim Transform** section
 2. Click **Add claim transform**
 3. Click **External claims API**
 4. Select **Add claim** or **Replace claim**
 5. Add the selected claims e.g. `sub` in **Select claims**
 6. Add the base API URL without the `claims` folder in **API URL**
 7. Add the **API secret**
    ![Configure an external claims API claims transformation](images/configure-external-claims-config.png)
 8. Click **Update**

## Claim transform examples

### Split the `name` claim into the two claims `given_name` and `family_name`

The transformation will split the value in the `name` claim at the first occurring space and respectively add the `given_name` and `family_name` claims, if they do not already exist.  
If there is more than one space in the `name` claim value, new `given_name` and `family_name` claims will not be added because they already exist.

Use two `Regex map` claim transformations.

![Transform name to given_name and family_name](images/example-claim-transform-name-to-given_name-family_name.png)

- Find the `family_name` claim value with regex `^\S+\s(?<map>\S+)$`
- Find the `given_name` claim value with regex `^(?<map>\S+)\s\S+$`


### Remove the default added authentication method name from `sub`

The authentication method name is added by default to the `sub` claim value as a prefix divided by a pipe e.g., `some-auth-method|my-external-user-id`.

You can use a replace claim on the `sub` claim to remove the default-added prefix value.

The transformation will split the value in the `sub` claim and replace the claim with a new `sub` only containing the original ID.

Use a `Regex map` claim transformation and select the `Replace claim` action.

![Remove default added post authentication method name](images/example-claim-transform-remove-post-auth-method-name.png)

Find the ID without the default added post authentication method name with regex `^(nemlogin\|)(?<map>.+)$`

> You can do the same in a SAML 2.0 authentication method using the `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` claim (which contains the SAML 2.0 Authn Response `NameID` value) instead of the `sub` claim.
