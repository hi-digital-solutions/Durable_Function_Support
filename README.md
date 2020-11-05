# Durable Function Support

This is a library to make testing Azure [Durable Functions](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-overview) easier.

The library was extracted from a utility class and could use some clean-up.

## Await a Durable Function

Many of our durable orchestrators are triggered by a "normal" Azure function (e.g., HTTP-triggered or queue-triggered).  In our automated acceptance tests, we've struggled to balance keeping the test runtime short against making sure the test will pass.  This is especially challenging when we move past local development to another environment (e.g., Jenkins, Azure DevOps, GitHub Actions) where tests might run longer.  In a pattern like this...

```csharp
var response = await client.SendAsync(httpTriggerFunctionRequest);  // Trigger the function that starts the orchestrator
Thread.Sleep(15000);
// Assume the orchestrator had time to complete; continue with an assertion or next step
```

...we have found ourselves repeatedly bumping up `Sleep` times to get the tests to pass more reliably.  But 10 seconds times 10 tests adds almost two minutes per test run.  That certainly adds up.

The `Awaiter` (via the `DurableFunctionClient`) uses the [Durable Function API](https://docs.microsoft.com/en-us/azure/azure-functions/durable/durable-functions-http-api) to check status.

(Note:  As of now, the `DurableFunctionClient` assumes you are using the Azure function emulator to [run functions locally](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#start), with a `localhost` URL.)

```csharp
var client = new DurableFunctionClient(portNumber);  // Typically 7071
client.PurgeInstanceHistoriesAsync();  // Not strictly needed, but keeps tests from interfering with each other

var response = await client.SendAsync(httpTriggerFunctionRequest);  // Trigger the function that starts the orchestrator

var instanceId = await Awaiter.WaitForFunction(client)
// When the awaiter returns, it has either timed out or the durable function is done running
```

Specific instances can be awaited using `WaitForInstance`, or orchestrators with a particular name can be awaited using `WaitForInstanceByName`.
