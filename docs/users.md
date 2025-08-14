# Users
Users are saved in the environment's user repository. To achieve multiple user stores, you create additional environments and thus achieve more user stores.

There are two different types of users:
- [Internal users](users-internal.md) which are authenticated using the [login](login.md) authentication method.
- [External users](users-external.md) which are linked by an authenticated method to an external user/identity with a claim. The users are authenticated in an external Identity Provider and the users can be redeemed based on e.g. an `email` claim (see Provision and redeem section in external users doc).

Another option is to authenticate against an [existing external user store](external-login.md) via an API in. In this case the users are not saved as internal users in the environment. The uses can optionally be created as [external users](users-external.md).