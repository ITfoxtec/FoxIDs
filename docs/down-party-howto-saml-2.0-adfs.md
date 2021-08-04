# Down-party - Connect AD SF with SAML 2.0

FoxIDs can be connected to AD FS with a [down-party SAML 2.0](down-party-saml-2.0.md). Where AD FS is a SAML 2.0 Relying Party (RP) and FoxIDs is acting as an SAML 2.0 Identity Provider (IdP).

This example do login through the up-party `login`, which can be changed depending on the scenario.
 
## Configuring AD FS as Relying Party (RP)

**1 - Start by creating an down-party SAML 2.0 in [FoxIDs Control Client](control.md#foxids-control-client)**

The down-party SAML 2.0 can either be configured by manually adding the SAML 2.0 details or using the AD FS metadata `https://...adfs-domain.../federationmetadata/2007-06/federationmetadata.xml` *(future support)*.

**2 - Then go to the AD FS and create the Identity Provider (IdP)**

> An Identity Provider (IdP) is called a Claims Provider in AD FS.

In this part of the configuration you need to use the down-party SAML 2.0 metadata. It is possible to call a fictive down-party SAML 2.0 metadata in FoxIDs and thereby if preferred performing step 2 as the first step.

> FoxIDs down-party SAML 2.0 metadata `https://foxids.com/tenant-x/track-y/adfs-saml-rp1/saml/idpmetadata`  
> for 'tenant-x' and 'track-y' with the down-party name 'adfs-saml-rp1'.

> A down-party application can possibly support login through multiple [up-parties](parties.md#up-party) by adding the up-party name to the URL.  
> An up-party name e.g. `login` can possible be added to the metadata URL like this `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/idpmetadata`

Configure the Identity Provider (IdP) on AD FS using the down-party SAML 2.0 metadata.

Alternatively, the Identity Provider (IdP) can be configured manually on the AD FS with the following properties:

- The public FoxIDs track ('tenant-x' and 'track-y') certificate
- Hash algorithm, default SHA-256
- The FoxIDs track identifier `https://foxids.com/tenant-x/track-y/` or another identifier if configured
- Single sign-on service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/authn`
- Single logout service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/logout`

> An up-party name e.g. `login` can possible be added to the single sign-on and single logout service endpoint.

**3 - Then go to the AD FS Identity Provider (IdP) issuances claims configuration**

FoxIDs default issue the user's identity in the NameID claim with format persistent.

Other claims can optional be transformed and issued by FoxIDs. 
