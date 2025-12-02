namespace FoxIDs.Models.Logic
{
    public class PasswordPolicyState
    {
        public string Name { get; set; }

        public int Length { get; set; }

        public int MaxLength { get; set; }

        public bool CheckComplexity { get; set; }

        public bool CheckRisk { get; set; }

        public string BannedCharacters { get; set; }

        public int History { get; set; }

        public long MaxAge { get; set; }

        public long SoftChange { get; set; }
    }
}
