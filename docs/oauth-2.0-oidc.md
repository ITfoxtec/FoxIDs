# OAuth 2.0 and OpenID Connect
OAuth 2.0, OpenID Connect, JWT and JWT claims are first class citizens in FoxIDs. Internally claims are always represented as JWT claims and request / response properties are described with OAuth 2.0 and OpenID Connect attributes. When FoxIDs converts between standards it also converts to the same internal representation using JWT claims and OAuth 2.0 / OpenID Connect attributes.

FoxIDs can act as an [OpenID Provider (OP)](#openid-Provider-OP) supporting authenticating the client using OpenID Connect. The client can request an access token for multiple API's defined as OAuth 2.0 resources.

Future support:
- FoxIDs acting as an OpenID Connect RP (client) authenticating with an external OP.
- *(Maybe support) FoxIDs acting as an OAuth 2.0 resource owner supporting plain OAuth 2.0 client authorization.*
- *(Maybe support) FoxIDs acting as an OAuth 2.0 client authorizing with a resource owner.*

FoxIDs do not support plain OAuth 2.0 client authorization acting as an OAuth 2.0 resource owner because it is less secure then using OpenID Connect and not recemented to use in the future.

## OpenID Provider (OP)


Login

Logout – front channel logout


Session


Client secret and PKCE
	Both PKCE and secret is validated.

	PKCE and client secret is not validated Implicit Grant.  




Default both id token and access token with the client as audience. The default client resource can be removed from the access token.
Access token support multiple audiences to API's defined as OAuth 2.0 resources
	OAuth 2.0 Bearer Token


## Client Credentials Grant

PKCE is not validated in Client Credentials Grant

## Resource Owner Password Credentials Grant
Resource Owner Password Credentials Grant is not supported.