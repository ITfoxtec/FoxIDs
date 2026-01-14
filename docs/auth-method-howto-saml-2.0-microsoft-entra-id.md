<!--
{
    "title":  "Connect to Microsoft Entra ID with SAML 2.0 (Template)",
    "description":  "Use the Microsoft Entra ID template to configure a SAML 2.0 authentication method with an Entra ID enterprise application.",
    "ogTitle":  "Connect to Microsoft Entra ID with SAML 2.0 (Template)",
    "ogDescription":  "Use the Microsoft Entra ID template to configure a SAML 2.0 authentication method with an Entra ID enterprise application.",
    "ogType":  "article",
    "ogImage":  "/images/foxids_logo.png",
    "twitterCard":  "summary_large_image",
    "additionalMeta":  {
                           "keywords":  "auth method howto saml 2.0 microsoft entra id, FoxIDs docs"
                       }
}
-->

# Connect to Microsoft Entra ID with SAML 2.0 (Template)

Use the Microsoft Entra ID template to configure a SAML 2.0 authentication method with Microsoft Entra ID. The template shows the Entity ID, ACS URL, and Single logout URL you need in Entra ID and requires the enterprise application metadata URL.

## OpenID Connect bridge
By configuring a [SAML 2.0 authentication method](auth-method-saml-2.0.md) and an [OpenID Connect application registration](app-reg-oidc.md) FoxIDs becomes a [bridge](bridge.md) between SAML 2.0 and OpenID Connect. FoxIDs then handles the SAML 2.0 connection as a Relying Party (RP) / Service Provider (SP) and you only need to care about OpenID Connect in your application.

## Create the authentication method in FoxIDs

**1) Start in FoxIDs Control Client**

1. Go to the **Authentication** tab
2. Click **New authentication**
3. Select **Microsoft Entra ID - SAML 2.0**
4. Enter a name for the authentication method

<!-- Screenshot: FoxIDs - select Microsoft Entra ID template -->

**2) Copy the FoxIDs application information**

1. Copy the **Entity ID**
2. Copy the **ACS URL**
3. Copy the **Single logout URL**
4. Keep the page open

<!-- Screenshot: FoxIDs - Microsoft Entra ID application information -->

## Create the Microsoft Entra ID enterprise application

**3) Create the enterprise application in Microsoft Entra ID**

1. Open the Microsoft Entra admin center
2. Go to **Enterprise applications**
3. Select **New application** and create a non-gallery application

<!-- Screenshot: Entra ID - create enterprise application -->

**4) Configure SAML single sign-on**

1. Open the **Single sign-on** blade and choose **SAML**
2. In **Basic SAML Configuration**, add the values from FoxIDs:
   - **Entity ID** (Identifier)
   - **ACS URL** (Reply URL)
   - **Single logout URL** (Logout URL)
3. Save the SAML configuration

<!-- Screenshot: Entra ID - basic SAML configuration -->

**5) Copy the federation metadata URL**

1. In the **SAML Certificates** section, copy the **App Federation Metadata URL**

<!-- Screenshot: Entra ID - App Federation Metadata URL -->

## Finish the authentication method in FoxIDs

**6) Save the metadata URL and create**

1. Paste the **App Federation Metadata URL** into the required **Metadata URL** field
2. Click **Create**

<!-- Screenshot: FoxIDs - Microsoft Entra ID template settings -->
