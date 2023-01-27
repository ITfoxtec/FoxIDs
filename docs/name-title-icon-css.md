# Name, browser title, browser icon and CSS

## Display name

A display name for you organisation, company or system can be configured on each track. When FoxIDs send an email to a user the email text is customized by adding the display name.

The name is configured in the track settings in [FoxIDs Control Client](control.md#foxids-control-client).

1. Select Settings
2. Add the name in the Track display name field
3. Click Update

## Add browser title, browser icon and CSS

The FoxIDs user interface can be customized per [up-party login](login). This means that a single FoxIDs track can support multiple user interface designs with different browser titles, browser icons and CSS.

> FoxIDs use Bootstrap 4.6 and Flexbox CSS.

Find the up-party login in [FoxIDs Control Client](control.md#foxids-control-client) that you want to configure.

 1. Select show advanced settings
 4. Add the browser title text
 4. Add the browser icon URL from an external site, supported image formats: ico, png, gif, jpeg and webp
 2. Add the CSS to the CSS field, if necessary drag the field bigger
 5. Click Update

 After update the title, icon and CSS is instantly active.

 ![Configure title, icon and CSS](images/configure-login-title-icon-css.png)

 ## CSS examples

 Change background and add logo text. It is also possible to add a logo image.

    body {
        background: #7c8391;
    }

    .brand-content-text {
        visibility: hidden;
    }

    .brand-content-text:before {
        color: #6ad54a;
        content: "Test logo";
        visibility: visible;
    }

![Configure background and add logo with CSS](images/configure-login-css-backbround-logo.png)    

Add a background image from an external site.

    body {
        background: #FFF;
        background: url(https://some-external-site.com/image.png);
        background-position: no-repeat center center fixed;
        background-color: inherit;
        background-repeat: no-repeat;
        background-size:cover;
    }

![Configure background image](images/configure-login-css-backbround-image.png)   

 Add information to the login box.

    div.page-content:before {
      font-weight: bold;
      font-style: italic;
      content: "Login with test user 'test1@foxids.com' or 'test2@foxids.com' and password 'TestAccess!'";
    }

![Configure login box with CSS](images/configure-login-css-sample-test.png)