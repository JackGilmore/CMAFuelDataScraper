using AngleSharp.Html.Parser;
using AngleSharp.Io;
using CMAFuelDataScraper.Models;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Http.Json;

namespace CMAFuelDataScraper
{
    internal class Program
    {
        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        private static readonly HttpClient HttpClient = new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate })
        {
            Timeout = TimeSpan.FromSeconds(30)
        };


        static async Task Main(string[] args)
        {
            Console.WriteLine("CMA Fuel Data Scraper started.");

            // Retrieve CMA Fuel URL from configuration and validate
            var cmaFuelUrl = Configuration["CMAFuelUrl"];
            ArgumentException.ThrowIfNullOrEmpty(cmaFuelUrl, nameof(cmaFuelUrl));

            // Get HttpClient User Agent and validate
            var userAgent = Configuration["UserAgent"];
            ArgumentException.ThrowIfNullOrEmpty(userAgent, nameof(userAgent));

            // Config HttpClient
            HttpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);
            HttpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "*/*");
            HttpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-GB");

            // Make HTTP GET request to the CMA Fuel URL
            Console.WriteLine($"Fetching page from: {cmaFuelUrl}");
            var requestUri = new Uri(cmaFuelUrl);
            var response = await HttpClient.GetAsync(requestUri);

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

            Console.WriteLine($"Retrieved {retailers.Count} retailers from the CMA Fuel page.");

            Console.WriteLine("Individual retailer scraping started.");

            // Concurrency control: value can be overridden via config "MaxParallelRequests"
            var maxParallelRequests = 5;
            if (int.TryParse(Configuration["MaxParallelRequests"], out var configured) && configured > 0)
            {
                maxParallelRequests = configured;
            }

            using var semaphore = new SemaphoreSlim(maxParallelRequests);
            var ctsAll = new CancellationTokenSource(TimeSpan.FromMinutes(3));

            var fetchTasks = retailers.Select(async retailer =>
            {
                await semaphore.WaitAsync(ctsAll.Token).ConfigureAwait(false);
                try
                {
                    var retailerData = await FetchRetailerDataSync(retailer.SourceUrl, ctsAll.Token);
                    if (retailerData is null)
                    {
                        Console.WriteLine($"Failed to fetch retailer: {retailer.Name} ({retailer.SourceUrl})");
                        return;
                    }

                    Console.WriteLine($"Fetched {retailer.Name} ({retailer.SourceUrl}) - {retailerData.Stations?.Length} stations - Last updated: {retailerData.LastUpdated}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Fetch cancelled for {retailer.Name} ({retailer.SourceUrl})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching {retailer.Name} ({retailer.SourceUrl}): {ex.Message}");
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToArray();

            await Task.WhenAll(fetchTasks);

            Console.WriteLine("All retailer scraping tasks completed.");
        }

        private static async Task<RetailerDto?> FetchRetailerDataSync(string sourceUrl, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                return null;
            }

            try
            {
                // Ensure absolute URI
                if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri))
                {
                    return null;
                }

                var request = new HttpRequestMessage(System.Net.Http.HttpMethod.Get, uri);

                var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return null;
                }

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadFromJsonAsync<RetailerDto>(cancellationToken).ConfigureAwait(false);
                return json;
            }
            catch (HttpRequestException)
            {
                return null;
            }
            catch (TaskCanceledException)
            {
                // timeout or cancellation
                return null;
            }
        }

    }
}
