# Connect PingIdentity / PingOne with SAML 2.0 authentication method

FoxIDs can be connected to PingOne with a [SAML 2.0 authentication method](auth-method-saml-2.0.md). Where PingOne is a SAML 2.0 Identity Provider (IdP) and FoxIDs is acting as an SAML 2.0 Relying Party (RP).

> Take a look at the PingOne sample configuration in FoxIDs Control: [https://control.foxids.com/test-corp](https://control.foxids.com/test-corp)  
> Get read access with the user `reader@foxids.com` and password `TestAccess!` then select the `- (dash is production)` environment and the `Authentication methods` tab.
 
## Configuring PingOne as Identity Provider (IdP)

**1 - Start by creating an SAML 2.0 authentication method in [FoxIDs Control Client](control.md#foxids-control-client)**

 1. Add the name
 2. Then the SAML 2.0 Metadata is created with the authentication method name, copy the metadata URL

 **2 - Then go to PingOne and create the application (Relying Party)**

  1. Add the application name
  2. Choose Application Type: SAML Application
  3. Click Configure
  4. In the SAML configuration page, select Import From URL and import the FoxIDs authentication method metadata URL
  5. Click save
  6. Select the Configuration tab and copy the IDP Metadata URL
  7. Enable the application (sliding button top right corner)


> Currently FoxIDs only support PingOne if either the `Sign Assertion` or `Sign Response` option is selected, the option `Sign Assertion & Response` is not supported. Please see the [issue](https://github.com/ITfoxtec/ITfoxtec.Identity.Saml2/issues/107).

**3 - Then go back to the SAML 2.0 authentication method in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Add the PingOne metadata URL in the Metadata URL field.
2. Click Create

You are done. The SAML 2.0 authentication method can now be used as an authentication method for application registrations in the environment.