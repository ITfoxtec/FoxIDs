# SAML 2.0 bridge

FoxIDs default bridge between [SAML 2.0](saml-2.0.md) and [OpenID Connect](oidc.md) / [OAuth 2.0](oauth-2.0.md) without any additional configuration. 

If you configure a [SAML 2.0 up-party](up-party-saml-2.0.md) to an external Identity Provider (IdP) and connect your app as a [OpenID Connect down-party](down-party-oidc.md). A log in request from your app is roted as a external SAML 2.0 log in requests. 
The SAML 2.0 log in response is subsequently mapped to a OpenID Connect response for your app.

![Bridge SAML 2.0 to OpenID Connect](images/bridge-saml-oidc.svg)

It is likewise possible bridge the reverse route starting the log in request from a [SAML 2.0 down-party](down-party-saml-2.0.md) app and routing to an external OpenID Provider (OP) configured as [OpenID Connect up-party](up-party-oidc.md).
And likewise the response is mapped to a SAML 2.0 response.

![Bridge OpenID Connect to SAML 2.0](images/bridge-oidc-saml.svg)

FoxIDs support to bridge both log in, logout and single logout between SMAL 2.0 and OpenID Connect.

## One track - one Identity Provider
All bridge functionality can be combined in the same track. Making it possible to enable an OpenID Connect app to log in via both a SAML 2.0 or OpenID Connect up-party at the same time. 
The OpenID Connect app can either select the up-party grammatically or let the user select on a [home realm discovery (HRD)](login.md#home-realm-discovery-hrd) page.

It is recommended to have an application infrastructure with OpenID Connect enabled clients and [OAuth 2.0](down-party-oauth-2.0.md) enable APIs. Where all applications (clients and APIs) trust the same Identity Provider (IdP). One IdP is equal to one track FoxIDs.
By utilized the bridge functionality in FoxIDs SAML 2.0 tokens is mapped to ID tokens and access tokens which can used to authenticate OpenID Connect apps and to call existing APIs.

## Token exchange
If a user is authenticated in a app which trust an external SAML 2.0 Identity Provider (IdP) ...  in possession of ...

 zero trust (never trust, always verify) ...

In a case where a SAML 2.0 enabled application which trust an external SAML 2.0 Identity Provider (IdP) need to call an OAuth 2.0 enable APIs.
The application first needs to obtain an access token for the API, before beeing able to actually call the app

The SAML 2.0 token can be exchanged to an access token for the API. 


## Claim mappings





Saml token exchange på bridge tekst







map claims - FoxIDs use JWT claims inside 
    create JWT til SAML claim mappings

