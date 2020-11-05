using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Hids.DurableFunctionSupport;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace AwaiterTests
{
    public class GetStatusTests : IDisposable
    {
        WireMockServer server;

        public GetStatusTests()
        {
            server = WireMockServer.Start();
        }

        public void Dispose()
        {
            server.Stop();
        }

        [Fact]
        public async Task CanGetEmptyStatusListAsync()
        {
            server.Reset();

            var expected = new List<DurableFunctionStatus>();

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
                        .WithBody("[]")
                );

            Awaiter awaiter = new Awaiter(server.Ports[0]);
            var actual = await awaiter.GetAllFunctionStatuses();

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task CanGetListOfStatusWithOneItem()
        {
            server.Reset();

            var expected = new List<DurableFunctionStatus>()
            {
                new DurableFunctionStatus()
                {
                    Name = "MyDurableOrchestrator",
                    InstanceId = "39432fc3815f4900a0a4357febec5012",
                    RuntimeStatus = "Running"
                }
            };

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
                                ""instanceId"": ""39432fc3815f4900a0a4357febec5012"",
                                ""runtimeStatus"": ""Running"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-04T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-04T21:44:46Z""
                            }
                        ]")
                );

            Awaiter awaiter = new Awaiter(server.Ports[0]);
            var actual = await awaiter.GetAllFunctionStatuses();

            actual.Should().BeEquivalentTo(expected);
        }

        [Fact]
        public async Task CanGetListOfStatusWithMultipleItems()
        {
            server.Reset();

            var expected = new List<DurableFunctionStatus>()
            {
                new DurableFunctionStatus()
                {
                    Name = "MyDurableOrchestrator",
                    InstanceId = "39432fc3815f4900a0a4357febec5012",
                    RuntimeStatus = "Running"
                },
                new DurableFunctionStatus()
                {
                    Name = "MyDurableOrchestrator",
                    InstanceId = "93581509a6898c110182fedbeef29616",
                    RuntimeStatus = "Terminated"
                },
            };

            server.ResetMappings();
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
                                ""instanceId"": ""39432fc3815f4900a0a4357febec5012"",
                                ""runtimeStatus"": ""Running"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-04T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-04T21:44:46Z""
                            },
                            {
                                ""name"": ""MyDurableOrchestrator"",
                                ""instanceId"": ""93581509a6898c110182fedbeef29616"",
                                ""runtimeStatus"": ""Terminated"",
                                ""input"": [],
                                ""customStatus"": null,
                                ""output"": null,
                                ""createdTime"": ""2020-11-03T21:44:45Z"",
                                ""lastUpdatedTime"": ""2020-11-03T21:44:46Z""
                            }
                        ]")
                );

            Awaiter awaiter = new Awaiter(server.Ports[0]);
            var actual = await awaiter.GetAllFunctionStatuses();

            actual.Should().BeEquivalentTo(expected);
        }
    }
}
