using AngleSharp.Html.Parser;
using CMAFuelDataScraper.Models;
using Microsoft.Extensions.Configuration;

namespace CMAFuelDataScraper
{
    internal class Program
    {
        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        static async Task Main(string[] args)
        {
            Console.WriteLine("CMA Fuel Data Scraper started.");

            // Retrieve CMA Fuel URL from configuration and validate
            var cmaFuelUrl = Configuration["CMAFuelUrl"];
            ArgumentException.ThrowIfNullOrEmpty(cmaFuelUrl, nameof(cmaFuelUrl));

            // Make HTTP GET request to the CMA Fuel URL
            Console.WriteLine($"Fetching page from: {cmaFuelUrl}");
            var httpClient = new HttpClient();
            var requestUri = new Uri(cmaFuelUrl);
            var response = await httpClient.GetAsync(requestUri);

            // Exit early if the response is not successful
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Failed to retrieve page. Status code: {response.StatusCode}");
                return;
            }

            // Read and validate the page response content
            var content = await response.Content.ReadAsStringAsync();
            ArgumentException.ThrowIfNullOrEmpty(content, "Page response content is empty");
            Console.WriteLine("Page data retrieved successfully");

            // Parse the HTML content using AngleSharp
            var parser = new HtmlParser();
            var document = await parser.ParseDocumentAsync(content);

            // Extract the table of participating retailers
            var retailersTable = document.QuerySelector("#participating-retailers + table");
            if (retailersTable == null || retailersTable.ChildElementCount == 0)
            {
                Console.WriteLine("Retailers table not found or empty in the HTML document.");
                return;
            }

            // Extract data from the table rows
            var rows = retailersTable.QuerySelectorAll("tbody tr");

            var retailers = new List<Retailer>();

            foreach (var row in rows)
            {
                var retailerName = row.Children[0].TextContent;
                var retailerUrl = row.Children[1].TextContent;

                Console.WriteLine($"Retailer: {retailerName} | URL: {retailerUrl}");

                retailers.Add(new Retailer
                {
                    Name = retailerName,
                    SourceUrl = retailerUrl
                });
            }

            Console.WriteLine("Retrieved {retailers.Count} retailers from the CMA Fuel page.");

            Console.WriteLine("Individual retailer scraping started.");
        }
    }
}
