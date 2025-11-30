using CMAFuelDataScraper.Models;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace CMAFuelDataScraper.Services
{
    internal class RetailerDataFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RetailerDataFetcher> _logger;

        public RetailerDataFetcher(HttpClient httpClient, ILogger<RetailerDataFetcher> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<RetailerDto?> FetchRetailerDataAsync(string sourceUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                return null;
            }

            try
            {
                if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
                {
                    _logger.LogWarning("Invalid URL format: {SourceUrl}", sourceUrl);
                    return null;
                }

                var request = new HttpRequestMessage(HttpMethod.Get, uri);
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Retailer data not found at: {SourceUrl}", sourceUrl);
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<RetailerDto>(cancellationToken).ConfigureAwait(false);
                return json;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP error fetching retailer data from: {SourceUrl}", sourceUrl);
                return null;
            }
            catch (TaskCanceledException)
            {
                _logger.LogWarning("Request timeout or cancelled for: {SourceUrl}", sourceUrl);
                return null;
            }
        }

        public async Task FetchAllRetailersAsync(List<Retailer> retailers, int maxParallelRequests, TimeSpan timeout)
        {
            _logger.LogInformation("Individual retailer scraping started with {MaxParallelRequests} parallel requests", maxParallelRequests);

            using var semaphore = new SemaphoreSlim(maxParallelRequests);
            using var ctsAll = new CancellationTokenSource(timeout);

            var fetchTasks = retailers.Select(async retailer =>
            {
                await semaphore.WaitAsync(ctsAll.Token).ConfigureAwait(false);
                try
                {
                    var retailerData = await FetchRetailerDataAsync(retailer.SourceUrl, ctsAll.Token);
                    if (retailerData is null)
                    {
                        _logger.LogWarning("Failed to fetch retailer: {RetailerName} ({SourceUrl})", retailer.Name, retailer.SourceUrl);
                        return;
                    }

                    _logger.LogInformation("Fetched {RetailerName} ({SourceUrl}) - {StationCount} stations - Last updated: {LastUpdated}", 
                        retailer.Name, retailer.SourceUrl, retailerData.Stations?.Length ?? 0, retailerData.LastUpdated);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Fetch cancelled for {RetailerName} ({SourceUrl})", retailer.Name, retailer.SourceUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching {RetailerName} ({SourceUrl})", retailer.Name, retailer.SourceUrl);
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(fetchTasks);

            _logger.LogInformation("All retailer scraping tasks completed");
        }
    }
}
