using AngleSharp.Io;
using System.Net;

namespace CMAFuelDataScraper.Services
{
    internal static class HttpClientFactory
    {
        public static HttpClient CreateClient(string userAgent)
        {
            ArgumentException.ThrowIfNullOrEmpty(userAgent, nameof(userAgent));

            var client = new HttpClient(new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            })
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Add(HeaderNames.UserAgent, userAgent);
            client.DefaultRequestHeaders.Add(HeaderNames.Accept, "*/*");
            client.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, "en-GB");

            return client;
        }
    }
}
