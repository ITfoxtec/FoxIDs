using FoxIDs.Infrastructure.DataAnnotations;
using OpenSearch.Net;
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
        /// Default log lifetime.
        /// </summary>
        [Required]
        public LogLifetimeOptions LogLifetime { get; set; } = LogLifetimeOptions.Max180Days;

        /// <summary>
        /// Default log Name.
        /// </summary>
        [Required]
        public string LogName { get; set; } = Constants.Logs.LogName;

        /// <summary>
        /// Allow insecure certificates.
        /// </summary>
        public bool AllowInsecureCertificates { get; set; }
    }
}
