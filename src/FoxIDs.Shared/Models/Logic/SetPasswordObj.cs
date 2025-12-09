namespace FoxIDs.Models.Logic
{
    public class SetPasswordObj
    {
        public string Password { get; set; }

        public string PasswordHashAlgorithm { get; set; }

        public string PasswordHash { get; set; }

        public string PasswordHashSalt { get; set; }

        public long? PasswordLastChanged { get; set; }

        public bool ChangePassword { get; set; }
    }
}
