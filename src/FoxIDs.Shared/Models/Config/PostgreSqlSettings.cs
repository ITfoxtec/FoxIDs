using System.ComponentModel.DataAnnotations;

namespace FoxIDs.Models.Config
{
    public class PostgreSqlSettings
    {
        /// <summary>
        /// Connection string.
        /// E.g. "Host=localhost;Username=postgres;Password=postgres;Database=foxids"
        /// </summary>
        [Required]
        public string ConnectionString { get; set; }
        /// <summary>
        /// Table name.
        /// </summary>
        [Required]
        public string TableName { get; set; } = "foxids";
    }
}
