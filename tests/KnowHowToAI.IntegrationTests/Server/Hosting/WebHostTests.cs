using System.Net;
using System.Net.Sockets;
using KnowHowToAI.Server;
using KnowHowToAI.Server.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Belegt die echte Kestrel-Hostgrenze vor dem Ausbau von Browser- oder HTTP-Endpunkten.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WebHostTests
{
    [Fact]
    public async Task Application_StartsOnDynamicLoopbackPort_WhenMigrationsAreDisabled()
    {
        using var application = CreateApplication("http://127.0.0.1:0");

        await application.StartAsync();

        var address = GetBoundAddress(application);
        using var client = new HttpClient();
        using var response = await client.GetAsync(address);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await application.StopAsync();
    }

    [Fact]
    public async Task InvalidOptions_ReturnStartupFailureBeforeKestrelBinds()
    {
        using var application = Program.CreateApplication(
        [
            "--urls", "http://127.0.0.1:0",
            "--KnowHowToAI:Migrations:ApplyOnStartup=false",
            "--KnowHowToAI:Storage:CommandTimeoutSeconds=0"
        ]);

        var exitCode = await Program.RunApplicationAsync(application);

        Assert.Equal(ServerExitCodes.StartupFailure, exitCode);
        Assert.Empty(GetAddresses(application));
    }

    [Fact]
    public async Task Cancellation_StopsKestrelAndReleasesTheBoundPort()
    {
        using var application = CreateApplication("http://127.0.0.1:0");
        var applicationStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        application.Lifetime.ApplicationStarted.Register(applicationStarted.SetResult);

        var runTask = Program.RunApplicationAsync(application);
        await applicationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var port = new Uri(GetBoundAddress(application)).Port;

        application.Lifetime.StopApplication();
        var exitCode = await runTask.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(ServerExitCodes.Success, exitCode);
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
    }

    [Fact]
    public async Task OccupiedPort_ReturnsStartupFailureAndCanBeUsedAfterRelease()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var address = $"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}";

        using (var blockedApplication = CreateApplication(address))
        {
            var exception = await Assert.ThrowsAnyAsync<IOException>(() => blockedApplication.StartAsync());

            Assert.NotNull(exception);
        }

        listener.Stop();
        using var application = CreateApplication(address);
        await application.StartAsync();

        Assert.Equal(address, GetBoundAddress(application));
        await application.StopAsync();
    }

    private static WebApplication CreateApplication(string address) =>
        Program.CreateApplication(
        [
            "--urls", address,
            "--KnowHowToAI:Migrations:ApplyOnStartup=false"
        ]);

    private static string GetBoundAddress(WebApplication application) =>
        Assert.Single(GetAddresses(application));

    private static ICollection<string> GetAddresses(WebApplication application) =>
        application.Services
            .GetRequiredService<IServer>()
            .Features
            .Get<IServerAddressesFeature>()!
            .Addresses;
}
