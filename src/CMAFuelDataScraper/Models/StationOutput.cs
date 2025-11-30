using System.Text.Json.Serialization;

namespace CMAFuelDataScraper.Models
{
    internal class StationOutput
    {
        [JsonPropertyName("retailer_name")]
        public required string RetailerName { get; set; }

        [JsonPropertyName("site_id")]
        public string? SiteId { get; set; }

        [JsonPropertyName("brand")]
        public string? Brand { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("postcode")]
        public string? Postcode { get; set; }

        [JsonPropertyName("latitude")]
        public float? Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public float? Longitude { get; set; }

        [JsonIgnore]
        public Dictionary<string, decimal?>? Prices { get; set; }

        [JsonExtensionData]
        public Dictionary<string, object?>? ExtensionData { get; set; }
    }
}
