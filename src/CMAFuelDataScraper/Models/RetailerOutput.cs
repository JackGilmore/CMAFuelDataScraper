using System.Text.Json.Serialization;

namespace CMAFuelDataScraper.Models
{
    internal class RetailerOutput
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("source_url")]
        public required string SourceUrl { get; set; }

        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }
    }
}
