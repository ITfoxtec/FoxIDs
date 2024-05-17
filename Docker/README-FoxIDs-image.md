# What is FoxIDs?
[FoxIDs](https://www.foxids.com) is a Identity Services that easily allows you to implement Identity Management on your websites against a variety of industry standards (OAuth 2.0, OpenID Connect and SAML 2.0) 
and services like Microsoft, Google and Facebook, etc.

FoxIDs holds your environments (prod, QA, test, dev or corporate, external-idp, app-a, app-b) and possible [interconnect](https://www.foxids.com/docs/howto-environmentlink-foxids) the environments. 
Each environment is an Identity Provider with a [user repository](https://www.foxids.com/docs/users) and a unique [certificate](https://www.foxids.com/docs/certificates). 
An environment is connected to external Identity Provider with [OpenID Connect 1.0](https://www.foxids.com/docs/auth-method-oidc) or [SAML 2.0](https://www.foxids.com/docs/auth-method-saml-2.0). 
The environment is configured as IdP for applications and APIs with [OAuth 2.0](https://www.foxids.com/docs/app-reg-oauth-2.0), [OpenID Connect 1.0](https://www.foxids.com/docs/app-reg-oidc) or 
[SAML 2.0](https://www.foxids.com/docs/app-reg-saml-2.0).  
The user's [login](https://www.foxids.com/docs/login) experience is configured and optionally [customized](https://www.foxids.com/docs/customization).

FoxIDs consist of two services:

- FoxIDs - identity service (this image). The service handles user login, OAuth 2.0, OpenID Connect 1.0 and SAML 2.0.
- [FoxIDs Control](https://www.foxids.com/docs/control) (the [FoxIDs Control image](https://hub.docker.com/r/foxids/foxids-control)), which is used to configure FoxIDs in a user interface or by calling an API.

# Security
By default FoxIDs is configuration with pods in Kubernetes. It is recommended to use a [Kubernetes Service Mesh](https://www.toptal.com/kubernetes/service-mesh-comparison) to achieve a zero-trust architecture. 
Where the internal traffic is secured with mutual TLS (mTLS) encryption.

Using Docker Compose all components is configuration to use a single network.

# How to use this image

FoxIDs can be deployed in a Kubernetes (K8s) cluster or in Docker.

Please see the [deployment](https://www.foxids.com/docs/deployment) documentation for [Kubernetes](https://www.foxids.com/docs/deployment-k8s) or [Docker](https://www.foxids.com/docs/deployment-docker).
