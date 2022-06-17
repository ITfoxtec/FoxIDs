using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxIDs.Models.Config
{
    public class CacheSettings
    {
        /// <summary>
        /// Time to cache custom domains in seconds (default 12 hours).
        /// </summary>
        public int CustomDomainCacheLifetime { get; set; } = 43200;
    }
}
