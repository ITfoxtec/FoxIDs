Docker Desktop
Create persistent volume on host file system - not recommended for production
kubectl apply -f k8s-mongo-pv-dockerdesktop.yaml
Create persistent volume claim
kubectl apply -f k8s-mongo-pvc-dockerdesktop.yaml

StorageClass
Dynamic storage Provisioning with StorageClass 
https://kubernetes.io/docs/concepts/storage/dynamic-provisioning/
Create persistent volume claim
kubectl apply -f k8s-mongo-pvc-dynamic.yaml

Or other persistent volume and subsequently persistent volume claim with the mongo-data-pvc

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