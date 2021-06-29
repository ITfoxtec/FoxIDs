# Down-party - Connect AD SF with SAML 2.0

FoxIDs can be connected to AD FS with a [down-party SAML 2.0 Relying Party (RP)](down-party-saml-2.0.md). Where AD FS is a SAML 2.0 Relying Party (RP) and FoxIDs is acting as an SAML 2.0 Identity Provider (IdP).

An AD FS can be connected to FoxIDs with SAML 2.0 acting as an RP where FoxIDs is acting as an IdP.

This example requests login through the `login` up-party, which can be changed depending on the scenario.
 
Configuring AD FS as RP using the following steps.

## 1) AD FS as a SAML 2.0 RP on FoxIDs
First the AD FS SAML 2.0 RP is configured in a FoxIDs track as an SAML 2.0 down-party through [FoxIDs Control](control.md). The RP down-party can either be configured by adding the SAML 2.0 details or using the AD FS metadata `https://...adfs-domain.../federationmetadata/2007-06/federationmetadata.xml` *(future support)*.

## 2) FoxIDs as a SAML 2.0 IdP on AD FS
After configuring the AD FS SAML 2.0 down-party in a FoxIDs track a SAML 2.0 IdP metadata is exposed, which can be used to configure FoxIDs as a IdP on AD FS.

> FoxIDs SAML 2.0 IdP metadata `https://foxids.com/tenant-x/track-y/adfs-saml-rp1/saml/idpmetadata`  
> for 'tenant-x' and 'track-y' with the down-party name 'adfs-saml-rp1'

Alternatively, FoxIDs can be configured manually as an IdP on the AD FS with the following properties:

- The public FoxIDs track ('tenant-x' and 'track-y') certificate
- Hash algorithm, default SHA-256
- The FoxIDs track identifier `https://foxids.com/tenant-x/track-y/` or another configured identifier
- Single sign-on service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/authn`
- Single logout service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/logout`

## 3) FoxIDs issuances claims
FoxIDs default issue the user's identity in the NameID claim with format persistent.
