namespace FoxIDs.Models.Config
{
    public class SendgridSettings
    {
        /// <summary>
        /// From name (name associated to the email address).
        /// </summary>
        public string FromName { get; set; }

        /// <summary>
        /// From email.
        /// </summary>
        public string FromEmail { get; set; }

        /// <summary>
        /// API key.
        /// </summary>
        public string ApiKey { get; set; }
    }
}
