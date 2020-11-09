using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Hids.DurableFunctionSupport
{
    /// <summary>
    /// Makes requests to the Function Host for Durable Function status
    /// </summary>
    public class DurableFunctionClient
    {
        /// <summary>
        /// Port this client will contact the function host on
        /// </summary>
        public int Port { get; private set; }

        /// <summary>
        /// Base URL for the function host.  Typically http://localhost:{port}
        /// </summary>
        public string BaseUrl { get; private set; }
        private static HttpClient client;

        /// <summary>
        /// Constructs a new DurableFunctionClient with a base URL of http://localhost:{port}
        /// </summary>
        /// <param name="port">Port the function host is expected to be running on</param>
        public DurableFunctionClient(int port)
        {
            Port = port;
            client = new HttpClient();
            BaseUrl = $"http://localhost:{port}";
        }

        /// <summary>
        /// Gets the history of all Durable Functions on the function host.
        /// </summary>
        public async Task<IEnumerable<DurableFunctionStatus>> GetAllFunctionStatuses()
        {
            var url = $"{BaseUrl}/runtime/webhooks/durabletask/instances";

            var request = new HttpRequestMessage(
                HttpMethod.Get,
                url
            );

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IEnumerable<DurableFunctionStatus>>(responseContent);
        }

        /// <summary>
        /// Deletes the history of all Durable Functions on the function host.
        /// </summary>
        /// <remarks>
        /// This is useful in a test scenario when you expect only a single instance.  Instead of
        /// having to search by name, status, or try to be clever based on execution times, the
        /// test can use this method to clear history, wait for an instance to show up in the
        /// history, then wait for that instance to finish.
        /// </remarks>
        /// <returns>
        /// True if instances were deleted, false otherwise
        /// </returns>
        public async Task<bool> PurgeInstanceHistoriesAsync()
        {
            var createdTimeFrom = "2000-01-01T00:00:00Z";  // Docs say this is optional, but it's not

            var url = $"{BaseUrl}/runtime/webhooks/durabletask/instances";
            var uriBuilder = new UriBuilder(url);
            uriBuilder.Query = $"createdTimeFrom={createdTimeFrom}";

            var request = new HttpRequestMessage(
                HttpMethod.Delete,
                uriBuilder.Uri
            );

            var response = await client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            // Returns 404 if no instances found matching the criteria - expected if no functions have run yet
            return (response.StatusCode != System.Net.HttpStatusCode.NotFound);
        }
    }
}