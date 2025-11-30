using CMAFuelDataScraper.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CMAFuelDataScraper
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                })
                .AddSingleton<IConfiguration>(configuration)
                .AddSingleton(sp =>
                {
                    var userAgent = configuration["UserAgent"];
                    ArgumentException.ThrowIfNullOrEmpty(userAgent, nameof(userAgent));
                    return HttpClientFactory.CreateClient(userAgent);
                })
                .AddSingleton<CmaFuelPageScraper>()
                .AddSingleton<RetailerDataFetcher>()
                .AddSingleton<DataTransformer>()
                .AddSingleton<JsonLinesWriter>()
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("CMA Fuel Data Scraper started");

            try
            {
                await RunScraperAsync(serviceProvider, configuration, logger);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Application failed with unhandled exception");
            }
            finally
            {
                await serviceProvider.DisposeAsync();
            }
        }

        private static async Task RunScraperAsync(ServiceProvider serviceProvider, IConfiguration configuration, ILogger<Program> logger)
        {
            var cmaFuelUrl = configuration["CMAFuelUrl"];
            ArgumentException.ThrowIfNullOrEmpty(cmaFuelUrl, nameof(cmaFuelUrl));

            var scraper = serviceProvider.GetRequiredService<CmaFuelPageScraper>();
            List<Models.Retailer> retailers;

            try
            {
                retailers = await scraper.ScrapeRetailersAsync(cmaFuelUrl);
                logger.LogInformation("Retrieved {RetailerCount} retailers from the CMA Fuel page", retailers.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error scraping retailers");
                return;
            }

            var maxParallelRequests = GetMaxParallelRequests(configuration);
            var timeout = TimeSpan.FromMinutes(3);

            var fetcher = serviceProvider.GetRequiredService<RetailerDataFetcher>();
            var fetchedData = await fetcher.FetchAllRetailersAsync(retailers, maxParallelRequests, timeout);

            if (fetchedData.Count == 0)
            {
                logger.LogWarning("No retailer data was successfully fetched");
                return;
            }

            var transformer = serviceProvider.GetRequiredService<DataTransformer>();
            var retailerOutputs = fetchedData.Select(kvp => DataTransformer.TransformRetailer(kvp.Key, kvp.Value)).ToList();
            var stationOutputs = fetchedData.SelectMany(kvp => DataTransformer.TransformStations(kvp.Key.Name, kvp.Value)).ToList();

            var writer = serviceProvider.GetRequiredService<JsonLinesWriter>();
            await writer.WriteRetailersAsync(retailerOutputs, "retailers.jsonl");
            await writer.WriteStationsAsync(stationOutputs, "stations.jsonl");

            logger.LogInformation("Data export completed successfully");
        }

        private static int GetMaxParallelRequests(IConfiguration configuration)
        {
            const int defaultMaxParallelRequests = 5;

            if (int.TryParse(configuration["MaxParallelRequests"], out var configured) && configured > 0)
            {
                return configured;
            }

            return defaultMaxParallelRequests;
        }
    }
}
