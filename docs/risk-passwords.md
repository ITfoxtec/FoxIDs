# Risk passwords

You can achieve higher password quality and a higher level of security by using risk passwords for password validation. 

Hundreds of millions of real world passwords previously exposed in data breaches is collected as risk passwords. By validating that the leaked passwords are not reused, you significantly increase the level of password security.

**1) Download risk passwords (pwned passwords)**  
Download the `SHA-1` pwned passwords in a single file from [haveibeenpwned.com/passwords](https://haveibeenpwned.com/Passwords) using the [PwnedPasswordsDownloader tool](https://github.com/HaveIBeenPwned/PwnedPasswordsDownloader).

> Be aware that it takes some time to download all risk passwords.

**2) Upload risk passwords to FoxIDs**  
You can upload risk passwords with the FoxIDs seed tool console application. The seed tool code is [downloaded](https://github.com/ITfoxtec/FoxIDs/tree/master/tools/FoxIDs.SeedTool) and need to be compiled and [configured](#configure-the-seed-tool) to run.

> The risk passwords is uploaded ones per FoxIDs deployment in the master tenant.

**3) Test**  
You can read the number of risk passwords uploaded to FoxIDs in [FoxIDs Control Client](control.md#foxids-control-client) master tenant on the Settings / Risk Passwords tap. And you can test if a password is okay or has appeared in breaches.

## Configure the Seed Tool

The seed tool is configured in the `appsettings.json` file.

Access to upload risk passwords is granted in the `master` tenant.

Create a seed tool OAuth 2.0 client in the [FoxIDs Control Client](control.md#foxids-control-client):

1. Login to the **master** tenant
2. Select the **Applications** tab
3. Click **New Application**
4. Click **Backend Application**
    a. Select **Show advanced**
    b. Add a **Name** e.g., `Seed tool`
    c. Set the **Client ID** to `foxids_seed`
    d. Click **Register**
    e. Remember the **Client secret**.
    f. Click **Close**
5. Click on your client registration in the list to open it
6. In the **Resource and scopes** section - *This will granted the client access to the master tenant*
    a. Click **Add Resource and scope** and add the resource `foxids_control_api`
    b. Then click **Add Scope** and add the scope `foxids:master` 
7. Select **Show advanced**
8. In the **Issue claims** section - *This will granted the client the tenant administrator role*
    a. Click **Add Claim** and add the claim `role`
    b. Then click **Add Value** and add the claim value `foxids:tenant.admin`
9. Click **Update**

![FoxIDs Control Client - seed tool client](images/upload-risk-passwords-seed-client.png)

Add your FoxIDs and FoxIDs Control API endpoints and client secret and local risk passwords (pwned passwords) file to the seed tool configuration. 

```json
"SeedSettings": {
    "FoxIDsEndpoint": "https://foxidsxxxx.azurewebsites.net",
    "FoxIDsControlEndpoint": "https://foxidscontrolxxxx.azurewebsites.net",
    "ClientSecret": "xxx",
    ...
    "PwnedPasswordsPath": "c:\\... xxx ...\\pwned-passwords-sha1-ordered-by-count-v4.txt"
}
```

## Run the Seed Tool

Run the seed tool executable SeedTool.exe or run the seed tool directly from Visual Studio. 

* Click 'p' to start uploading risk passwords  

The risk password upload will take a while.