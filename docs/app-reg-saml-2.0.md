# SAML 2.0 application registration

FoxIDs SAML 2.0 application registration enable you to connect an SAML 2.0 based application. 

[SAML (Security Assertion Markup Language) 2.0](https://docs.oasis-open.org/security/saml/v2.0/saml-core-2.0-os.pdf) is an XML-based authentication and authorization standard that 
allows secure Single Sign-On (SSO) between an Identity Provider (IdP) and a Service Provider (SP).  
The two SAML 2.0 flows: SP-Initiated Login flow and IdP-initiated Login flow are supported by default.

![FoxIDs SAML 2.0 application registration](images/connections-app-reg-saml.svg)

Your application become a SAML 2.0 Relying Party (RP) and FoxIDs acts as an SAML 2.0 Identity Provider (IdP).

FoxIDs support [SAML 2.0 redirect and post bindings](https://docs.oasis-open.org/security/saml/v2.0/saml-bindings-2.0-os.pdf).

A application registration expose [SAML 2.0 metadata](https://docs.oasis-open.org/security/saml/v2.0/saml-metadata-2.0-os.pdf) where your application can discover the SAML 2.0 Identity Provider (IdP).

Both the login, logout and single logout [SAML 2.0 profiles](https://docs.oasis-open.org/security/saml/v2.0/saml-profiles-2.0-os.pdf) are supported. The Artifact profile is not supported.

> The FoxIDs generated SAML 2.0 metadata only contain logout and single logout information if logout is configured in the SAML 2.0 application registration.

How to guides:

- Connect [Amazon IAM Identity Center](auth-method-howto-saml-amazon-iam-identity-center.md)
- Connect [Context Handler test IdP (Danish identity broker)](howto-saml-2.0-context-handler#configuring-test-identity-provider-for-context-handler)

## Configuration
How to configure your application as an SAML 2.0 Relying Party (RP).

> The FoxIDs SAML 2.0 application registration metadata endpoint is `https://foxids.com/tenant-x/environment-y/application-saml-pr1(*)/saml/idpmetadata`.
> If the application is configured in tenant `tenant-x` and environment `environment-y` with the application registration name `application-saml-pr1`.

A application registration can possibly support login through multiple [authentication methods](connections.md#authentication-method) by adding the authentication method name to the URL.  
An authentication method name e.g. `login` can possible be added to the metadata URL like this `https://foxids.com/tenant-x/environment-y/application-saml-pr1(login)/saml/idpmetadata`.
Or you can use the default `*` notation, which enables login with all authentication methods.

The following screenshot shows the configuration of a FoxIDs SAML 2.0 application registration in [FoxIDs Control Client](control.md#foxids-control-client).  
Here the configuration is created with the applications metadata. The issued claims is limited to the configured set of claims, all claims can be issued with the `*` notation.

> More configuration options become available by clicking **Show advanced**.

![Configure SAML 2.0](images/configure-saml-app-reg.png)

> You can change SAML 2.0 claim collection and do claim tasks with [claim transforms and claim tasks](claim-transform-task.md).
> If you are creating a new claim, add the claim or `*` to the `Issue claims` list to issue the claim to your application.

## Require multi-factor authentication (MFA)
The SAML 2.0 Relying Party (RP) can require multi-factor authentication by specifying the `urn:foxids:mfa` value in the `RequestedAuthnContext.AuthnContextClassRef` property.

You can find sample code in the [AspNetCoreSamlSample](samples.md#aspnetcoresamlsample) samples [SamlController.cs](https://github.com/ITfoxtec/FoxIDs.Samples/blob/main/src/AspNetCoreSamlSample/Controllers/SamlController.cs) file.  
The `AuthnContextClassRef` property can be set in the `Login` method in `SamlController.cs`:

    public IActionResult Login(string returnUrl = null)
    {
        var binding = new Saml2RedirectBinding();
        binding.SetRelayStateQuery(new Dictionary<string, string>
        {
            { relayStateReturnUrl, returnUrl ?? Url.Content("~/") }
        });

        var saml2AuthnRequest = new Saml2AuthnRequest(saml2Config)
        {
            // To require MFA
            RequestedAuthnContext = new RequestedAuthnContext
            {
                Comparison = AuthnContextComparisonTypes.Exact,
                AuthnContextClassRef = new string[] { "urn:foxids:mfa" },
            }
        };

        return binding.Bind(saml2AuthnRequest).ToActionResult();
    }
