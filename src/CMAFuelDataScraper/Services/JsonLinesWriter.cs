using System.Text.Json;
using CMAFuelDataScraper.Models;
using Microsoft.Extensions.Logging;

namespace CMAFuelDataScraper.Services
{
    internal class JsonLinesWriter
    {
        private readonly ILogger<JsonLinesWriter> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = false
        };

        public JsonLinesWriter(ILogger<JsonLinesWriter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task WriteRetailersAsync(IEnumerable<RetailerOutput> retailers, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var writer = new StreamWriter(filePath, append: false);
                
                var count = 0;
                foreach (var retailer in retailers)
                {
                    var json = JsonSerializer.Serialize(retailer, _jsonOptions);
                    await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
                    count++;
                }

                _logger.LogInformation("Wrote {Count} retailers to {FilePath}", count, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing retailers to {FilePath}", filePath);
                throw;
            }
        }

        public async Task WriteStationsAsync(IEnumerable<StationOutput> stations, string filePath, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var writer = new StreamWriter(filePath, append: false);
                
                var count = 0;
                foreach (var station in stations)
                {
                    var json = JsonSerializer.Serialize(station, _jsonOptions);
                    await writer.WriteLineAsync(json.AsMemory(), cancellationToken);
                    count++;
                }

                _logger.LogInformation("Wrote {Count} stations to {FilePath}", count, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error writing stations to {FilePath}", filePath);
                throw;
            }
        }
    }
}
