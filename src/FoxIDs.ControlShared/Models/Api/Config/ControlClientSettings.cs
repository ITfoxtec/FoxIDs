namespace FoxIDs.Models.Api
{
    public class ControlClientSettings
    {
        public string FoxIDsEndpoint { get; set; }

        public string Version { get; set; }
        public string FullVersion { get; set; }

        public LogOptions LogOption { get; set; }

        public KeyStorageOptions KeyStorageOption { get; set; } 
    }
}
