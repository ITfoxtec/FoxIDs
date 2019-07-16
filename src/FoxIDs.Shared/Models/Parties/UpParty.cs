using System;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class UpParty : Party
    {
        public static string IdFormat(IdKey idKey) => $"party:up:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            Id = IdFormat(idKey);
        }
    }
}