using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Hids.DurableFunctionSupport
{
    /// <summary>
    /// Contains information about a Durable Function execution
    /// </summary>
    public class DurableFunctionStatus
    {
        public string Name { get; set; }
        public string InstanceId { get; set; }
        public string RuntimeStatus { get; set; }
    }

    /// <summary>
    /// Manages Durable Function history and status
    /// </summary>
    public class Awaiter
    {
        public int Port { get; private set; }
        public string BaseUrl { get; private set; }
        private static HttpClient client;

        /// <summary>
        /// Constructs a new Awaiter with a base URL of http://localhost:{port}
        /// </summary>
        /// <param name="port">Port the function host is expected to be running on</param>
        public Awaiter(int port)
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
        /// Waits for the selected Durable Function instance to finish execution (i.e., its status becomes Completed, Failed, or Terminated)
        /// </summary>
        /// <param name="instanceId">ID of the Durable Function, probably from a GetAllFunctionStatuses call</param>
        /// <param name="maxMilliseconds">Maximum time to wait for the instance to finish before returning</param>
        public async Task WaitForInstance(string instanceId, int maxMilliseconds = 60000)
        {
            var finishedStates = new string[] { "Completed", "Failed", "Terminated" };

            var totalTime = 0;
            var sleepTime = 1000;

            var statuses = await GetAllFunctionStatuses();
            while (totalTime < maxMilliseconds &&
                   !statuses.Any(s => s.InstanceId == instanceId && finishedStates.Contains(s.RuntimeStatus)))
            {
                Thread.Sleep(sleepTime);
                totalTime += sleepTime;
                statuses = await GetAllFunctionStatuses();
            }
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
        public async Task PurgeInstanceHistoriesAsync()
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
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine("No instances found to delete");
            }
        }
    }
}