using ITfoxtec.Identity.Util;
using System.Collections.Generic;
using System.Linq;

namespace FoxIDs.Util
{
    public static class RandomName
    {
        public static string GenerateDefaultName()
        {
            return RandomGenerator.GenerateCode(Constants.Models.DefaultNameLength).ToLower();
        }

        public static string GenerateDefaultName(IEnumerable<string> notNames, int count = 0)
        {
            var name = GenerateDefaultName();
            if (count < Constants.Models.DefaultNameMaxAttempts)
            {
                if (notNames?.Count() > 0 && notNames.Where(n => n.Equals(name)).Any())
                {
                    count++;
                    return GenerateDefaultName(notNames, count: count);
                }
            }
            return name;
        }
    }
}
