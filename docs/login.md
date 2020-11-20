# Login
FoxIDs support handling user login in a up-party login interface. There can be configured a number of up-party login's per track with different configurations. 
A track contains one [user repository](#user-repository) and all up-party login's configured in a track authenticate users with the same user repository.

//TODO


## User repository 
Each track contains a user repository where the users is saved in Cosmos DB. The users id, email and other claims are saved as text. The password is never saved needer in logs or in Cosmos DB. Instead a hash of the password is saved in Cosmos DB along with the rest of the user information.

### Password hash
FoxIDs is designed to support a growing number of algorithms with different iterations by saving information about the hash algorithm used alongside the actually hash in Cosmos DB. Thereby FoxIDs can validate an old hash algorithm and at the same time save new hashes with a new hash algorithm.

Currently FoxIDs support and use hash algorithm `P2HS512:10` which is defined as:

- The HMAC algorithm (RFC 2104) using the SHA-512 hash function (FIPS 180-4).
- With 10 iterations.
- Salt is generated from 64 bytes.
- Derived key length is 80 bytes.

Standard .NET liberals are used to calculate the hash.
