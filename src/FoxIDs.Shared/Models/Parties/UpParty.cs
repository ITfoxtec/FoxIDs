using System;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class UpParty : Party
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"party:up:{idKey.TenantName}:{idKey.TrackName}:{idKey.PartyName}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string name)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (name == null) new ArgumentNullException(nameof(name));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                PartyName = name,
            };

            return await IdFormatAsync(idKey);
        }

        public async Task SetIdAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            Id = await IdFormatAsync(idKey);
        }

        public PartyBindingPatterns PartyBindingPattern { get; set; } = PartyBindingPatterns.Brackets;
    }
}