using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class MailSettingsViewModel
    {
        [Required]
        [MaxLength(Constants.Models.User.EmailLength)]
        [EmailAddress]
        [RegularExpression(Constants.Models.User.EmailRegExPattern)]
        [Display(Name = "Send emails from email address")]
        public string FromEmail { get; set; }

        [Display(Name = "Select mail provider")]
        public MailProviders MailProvider { get; set; }        

        [MaxLength(Constants.Models.Track.SendEmail.SendgridApiKeyLength)]
        [Display(Name = "Sendgrid API key")]
        public string SendgridApiKey { get; set; }

        [MaxLength(Constants.Models.Track.SendEmail.SmtpHostLength)]
        [Display(Name = "SMTP host")]
        public string SmtpHost { get; set; }

        [Display(Name = "SMTP port")]
        public int SmtpPort { get; set; }

        [MaxLength(Constants.Models.Track.SendEmail.SmtpUsernameLength)]
        [Display(Name = "SMTP username")]
        public string SmtpUsername { get; set; }

        [MaxLength(Constants.Models.Track.SendEmail.SmtpPasswordLength)]
        [Display(Name = "SMTP password")]
        public string SmtpPassword { get; set; }
    }
}
