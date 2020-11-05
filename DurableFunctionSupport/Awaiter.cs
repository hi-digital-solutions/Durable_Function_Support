using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hids.DurableFunctionSupport
{
    /// <summary>
    /// Manages Durable Function history and status
    /// </summary>
    public static class Awaiter
    {
        private readonly static string[] finishedStates = new string[] { "Completed", "Failed", "Terminated" };

        /// <summary>
        /// Waits for the selected Durable Function instance to finish execution (i.e., its status becomes Completed, Failed, or Terminated)
        /// </summary>
        /// <param name="instanceId">ID of the Durable Function, probably from a GetAllFunctionStatuses call</param>
        /// <param name="maxMilliseconds">Maximum time to wait for the instance to finish before returning</param>
        public static async Task WaitForInstance(DurableFunctionClient client, string instanceId, int maxMilliseconds = 60000)
        {
            var totalTime = 0;
            var sleepTime = 1000;

            var statuses = await client.GetAllFunctionStatuses();
            while (totalTime < maxMilliseconds &&
                   !statuses.Any(s => s.InstanceId == instanceId && finishedStates.Contains(s.RuntimeStatus)))
            {
                Thread.Sleep(sleepTime);
                totalTime += sleepTime;
                statuses = await client.GetAllFunctionStatuses();
            }
        }

        /// <summary>
        /// Waits for any instance of the named Durable Function to finish execution (i.e., its status becomes Completed, Failed, or Terminated)
        /// </summary>
        /// <param name="name">Name of the Durable Function</param>
        /// <param name="maxMilliseconds">Maximum time to wait for any instance with that name to finish before returning</param>
        public static async Task WaitForInstanceByName(DurableFunctionClient client, string name, int maxMilliseconds = 60000)
        {
            var finishedStates = new string[] { "Completed", "Failed", "Terminated" };

            var totalTime = 0;
            var sleepTime = 1000;

            var statuses = await client.GetAllFunctionStatuses();
            while (totalTime < maxMilliseconds &&
                   !statuses.Any(s => s.Name == name && finishedStates.Contains(s.RuntimeStatus)))
            {
                Thread.Sleep(sleepTime);
                totalTime += sleepTime;
                statuses = await client.GetAllFunctionStatuses();
            }
        }
    }
}