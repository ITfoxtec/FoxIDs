using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class OptionsSettings : IValidatableObject
    {
        /// <summary>
        /// Specify the selected log option.
        /// </summary>
        public LogOptions Log { get; set; } = LogOptions.Stdout;

        /// <summary>
        /// Specify the selected data storage option.
        /// </summary>
        public DataStorageOptions DataStorage { get; set; } = DataStorageOptions.File;

        /// <summary>
        /// Specify the selected key storage option.
        /// </summary>
        public KeyStorageOptions KeyStorage { get; set; } = KeyStorageOptions.None;

        /// <summary>
        /// Specify the selected cache option.
        /// </summary>
        public CacheOptions Cache { get; set; } = CacheOptions.File;

        /// <summary>
        /// Specify the if and how data is cached option.
        /// </summary>
        public DataCacheOptions DataCache { get; set; } = DataCacheOptions.None;

        public virtual IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            if (Cache != CacheOptions.Redis && DataCache != DataCacheOptions.None)
            {
                results.Add(new ValidationResult($"The field {nameof(DataCache)} can only be different from {DataCacheOptions.None} if the field {nameof(Cache)} is {CacheOptions.Redis}.", new[] { nameof(DataCache) }));
            }

            return results;
        }
    }
}
