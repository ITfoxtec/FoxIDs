using System.Collections.Generic;

namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Work around until https://github.com/RicoSuter/NJsonSchema/pull/1073 is resolved.
    /// </summary>
    public class JsonWebKey
    {
        public string Kid { get; set; }

        public string Y { get; set; }

        public string X5u { get; set; }

        public string X5t { get; set; }

        public List<string> X5c { get; set; }

        public string X { get; set; }

        public string Use { get; set; }

        public string QI { get; set; }

        public string Q { get; set; }

        public string P { get; set; }

        public List<string> Oth { get; set; }

        public string N { get; set; }

        public string Kty { get; set; }

        public List<string> KeyOps { get; set; }

        public string K { get; set; }

        public string E { get; set; }

        public string DQ { get; set; }

        public string DP { get; set; }

        public string D { get; set; }

        public string Crv { get; set; }

        public string Alg { get; set; }
    }
}
