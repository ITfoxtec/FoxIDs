# Kubernetes

Deploy FoxIDs in your Kubernetes (K8s) cluster or Docker Desktop with Kubernetes enabled.  

This is a description of how to make a default deployment and some considerations. It is expected that you will need to customize the yaml files to suit your needs, preferences and environment.

Pre requirements:
- You have a Kubernetes cluster or Docker Desktop with Kubernetes enabled. 
- You have basic knowledge about Kubernetes.

This deployment include:

- Two websites one for FoxIDs and one for the FoxIDs Control (Client and API) in two docker images [foxids/foxids](https://hub.docker.com/repository/docker/foxids/foxids/general) and [foxids/foxids-control](https://hub.docker.com/repository/docker/foxids/foxids-control/general). 
- The two websites is exposed on two different domains secured with automatically generated [Let's Encrypt](https://letsencrypt.org) certificates.
- MongoDB is a NoSQL database and contains all data including tenants, environments and users. Deployed with the [official MongoDB](https://hub.docker.com/_/mongo) Docker image.
- Redis cache holds sequence (e.g., login and logout sequences) data, data cache to improve performance and handle counters to secure authentication against various attacks. Deployed with the [official Redis](https://hub.docker.com/_/redis) Docker image.
- Logs are written to `stdout` where the logs can be picked up by Kubernetes.

## Deployment

The deployment is carried out in the described order.

### Get ready
Clone the [git repository](https://github.com/ITfoxtec/FoxIDs) or download as ZIP. The K8s yaml configuration files is in `./Kubernetes` folder.  
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
You can optionally configure a global email provider or later configure [email providers](email) per environment. FoxIDs supports sending emails with SendGrid and SMTP.

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




- 
- 
//TODO


HTTP / HTTPS
Apply Ingress, the configuration require a Nginx controller. You can optionally change the configuration to use another controller.

Install Ingress-Nginx controller
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.10.0/deploy/static/provider/cloud/deploy.yaml
    Verify installation 
    kubectl -n ingress-nginx get pod

DNS recourts need to point to the two domains to enable the Let's Encrypt online validation.

Install Cert-manager
kubectl apply -f https://github.com/cert-manager/cert-manager/releases/download/v1.14.0/cert-manager.yaml
    Verify installation 
    kubectl get pods --namespace cert-manager

Consider to start with Let's Encrypt in staging to not hit the Let's Encrypt production rate limit.

Add your email.

Configure k8s-letsencrypt-issuer.yaml, with your email and to use stating or production
kubectl apply -f k8s-letsencrypt-issuer.yaml
    Verify certificate issuer
    kubectl describe ClusterIssuer letsencrypt-staging
    kubectl describe ClusterIssuer letsencrypt-production

kubectl apply -f k8s-foxids-ingress-deployment.yaml

Verify certificate
    kubectl describe certificate letsencrypt-staging
    kubectl describe certificate letsencrypt-production

### Seed data
The database is automatically seeded based on the configured domains. Therefor, you need to delete the database if the domains are changed.  
To delete the data; You can either stop the database pod and delete the physical database folder or files. 
Or expose the database port and open the database in MongoDB Compress ([download MongoDB Compass Download (GUI)](https://www.mongodb.com/try/download/compass)) and delete the database.  
Thereafter, the FoxIDs Control pod needs to be restarted to initiate a new seed process.

Advanced option: The domains can also be changed by hand din the database.

### Considerations
This section lists some deployment considerations.

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

**Kubernetes Service Mesh**  
Consider [Kubernetes Service Mesh](https://www.toptal.com/kubernetes/service-mesh-comparison) to achieve a zero-trust architecture. Wher the internal trafic is securet with mutual TLS (mTLS) encryption.

**Log**  
Consider how to handle logs and collect logs from the containers written to `stdout`.





- 
- 
//TODO

[Logging Architecture](https://kubernetes.io/docs/concepts/cluster-administration/logging/)


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

### Useful commands
This is a list of commands which may be useful during deployment to view details and to make deployment changes.

Create pod
```cmd
kubectl apply -f pod-xxx.yaml
```

Tear down pod
```cmd
kubectl delete -f pod-xxx.yaml
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
