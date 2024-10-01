# Upload risk passwords

You can increase the password security level by uploading risk passwords. 

You can upload risk passwords with the FoxIDs seed tool console application. The seed tool code is [downloaded](https://github.com/ITfoxtec/FoxIDs/tree/master/tools/FoxIDs.SeedTool) and need to be compiled and [configured](#configure-the-seed-tool) to run.

Download the `SHA-1` pwned passwords in a single file from [haveibeenpwned.com/passwords](https://haveibeenpwned.com/Passwords) using the [PwnedPasswordsDownloader tool](https://github.com/HaveIBeenPwned/PwnedPasswordsDownloader).

> Be aware that it takes some time to upload all risk passwords.

You can read the number of risk passwords uploaded to FoxIDs in [FoxIDs Control Client](https://www.foxids.com/action/login) master tenant on the Settings / Risk Passwords tap. And you can test if a password is okay or has appeared in breaches.

## Configure the Seed Tool

The seed tool is configured in the `appsettings.json` file.

Access to upload risk passwords is granted in the `master` tenant.

Create a seed tool OAuth 2.0 client in the [FoxIDs Control Client](https://www.foxids.com/action/login):

1. Login to the `master` tenant and select the Applications tab
2. Create a OAuth 2.0 application registration, click `OAuth 2.0 - Client Credentials Grant`.
3. Set the client id to `foxids_seed`.
4. Remember the client secret.
5. In the resource and scopes section. Grant the sample seed client access to the FoxIDs Control API resource `foxids_control_api` with the scope `foxids:master`.
6. Click show advanced. 
7. In the issue claims section. Add a claim with the name `role` and the value `foxids:tenant.admin`. This will grant the client the administrator role. 

![FoxIDs Control Client - seed tool client](images/upload-risk-passwords-seed-client.png)

Add the FoxIDs and FoxIDs Control API endpoints and client secret to the seed tool configuration. 

```json
"SeedSettings": {
    "FoxIDsEndpoint": "https://foxidsxxxx.azurewebsites.net",
    "FoxIDsControlEndpoint": "https://foxidscontrolxxxx.azurewebsites.net",
    "ClientSecret": "xxx",
    ...
}
```

## Run the Seed Tool

Run the seed tool executable SeedTool.exe or run the seed tool directly from Visual Studio. 

* Click 'p' to start uploading risk passwords  

The risk password upload will take a while.