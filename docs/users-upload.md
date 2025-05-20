# Upload users

Upload your users to an environment, with or without a password:

- You can upload the users with there password, if you know the users passwords. 
- Otherwise, you can upload the users without a password and the users are then requested to set a password with a email or SMS code. Require the users to have either a email or phone number.

The users is bulk uploaded to an environment with 10,000 users at the time supporting upload of millions of users. You can either user the [FoxIDs Control API](control.md#foxids-control-api) directly or use the [seed tool](#upload-with-seed-tool). 

## Upload with seed tool

The seed tool reads users from a `SVC` file and upload the users to the configured environment.

### SVC file


Format:


### Download and configure the seed tool

First download the `FoxIDs.SeedTool-x.x.x-win-x64.zip` file for Windows or `FoxIDs.SeedTool-x.x.x-linux-x64.zip` file for Linux from the [FoxIDs release](https://github.com/ITfoxtec/FoxIDs/releases) and unpack the seed tool.

The seed tool is configured in the `appsettings.json` file.

Access to upload users is granted in your `master` environment.

Create a seed tool OAuth 2.0 client in the [FoxIDs Control Client](control.md#foxids-control-client):

*This will grant the seed tool full access to your tenant, for least privileges please see [API access rights](control.md#api-access-rights).*

1. Login to your tenant
2. Select the **master** environment (in the top center environment selector)
3. Select the **Applications** tab
4. Click **New Application**
5. Click **Backend Application**
    a. Select **Show advanced**
    b. Add a **Name** e.g., `Seed tool`
    c. Change the **Client ID** to `foxids_seed`
    d. Click **Register**
    e. Remember the **Authority**.
    e. Remember the **Client secret**.
    f. Click **Close**
6. Click on your client registration in the list to open it
7. In the **Resource and scopes** section 
    a. Click **Add Resource and scope** and add the resource `foxids_control_api`
    b. Then click **Add Scope** and add the scope `foxids:tenant` 
8. Select **Show advanced**
9. In the **Issue claims** section
    a. Click **Add Claim** and add the claim `role`
    b. Then click **Add Value** and add the claim value `foxids:tenant`
10. Click **Update**

![FoxIDs Control Client - master seed tool client](images/seed-tool-client.png)

Add your FoxIDs Control API endpoint, the **Authority**, the **Client secret** and `SVC` file path to the seed tool configuration. 

```json
"SeedSettings": {
    "FoxIDsControlEndpoint": "https://control.yyyyxxxx.com", // custom domain or local development https://localhost:44331
    "Authority": "https://id.yyyyxxxx.com/zzzzz/master/foxids_seed/", // with custom domain or local development "https://https://localhost:44331/zzzzz/master/foxids_seed/"
    "ClientId": "foxids_master_seed",
    "ClientSecret": "xxxxxx",
    "UsersSvcPath": "c:\\... xxx ...\\users.svc"
}
```

### Run the seed tool

1. Start a Command Prompt 
2. Run the seed tool with `SeedTool.exe`
3. Click `U` to start uploading users  

> The users upload can take a while depending on the number of users.
