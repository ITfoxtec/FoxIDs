# Development

FoxIds.com (SaaS) is configured with the test tenant `test-corp`. The tenant is configured with the [.NET samples](samples.md) and connected to multiple [OpenID Connect](up-party-oidc.md) and [SAML 2.0](up-party-saml-2.0.md) IdPs.

You can take a look at the `test-corp` tenant in Foxids Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
Get read access with the user `reader@foxids.com` and password `TestAccess!`

Online samples:  
  - Open the [OpenID Connect sample](https://aspnetcoreoidcallupsample.itfoxtec.com) where you can log in directly in Foxids or by a connected IdP. The authenticated user's claims are listed after log in and you can call the APIs directly
    or by the use of [token exchange](token-exchange.md).  
    See more information in the [sample docs](samples.md#aspnetcoreoidcauthcodealluppartiessample).
  - If you authenticate with the [IdP SAML 2.0 sample](https://aspnetcoresamlidpsample.itfoxtec.com/) in the [OpenID Connect sample](https://aspnetcoreoidcallupsample.itfoxtec.com) 
    you can find the users session and initiate single logout from the [IdP SAML 2.0 sample](https://aspnetcoresamlidpsample.itfoxtec.com/).  
    See more information in the [sample docs](samples.md#aspnetcoresamlidpsample).
