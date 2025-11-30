using System.Text.Json.Serialization;

namespace CMAFuelDataScraper.Models
{
    internal class RetailerDto
    {
        [JsonPropertyName("last_updated")]
        public string? LastUpdated { get; set; }

        [JsonPropertyName("stations")]
        public Station[]? Stations { get; set; }

        public class Station
        {
            [JsonPropertyName("site_id")]
            public string? SiteId { get; set; }

            [JsonPropertyName("brand")]
            public string? Brand { get; set; }

            [JsonPropertyName("address")]
            public string? Address { get; set; }

            [JsonPropertyName("postcode")]
            public string? Postcode { get; set; }

            [JsonPropertyName("location")]
            public Location? Location { get; set; }

            [JsonPropertyName("prices")]
            public Dictionary<string, decimal?>? Prices { get; set; }
        }

        public class Location
        {
            [JsonPropertyName("latitude")]
            public float Latitude { get; set; }

            [JsonPropertyName("longitude")]
            public float Longitude { get; set; }
        }
    }
}
