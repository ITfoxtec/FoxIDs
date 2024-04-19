# Kubernetes

Clone or download ...
Navigate to the /Kubernetes folder

Kubernetes i Docker Desktop
Create persistent volume on host file system - not recommended for production

Mongo
    Create persistent volume 
    kubectl apply -f k8s-mongo-pv-dockerdesktop.yaml
    Create persistent volume claim
    kubectl apply -f k8s-mongo-pvc-dockerdesktop.yaml

Redis
    Create persistent volume 
    kubectl apply -f k8s-redis-pv-dockerdesktop.yaml
    Create persistent volume claim
    kubectl apply -f k8s-redis-pvc-dockerdesktop.yaml

StorageClass
Dynamic storage Provisioning with StorageClass 
https://kubernetes.io/docs/concepts/storage/dynamic-provisioning/
Create persistent volume claim
Mongo
    kubectl apply -f k8s-mongo-pvc-dynamic.yaml
Redis
    kubectl apply -f k8s-redis-pvc-dynamic.yaml


Or another persistent volume and subsequently persistent volume claim with the names mongo-data-pvc and redis-data-pvc

Set username and password
kubectl apply -f k8s-mongo-secret.yaml
Generate base64
  Windows
    powershell "[convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes(\"the text\"))"
  Linux / Mac
    echo -n "the text" | base64

kubectl apply -f k8s-mongo-deployment.yaml
kubectl apply -f k8s-mongo-configmap.yaml


kubectl apply -f k8s-redis-deployment.yaml
kubectl apply -f k8s-redis-configmap.yaml

kubectl apply -f k8s-foxids-deployment.yaml

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


Consider to encapsilating the resources with a namespace
  Create namespace
  kubectl create namespace test

  List namespaces
  kubectl get namespaces

  Applay namespace on creation 
  kubectl apply -f xxx.yaml --namespace=test

Consider MongoDB Operator
  MongoDB Community Kubernetes Operator (free)
  https://github.com/mongodb/mongodb-kubernetes-operator

  MongoDB Enterprise Kubernetes Operator
  https://www.mongodb.com/docs/kubernetes-operator/stable/tutorial/install-k8s-operator/

Consider backup
  Backup the persistent volume physical data store

  Backup with a Kubernetes Cron Job
  https://medium.com/@shashwatmahar12/kubernetes-install-mongodb-from-helm-cron-job-to-backup-mongodb-replica-set-5fd8df51fe93
  
  Backup is supported in MongoDB Enterprise Kubernetes Operator

