namespace FoxIDs.SeedTool.Models.ApiModels
{
    public class CreateUserApiModel : UserBaseApiModel
    {
        public string Password { get; set; }

        public string PasswordHashAlgorithm { get; set; }

        public string PasswordHash { get; set; }

        public string PasswordHashSalt { get; set; }
    }
}
