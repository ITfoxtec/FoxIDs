using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FoxIDs
{
    public static class HashExtensions
    {
        public static string Sha1Hash(this string password)
        {
            using (var sha1Provider = new SHA1CryptoServiceProvider())
            {
                var hash = sha1Provider.ComputeHash(Encoding.UTF8.GetBytes(password));
                return string.Concat(hash.Select(b => b.ToString("X2")));
            }
        }
    }
}
