# Connect to Amazon IAM Identity Center with SAML 2.0

Connect FoxIDs as an **external identity provider for Amazon IAM Identity Center** with SAML 2.0.

By configuring an [OpenID Connect authentication method](auth-method-oidc.md) and Amazon IAM Identity Center as a [SAML 2.0 application](app-reg-saml-2.0.md) FoxIDs become a [bridge](bridge.md) between OpenID Connect and SAML 2.0 and automatically convert JWT (OAuth 2.0) claims to SAML 2.0 claims.

## Configure Amazon IAM Identity Center

This guide describes how to set up FoxIDs as an external identity provider for Amazon IAM Identity Center. Users are connected with their email address and must already exist in Amazon IAM Identity Center.

**1 - Start by configuring a certificate in [FoxIDs Control Client](control.md#foxids-control-client)**

You are required to upload the SAML 2.0 metadata from FoxIDs to Amazon IAM Identity Center. It is therefore necessary to use a long-lived certificate in FoxIDs, e.g. valid for 3 years.

1. Select the **Certificates** tab
2. Click **Change Container type**
![Change certificate container type in FoxIDs](images/howto-certificate-type.png)
3. Find **Self-signed or your certificate** and click **Change to this container type**
4. The self-signed certificate is valid for 3 years, and you can optionally upload your own certificate
![Change certificate in FoxIDs](images/howto-certificate-change.png)


**2 - Then go to the Amazon IAM Identity Center page in the [AWS portal](https://aws.amazon.com/)**

 1. Navigate to **Amazon IAM Identity Center**
 2. Click **Settings** 
 3. Click **Choose identity source** (under the **Identity source** section in the **Actions** menu)
 4. Select **External identity provider**
 5. Click **Next**
 6. Copy the **IAM Identity Center Assertion Consumer Service (ACS) URL** and save it for later
 7. Copy the **IAM Identity Center issuer URL** and save it for later
![Read ACS and issuer in Amazon IAM Identity Center](images/app-reg-howto-saml-amazon-iam-ic-acs-issuer.png)

**3 - Then create a SAML 2.0 application in [FoxIDs Control Client](control.md#foxids-control-client)**

1. Select the **Applications** tab
2. Click **New application**
3. Click **Show advanced**
4. Click **Web application (SAML 2.0)**
5. Add the **Name** e.g. `Amazon IAM Identity Center`
6. Set the **Application issuer** to the **IAM Identity Center issuer URL** you copied
7. Set the **Assertion consumer service (ACS) URL** to the **IAM Identity Center Assertion Consumer Service (ACS) URL** you copied
![Add issuer and ACS in FoxIDs](images/app-reg-howto-saml-amazon-iam-ic-create.png)
8. Click **Create**
9. Click **Change application** to open the application in edit mode
10. Click **Show advanced**
11. Set the **Authn request binding** to **Post**
12. Set the **NameID format** to `urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress`
![Set binding and NameID format in FoxIDs](images/app-reg-howto-saml-amazon-iam-ic-binding-format.png)
13. At the bottom of the application, set the **NameID format in metadata** to `urn:oasis:names:tc:SAML:1.1:nameid-format:emailAddress`
14. Click **Update**
15. Go to the top of the application and find the **SAML 2.0 Metadata URL** link, open it in a browser, and save the page as an XML file
 
**4 - Go back to the Amazon IAM Identity Center in [AWS portal](https://aws.amazon.com/)**

1. Find the **IdP SAML metadata** and click **Choose file**
2. Select the metadata file from FoxIDs
3. Click **Next**
4. Enter `ACCEPT`
5. Click **Change identity source**
6. In the **Identity source** section, select the **AWS access portal URL** link to test the sign-in flow (you may need to create a user in FoxIDs first)

> Amazon IAM Identity Center does not support logout.