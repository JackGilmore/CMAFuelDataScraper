using Microsoft.Extensions.Configuration;

namespace CMAFuelDataScraper
{
    internal class Program
    {
        private static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        static void Main(string[] args)
        {
            Console.WriteLine("CMA Fuel Data Scraper started.");

            var cmaFuelUrl = Configuration["CMAFuelUrl"];

            ArgumentException.ThrowIfNullOrEmpty(cmaFuelUrl, nameof(cmaFuelUrl));
        }
    }
}
