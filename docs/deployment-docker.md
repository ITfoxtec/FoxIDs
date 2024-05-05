# Docker

Deploy FoxIDs in Docker Desktop on a dev or test machine.

This is a description of how to make a default [deployment](#deployment) and [login](#first-login) for the first time.

Pre requirements:
- You have Docker Desktop installed. 
- You have basic knowledge about Docker.

> This is a list of [useful commands](#useful-commands) in the end of this description.

This deployment include:

- Two websites one for FoxIDs and one for the FoxIDs Control (Client and API) in two docker images [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) and [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general) or generated from code with `Dockerfile` files.
- The two websites is exposed on two different ports.
- MongoDB is a NoSQL database and contains all data including tenants, environments and users. Deployed with the [official MongoDB](https://hub.docker.com/_/mongo) Docker image.
- Redis cache holds sequence (e.g., login and logout sequences) data, data cache to improve performance and handle counters to secure authentication against various attacks. Deployed with the [official Redis](https://hub.docker.com/_/redis) Docker image.
- Logs are written to `stdout` where the logs can be picked up by Docker.

> Optionally use PostgreSql instead of MongoDB and optionally opt out Redis and save the cache in the database (MongoDB or PostgreSql). Without a Redis cache you need to select `None` as data cache.

## Deployment

The deployment is carried out in the described order.

### Get ready
Clone the [git repository](https://github.com/ITfoxtec/FoxIDs) or download as ZIP. The Docker yaml configuration files is in the `./Docker` folder.  
Open a console and navigate to the `./Docker` folder.

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
The two FoxIDs websites is configured with either images from Docker Hub or images generated from code with `Dockerfile` files. And optional configured to use either only HTTP or both HTTP/HTTPS with a development certificate.  
The official MongoDB and Redis images is pulled from Docker Hub.

**Email provider**  
You can optionally configure a global email provider or later configure [email providers](email) per environment. FoxIDs supports sending emails with SendGrid and SMTP.

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
    value: "my@email-address.org"
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