# [FoxIDs](https://www.foxids.com)


**FoxIDs is a Identity Services (IDS) with support for [OAuth 2.0](https://www.foxids.com/docs/oauth-2.0), [OpenID Connect 1.0](https://www.foxids.com/docs/oidc) and [SAML 2.0](https://www.foxids.com/docs/saml-2.0).**

> Hosted in Azure Europe / Ownership and data in Europe.

FoxIDs is both an [authentication](https://www.foxids.com/docs/login) platform and a security broker where FoxIDs support converting from [OpenID Connect 1.0](https://www.foxids.com/docs/oidc) to [SAML 2.0](https://www.foxids.com/docs/saml-2.0).

FoxIDs is designed as service with multi-tenant support. Your tenant holds your environments (prod, QA, test, dev or corporate, external-idp, app-a, app-b) and possible [interconnect](https://www.foxids.com/docs/howto-environmentlink-foxids) the environments.  
Each environment is an Identity Provider with a [user repository](https://www.foxids.com/docs/users) and a unique [certificate](https://www.foxids.com/docs/certificates). 
An environment can be connected to external Identity Provider with [OpenID Connect 1.0](https://www.foxids.com/docs/auth-method-oidc) or [SAML 2.0](https://www.foxids.com/docs/auth-method-saml-2.0) authentication methods. 
The environment is configured as the IdP for applications and APIs with [OAuth 2.0](https://www.foxids.com/docs/app-reg-oauth-2.0), [OpenID Connect 1.0](https://www.foxids.com/docs/app-reg-oidc) or [SAML 2.0](https://www.foxids.com/docs/app-reg-saml-2.0) application registrations.  
The user's [log in](https://www.foxids.com/docs/login) experience is configured and optionally [customized](https://www.foxids.com/docs/customization).

FoxIDs consist of two services:

- [FoxIDs](https://www.foxids.com/docs/connections) - identity service, which handles user log in, OAuth 2.0, OpenID Connect 1.0 and SAML 2.0.
- [FoxIDs Control](https://www.foxids.com/docs/control), which is used to configure FoxIDs in a user interface or by calling an API.

Hosting:

- FoxIDs SaaS is available at [FoxIDs.com](https://www.foxids.com/action/createtenant) as an Identity Services (IDS).  
- You can [deploy](https://www.foxids.com/docs/deployment) FoxIDs anywhere using Docker or Kubernetes (K8s).

> For more information please see the [get started](https://www.foxids.com/docs/get-started) guide.

## Support

If you have questions please ask them on [Stack Overflow](https://stackoverflow.com/questions/tagged/foxids). Tag your questions with 'foxids' and I will answer as soon as possible.

Otherwise you can use [support@foxids.com](mailto:support@foxids.com) for topics not suitable for Stack Overflow.
