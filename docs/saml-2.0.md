# SAML 2.0
FoxIDs can both act as a SAML 2.0 [IdP](#idp) and [RP](#rp). 

> The FoxIDs SAML 2.0 metadata only include logout and single logout information if logout is configured in the SAML 2.0 up-party or down-party.

How to:
- Connect [AD FS as RP](#connect-ad-fs-as-rp) to FoxIDs
- Connect [AD FS as IdP](#connect-ad-fs-as-idp) to FoxIDs

## Claim mappings
Claim mapping between SAML claim types and JWT claim types can be configured in the setting menu in [FoxIDs Control](control.md).

![Configure JWT and SAML mappings](images/configure-jwt-saml-mappings.png)

## IdP 
(identity provider)
//TODO

## RP 
(relying party also called SP, service provider)
//TODO

## Connect AD FS as RP
An AD FS can be connected to FoxIDs with SAML 2.0 acting as an RP where FoxIDs is acting as an IdP.

This example requests login through the `login` up-party, which can be changed depending on the scenario.
 
Configuring AD FS as RP using the following steps.

### 1) AD FS as a SAML 2.0 RP on FoxIDs
First the AD FS SAML 2.0 RP is configured in a FoxIDs track as an SAML 2.0 down-party through [FoxIDs Control](control.md). The RP down-party can either be configured by adding the SAML 2.0 details or using the AD FS metadata `https://...adfs-domain.../federationmetadata/2007-06/federationmetadata.xml` *(future support)*.

### 2) FoxIDs as a SAML 2.0 IdP on AD FS
After configuring the AD FS SAML 2.0 down-party in a FoxIDs track a SAML 2.0 IdP metadata is exposed, which can be used to configure FoxIDs as a IdP on AD FS.

> FoxIDs SAML 2.0 IdP metadata `https://foxids.com/tenant-x/track-y/adfs-saml-rp1/saml/idpmetadata`  
> for 'tenant-x' and 'track-y' with the down-party name 'adfs-saml-rp1'

Alternatively, FoxIDs can be configured manually as an IdP on the AD FS with the following properties:

- The public FoxIDs track ('tenant-x' and 'track-y') certificate
- Hash algorithm, default SHA-256
- The FoxIDs track identifier `https://foxids.com/tenant-x/track-y/` or another configured identifier
- Single sign-on service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/authn`
- Single logout service endpoint `https://foxids.com/tenant-x/track-y/adfs-saml-rp1(login)/saml/logout`

### 3) FoxIDs issuances claims
FoxIDs default issue the user's identity in the NameID claim with format persistent.

## Connect AD FS as IdP
An AD FS can be connected to FoxIDs with SAML 2.0 acting as an IdP where FoxIDs is acting as an RP.
 
Configuring AD FS as IdP using the following steps.

### 1) AD FS as a SAML 2.0 IdP on FoxIDs
First the AD FS SAML 2.0 IdP is configured in a FoxIDs track as an SAML 2.0 up-party through [FoxIDs Control](control.md). The IdP up-party can either be configured by adding the SAML 2.0 details or using the AD FS metadata `https://...adfs-domain.../federationmetadata/2007-06/federationmetadata.xml` *(future support)*.

**Recommended SAML 2.0 bindings**
- Authn request and response binding: Post
- Logout request and response binding: Post

Remark; The authn request redirect binding can result in a long query string which can cause problems I some devices.

![Configure SAML 2.0 AD FS up-party](images/configure-saml-adfs-up-party.png)

### 2) FoxIDs as a SAML 2.0 RP on AD FS
After the AD FS SAML 2.0 up-party has been configured in a FoxIDs track a SAML 2.0 RP metadata is exposed, which can be used to configure FoxIDs as an RP on AD FS.

> FoxIDs SAML 2.0 RP metadata `https://foxids.com/tenant-x/track-y/(adfs-saml-idp1)/saml/spmetadata`  
> for 'tenant-x' and 'track-y' with the up-party name 'adfs-saml-idp1'

Alternatively, FoxIDs can be configured manually as an RP on the AD FS with the following properties:

- The public FoxIDs track ('tenant-x' and 'track-y') certificate
- Hash algorithm, default SHA-256
- The FoxIDs track identifier `https://foxids.com/tenant-x/track-y/` or another configured identifier
- Assertion consumer service endpoint `https://foxids.com/tenant-x/track-y/(adfs-saml-idp1)/saml/acs`
- Single logout (logout) service endpoint `https://foxids.com/tenant-x/track-y/(adfs-saml-idp1)/saml/singlelogout/`

### 3) AD FS issuances claims
It is recommended to add the NameID claim `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier` (in AD FS called the NameIdentifier) to enable the SessionIndex. Without the NamID claim AD FS do not add the SessionIndex to the SAML token and it will therefore not be possible to do logout or single logout.

FoxIDs require AD FS to issue the users identity in either the NameID or at least one of the following claims:

- NameID `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier`
- UPN `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/upn`
- Email `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress`
- Name `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`

