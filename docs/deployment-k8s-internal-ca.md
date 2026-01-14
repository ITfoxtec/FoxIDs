<!--
{
    "title":  "Kubernetes internal CA",
    "description":  "Trust an internal PKI/root CA when FoxIDs is deployed in Kubernetes behind a TLS-terminating proxy.",
    "ogTitle":  "Kubernetes internal CA",
    "ogDescription":  "Trust an internal PKI/root CA when FoxIDs is deployed in Kubernetes behind a TLS-terminating proxy.",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "kubernetes, internal ca, root ca, pki, FoxIDs docs"
                       }
}
-->

# Kubernetes internal CA

When FoxIDs is deployed in Kubernetes and outbound traffic is routed through a proxy that terminates TLS and re-issues certificates from an internal root CA, the FoxIDs containers must trust that root CA.

This configuration applies only to outbound HTTPS traffic from the FoxIDs and FoxIDs Control pods, such as calls to external services made through the proxy. It does not affect how inbound TLS traffic is terminated.

The steps below describe the simplest configuration, which does not require any modifications to the container images.

## 1) Create a bundle file (PEM) containing the internal roots
If you already have a combined bundle, use it. Otherwise, concatenate the internal root certificates into one PEM file and create a ConfigMap:

```bash
cat root1.crt root2.crt root3.crt > extra-roots.pem
kubectl -n <foxids-namespace> create configmap foxids-extra-ca --from-file=extra-roots.pem
```

(Use a Secret instead of a ConfigMap if the roots are private.)

## 2) Mount the bundle and set SSL_CERT_FILE in both deployments
Update `Kubernetes/k8s-foxids-deployment.yaml` and add the volume, mount, and environment variable in both deployments.

FoxIDs deployment:

```yaml
spec:
  template:
    spec:
      volumes:
        - name: extra-ca
          configMap:
            name: foxids-extra-ca
            items:
              - key: extra-roots.pem
                path: extra-roots.pem
      containers:
        - name: foxids
          volumeMounts:
            - name: extra-ca
              mountPath: /etc/ssl/certs/extra-roots.pem
              subPath: extra-roots.pem
          env:
            - name: SSL_CERT_FILE
              value: /etc/ssl/certs/extra-roots.pem
```

FoxIDs Control deployment:

```yaml
spec:
  template:
    spec:
      volumes:
        - name: extra-ca
          configMap:
            name: foxids-extra-ca
            items:
              - key: extra-roots.pem
                path: extra-roots.pem
      containers:
        - name: foxids-control
          volumeMounts:
            - name: extra-ca
              mountPath: /etc/ssl/certs/extra-roots.pem
              subPath: extra-roots.pem
          env:
            - name: SSL_CERT_FILE
              value: /etc/ssl/certs/extra-roots.pem
```

## Important notes for .NET / ASP.NET
If the containers need to call public services, a bundle that contains only private roots can break those calls because it replaces the default trust store. 
In that case:

- Create a combined bundle (public + private) and mount it (recommended).
- Or extend the system trust store inside the image instead of using `SSL_CERT_FILE`.
