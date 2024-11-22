# Claim transforms

Each FoxIDs authentication method and application registration handle [claims](claim.md) and support claim transformations. 
This means that two sets of claim transforms can be executed on each user authentication. 
First executing claim transforms on the authentication method and then claim transforms on the application registration. 

If you create a new claim in a claim transform, the claim is send from the authentication method if the claim or `*` is in the `Forward claims` list. 
The application registration receives the claim and issues the claim if the claim or `*` is in the `Issue claims` list or alternative if the claim is in a requested scope's `Voluntary claims` list. 

Please see [claim transform examples](#claim-transform-examples)

> Enable `Log claim trace` in the [log settings](logging.md#log-settings) to see the claims before and after transformation in the [logs](logging.md). 

Claim transforms can e.g., be configured in a login authentication method.

![FoxIDs authentication method claim transform](images/configure-claim-transform-auth-method.png)

And likewise claim transforms can e.g., be configured in a OpenID Connect application registration.

![FoxIDs application registration claim transform](images/configure-claim-transform-app-reg.png)

> Claims are by default represented as JWT claims. If the authentication method or application registration is SAML 2.0 the claims is represented as SAML 2.0 claims.

A claim transform will do one of op to five different actions depending on the particular claim transform type.

Claim transform actions:

- `Add claim` - add a new claim
- `Add claim, if not match` - do the add action if the condition does not match
- `Replace claim` - add a new claim and remove existing claims if one or mere exist
- `Replace claim, if not match` - do the replace action if the condition does not match
- `Remove claim` - remove the claims if one or mere exist

The claim transforms is executed in order and the actions is therefore executed in order. This means that it is possible to create a local variable by adding a claim and later in the sequence removing the same claim again.

With the `Add claim, if not match` actions it is possible to add a claim if another claim or a claim with a value do not exist.

Claim transform types that support all actions:

- `Match claim` - do the action if the claim type match
- `Match claim and value` - do the action if the claim type and claim value match
- `Regex match` - do the action if the claim type match and claim value match the regular expression

Claim transform types that support `Add claim` and `Replace claim` and `Add claim, if new claim do not exist` actions:

- `Map` - do the action if the claim type match, then map the claim value to a new claim
- `Regex map` - do the action if the claim type match and claim value match the regular expression group, then map the group value to a new claim

Claim transform types that support `Add claim` and `Replace claim` actions:

- `Constant` - always do the action (add/replace a claim with a constant value)
- `Concatenate` - do the action if one or more of the claim types match, then concatenate the claim values to a new claim
- `External claims API` - Call an external API with the selected claims to add/replace claims with external claims
- `DK XML privilege to JSON` - Converting the [DK privilege to JSON](claim-transform-dk-privilege). 

## Claim transform examples

### Split the `name` claim into the two claims `given_name` and `family_name`

The transformation will split the value in the `name` claim at the first occurring space and respectively add the `given_name` and `family_name` claims, if they do not already exist.  
If there are more than one space in the `name` claim value. New `given_name` and `family_name` claims will not be added because they already exist.

Use two `Regex map` claim transformations.

![Transform name to given_name and family_name](images/example-claim-transform-name-to-given_name-family_name.png)

- Find the `family_name` claim value with regex `^\S+\s(?<map>\S+)$`
- Find the `given_name` claim value with regex `^(?<map>\S+)\s\S+$`


### Remove the default added authentication method name from `sub`

The authentication method name is default added to the `sub` claim value as a post name divided by a pipe e.g., `some-auth-method|my-external-user-id`.

You can do a replace claim on the `sub` claim to remove the default added post value.

The transformation will split the value in the `sub` claim and replace the claim with a new `sub` only containing the original ID.

Use a `Regex map` claim transformation and select the `Replace claim` action.

![Remove default added post authentication method name](images/example-claim-transform-remove-post-auth-method-name.png)

Find the ID without the default added post authentication method name with regex `^(nemlogin\|)(?<map>.+)$`

> You can do the same in a SAML 2.0 authentication method using the `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` claim instead of the `sub` claim.