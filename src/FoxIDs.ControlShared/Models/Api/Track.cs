using FoxIDs.Infrastructure.DataAnnotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Api
{
    public class Track : INameValue
    {
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern)]
        public string Name { get; set; }

        [Length(Constants.Models.Claim.ClaimsMapMin, Constants.Models.Claim.ClaimsMapMax)]
        public List<ClaimMap> ClaimMappings { get; set; }

        [Length(Constants.Models.Track.ResourcesMin, Constants.Models.Track.ResourcesMax)]
        public List<ResourceItem> Resources { get; set; }

        [Range(Constants.Models.Track.SequenceLifetimeMin, Constants.Models.Track.SequenceLifetimeMax)] // 30 seconds to 3 hours
        public int SequenceLifetime { get; set; } = 600;

        [Range(Constants.Models.Track.PasswordLengthMin, Constants.Models.Track.PasswordLengthMax)]
        public int PasswordLength { get; set; } = 6;

        [Required]
        public bool? CheckPasswordComplexity { get; set; } = true;

        [Required]
        public bool? CheckPasswordRisk { get; set; } = true;

        [Length(Constants.Models.Track.AllowIframeOnDomainsMin, Constants.Models.Track.AllowIframeOnDomainsMax, Constants.Models.Track.AllowIframeOnDomainsLength)]
        public List<string> AllowIframeOnDomains { get; set; }
    }
}
