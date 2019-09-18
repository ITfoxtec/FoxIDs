using System;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class UpParty : Party
    {
        public static async Task<string> IdFormat(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"party:up:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";
        }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormat(idKey);
        }
    }
}