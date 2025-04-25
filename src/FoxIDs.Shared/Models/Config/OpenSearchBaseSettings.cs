using FoxIDs.Infrastructure.DataAnnotations;
using System;
using System.Collections.Generic;

namespace FoxIDs.Models.Config
{
    public class OpenSearchBaseSettings
    {
        /// <summary>
        /// Specify one or many nodes in the OpenSearch cluster.
        /// </summary>
        [ListLength(1, 100)]
        public List<Uri> Nodes { get; set; }
    }
}
