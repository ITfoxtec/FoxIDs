# FAQ

##### Way am I unable to login for a moment when I change the certificate container types to 'Key Vault renewed self-signed certificates'?
The first certificate have to be generated by Key Vault before the track can perform logins again. Thereafter the certificate is renewed without seamlessly.

##### I am unable to logout of a client using OIDC if I login and theafter changed the certificate container type.
The problem occurs if the OIDC logout require an ID Token before accepting logout. In this case the ID Token is invalid because the container type and there by the signing certificate have changed.
The problem will occur in the FoxIDs Controle Client. You need to close the browser and start over.

