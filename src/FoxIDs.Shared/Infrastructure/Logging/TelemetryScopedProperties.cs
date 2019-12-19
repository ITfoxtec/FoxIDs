using System.Collections.Generic;

namespace FoxIDs.Infrastructure
{
    public class TelemetryScopedProperties
    {
        public IDictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>();

        public void SetScopeProperty(KeyValuePair<string, string> prop)
        {
            Properties[prop.Key] = prop.Value;
        }

        public void SetScopeProperties(IDictionary<string, string> props)
        {
            if (props != null)
            {
                foreach (var prop in props)
                {
                    SetScopeProperty(prop);
                }
            }
        }
    }
}
