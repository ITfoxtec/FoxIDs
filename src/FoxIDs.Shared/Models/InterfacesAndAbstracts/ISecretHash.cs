namespace FoxIDs.Models
{
    public interface ISecretHash
    {
        string HashAlgorithm { get; set; }

        string Hash { get; set; }

        string HashSalt { get; set; }
    }
}
