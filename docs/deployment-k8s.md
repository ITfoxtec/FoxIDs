Docker Desktop
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

kubectl apply -f k8s-foxids-deployment.yml


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


HTTPS