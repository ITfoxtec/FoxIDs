# IIS on Windows Server

Deploy FoxIDs on Windows Server with MongoDB and OpenSearch.

This is a description of how to make a default [deployment](#deployment) and [log in for the first time](#first-login).

A FoxIDs installation is like a bucket, there is no external dependencies and it's easy to archive a very high uptime with little effort. 
**FoxIDs are updated by downloading source code from the [release](https://github.com/ITfoxtec/FoxIDs/releases) and ????  **
New FoxIDs releases is backwards compatible, please consult the [release notes](https://github.com/ITfoxtec/FoxIDs/releases) before updating.

Pre requirements:
- You have a Windows Server with Internet Information Services (IIS) - fully updated. 
- You have basic knowledge about Windows Servers and IIS.

This guid describe how to install FoxIDs on a single server but you can easily divide the installation on to different servers.

This deployment include:

- Two websites one for FoxIDs and one for the FoxIDs Control (Client and API) deployed with xcopy from source code.
- The two websites is exposed on two different domains / sub-domains.
- MongoDB is a NoSQL database and contains all data including tenants, environments and users. Deploy [MongoDB Community Edition](https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-windows/) on Windows. You can optionally use your own PostgreSQL instance instead of MongoDB.
- Default holds the cache in the database. Optionally use a Redis cache if you are installing a FoxIDs cluster with high throughput. The cache holds sequences (e.g., login and logout) and handle counters to secure authentication against various attacks and data cache (Redis only) to improve performance. Redis is deployed with the [official Redis](https://hub.docker.com/_/redis) Docker image.

## Deployment

The deployment is carried out in the described order.

### Install database

Download and install [MongoDB Community Edition](https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-windows/).  
Or optionally download and install [PostgreSQL](https://www.postgresql.org/download/windows/).

MongoDB's default endpoint `mongodb://localhost:27017`
 
    ?? mongo db password ??


### .NET runtime


1)
Install on Windows Server(s) with Internet Information Services (IIS).

2)
Create two websites in IIS 
 -corresponding folders
 -domains
 -hosts localhost to domains
 -change app service **.NET CLR Version** to `No Managed Code`

3)
Install your own certificate or Let's encrypt

4)
xcoppy the two FoxIDs sites to folders
   -change config
        -domains
        -maybe DB


m


Find logs in: C:\inetpub\logs\LogFiles
Depending on the load, considder using OpenSearch in production














### Get ready
Download the source code from the desired FoxIDs [release](https://github.com/ITfoxtec/FoxIDs/releases) and unpack the ZIP file.  


MongoDB Community Server Download download
https://www.mongodb.com/docs/manual/tutorial/install-mongodb-on-windows/

Separate MongoDB Compass Download (GUI) download - also part of the MongoDB Community Server Download
https://www.mongodb.com/try/download/compass



### Volume 
You need a volume for MongoDB with the name `foxids-data` where data is saved.

Either create a `volume` for MongoDB on your Windows host file system in e.g., the folder `C:\data\foxids-data`. Important: create the folders before running the command.
```cmd
docker volume create --driver local --opt type=none --opt device=C:\data\foxids-data --opt o=bind foxids-data
```

OR, create a `volume` for MongoDB which is managed by Docker.
```cmd
docker volume create foxids-data
```
 
### Deploy containers
The two FoxIDs websites is configured with either images from [Docker Hub](https://hub.docker.com/u/foxids) or images generated from code with `Dockerfile` files. And optional configured to use either only HTTP or both HTTP/HTTPS with a development certificate.  
The official MongoDB and Redis images is pulled from Docker Hub.

**Email provider**  
You can optionally configure a global email provider or later configure [email providers](email.md) per environment. FoxIDs supports sending emails with SendGrid and SMTP.

The global email provider is configured in the `docker-compose-image.yaml` or the `docker-compose-project.yaml` file on the `foxids` service in the `environment:` section.  
This example show how to add Outlook / Microsoft 365 with SMTP:

```yaml
- name: "Settings__Smtp__FromEmail"
    value: "my@email-address.org"
- name: "Settings__Smtp__FromName" # Optional from name associated to the email address 
    value: "e.g, my company name"
- name: "Settings__Smtp__Host"
    value: "smtp.office365.com"
- name: "Settings__Smtp__Port"
    value: "587"
- name: "Settings__Smtp__Username"
    value: "my@email-address.com"
- name: "Settings__Smtp__Password"
    value: "xxxxxxx"
```

**Deploy**  
Create the deployment, select one of the three following ways:

1) All based on images from Docker Hub and with HTTP
```yaml
docker-compose -f docker-compose-image.yaml -f docker-compose.development-http.yaml up -d
```

2) Partial based on images generated from code with `Dockerfile` files and with HTTP
```yaml
docker-compose -f docker-compose-project.yaml -f docker-compose.development-http.yaml up -d
```

3) Partial based on images generated from code with `Dockerfile` files and with HTTP/HTTPS - require the development certificate to be present.
```yaml
docker-compose -f docker-compose-project.yaml -f docker-compose.development-https.yaml up -d
```

## First login
Open your FoxIDs Control site (<a href="http://localhost:8801" target="_blank">http://localhost:8801</a> or <a href="https://localhost:8401" target="_blank">https://localhost:8401</a>) in a browser. It should redirect to the FoxIDs site where you login with the default admin user `admin@foxids.com` and password `FirstAccess!` (you are required to change the password on first login).  
You are then redirected back to the FoxIDs Control site in the `master` tenant. You can add more tenants in the master tenant and e.g., configure admin users.

Then click on the `main` tenant and authenticate once again with the same default admin user email and password (the default admin user email and password is the same for both the `master` tenant and the `main` tenant, but it is two different users).  
You are now logged into the `main` tenant and can start to configure your [applications and authentication methods](connections.md).

## Useful commands
This is a list of commands which may be useful during deployment to view details and to make deployment changes.

Tear down the deployment
```cmd
docker-compose -f docker-compose-image.yaml -f docker-compose.development-http.yaml down
# or
docker-compose -f docker-compose-project.yaml -f docker-compose.development-http.yaml down
# or
docker-compose -f docker-compose-project.yaml -f docker-compose.development-https.yaml down
```

Build image with `Dockerfile` file
```cmd
docker build -f ./src/foxids/Dockerfile . -t foxids:x.x.x    # x.x.x is the version
# or
docker build -f ./src/foxids.control/Dockerfile . -t foxids-control:x.x.x    # x.x.x is the version
```

Stop container
```cmd
docker stop xxx
```

Remove container
```cmd
docker rm xxx
```

Remove image
```cmd
docker rmi xxx
```

List volumes
```cmd
docker volume ls
```

Remove volume
```cmd
docker volume rm xxx
```

Remove all unused volumes
```cmd
docker volume prune
```

Show logs in container
```cmd
Docker logs xxx
```