# Kubernetes

Deploy FoxIDs in your Kubernetes (K8s) cluster or Docker Desktop with Kubernetes enabled.

This is a description of how to make a default [deployment](#deployment) and [log in for the first time](#first-login) as well as some [considerations](#considerations). It is expected that you will need to customize the yaml files to suit your needs, preferences and environment.

A FoxIDs installation is like a bucket, there is no external dependencies and it's easy to archive a very high uptime with little effort. 
FoxIDs are updated by updating the two docker images [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) and [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general)
to a new version. New FoxIDs releases is backwards compatible, please consult the [release notes](https://github.com/ITfoxtec/FoxIDs/releases) before updating.

Pre requirements:
- You have a Kubernetes cluster or Docker Desktop with Kubernetes enabled. 
- You have basic knowledge about Kubernetes.
- You have `kubectl` installer.

> This is a list of [useful commands](#useful-commands) in the end of this description.

This deployment include:

- Two websites one for FoxIDs and one for the FoxIDs Control (the admin Client and API) in two docker images [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) and [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general). 
- The two websites is exposed on two different domains secured with automatically generated [Let's Encrypt](https://letsencrypt.org) certificates.
- MongoDB is a NoSQL database and contains all data including tenants, environments and users. Deployed with the [official MongoDB](https://hub.docker.com/_/mongo) Docker image. You can optionally use your own PostgreSQL instance instead of MongoDB. 
- Default holds the cache in the database. Optionally use a Redis cache if you are installing a FoxIDs cluster with high throughput. The cache holds sequences (e.g., login and logout) and handle counters to secure authentication against various attacks and data cache (Redis only) to improve performance. Redis is deployed with the [official Redis](https://hub.docker.com/_/redis) Docker image.
- Logs are written to `stdout` where the logs can be picked up by Kubernetes.

## Deployment

The deployment is carried out in the described order.

### Get ready
Clone the [git repository](https://github.com/ITfoxtec/FoxIDs) or download as ZIP. The K8s yaml configuration files is in the `./Kubernetes` folder.  
Open a console and navigate to the `./Kubernetes` folder.

### Persistent volumes 
You need persistent volumes for MongoDB and optionally Redis.

In a Kubernetes cluster use or create suitable persistent volumes and create a `persistent volume claim` with the name `mongo-data-pvc` for MongoDB and optionally one for Redis with the name `redis-data-pvc`.

You might be able to use [dynamic storage provisioning](https://kubernetes.io/docs/concepts/storage/dynamic-provisioning/) with `StorageClass`.

Create `persistent volume claim` for Mongo
```cmd
kubectl apply -f k8s-mongo-pvc-dynamic.yaml
```

Optionally create `persistent volume claim` for Redis
```cmd
kubectl apply -f k8s-redis-pvc-dynamic.yaml
```

**Kubernetes in Docker Desktop**

If you are using Kubernetes in Docker Desktop you can create persistent volumes on the host file system - not recommended for production.

Create `persistent volume` for MongoDB
```cmd
kubectl apply -f k8s-mongo-pv-dockerdesktop.yaml
```

Create `persistent volume claim` for MongoDB
```cmd
kubectl apply -f k8s-mongo-pvc-dockerdesktop.yaml
```

Optionally create `persistent volume` for Redis
```cmd
kubectl apply -f k8s-redis-pv-dockerdesktop.yaml
```

Optionally create `persistent volume claim` for Redis
```cmd
kubectl apply -f k8s-redis-pvc-dockerdesktop.yaml
```

### Namespace
This guide generally uses the namespace `foxids`, consider changing the namespace to suit your kubernetes environment.

Create namespace
```cmd
kubectl create namespace foxids
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
kubectl apply -f k8s-mongo-secret.yaml -n foxids
```

Create MongoDB  
*Optionally expose MongoDB on port 27017 by uncomment the `LoadBalancer`*
```cmd
kubectl apply -f k8s-mongo-deployment.yaml -n foxids
```

Add a `ConfigMap` for the MongoDB service
```cmd
kubectl apply -f k8s-mongo-configmap.yaml -n foxids
```

### Optionally use PostgreSQL instead of MongoDB
Change the username value in `postgres-username` and password value in `postgres-password` to match you PostgreSQL instance in `k8s-postgres-secret.yaml`. The username and password is base64 encoded.

Add the PostgreSQL secret
```cmd
kubectl apply -f k8s-postgres-secret.yaml -n foxids
```
Change the PostgreSQL database endpoint in `postgres-db` to match you PostgreSQL instance in `k8s-postgres-configmap.yaml`

Add a `ConfigMap` for the PostgreSQL service
```cmd
kubectl apply -f k8s-postgres-configmap.yaml -n foxids
```

### Optionally use Redis

Optionally create Redis  
```cmd
kubectl apply -f k8s-redis-deployment.yaml -n foxids
```

Optionally add a `ConfigMap` for the Redis service
```cmd
kubectl apply -f k8s-redis-configmap.yaml -n foxids
```

### FoxIDs websites
**Domains**  
The two FoxIDs websites is configured to use two domains that you create and manage in your DNS. Configure the `k8s-foxids-deployment.yaml` file with your selected domains:

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
    value: "my@email-address.com"
- name: "Settings__Smtp__Password"
    value: "xxxxxxx"
```

**Important if you are using PostgreSQL**  
Change the database and cache configuration in `k8s-foxids-deployment.yaml` (two places in the file).

Select PostgreSQL as database instead of MongoDb
```yaml
- name: "Settings__Options__DataStorage"
   # value: "MongoDb"
   value: "PostgreSql"  # PostgreSql database
```

Select PostgreSQL as cache instead of MongoDb unless you are using Redis
```yaml
- name: "Settings__Options__Cache"
   # value: "MongoDb"
   value: "PostgreSql"  # Cache in PostgreSql database
   # value: "Redis"  # Cache in Redis 
```

Uncomment the PostgreSQL access configuration
```yaml
- name: POSTGRES_USERNAME
    valueFrom:
    secretKeyRef:
        name: postgres-secret
        key: postgres-username
- name: POSTGRES_PASSWORD
    valueFrom: 
    secretKeyRef:
        name: postgres-secret
        key: postgres-password
- name: POSTGRES_SERVER
    valueFrom: 
    configMapKeyRef:
        name: postgres-configmap
        key: database_url
- name: "Settings__PostgreSql__ConnectionString"
    value: "Host=$(POSTGRES_SERVER);Username=$(POSTGRES_USERNAME);Password=$(POSTGRES_PASSWORD);Database=FoxIDs"
```

**Important if you are using Redis**  
Change the cache configuration in `k8s-foxids-deployment.yaml` (two places in the file).

Select Redis as cache instead of MongoDb
```yaml
- name: "Settings__Options__Cache"
   # value: "MongoDb"
   # value: "PostgreSql"  # Cache in PostgreSql database
   value: "Redis"  # Cache in Redis
```

Uncomment the Redis access configuration
```yaml
- name: REDIS_SERVER
    valueFrom: 
    configMapKeyRef:
        name: redis-configmap
        key: database_url
- name: "Settings__RedisCache__ConnectionString"
    value: "$(REDIS_SERVER):6379"
```

**Deploy**  
Create the two FoxIDs websites
```cmd
kubectl apply -f k8s-foxids-deployment.yaml -n foxids
```

The configuration require a Nginx controller. You can optionally change the configuration to use another controller.

Pre requirements:
- You have [Helm](https://docs.helm.sh/) installer.  
  *Install Helm on windows with this CMD command `winget install Helm.Helm`*

Install Ingress-Nginx controller with two commands
```cmd
helm repo add ingress-nginx https://kubernetes.github.io/ingress-nginx --force-update
helm -n ingress-nginx install ingress-nginx ingress-nginx/ingress-nginx --create-namespace
```
Optionally verify Ingress-Nginx installation 
```cmd
kubectl get pod -n ingress-nginx
```
If you try again in a few minutes you should get an EXTERNAL-IP
```cmd
kubectl get svc -n ingress-nginx ingress-nginx-controller
```

> DNS records to the two domains need to point to the installations IP address to enable the Let's Encrypt online validation.  
> The firewall needs to accept requests on port 80 and 443. Let's encrypt validates the domain ownership on port 80.

Optionally scale the Ingress-Nginx controller 
```cmd
kubectl scale deployment ingress-nginx-controller -n ingress-nginx --replicas=2
```

Install Cert-manager with two commands
```cmd
helm repo add jetstack https://charts.jetstack.io --force-update
helm install cert-manager jetstack/cert-manager --namespace cert-manager --create-namespace --set crds.enabled=true
```
Optionally verify Cert-manager installation 
```cmd
kubectl get pods -n cert-manager
```

Add your email in the `k8s-letsencrypt-issuer.yaml` (two places) file.

Configure Let's Encrypt
```cmd
kubectl apply -f k8s-letsencrypt-issuer.yaml -n foxids
```

The `k8s-foxids-ingress-deployment.yaml` file is configured with the domains:

- The FoxIDs site domain `id.itfoxtec.com` (two places in the file) is change to your domain - `id.my-domain.com`
- The FoxIDs Control site domain `control.itfoxtec.com` (two places in the file) is change to your domain - `control.my-domain.com`

> Consider to start with Let's Encrypt in staging to avoid hitting the Let's Encrypt production rate limit (staging certificates is not trusted by the browser).  
> Optionally select to use stating or production in the `k8s-foxids-ingress-deployment.yaml` file, default configured for production.

Add ingress with certificate bound domains
```cmd
kubectl apply -f k8s-foxids-ingress-deployment.yaml -n foxids
```

> Impotent! Ingress is installed with the annotations `nginx.ingress.kubernetes.io/proxy-buffers-number: "4"` and `nginx.ingress.kubernetes.io/proxy-buffer-size: "32k"` 
   to support SAML 2.0 where HTTP responses can be quite large.

Optionally verify Ingress
```cmd
kubectl get ingress -n foxids
```

Optionally verify certificate issuer
```cmd
kubectl describe ClusterIssuer letsencrypt-production -n foxids
#staging 
# kubectl describe ClusterIssuer letsencrypt-staging -n foxids
```

Optionally check if the certificate is ready (READY should be True)
```cmd
kubectl get certificate -n foxids
```

And optionally verify the certificate
```cmd
kubectl describe certificate letsencrypt-production -n foxids
#staging 
# kubectl describe certificate letsencrypt-staging -n foxids
```

## First login
Open your FoxIDs Control site domain in a browser. It should redirect to the FoxIDs site where you login with the default admin user `admin@foxids.com` and password `FirstAccess!` (you are required to change the password on first login).  
You are then redirected back to the FoxIDs Control site in the `master` tenant. You can add more tenants in the master tenant and e.g., configure admin users.

Then click on the `main` tenant and authenticate once again with the same default admin user email and password (the default admin user email and password is the same for both the `master` tenant and the `main` tenant, but it is two different users).  
You are now logged into the `main` tenant and can start to configure your [applications and authentication methods](connections.md).

### Seed data
The database is automatically seeded based on the configured domains. Therefor, you need to delete the database if the domains are changed.  
To delete the data; You can either stop the database pod and delete the physical database folder or files. 
Or expose the database endpoint and open the database in MongoDB Compress ([download MongoDB Compass Download (GUI)](https://www.mongodb.com/try/download/compass)) and delete the database.  
Thereafter, the FoxIDs Control pod needs to be restarted to initiate a new seed process.

Advanced option: The domains can also be changed by hand din the database.

## Considerations
This section lists some deployment and security considerations.

**Kubernetes Service Mesh**  
It is recommended to use a [Kubernetes Service Mesh](https://www.toptal.com/kubernetes/service-mesh-comparison) to achieve a zero-trust architecture. Where the internal traffic is secured with mutual TLS (mTLS) and encryption.

**Namespace**  
This guide generally uses the namespace `foxids`, consider changing the namespace to suit your kubernetes environment.

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
All logs from FoxIDs including errors, trace and events is written to `stdout`. Consider how to handle [application logs](https://kubernetes.io/docs/concepts/cluster-administration/logging/) and collect logs from the containers.

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
Consider whether MongoDB data needs to be backed up and at what level, here are three possible solutions. It is considered less important to backup Redis.

1. Backup the persistent volume physical data store.
2. [Backup with a Kubernetes Cron Job](https://medium.com/@shashwatmahar12/kubernetes-install-mongodb-from-helm-cron-job-to-backup-mongodb-replica-set-5fd8df51fe93).
3. Backup is supported in MongoDB Enterprise Kubernetes Operator.

## Update
FoxIDs is updated by updating each FoxIDs image to a new version, the two FoxIDs images is backwards compatible. First update the [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) image and then the [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general) image.

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

Get pod logs
```cmd
kubectl logs xxx
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