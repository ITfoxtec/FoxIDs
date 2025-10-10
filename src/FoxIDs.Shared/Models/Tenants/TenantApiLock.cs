using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace FoxIDs.Models
{
    public class TenantApiLock : DataTtlDocument
    {
        public const string TracksScope = "tracks";
        public const string UsersScope = "users";

        public const int DefaultLifetimeSeconds = 500;
        public const int AcquireRetryDelayMilliseconds = 400;
        public const int MaxAcquireAttempts = 20;

        public static readonly TimeSpan AcquireRetryDelay = TimeSpan.FromMilliseconds(AcquireRetryDelayMilliseconds);

        public static async Task<string> IdFormatAsync(IdKey idKey)
        {
            if (idKey == null) new ArgumentNullException(nameof(idKey));
            await idKey.ValidateObjectAsync();

            return $"{Constants.Models.DataType.TenantApiLock}:{idKey.TenantName}:{idKey.Scope}";
        }

        public static async Task<string> IdFormatAsync(string tenantName, string scope)
        {
            return await IdFormatAsync(new IdKey { TenantName = tenantName, Scope = scope });
        }

        [Required]
        [MaxLength(Constants.Models.TenantApiLock.IdLength)]
        [RegularExpression(Constants.Models.TenantApiLock.IdRegExPattern)]
        [JsonProperty(PropertyName = "id")]
        public override string Id { get; set; }

        [Required]
        [MaxLength(Constants.Models.Tenant.NameLength)]
        [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
        [JsonProperty(PropertyName = "tenant_name")]
        public string TenantName { get; set; }

        [Required]
        [MaxLength(Constants.Models.TenantApiLock.ScopeLength)]
        [RegularExpression(Constants.Models.TenantApiLock.ScopeRegExPattern)]
        [JsonProperty(PropertyName = "scope")]
        public string Scope { get; set; }

        [MaxLength(Constants.Models.TenantApiLock.RequestIdLength)]
        [RegularExpression(Constants.Models.TenantApiLock.RequestIdRegExPattern)]
        [JsonProperty(PropertyName = "request_id")]
        public string RequestId { get; set; }

        public class IdKey
        {
            [Required]
            [MaxLength(Constants.Models.Tenant.NameLength)]
            [RegularExpression(Constants.Models.Tenant.NameDbRegExPattern)]
            public string TenantName { get; set; }

            [Required]
            [MaxLength(Constants.Models.TenantApiLock.ScopeLength)]
            [RegularExpression(Constants.Models.TenantApiLock.ScopeRegExPattern)]
            public string Scope { get; set; }
        }
    }
}
