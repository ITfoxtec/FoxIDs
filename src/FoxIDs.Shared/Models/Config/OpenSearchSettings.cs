using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class OpenSearchSettings
    {
        /// <summary>
        /// Specify one or many nodes in the OpenSearch cluster.
        /// </summary>
        [ListLength(1, 100)]
        public List<Uri> Nodes { get; set; }

        /// <summary>
        /// Default log lifetime in months (default 6 months).
        /// </summary>
        [Required]
        public int LogLifetimeMonths { get; set; } = 6;
    }
}
