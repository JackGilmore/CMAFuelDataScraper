using AngleSharp.Html.Parser;
using CMAFuelDataScraper.Models;
using Microsoft.Extensions.Logging;

namespace CMAFuelDataScraper.Services
{
    internal class CmaFuelPageScraper
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<CmaFuelPageScraper> _logger;

        public CmaFuelPageScraper(HttpClient httpClient, ILogger<CmaFuelPageScraper> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<Retailer>> ScrapeRetailersAsync(string cmaFuelUrl)
        {
            ArgumentException.ThrowIfNullOrEmpty(cmaFuelUrl, nameof(cmaFuelUrl));

            _logger.LogInformation("Fetching page from: {CmaFuelUrl}", cmaFuelUrl);

            var requestUri = new Uri(cmaFuelUrl);
            var response = await _httpClient.GetAsync(requestUri);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to retrieve page. Status code: {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            ArgumentException.ThrowIfNullOrEmpty(content, "Page response content is empty");
            _logger.LogInformation("Page data retrieved successfully");

            return await ParseRetailersFromHtml(content);
        }

        private async Task<List<Retailer>> ParseRetailersFromHtml(string htmlContent)
        {
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(htmlContent);

            var retailersTable = document.QuerySelector("#participating-retailers + table");
            if (retailersTable == null || retailersTable.ChildElementCount == 0)
            {
                throw new InvalidOperationException("Retailers table not found or empty in the HTML document.");
            }

            var rows = retailersTable.QuerySelectorAll("tbody tr");
            var retailers = new List<Retailer>();

            foreach (var row in rows)
            {
                var retailerName = row.Children[0].TextContent;
                var retailerUrl = row.Children[1].TextContent;

                _logger.LogDebug("Found retailer: {RetailerName} | URL: {RetailerUrl}", retailerName, retailerUrl);

                retailers.Add(new Retailer
                {
                    Name = retailerName,
                    SourceUrl = retailerUrl
                });
            }

            return retailers;
        }
    }
}
