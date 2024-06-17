using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models.Master
{
    public class DataProtection : MasterDocument
    {
        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.DataProtection}:{idKey.Master}:{idKey.KeyName}";
        }

        public static async Task<string> IdFormatAsync(string keyName)
        {
            if (keyName == null) new ArgumentNullException(nameof(keyName));

            var idKey = new IdKey
            {
                KeyName = keyName
            };

            return await IdFormatAsync(idKey);
        }

        public static new string PartitionIdFormat(MasterDocument.IdKey idKey) => $"{idKey.Master}:{Constants.Models.DataType.DataProtection}";

        [MaxLength(Constants.Models.DataProtection.IdLength)]
        [RegularExpression(Constants.Models.DataProtection.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.DataProtection.KeyDataLength)]
        [JsonProperty(PropertyName = "key_data")]
        public string KeyData { get; set; }

        public new class IdKey : MasterDocument.IdKey
        {
            [Required]
            [MaxLength(Constants.Models.DataProtection.NameLength)]
            [RegularExpression(Constants.Models.DataProtection.NameRegExPattern)]
            public string KeyName { get; set; }
        }
    }
}
