using System.ComponentModel.DataAnnotations;

namespace FoxIDs.ResourceTranslateTool.Models
{
    public class TranslateSettings
    {
        /// <summary>
        /// The EmbeddedResource.json file path.
        /// </summary>
        [Required]
        public string EmbeddedResourceJsonPath { get; set; }

        /// <summary>
        /// DeepL API authentication key
        /// </summary>
        [Required]
        public string DeeplAuthenticationKey { get; set; }
        
        /// <summary>
        /// DeepL API URL
        /// </summary>
        [Required]
        public string DeeplServerUrl { get; set; }

        /// <summary>
        /// DeepL API authentication key
        /// </summary>
        [Required]
        public string GoogleProjectId { get; set; }
    }
}
