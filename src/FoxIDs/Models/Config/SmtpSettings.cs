namespace FoxIDs.Models.Config
{
    public class SmtpSettings
    {
        /// <summary>
        /// From email.
        /// </summary>
        public string FromEmail { get; set; }

        /// <summary>
        /// Host.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Port.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Username.
        /// </summary>
        public string Username { get; set; }
        /// <summary>
        /// Password.
        /// </summary>
        public string Password { get; set; }
    }
}
