# Certificates

When a environment is created it is default equipped with a self-signed certificate stored in Cosmos DB, called a contained certificate. The certificate can afterword's be updated / changed and likewise the certificate container type can be changed.

There are tree different certificate container types:

**Contained certificates (default)**
- Certificates is stored in Cosmos DB including private key.
- Self-signed certificates is created by FoxIDs or you can upload your one certificates.
- Support primary and secondary certificates, and certificate swap.
- Not automatically renewed.
- No cost per signing.

**Key Vault, renewed self-signed certificates**
- Certificates is stored in Key Vault and the private key is not exportable.
- Self-signed certificates is created by Key Vault.
- Automatically renewed with 3 month validity period. Renewed 10 days before expiration and exposed as the secondary certificate. Promoted to be the primary certificate 5 days before expiration.
- Key Vault cost per signing.

**Key Vault, upload your one certificate *(future support)***
- Certificates is stored in Key Vault and the private key is not exportable.
- Not automatically renewed.
- Key Vault cost per signing.

