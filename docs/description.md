<!--
{
    "title":  "Description",
    "description":  "FoxIDs is an Identity Service (IDS) that automatically handles OAuth 2.0, OpenID Connect 1.0, and SAML 2.0 so you can deliver secure sign-in flows without running the underlying identity infrastructure yourself.",
    "ogTitle":  "Description",
    "ogDescription":  "FoxIDs is an Identity Service (IDS) that automatically handles OAuth 2.0, OpenID Connect 1.0, and SAML 2.0 so you can deliver secure sign-in flows without running the underlying identity infrastructure yourself.",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "description, FoxIDs docs"
                       }
}
-->

# Description
FoxIDs is an Identity Service (IDS) that automatically handles [OAuth 2.0](oauth-2.0.md), [OpenID Connect 1.0](oidc.md), and [SAML 2.0](saml-2.0.md) so you can deliver secure sign-in flows without running the underlying identity infrastructure yourself.

> Hosted in Europe - Ownership and data remain in Europe.

## Platform overview
- **Unified identity hub**: Use FoxIDs as both an [authentication](login.md) platform and a federation broker. Bridge protocols by [converting](bridge.md) between OpenID Connect 1.0 and SAML 2.0 when needed.
- **Multi-tenant design**: Each tenant can host multiple environments (for example prod, QA, test, dev or corporate, external-idp, app-a, app-b) and optionally [interconnect](howto-environmentlink-foxids.md) them.
- **Per-environment security**: Every environment is its own Identity Provider with a dedicated [user repository](users.md) and [certificate](certificates.md). Connect to external IdPs using [OpenID Connect 1.0](auth-method-oidc.md) or [SAML 2.0](auth-method-saml-2.0.md), and register applications with [OAuth 2.0](app-reg-oauth-2.0.md), [OpenID Connect 1.0](app-reg-oidc.md), or [SAML 2.0](app-reg-saml-2.0.md).
- **Customisable experiences**: Tailor the user [login](login.md) journey and optionally [customise](customisation.md) branding, texts, and behaviour per environment.

> Explore the FoxIDs test configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Sign in with `reader@foxids.com` and password `gEh#V6kSw` for read-only access.

## Services
- [FoxIDs](connections.md): The runtime identity service that manages user authentication and the OAuth 2.0, OpenID Connect 1.0, and SAML 2.0 protocol flows.
- [FoxIDs Control](control.md): The administration surface available as a UI and API for configuring tenants, environments, connections, and applications.

## Hosting options
- **FoxIDs Cloud (SaaS)**: Consume FoxIDs as a managed Identity Service at [FoxIDs Cloud](https://www.foxids.com/action/createtenant).
- **Self-hosted**: [Deploy](deployment.md) FoxIDs yourself on IIS, Docker or Kubernetes (K8s) when you need full control over the hosting environment.

> New to FoxIDs? Start with the [get started](get-started.md) guide.

## Source code availability
The FoxIDs source code lives on [GitHub](https://github.com/ITfoxtec/FoxIDs). The [license](https://github.com/ITfoxtec/FoxIDs/blob/main/LICENSE) lets you install and use FoxIDs for non-production scenarios, and grants small companies, personal projects, and non-profit educational institutions the right to run FoxIDs in production.

## Selection by URL
FoxIDs separates tenants, environments, and [connections](connections.md) with a consistent URL structure.

- Base host example: `https://foxidsxxxx.com/`
- Tenant segment: `https://foxidsxxxx.com/tenant-x/`
- Environment segment: `https://foxidsxxxx.com/tenant-x/environment-y/`
- Application registration: `https://foxidsxxxx.com/tenant-x/environment-y/application-z/`
- Authentication method: `https://foxidsxxxx.com/tenant-x/environment-y/(auth-method-s)/`

When FoxIDs handles a login sequence that results in a session cookie, the cookie stays scoped to the specific URL.

During OpenID Connect or SAML 2.0 flows, clients choose the authentication method by appending the method name in round brackets after the application registration name:  
`https://foxidsxxxx.com/tenant-x/environment-y/application-z(auth-method-s)/`

Selecting multiple authentication methods:

- **Default**: Allow every permitted authentication method with a star `*`:  
  `https://foxidsxxxx.com/tenant-x/environment-y/application-z(*)/`
- **List**: Pick up to four methods separated by commas:  
  `https://foxidsxxxx.com/tenant-x/environment-y/application-z(auth-method-s1,auth-method-s2,auth-method-s3,auth-method-s4)/`
- **Profiles**: Address a predefined authentication profile using `+`:  
  `https://foxidsxxxx.com/tenant-x/environment-y/application-z(auth-method-s+profile-u)/`

> Configure the permitted authentication methods inside each application registration.

A client using the client credentials grant does not have to specify the authentication method. The same applies when requesting an OpenID Connect discovery document or a SAML 2.0 metadata endpoint.

