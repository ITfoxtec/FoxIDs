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
        /// Schema name.
        /// </summary>
        [Required]
        public string SchemaName { get; set; } = "foxids";
        /// <summary>
        /// Table name.
        /// </summary>
        [Required]
        public string TableName { get; set; } = "foxids";
    }
}
