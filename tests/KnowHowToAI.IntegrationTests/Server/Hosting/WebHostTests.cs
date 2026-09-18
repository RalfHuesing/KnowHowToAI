using System.Net;
using System.Net.Sockets;
using KnowHowToAI.Server;
using KnowHowToAI.Server.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace KnowHowToAI.IntegrationTests.Server.Hosting;

/// <summary>
/// Belegt die echte Kestrel-Hostgrenze vor dem Ausbau von Browser- oder HTTP-Endpunkten.
/// </summary>
[Trait("Category", "Integration")]
public sealed class WebHostTests
{
    [Theory]
    [InlineData("/mcp")]
    [InlineData("/api")]
    [InlineData("/api/test")]
    [InlineData("/unbekannt")]
    public async Task UnmappedRoutes_ReturnEmptyNotFoundResponses(string path)
    {
        using var application = CreateApplication("http://127.0.0.1:0");

        await application.StartAsync();

        using var client = new HttpClient();
        using var response = await client.GetAsync($"{GetBoundAddress(application)}{path}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        await application.StopAsync();
    }

    [Fact]
    public async Task RootRoute_ReturnsTheBlazorShell()
    {
        using var application = CreateApplication("http://127.0.0.1:0");
        await application.StartAsync();

        using var client = new HttpClient();
        using var response = await client.GetAsync(GetBoundAddress(application));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType!.MediaType!, StringComparison.OrdinalIgnoreCase);
        using var frameworkResponse = await client.GetAsync($"{GetBoundAddress(application)}/_framework/blazor.web.js");
        Assert.Equal(HttpStatusCode.OK, frameworkResponse.StatusCode);
        await application.StopAsync();
    }

    [Theory]
    [InlineData("/api", "GET")]
    [InlineData("/api", "POST")]
    [InlineData("/api", "PUT")]
    [InlineData("/api", "PATCH")]
    [InlineData("/api", "DELETE")]
    [InlineData("/api", "HEAD")]
    [InlineData("/api", "OPTIONS")]
    [InlineData("/api/test", "GET")]
    [InlineData("/api/test", "POST")]
    [InlineData("/api/test", "PUT")]
    [InlineData("/api/test", "PATCH")]
    [InlineData("/api/test", "DELETE")]
    [InlineData("/api/test", "HEAD")]
    [InlineData("/api/test", "OPTIONS")]
    public async Task ReservedApiRoutes_ReturnEmptyNotFoundResponses_ForUsualHttpMethods(string path, string method)
    {
        using var application = CreateApplication("http://127.0.0.1:0");

        await application.StartAsync();

        using var client = new HttpClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), $"{GetBoundAddress(application)}{path}");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());
        await application.StopAsync();
    }

    [Fact]
    public void ReservedApiRoutes_AreMappedExplicitlyForUsualHttpMethods()
    {
        using var application = CreateApplication("http://127.0.0.1:0");

        var reservedEndpoints = ((IEndpointRouteBuilder)application).DataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is "/api" or "/api/{**reservedPath}")
            .OrderBy(endpoint => endpoint.RoutePattern.RawText, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(2, reservedEndpoints.Length);
        Assert.All(reservedEndpoints, endpoint =>
        {
            var httpMethods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>();

            Assert.NotNull(httpMethods);
            Assert.Equal(
            ["GET", "POST", "PUT", "PATCH", "DELETE", "HEAD", "OPTIONS"],
            httpMethods.HttpMethods);
        });
    }

    [Fact]
    public async Task Application_StartsOnDynamicLoopbackPort_WhenMigrationsAreDisabled()
    {
        using var application = CreateApplication("http://127.0.0.1:0");

        await application.StartAsync();

        var address = GetBoundAddress(application);
        using var client = new HttpClient();
        using var response = await client.GetAsync(address);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await application.StopAsync();
    }

    [Fact]
    public async Task ExplicitDynamicUrlOverride_UsesOneOriginPerHostStart()
    {
        using var firstApplication = CreateApplication("http://127.0.0.1:0");
        using var secondApplication = CreateApplication("http://127.0.0.1:0");

        await firstApplication.StartAsync();
        await secondApplication.StartAsync();

        Assert.NotEqual(GetBoundAddress(firstApplication), GetBoundAddress(secondApplication));

        await secondApplication.StopAsync();
        await firstApplication.StopAsync();
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
