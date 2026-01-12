using Newtonsoft.Json;

namespace FoxIDs.Models.Modules
{
    public class NemLoginSubjectMatchesCprRequest
    {
        [JsonProperty("cpr")]
        public string Cpr { get; set; }

        [JsonProperty("subjectNameID")]
        public string SubjectNameID { get; set; }

        [JsonProperty("entityID")]
        public string EntityID { get; set; }
    }
}
