using ITfoxtec.Identity.Util;

namespace FoxIDs.Client.Util
{
    public static class SecretGenerator
    {
        public static string GenerateNewSecret()
        {
            return RandomGenerator.Generate(32);
        }

        public static string GenerateNewPassword()
        {
            return RandomGenerator.GenerateSimplePassword(8);
        }
    }
}
