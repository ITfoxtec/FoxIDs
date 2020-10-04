# SAML 2.0
FoxIDs can both act as a SAML 2.0 [IdP (identity provider)](#idp) and [RP (relying party)](#rp). 

Description of how to connect FoxIDs to:
* [AD FS](#connecting-ad-fs)

## IdP
//TODO

## RP
//TODO

## Connecting AD FS
AD FS can be connected to FoxIDs with SAML 2.0. Either [AD FS is acting as an IdP](#ad-fs-as-idp) (identity provider) and FoxIDs is acting as an RP (relying party) or [AD FS is acting as an RP](#ad-fs-as-rp) and FoxIDs is acting as an IdP.

### AD FS as IdP
Configuring AD FS as IdP in the following steps.

#### AD FS as a SAML 2.0 IdP on FoxIDs
First the AD FS is configured in FoxIDs as an SAML 2.0 IdP up party either through the [[API|Api.md]] or [[Portal|Portal.md]]. The IdP up party can either be configured with the AD FS metadata "https<i>:</i>//*adfs-domain*/federationmetadata/2007-06/federationmetadata.xml" or by configuring the SAML 2.0 details.

##### Recommended bindings
Authn binding
* authn_binding.request_binding: Redirect
* authn_binding.response_binding: Post
Logout binding
* logout_binding.request_binding: Post
* logout_binding.response_binding: Post

#### FoxIDs as a SAML 2.0 RP on the AD FS
After configuring the AD FS SAML 2.0 IdP up party FoxIDs exposes a SAML 2.0 RP metadata (RP is also called SP, service provider), which can be used to configure FoxIDs as a RP on the AD FS.

> FoxIDs SAML 2.0 RP metadata "https<i>:</i>//foxids.com/*tenant-x*/*track-y*/(*adfs-idp-party*)/saml/spmetadata" 
> for 'tenant-x' and 'track-y' with the AD FS IdP up party name 'adfs-idp-party'

Alternatively, FoxIDs can be configured manually as a RP on the AD FS with the following informations:

* The public FoxIDs track ('tenant-x' and 'track-y') certificate
* SHA-256
* The FoxIDs track ('tenant-x' and 'track-y') identifier "https<i>:</i>//foxids.com/*tenant-x*/*track-y*" or another configured identifier
* Assertion consumer service endpoint "https<i>:</i>//foxids.com/*tenant-x*/*track-y*/(*adfs-idp-party*)/saml/Acs"
* Logout endpoint "https<i>:</i>//foxids.com/*tenant-x*/*track-y*/(*adfs-idp-party*)/saml/SingleLogout"

##### AD FS claims
It is recommended to add the NameID (in AD FS called the NameIdentifier "http<i>:</i>//schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" claim) to enable the SessionIndex.
Without the NamID AD FS do not add the SessionIndex to the SAML token and it is not possible to do single logout.

FoxIDs requere AD FS to issue either the NameID or at least one of the folowing claims with the users identity:

* UPN "http<i>:</i>//schemas.xmlsoap.org/ws/2005/05/identity/claims/upn"
* Email "http<i>:</i>//schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"
* Name "http<i>:</i>//schemas.xmlsoap.org/ws/2005/05/identity/claims/name"

### AD FS as RP
//TODO

#### FoxIDs as a SAML 2.0 IdP on AD FS
//TODO

> FoxIDs SAML 2.0 RP metadata [https<i>:</i>//foxids.com/*tenant-x*/*track-y*/*adfs-rp-party*/saml/idpmetadata] 
> for 'tenant-x' and 'track-y' with the AD FS RP party name 'adfs-rp-party'
