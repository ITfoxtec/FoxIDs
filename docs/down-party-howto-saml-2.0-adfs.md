# Connect AD FS with SAML 2.0 application registration

Foxids can be connected to AD FS with a [SAML 2.0 application registration](app-reg-saml-2.0.md). Where AD FS is a SAML 2.0 Relying Party (RP) and Foxids is acting as an SAML 2.0 Identity Provider (IdP).

This example do login through the authentication method `login`, which can be changed depending on the scenario.
 
## Configuring AD FS as Relying Party (RP)

**1 - Start by creating an SAML 2.0 application registration in [Foxids Control Client](control.md#foxids-control-client)**

The SAML 2.0 application registration can either be configured by manually adding the SAML 2.0 details or using the AD FS metadata `https://...adfs-domain.../federationmetadata/2007-06/federationmetadata.xml` *(future support)*.

**2 - Then go to the AD FS and create the Identity Provider (IdP)**

> An Identity Provider (IdP) is called a Claims Provider in AD FS.

In this part of the configuration you need to use the SAML 2.0 application registration metadata. It is possible to call a fictive SAML 2.0 application registration metadata in Foxids and thereby if preferred performing step 2 as the first step.

> Foxids SAML 2.0 application registration metadata `https://foxids.com/tenant-x/track-y/adfs-saml-rp1/saml/idpmetadata`  
> for 'tenant-x' and 'track-y' with the application registration name 'adfs-saml-rp1'.

> A application registration can possibly support login through multiple [authentication methods](parties.md#up-party) by adding the authentication method name to the URL.  
> An authentication method name e.g. `login` can possible be added to the metadata URL like this `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/idpmetadata`

Configure the Identity Provider (IdP) on AD FS using the SAML 2.0 application registration metadata.

Alternatively, the Identity Provider (IdP) can be configured manually on the AD FS with the following properties:

- The public Foxids track ('tenant-x' and 'track-y') certificate
- Hash algorithm, default SHA-256
- The Foxids track identifier `https://foxids.com/tenant-x/track-y/` or another identifier if configured
- Single sign-on service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/authn`
- Single logout service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/logout`

> An authentication method name e.g. `login` can possible be added to the single sign-on and single logout service endpoint.

**3 - Then go to the AD FS Identity Provider (IdP) issuances claims configuration**

Foxids default issue the user's identity in the NameID claim with format persistent.

Other claims can optional be transformed and issued by Foxids. 
