using Newtonsoft.Json;
namespace FoxIDs.Models
{
    /// <summary>
    /// A serializable rapper of DateOnly, the .NET version do not have public set on the properties. 
    /// Mongo can not serialize DateOnly (probably also apply to other DB providers) A problem from "MongoDB.Driver" version 3.0.0.
    /// </summary>
    public class DateOnlySerializable
    {
        public DateOnlySerializable()
        { }

        public DateOnlySerializable(int year, int month, int day)
        {
            Year = year;
            Month = month; 
            Day = day;
        }

        [JsonProperty(PropertyName = "year")]
        public int Year { get; set; }

        [JsonProperty(PropertyName = "month")] 
        public int Month { get; set; }

        [JsonProperty(PropertyName = "day")]
        public int Day { get; set; }
    }
}
