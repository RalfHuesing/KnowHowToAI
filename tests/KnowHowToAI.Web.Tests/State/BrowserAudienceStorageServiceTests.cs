using Bunit;
using KnowHowToAI.Server.Web.State;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class BrowserAudienceStorageServiceTests : BunitContext
{
    [Fact]
    public async Task GetLastAudienceIdAsync_ReturnsStoredValue()
    {
        JSInterop.Setup<string?>("localStorage.getItem", BrowserAudienceStorageService.StorageKey)
            .SetResult("Developer");

        var service = new BrowserAudienceStorageService(JSInterop.JSRuntime, NullLogger<BrowserAudienceStorageService>.Instance);
        var result = await service.GetLastAudienceIdAsync();

        Assert.Equal("Developer", result);
    }

    [Fact]
    public async Task GetLastAudienceIdAsync_WhenJsThrowsInvalidOperation_ReturnsNull()
    {
        JSInterop.Setup<string?>("localStorage.getItem", BrowserAudienceStorageService.StorageKey)
            .SetException(new InvalidOperationException("Prerendering not supported"));

        var service = new BrowserAudienceStorageService(JSInterop.JSRuntime, NullLogger<BrowserAudienceStorageService>.Instance);
        var result = await service.GetLastAudienceIdAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastAudienceIdAsync_WhenJsThrowsJsException_ReturnsNull()
    {
        JSInterop.Setup<string?>("localStorage.getItem", BrowserAudienceStorageService.StorageKey)
            .SetException(new JSException("SecurityError"));

        var service = new BrowserAudienceStorageService(JSInterop.JSRuntime, NullLogger<BrowserAudienceStorageService>.Instance);
        var result = await service.GetLastAudienceIdAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SetLastAudienceIdAsync_SetsValueInLocalStorage()
    {
        var invocation = JSInterop.SetupVoid("localStorage.setItem", BrowserAudienceStorageService.StorageKey, "Admin");
        invocation.SetVoidResult();

        var service = new BrowserAudienceStorageService(JSInterop.JSRuntime, NullLogger<BrowserAudienceStorageService>.Instance);
        await service.SetLastAudienceIdAsync("Admin");

        invocation.VerifyInvoke("localStorage.setItem");
    }

    [Fact]
    public async Task SetLastAudienceIdAsync_WhenJsThrowsInvalidOperation_DoesNotThrow()
    {
        JSInterop.SetupVoid("localStorage.setItem", BrowserAudienceStorageService.StorageKey, "Admin")
            .SetException(new InvalidOperationException("Prerendering not supported"));

        var service = new BrowserAudienceStorageService(JSInterop.JSRuntime, NullLogger<BrowserAudienceStorageService>.Instance);
        await service.SetLastAudienceIdAsync("Admin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetLastAudienceIdAsync_WithNullOrWhitespace_ThrowsArgumentException(string? invalidAudienceId)
    {
        var service = new BrowserAudienceStorageService(JSInterop.JSRuntime, NullLogger<BrowserAudienceStorageService>.Instance);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.SetLastAudienceIdAsync(invalidAudienceId!).AsTask());
    }
}
