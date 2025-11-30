using CMAFuelDataScraper.Models;

namespace CMAFuelDataScraper.Services
{
    internal class DataTransformer
    {
        public static RetailerOutput TransformRetailer(Retailer retailer, RetailerDto dto)
        {
            return new RetailerOutput
            {
                Name = retailer.Name,
                SourceUrl = retailer.SourceUrl,
                LastUpdated = dto.LastUpdated
            };
        }

        public static IEnumerable<StationOutput> TransformStations(string retailerName, RetailerDto dto)
        {
            if (dto.Stations is null)
            {
                yield break;
            }

            foreach (var station in dto.Stations)
            {
                var output = new StationOutput
                {
                    RetailerName = retailerName,
                    SiteId = station.SiteId,
                    Brand = station.Brand,
                    Address = station.Address,
                    Postcode = station.Postcode,
                    Latitude = station.Location?.Latitude,
                    Longitude = station.Location?.Longitude,
                    Prices = station.Prices
                };

                if (station.Prices is not null)
                {
                    output.ExtensionData = station.Prices
                        .Where(kvp => kvp.Value.HasValue)
                        .ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value!.Value);
                }

                yield return output;
            }
        }
    }
}
