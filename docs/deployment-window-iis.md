# Windows Server with IIS

Deploy FoxIDs on Windows Server with Internet Information Services (IIS).

This is a description of how to do a default [deployment](#deployment) and [log in for the first time](#first-login).

A FoxIDs installation is like a bucket, there is no external dependencies and it's easy to archive a very high uptime with little effort. 
FoxIDs are updated by downloading the `FoxIDs-x.x.x-win-x64.zip` file from the new [release](https://github.com/ITfoxtec/FoxIDs/releases) and then updating the files in the two websites (not overriding `appsettings.json`).
New FoxIDs releases is backwards compatible, please consult the release notes before updating.

Pre requirements:
- You have a Windows Server (or Windows 10/11) with Internet Information Services (IIS). 
- You have basic knowledge about Windows Servers and IIS.

This guid describe how to install FoxIDs on a single server but you can divide the installation on to different servers.

This deployment include:

- Two websites one for FoxIDs and one for the FoxIDs Control (Admin Client and API).
- The two websites are exposed on two different domains / sub-domains.
- NoSQL database containing all data including tenants, environments and users. Either deploy **MongoDB Community Edition** or **PostgreSQL**.
- FoxIDs logs are default saved in files. Depending on the load, consider to use [OpenSearch](#opensearch) in production.

## Deployment

The deployment is carried out in the described order.

### Install database

Download and install [MongoDB Community Edition](https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-windows/) or download and install [PostgreSQL](https://www.postgresql.org/download/windows/).

**MongoDB**  
A self-managed MongoDB deployment is default installed with access control disabled. Consider to [configure MongoDB authentication](https://www.mongodb.com/docs/manual/tutorial/configure-scram-client-authentication/) depending on your installation.  
MongoDBs default endpoint / connection string: `mongodb://localhost:27017`

You can install the MongoDB Compass (GUI) with the MongoDB Community Edition or [download](https://www.mongodb.com/try/download/compass) and install the MongoDB admin application separately.

**PostgreSQL**  
PostgreSQL is default deployed with the user `postgres` and a password provided by you during installation.

- Open pgAdmin and create a database for FoxIDs called `FoxIDs`

PostgreSQL default endpoint / connection string: `Host=localhost;Username=postgres;Password=xxxx;Database=FoxIDs`

### Add two websites
Add two websites on IIS.

Add the FoxIDs website:
- Site name `FoxIDs`
- Physical path e.g. `C:\inetpub\FoxIDs`
- The `http` domain binding for your domain e.g. `http://id.my-domain.com`
- Change app service **.NET CLR Version** to `No Managed Code`

  ![ Windows Server with IIS - add the FoxIDs website](images/deployment-window-iis-add-website.png)

And add the FoxIDs Control website:
- Site name `FoxIDs.Control`
- Physical path e.g. `C:\inetpub\FoxIDs.Control`
- The `http` domain binding for your domain e.g. `http://control.my-domain.com`
- Change app service **.NET CLR Version** to `No Managed Code`

> Optionally add the two domains to the servers' `hosts` file `C:\Windows\System32\drivers\etc\hosts` to enable local test on the server:   
> ```js
>    127.0.0.1     id.my-domain.com
>    127.0.0.1     control.my-domain.com
> ```

### HTTP and HTTPS
FoxIDs support both HTTP and HTTPS, but you should always use HTTPS in production.

> You can skip this section if you wish to run FoxDs on HTTP without a certificate.

You can either use your own certificate(s) or have a certificate created by Let's encrypt.

**Use your own certificate(s)**  
Install your own certificate(s) on the server and configure `https` bindings for the two websites.

![ Windows Server with IIS - add the FoxIDs website](images/deployment-window-iis-my-cert.png)

**Certificate created by Let's encrypt**  
Create and add Let's encrypt certificate to the two websites with [win-acme](https://github.com/win-acme/win-acme).

Download the `win-acme.v2.x.x.x64.pluggable.zip` file from the latest [win-acme release](https://github.com/PKISharp/win-acme/releases).

1. Unpack and place the `win-acme.v2.x.x.x.x64.pluggable` folder a permanent place e.g. on the C drive. The component is subsequently registered to run in Windows Task Scheduler.
2. Start an elevated Command Prompt in administrative mode 
3. Navigate to the `win-acme.v2.x.x.x.x64.pluggable` folder
4. Run `wacs.exe`
5. Click `N`
6. Select the websites `FoxIDs` and `FoxIDs.Control`, probably by typing: `2`, `3`
7. Click `A`
8. Pick the `FoxIDs` sits as the main host
9. Agree with the terms (click `N` and then click `Y`)
10. Add your email

The two websites now have `https` bindings with the certificate created by Let's encrypt and the certificate will automatically be updated for every 3 months or so.

### Xcopy deploy FoxIDs to websites
Download the `FoxIDs-x.x.x-win-x64.zip` file from the [FoxIDs release](https://github.com/ITfoxtec/FoxIDs/releases) and unpack the ZIP file. The zip file contains two folders one for the FoxIDs site and one for the FoxIDs Control site.

- Xcopy the zip file folder FoxIDs into the websites physical path e.g. `C:\inetpub\FoxIDs`
- And Xcopy the zip file folder FoxIDs.Control into the websites physical path e.g. `C:\inetpub\FoxIDs.Control`

Configure both the FoxIDs site and the FoxIDs Control site in the `appsettings.json` files, located in e.g. `C:\inetpub\FoxIDs\appsettings.json` and `C:\inetpub\FoxIDs.Control\appsettings.json`

1. Set the FoxIDs site domain in **FoxIDsEndpoint** e.g. `http://id.my-domain.com` or `https://id.my-domain.com`
2. In FoxIDs Control site only. Set the FoxIDs Control site domain in **FoxIDsControlEndpoint** e.g. `http://control.my-domain.com` or `https://control.my-domain.com` 
3. If the domain starts with `http://...`, remove the commenting out of `"UseHttp": true,`
4. Configure the database access, either to **MongoDB**
    ```json
    "Options": {
        "Log": "Stdout",
        "DataStorage": "MongoDb",
        "KeyStorage": "None",
        "Cache": "MongoDb",
        "DataCache": "None"
    },
    "MongoDb": {
        "ConnectionString": "mongodb://localhost:27017"
    },
    ```
    or to **PostgreSql** with your `postgres` user's password
    ```json
    "Options": {
        "Log": "Stdout",
        "DataStorage": "PostgreSql",
        "KeyStorage": "None",
        "Cache": "PostgreSql",
        "DataCache": "None"
    },
    "PostgreSql": {
      "ConnectionString": "Host=localhost;Username=postgres;Password=xxxxxxxx;Database=FoxIDs"
    },
    ```
5. Optionally configure to send emails with SMTP.

### FoxIDs Logs
FoxIDs log files are default saved in `C:\inetpub\logs\LogFiles`. You can change the path in the `web.config` file in the two websites.

The logs contain errors, warnings, events and trace.

### OpenSearch
Depending on the load, consider to use OpenSearch in production instead of log files.

Download [OpenSearch](https://docs.opensearch.org/docs/latest/install-and-configure/install-opensearch/windows/) or download from the [download page](https://opensearch.org/downloads/).

1. Create a folder on a permanent place e.g. `C:\opensearch` on the C drive. The OpenSearch `.bat` file is subsequently registered to run in Windows Task Scheduler.
2. Move the downloaded file `opensearch-x.x.x-windows-x64.zip` to the folder and unpack the file - *the file names are to log to unpack in the default download folder*
3. Start a Command Prompt 
4. Navigate to the `opensearch-x.x.x` folder
5. Set an administrator password, run `set OPENSEARCH_INITIAL_ADMIN_PASSWORD=<custom-admin-password>`
6. Start service, run `.\opensearch-windows-install.bat`
7. Start another Command Prompt 
8. Test the OpenSearch, run test request `curl.exe -X GET https://localhost:9200 -u "admin:<custom-admin-password>" --insecure`
9. Test the OpenSearch plugins, run test request `curl.exe -X GET https://localhost:9200/_cat/plugins?v -u "admin:<custom-admin-password>" --insecure`
10. Go back to the OpenSearch Command Prompt and stop OpenSearch by clicking `ctrl+c` and then `y`

Create a task to rune OpenSearch
1. Open **Task Scheduler**
2. Click **Create Task...**
3. Add the **Name** `OpenSearch`
4. Change the account that run the task, click **Change User or Group...**
5. Write `NETWORK SERVICE` and click **OK**
6. Select the **Actions** tab
7. Click **New...**
8. In **Program/script** start the `.bat` file e.g., write `C:\opensearch\opensearch-x.x.x\opensearch-windows-install.bat` and click **OK**
9. Select the **Settings** tab
10. Select the setting **If the task fails, restart every:**
11. Deselect the setting (remove the checkmark) **Stop the task if it runs longer then:**
12. Click **OK**
13. Start the task

OpenSearch is default started with a self-signed certificate. You can configure a domain and a certificate but, in this guide, the self-signed certificate is retained and FoxIDs is configured to accept the certificate.

Configure OpenSearch in both the FoxIDs site and the FoxIDs Control site in the `appsettings.json` files, located in e.g. `C:\inetpub\FoxIDs\appsettings.json` and `C:\inetpub\FoxIDs.Control\appsettings.json`

```json
"Options": {
    "Log": "OpenSearchAndStdoutErrors",
    //DB configuration...
},
"OpenSearch": {
    "Nodes": [ "https://admin:xxxxxxxx@localhost:9200/" ],
    "LogLifetime": "Max180Days", 
    "AllowInsecureCertificates": true //Accept self-signed certificate
},
```

## First login
Open your FoxIDs Control site (<a href="http://control.my-domain.com" target="_blank">http://control.my-domain.com</a> or <a href="https://control.my-domain.com" target="_blank">https://control.my-domain.com</a>) in a browser. 
It should redirect to the FoxIDs site where you login with the default admin user `admin@foxids.com` and password `FirstAccess!` (you are required to change the password on first login).  
You are then redirected back to the FoxIDs Control site in the `master` tenant. You can add more admin users in the master tenant.

Then click on the `main` tenant and authenticate once again with the same default admin user `admin@foxids.com` and password `FirstAccess!` (again, you are required to change the password).

> The default admin user and password are the same for both the `master` tenant and the `main` tenant, but it is two different users. 

You are now logged into the `main` tenant and can start to configure your [applications and authentication methods](connections.md).