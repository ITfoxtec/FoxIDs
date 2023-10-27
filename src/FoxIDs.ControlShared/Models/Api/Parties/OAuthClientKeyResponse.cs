namespace FoxIDs.Models.Api
{
    /// <summary>
    /// OAuth client key import response.
    /// </summary>
    public class OAuthClientKeyResponse : INameValue
    {
        /// <summary>
        /// Primary keys external name.
        /// </summary>        
        public string Name { get; set; }

        /// <summary>
        /// Primary key.
        /// </summary>
        public ClientKey PrimaryKey { get; set; }
    }
}
