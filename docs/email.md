# Email provider

FoxIDs supports sending email with SendGrid and SMTP. Both can be configured as an email provider in [each track](#configure-email-provider-in-track) or [generally](#configure-email-provider-generally) in the FoxIDs site configuration.  
FoxIDs sends emails to the users for e.g., account verification and password reset.  

> You can either [create Sendgrid in Azure](https://docs.microsoft.com/en-us/azure/sendgrid-dotnet-how-to-send-email) or directly on [Sendgrid](https://Sendgrid.com), there are more free emails in an Azure managed Sendgrid.

## Configure email provider in track

The email provider can be configured in each track, where the from email address is required.  
If an email provider is configured in the track, it is used instead of any [general](#configure-email-provider-generally) configured email provider.

Configuring SendGrid:
![FoxIDs email provider - SendGrid](images/configure-email-provider-track-sendgrid.png)

Configuring SMTP:
![FoxIDs email provider - SMTP](images/configure-email-provider-track-smtp.png)

## Configure email provider generally

The email provider can be configured generally in the FoxIDs sites application settings. The from email address is required.  
If both a SendGrid and SMTP email provider is configured the SendGrid email provider is used.

Configuring SendGrid with the application setting names:

- Settings:Sendgrid:FromEmail
- Settings:Sendgrid:ApiKey

Configuring SMTP with the application setting names:

- Settings:Sendgrid:FromEmail
- Settings:Sendgrid:Host
- Settings:Sendgrid:Port
- Settings:Sendgrid:Username
- Settings:Sendgrid:Password