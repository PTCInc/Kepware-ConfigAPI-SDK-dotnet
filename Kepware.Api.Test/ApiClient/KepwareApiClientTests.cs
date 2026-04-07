using Kepware.Api.ClientHandler;
using Shouldly;
using Xunit;

namespace Kepware.Api.Test.ApiClient;

public class KepwareApiClientTests : TestApiClientBase
{
    [Fact]
    public void KepwareApiClient_Constructor_ShouldExposeDataLoggerHandler()
    {
        _kepwareApiClient.Project.DataLogger.ShouldNotBeNull();
        _kepwareApiClient.Project.DataLogger.ShouldBeOfType<DataLoggerApiHandler>();
    }

    [Fact]
    public void KepwareApiClient_ProjectDataLogger_ShouldNotBeNull()
    {
        // Verify Project.DataLogger is the same handler instance exposed at the constructor level.
        // This confirms the injection chain KepwareApiClient → ProjectApiHandler → DataLoggerApiHandler.
        var handler = _kepwareApiClient.Project.DataLogger;
        handler.ShouldNotBeNull();
        ReferenceEquals(handler, _kepwareApiClient.Project.DataLogger).ShouldBeTrue();
    }

    [Fact]
    public void TestApiClientBase_ShouldWireDataLoggerLoggerMock()
    {
        // Verify that the base class sets up the DataLogger logger mock,
        // ensuring that DataLoggerApiHandler can receive an ILogger<DataLoggerApiHandler>.
        _loggerMockDataLogger.ShouldNotBeNull();
    }
}
