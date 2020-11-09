namespace Hids.DurableFunctionSupport
{
    /// <summary>
    /// Contains information about a Durable Function execution
    /// </summary>
    public class DurableFunctionStatus
    {
        /// <summary>
        /// Name of the orchestrator function
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Instance of the orchestrator function execution
        /// </summary>
        public string InstanceId { get; set; }

        /// <summary>
        /// Status of the execution as reported by the function host
        /// </summary>
        public string RuntimeStatus { get; set; }
    }
}