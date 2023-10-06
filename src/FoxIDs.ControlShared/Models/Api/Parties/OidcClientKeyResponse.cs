namespace FoxIDs.Models.Api
{
    /// <summary>
    /// Oidc client key import response.
    /// </summary>
    public class OidcClientKeyResponse : INameValue
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
