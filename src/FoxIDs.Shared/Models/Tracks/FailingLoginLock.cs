using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class FailingLoginLock : DataTtlDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.FailingLoginLock}:{idKey.TenantName}:{idKey.TrackName}:{idKey.UserIdentifier}:{(int)idKey.FailingLoginType}";
        }

        public static async Task<string> IdFormatAsync(RouteBinding routeBinding, IdKey idKey)
        {
            if (routeBinding == null) new ArgumentNullException(nameof(routeBinding));
            if (idKey == null) new ArgumentNullException(nameof(idKey));

            return await IdFormatAsync(new IdKey
            {
                TenantName = routeBinding.TenantName,
                TrackName = routeBinding.TrackName,
                UserIdentifier = idKey.UserIdentifier,
                FailingLoginType = idKey.FailingLoginType
            });
        }

        [Required]
        [MaxLength(Constants.Models.FailingLoginLock.IdLength)]
        [RegularExpression(Constants.Models.FailingLoginLock.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.FailingLoginLock.UserIdentifierLength)]
        [RegularExpression(Constants.Models.FailingLoginLock.UserIdentifierRegExPattern)]
        public string UserIdentifier { get; set; }

        [Required] 
        public FailingLoginTypes FailingLoginType { get; set; }

        [Required]
        [JsonProperty(PropertyName = "ct")]
        public long CreateTime { get; set; }

        public class IdKey : Track.IdKey
        {
            [MaxLength(Constants.Models.FailingLoginLock.UserIdentifierLength)]
            [RegularExpression(Constants.Models.FailingLoginLock.UserIdentifierRegExPattern)]
            public string UserIdentifier { get; set; }

            public FailingLoginTypes FailingLoginType { get; set; }
        }
    }
}
