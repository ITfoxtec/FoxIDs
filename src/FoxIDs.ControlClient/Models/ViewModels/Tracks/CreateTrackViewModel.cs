﻿using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Client.Models.ViewModels
{
    public class CreateTrackViewModel
    {
        /// <summary>
        /// Track name.
        /// </summary>
        [Required]
        [MaxLength(Constants.Models.Track.NameLength)]
        [RegularExpression(Constants.Models.Track.NameRegExPattern, ErrorMessage = "The field {0} can contain letters, numbers, '-' and '_'.")]
        [Display(Name = "Track name (use '-' for production track)")]
        public string Name { get; set; }
    }
}
