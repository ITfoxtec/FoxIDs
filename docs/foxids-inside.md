# FoxIDs inside

## Limitations

Basically, all strings handled in FoxIDs is limited in one way or the other for performance and security reasons. Strings is either truncated or an exception is thrown if they exceed the maximum allowed length. 

The most important limitations are listed below.

**URL**  
The URLs maximum allowed length is 10k (10240) characters. The subsequently query strings maximum allowed length is also 10k (10240) characters.

**Claim**  
A claim has both at type and a value. The claim types maximum allowed length is 80 characters for JWT (access tokens and ID tokens) and 300 characters for SAML 2.0. 
The claim values maximum length is 8000 characters for all token types. 
The limitation applies for each claim type and value separately.

**Tokens**   
A JWT (access tokens, ID tokens and refresh token) revived by FoxIDs is a allowed to have a maximum length of 50000 characters. Claims revived is truncated if they exceed the maximum allowed lengths.  
FoxIDs can create larger tokens, where each claim is capped instead of the entire token.

If a JWT is included as a claim it is truncated if it exceeds the maximum allowed claim value length. 

A SAML 2.0 requests maximum size is not directly limited. The request is indirectly limited if it is send using a redirect binding in the URL query string. 
Claims revived in a SAML 2.0 authn response (SAML 2.0 token) is truncated if they exceed the maximum allowed lengths.