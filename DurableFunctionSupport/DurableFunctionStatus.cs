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
}