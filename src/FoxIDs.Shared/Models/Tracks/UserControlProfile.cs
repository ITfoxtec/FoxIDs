using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class UserControlProfile : DataDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.UserControlProfile}:{idKey.TenantName}:{idKey.TrackName}:{idKey.UserHashId}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, string userHashId)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (userHashId == null) new ArgumentNullException(nameof(userHashId));

            var idKey = new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                UserHashId = userHashId
            };

            return await IdFormatAsync(idKey);
        }

        [Required]
        [MaxLength(Constants.Models.UserControlProfile.IdLength)]
        [RegularExpression(Constants.Models.UserControlProfile.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "last_track_name")]
        public string LastTrackName { get; set; }        

        public class IdKey : Track.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.UserControlProfile.UserHashIdLength)]
            public string UserHashId { get; set; }
        }
    }
}
