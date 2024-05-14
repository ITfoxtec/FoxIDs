# Kubernetes

Deploy FoxIDs in your Kubernetes (K8s) cluster or Docker Desktop with Kubernetes enabled.

This is a description of how to make a default [deployment](#deployment), [login](#first-login) for the first time and some [considerations](#considerations). It is expected that you will need to customize the yaml files to suit your needs, preferences and environment.

Pre requirements:
- You have a Kubernetes cluster or Docker Desktop with Kubernetes enabled. 
- You have basic knowledge about Kubernetes.

> This is a list of [useful commands](#useful-commands) in the end of this description.

This deployment include:

- Two websites one for FoxIDs and one for the FoxIDs Control (Client and API) in two docker images [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) and [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general). 
- The two websites is exposed on two different domains secured with automatically generated [Let's Encrypt](https://letsencrypt.org) certificates.
- MongoDB is a NoSQL database and contains all data including tenants, environments and users. Deployed with the [official MongoDB](https://hub.docker.com/_/mongo) Docker image.
- Redis cache holds sequence (e.g., login and logout sequences) data, data cache to improve performance and handle counters to secure authentication against various attacks. Deployed with the [official Redis](https://hub.docker.com/_/redis) Docker image.
- Logs are written to `stdout` where the logs can be picked up by Kubernetes.

> Optionally use PostgreSql instead of MongoDB and optionally opt out Redis and save the cache in the database (MongoDB or PostgreSql). Without a Redis cache you need to select `None` as data cache.

## Deployment

The deployment is carried out in the described order.

### Get ready
Clone the [git repository](https://github.com/ITfoxtec/FoxIDs) or download as ZIP. The K8s yaml configuration files is in the `./Kubernetes` folder.  
Open a console and navigate to the `./Kubernetes` folder.

### Persistent volumes 
You need persistent volumes for MongoDB and Redis.

If you are using Kubernetes in Docker Desktop you can create persistent volumes on the host file system - not recommended for production.

In a Kubernetes cluster use or create suitable persistent volumes and create two `persistent volume claim` with the names `mongo-data-pvc` for MongoDB and the name `redis-data-pvc` for Redis.

**Kubernetes in Docker Desktop**

Create `persistent volume` for MongoDB
```cmd
kubectl apply -f k8s-mongo-pv-dockerdesktop.yaml
```

Create `persistent volume claim` for MongoDB
```cmd
kubectl apply -f k8s-mongo-pvc-dockerdesktop.yaml
```

Create `persistent volume` for Redis
```cmd
kubectl apply -f k8s-redis-pv-dockerdesktop.yaml
```

Create `persistent volume claim` for Redis
```cmd
kubectl apply -f k8s-redis-pvc-dockerdesktop.yaml
```

**Otherwise**, you might be able to use [dynamic storage provisioning](https://kubernetes.io/docs/concepts/storage/dynamic-provisioning/) with `StorageClass`.

Create `persistent volume claim` for Mongo
```cmd
kubectl apply -f k8s-mongo-pvc-dynamic.yaml
```

Create `persistent volume claim` for Redis
```cmd
kubectl apply -f k8s-redis-pvc-dynamic.yaml
```

### MongoDB
Change the username and password for MongoDB in `k8s-mongo-secret.yaml`. The username and password is base64 encoded.

You base64 encoded "the text" in a command prompt depending on you platform:

Windows
```powershell
powershell "[convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(\"the text\"))"
```

Linux / Mac
```cmd
echo -n "the text" | base64
```

Add the MongoDB secret
```cmd
kubectl apply -f k8s-mongo-secret.yaml
```

Create MongoDB  
*Optionally expose MongoDB on port 27017 by uncomment the `LoadBalancer`*
```cmd
kubectl apply -f k8s-mongo-deployment.yaml
```

Add a `ConfigMap` for the MongoDB service
```cmd
kubectl apply -f k8s-mongo-configmap.yaml
```

### Redis

Create Redis  
```cmd
kubectl apply -f k8s-redis-deployment.yaml
```

Add a `ConfigMap` for the Redis service
```cmd
kubectl apply -f k8s-redis-configmap.yaml
```

### FoxIDs websites
**Domains**  
The two FoxIDs websites is configured to use two domains that you create and manage in your DNS. The `k8s-foxids-deployment.yaml` file is configured with the domains:

- The FoxIDs site domain `https://id.itfoxtec.com` (two places in the file) is change to your domain - `id.my-domain.com`
- The FoxIDs Control site domain `https://control.itfoxtec.com` is change to your domain - `control.my-domain.com`

**Email provider**  
You can optionally configure a global email provider or later configure [email providers](email.md) per environment. FoxIDs supports sending emails with SendGrid and SMTP.

The global email provider is configured in the `k8s-foxids-deployment.yaml` file on the `foxids` container/pod in the `env:` section.  
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
Create the two FoxIDs websites
```cmd
kubectl apply -f k8s-foxids-deployment.yaml
```

The configuration require a Nginx controller. You can optionally change the configuration to use another controller.

Install Ingress-Nginx controller
```cmd
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.0/deploy/static/provider/cloud/deploy.yaml
```
Optionally verify Ingress-Nginx installation 
```cmd
kubectl -n ingress-nginx get pod
```

> DNS records to the two domains need to point to the installation IP address to enable the Let's Encrypt online validation.

Install Cert-manager
```cmd
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.14.0/cert-manager.yaml
```
Optionally verify Cert-manager installation 
```cmd
kubectl get pods --namespace cert-manager
```

> Consider to start with Let's Encrypt in staging to avoid hitting the Let's Encrypt production rate limit (staging certificates is not trusted by the browser).

Add your email in the `k8s-letsencrypt-issuer.yaml` file. Optionally select to use stating or production in the `k8s-letsencrypt-issuer.yaml` and `k8s-foxids-ingress-deployment.yaml` files, default configured for production.

Configure Let's Encrypt
```cmd
kubectl apply -f k8s-letsencrypt-issuer.yaml
```

Optionally verify certificate issuer
```cmd
kubectl describe ClusterIssuer letsencrypt-production
#staging 
# kubectl describe ClusterIssuer letsencrypt-staging
```

The `k8s-foxids-ingress-deployment.yaml` file is configured with the domains:

- The FoxIDs site domain `id.itfoxtec.com` (two places in the file) is change to your domain - `id.my-domain.com`
- The FoxIDs Control site domain `control.itfoxtec.com` is change to your domain - `control.my-domain.com`

Add ingress with the domains and bound to the related certificates
```cmd
kubectl apply -f k8s-foxids-ingress-deployment.yaml
```

Optionally verify the certificate
```cmd
kubectl describe certificate letsencrypt-production
#staging 
# kubectl describe certificate letsencrypt-staging
```

## First login
Open your FoxIDs Control site domain in a browser. It should retired to the FoxIDs site where you login with the default admin user `admin@foxids.com` and password `FirstAccess!` (you are required to change the password on first login).  
You are then redirected back to the FoxIDs Control site in the `master` tenant. You can add more tenants in the master tenant and e.g., configure admin users.

Then click on the `main` tenant and authenticate once again with the same default admin user email and password (the default admin user is the same for both the `master` tenant and the `main` tenant, but it is two different users).  
You are now logged into the `main` tenant and can start to configure your [applications and authentication methods](connections.md).

### Seed data
The database is automatically seeded based on the configured domains. Therefor, you need to delete the database if the domains are changed.  
To delete the data; You can either stop the database pod and delete the physical database folder or files. 
Or expose the database port and open the database in MongoDB Compress ([download MongoDB Compass Download (GUI)](https://www.mongodb.com/try/download/compass)) and delete the database.  
Thereafter, the FoxIDs Control pod needs to be restarted to initiate a new seed process.

Advanced option: The domains can also be changed by hand din the database.

## Considerations
This section lists some deployment and security considerations.

**Kubernetes Service Mesh**  
It is recommended to use a [Kubernetes Service Mesh](https://www.toptal.com/kubernetes/service-mesh-comparison) to achieve a zero-trust architecture. Where the internal traffic is secured with mutual TLS (mTLS) encryption.

**Namespace**  
Consider encapsulating the resources with a namespace. The following commands are used to apply a namespace.

Create namespace
```cmd
kubectl create namespace test
```

List namespaces
```cmd
kubectl get namespaces
```

Apply namespace on pod creation 
```cmd
kubectl apply -f xxx.yaml --namespace=test
```

**Log**  
All logs from FoxIDs including errors, trace and envents is written to `stdout`. Consider how to handle [application logs](https://kubernetes.io/docs/concepts/cluster-administration/logging/) and collect logs from the containers.

**MongoDB Operator**  
Consider MongoDB Operator if you need multiple instances of MongoDB.

1. [MongoDB Community Kubernetes Operator (free)](https://github.com/mongodb/mongodb-kubernetes-operator)
2. [MongoDB Enterprise Kubernetes Operator](https://www.mongodb.com/docs/kubernetes-operator/stable/tutorial/install-k8s-operator/)

**Redis multiple pods / cluster**  
Consider a scaled Redis setup if you need multiple instances of Redis.

- [Redis master/replica setup in Kubernetes](https://medium.com/@bhargavapinky/redis-master-replica-setup-in-kubernetes-e0c35a5eb6aa)
- [Redis on Kubernetes](https://www.groundcover.com/blog/redis-cluster-kubernetes)
- [Redis Enterprise cluster on Kubernetes](https://redis.io/docs/latest/operate/kubernetes/recommendations/sizing-on-kubernetes/) and [architecture](https://redis.io/docs/latest/operate/kubernetes/architecture/)

**Backup**  
Consider if backup of the MongoDB data is required and at which level, here three possible solutions. It is considered less important to backup Redis.

1. Backup the persistent volume physical data store.
2. [Backup with a Kubernetes Cron Job](https://medium.com/@shashwatmahar12/kubernetes-install-mongodb-from-helm-cron-job-to-backup-mongodb-replica-set-5fd8df51fe93).
3. Backup is supported in MongoDB Enterprise Kubernetes Operator.

## Update
FoxIDs is updated by updating each image to a new version, the two FoxIDs images is backwards compatible. First update the [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) image and then the [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general) image.

It should likewise be possible to update the [MongoDB](https://hub.docker.com/_/mongo) image and [Redis](https://hub.docker.com/_/redis) images with data in persistent volumes. 

## Useful commands
This is a list of commands which may be useful during deployment to view details and to make deployment changes.

Create pod
```cmd
kubectl apply -f ks8-xxx.yaml
```

Tear down pod
```cmd
kubectl delete -f ks8-xxx.yaml
```

List pods
```cmd
kubectl get pods
```

Get pod description
```cmd
kubectl describe pod xxx
```

List deployments
```cmd
kubectl get deployments
```

List services
```cmd
kubectl get services
```

List secrets
```cmd
kubectl get secrets
```

List persistent volumes
```cmd
kubectl get pv
```

List persistent volume claims
```cmd
kubectl get pvc
```

List ingress
```cmd
kubectl get ingress
```

Sescribe ingress
```cmd
kubectl describe ingress xxx
```