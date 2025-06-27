namespace FoxIDs.Models.ViewModels
{
    public class CustomDElement : DynamicElementBase
    {
        public string DisplayName { get; set; }

        public int MaxLength { get; set; }

        public string RegEx { get; set; }

        public string ErrorMessage { get; set; }

        public string ClaimOut { get; set; }
    }
}
