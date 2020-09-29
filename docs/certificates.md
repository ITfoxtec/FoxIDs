# Certificates

## Certificate container types
**Contained certificates (default)**
- Certificates stored in Cosmos DB including private key.
- Self-signed certificates created by FoxIDs or upload your one certificates.
- Not automatically renewed.
- No cost per signing.

**Key Vault renewed self-signed certificates**
- Certificates stored in Key Vault and private key not exportable.
- Self-signed certificates created by Key Vault.
- Automatically renewed with 3 month validity period. Renewed 10 days before expiration and promoted to primary certificate 5 days before expiration.
- Cost per signing.

**Key Vault upload your one certificate _(not supported, to be implemented)_**
- Certificates stored in Key Vault and private key not exportable.
- Not automatically renewed.
- Cost per signing.
