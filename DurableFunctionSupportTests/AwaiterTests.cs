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

        [Theory]
        [InlineData("Completed")]
        [InlineData("Terminated")]
        [InlineData("Failed")]
        public void WaitsForInstanceByName(string status)
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

            Func<Task> waiting = () => Awaiter.WaitForInstanceByName(client, "MyDurableOrchestrator");
            waiting.Should().CompleteWithin(500.Milliseconds());
            server.LogEntries.Should().HaveCount(1);
        }

        [Theory]
        [InlineData("Completed")]
        [InlineData("Terminated")]
        [InlineData("Failed")]
        public async Task WaitsForInstanceByNameWhenNotInitiallyPresent(string status)
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
                                ""name"": ""NotTheDroidsYou'reLookingFor"",
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

            Func<Task> waiting = () => Awaiter.WaitForInstanceByName(client, "MyDurableOrchestrator");

            // Intentionally do not await - start WaitForInstance
            var task = waiting.Should().CompleteWithinAsync(6000.Milliseconds());

            // After 2 seconds, the function we are looking for starts up (and finishes quickly)
            Thread.Sleep(2000);
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

            await task;  // Allow the task to complete
            server.LogEntries.Should().HaveCountGreaterOrEqualTo(3);
        }

        [Fact]
        public void WaitForFunctionReturnsRightAwayIfAnyFunctionHasCompleted()
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
                                ""runtimeStatus"": ""Completed"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:46Z""
                            }
                        ]")
                );

            var client = new DurableFunctionClient(server.Ports[0]);

            Func<Task<string>> waiting = () => Awaiter.WaitForFunction(client);
            waiting.Should().CompleteWithin(500.Milliseconds()).Which.Should().Be("93581509a6898c110182fedbeef29616");
            server.LogEntries.Should().HaveCountGreaterOrEqualTo(1);
        }

        [Fact]
        public async Task WaitForFunctionStillWorksIfFunctionStartsAfterWaitingStarts()
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
                        .WithBody("[]")
                );

            var client = new DurableFunctionClient(server.Ports[0]);
            Func<Task<string>> waiting = () => Awaiter.WaitForFunction(client);

            // Intentionally do not await
            var task = waiting.Should().CompleteWithinAsync(7000.Milliseconds());

            // After 2 seconds, the function we are looking for starts up
            Thread.Sleep(2000);
            server
                .Given(
                    Request
                        .Create()
                        .WithPath("/runtime/webhooks/durabletask/instances")
                        .UsingGet()
                )
                .AtPriority(50) // Use priorities to ensure the right response is sent
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
                                ""lastUpdatedTime"": ""2020-11-03T21:44:50Z""
                            }
                        ]")
                );

            // After 2 more seconds, the function completes
            Thread.Sleep(2000);
            task.IsCompleted.Should().BeFalse();
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
                                ""runtimeStatus"": ""Completed"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:50Z""
                            }
                        ]")
                );

            var awaitedTask = await task;  // Allow the task to complete
            awaitedTask.Which.Should().Be("93581509a6898c110182fedbeef29616");
            server.LogEntries.Should().HaveCountGreaterOrEqualTo(5);
        }
    }
}
