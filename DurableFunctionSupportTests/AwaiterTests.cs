using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Hids.DurableFunctionSupport;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace AwaiterTests
{
    public class WaitForInstanceTests : IDisposable
    {
        WireMockServer server;

        public WaitForInstanceTests()
        {
            server = WireMockServer.Start();
        }

        public void Dispose()
        {
            server.Stop();
        }

        [Theory]
        [InlineData("Completed")]
        [InlineData("Terminated")]
        [InlineData("Failed")]
        public void ReturnsImmediatelyForFinishedTask(string status)
        {
            server.Reset();

            server
                .Given(
                    Request
                        .Create()
                        .WithPath("/runtime/webhooks/durabletask/instances")
                        .UsingGet()
                )
                .RespondWith(
                    Response
                        .Create()
                        .WithStatusCode(200)
                        .WithBody(@"[
                            {
                                ""name"": ""MyDurableOrchestrator"",
                                ""instanceId"": ""93581509a6898c110182fedbeef29616"",
                                ""runtimeStatus"": """ + status + @""",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:46Z""
                            }
                        ]")
                );

            var client = new DurableFunctionClient(server.Ports[0]);

            Func<Task> waiting = () => Awaiter.WaitForInstance(client, "93581509a6898c110182fedbeef29616");
            waiting.Should().CompleteWithin(1000.Milliseconds());
        }

        [Theory]
        [InlineData("Completed")]
        [InlineData("Terminated")]
        [InlineData("Failed")]
        public async Task ReturnsWhenFunctionCompletesAsync(string status)
        {
            server.Reset();

            server
                .Given(
                    Request
                        .Create()
                        .WithPath("/runtime/webhooks/durabletask/instances")
                        .UsingGet()
                )
                .AtPriority(100)
                .RespondWith(
                    Response
                        .Create()
                        .WithStatusCode(200)
                        .WithBody(@"[
                            {
                                ""name"": ""MyDurableOrchestrator"",
                                ""instanceId"": ""93581509a6898c110182fedbeef29616"",
                                ""runtimeStatus"": ""Running"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:46Z""
                            }
                        ]")
                );

            var client = new DurableFunctionClient(server.Ports[0]);

            Func<Task> waiting = () => Awaiter.WaitForInstance(client, "93581509a6898c110182fedbeef29616");

            // Intentionally do not await - start WaitForInstance
            var task = waiting.Should().CompleteWithinAsync(6000.Milliseconds());

            // After 4 seconds, the function goes from Running to a completed status
            Thread.Sleep(4000);
            server
                .Given(
                    Request
                        .Create()
                        .WithPath("/runtime/webhooks/durabletask/instances")
                        .UsingGet()
                )
                .AtPriority(1) // Use priorities to ensure the right response is sent
                .RespondWith(
                    Response
                        .Create()
                        .WithStatusCode(200)
                        .WithBody(@"[
                            {
                                ""name"": ""MyDurableOrchestrator"",
                                ""instanceId"": ""93581509a6898c110182fedbeef29616"",
                                ""runtimeStatus"": """ + status + @""",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:50Z""
                            }
                        ]")
                ); 

            await task;  // Now allow the task to complete
            server.LogEntries.Should().HaveCountGreaterOrEqualTo(4); // Should have made multiple requests for status   
        }

        [Fact]
        public void TimesOutWhenDurableFunctionDoesNotComplete()
        {
            server.Reset();

            server
                .Given(
                    Request
                        .Create()
                        .WithPath("/runtime/webhooks/durabletask/instances")
                        .UsingGet()
                )
                .RespondWith(
                    Response
                        .Create()
                        .WithStatusCode(200)
                        .WithBody(@"[
                            {
                                ""name"": ""MyDurableOrchestrator"",
                                ""instanceId"": ""93581509a6898c110182fedbeef29616"",
                                ""runtimeStatus"": ""Definitely Not A Completed Sort of Status"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:46Z""
                            }
                        ]")
                );

            var client = new DurableFunctionClient(server.Ports[0]);

            Func<Task> waiting = () => Awaiter.WaitForInstance(client, "93581509a6898c110182fedbeef29616", 5000);
            waiting.Should().CompleteWithin(5500.Milliseconds());
        }
    }
}
