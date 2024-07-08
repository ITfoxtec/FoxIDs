# External Login

External Login can authenticate users in an external user store using an [API call](#api). The API is called with a username and password, and the API then validate the username and password combination and return a response indicating success or failure.  
You would use an external login authentication method if you have an existing user store to leverage the user store as a possible authentication method in FoxIDs. It is possible to create [external users](users.md#external-users) and thereby e.g., ask the user for an email or name.

> If desired, you can possible owner time migrate users to FoxIDs and phase out the API and existing user store. However, it requires that an email has been added to the user.

External login support two username types:
- Email - users email as the username
- Text - a text-based username

If you choose a text-based username, the username is not validated in FoxIDs and in this case you can use a combination of different usernames, including using an email as username.

It is only possible to use [home realm discovery (HRD) based on the domain](login.md#home-realm-discovery-hrd) if you select email as the username type.

The external Login UI with a text-based username.

![External Login UI](images/configure-external-login-ui.png)

The external login UI can be [customized](customization.md).

## API

You need to implement a simple API which FoxIDs will call on each authentication request. Please also have a look at the [sample code](#api-sample).

The API has a base URL and the functionality is divided into folders. Currently, only the `authentication` folder for validating the username and password is support.  
*Other folders for changing passwords and creating new users will be added later.*

If the base URL for the API is `https://somewhere.org/mystore` the URL for the `authentication` folder will be `https://somewhere.org/mystore/authentication`.

### Request
The API call is secured with [HTTP Basic authentication scheme](https://datatracker.ietf.org/doc/html/rfc6749#section-2.3.1) where FoxIDs sends the ID `external_login` as username and the configured secret as password.

The API is called with HTTP POST and a JSON body.

This is a JSON body for the username type `email`:
```JSON
{
    "usernameType": 100,
    "username": "user1@somewhere.org",
    "password": "testpass1"
}
```

And this is a JSON body for the username type `text`:
```JSON
{
    "usernameType": 200,
    "username": "user1",
    "password": "testpass1"
}
```

The username types:
- `email` is `100` 
- `text` is `200`

### Response
**Success**  
On success the API should return HTTP code 200 and optionally a list of claims for the authenticated user.

For example, the user's, sub (unique ID), name, email and maybe even a role:
```JSON
{
    "claims": [
        {
            "type": "sub",
            "value": "somewhere/user2"
        },
        {
            "type": "given_name",
            "value": "Joe"
        },
        {
            "type": "family_name",
            "value": "Smith"
        },
        {
            "type": "email",
            "value": "user2@somewhere.org"
        },
        {
            "type": "role",
            "value": "some_access"
        }
    ]
}
```

**Error**  
The API should return HTTP code 401 (Unauthorized) if the Basic authentication is rejected.

The API should return HTTP code 403 (Forbidden) if the username and password combination is rejected.

In other errors the API should return HTTP code 400 (Bad Request), 500 (Internal Server Error) or another or another appropriate error code

In any error case a error description should be added to the return body. The error message can then later be found in the FoxIDs logs.  
The error message is NOT shown for the user.

## API Sample
The sample [ExternalLoginApiSample](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalLoginApiSample) show how to implement the API in ASP.NET Core 8.

You can user this [Postman collection](https://github.com/ITfoxtec/FoxIDs.Samples/tree/main/src/ExternalLoginApiSample/external-login-api.postman_collection.json) to call and test the sample with Postman.

## Configure 
Configure a external login authentication method to call your API in [FoxIDs Control Client](control.md#foxids-control-client).

 1. Navigate to the **Authentication Methods** tab
 2. Click **New authentication**
 3. Select **Show advanced**
 4. Select **External Login**
 5. Add the **Name**
 6. Select **Username type**
 7. Add the API base URI without the `authentication` folder in **API URI**
 8. Add the **API secret**
    ![Configure a external login authentication method](images/configure-external-login-config.png)
 9. Click **Create**

 Optionally click **Show advanced** in the top right corner of the configuration section to [customized](customization.md) the login UI.
