using Bunit;
using KnowHowToAI.Server.Web.State;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace KnowHowToAI.Web.Tests.State;

[Trait("Category", "Unit")]
public sealed class BrowserRoleStorageServiceTests : BunitContext
{
    [Fact]
    public async Task GetLastRoleIdAsync_ReturnsStoredValue()
    {
        JSInterop.Setup<string?>("localStorage.getItem", BrowserRoleStorageService.StorageKey)
            .SetResult("Developer");

        var service = new BrowserRoleStorageService(JSInterop.JSRuntime, NullLogger<BrowserRoleStorageService>.Instance);
        var result = await service.GetLastRoleIdAsync();

        Assert.Equal("Developer", result);
    }

    [Fact]
    public async Task GetLastRoleIdAsync_WhenJsThrowsInvalidOperation_ReturnsNull()
    {
        JSInterop.Setup<string?>("localStorage.getItem", BrowserRoleStorageService.StorageKey)
            .SetException(new InvalidOperationException("Prerendering not supported"));

        var service = new BrowserRoleStorageService(JSInterop.JSRuntime, NullLogger<BrowserRoleStorageService>.Instance);
        var result = await service.GetLastRoleIdAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task GetLastRoleIdAsync_WhenJsThrowsJsException_ReturnsNull()
    {
        JSInterop.Setup<string?>("localStorage.getItem", BrowserRoleStorageService.StorageKey)
            .SetException(new JSException("SecurityError"));

        var service = new BrowserRoleStorageService(JSInterop.JSRuntime, NullLogger<BrowserRoleStorageService>.Instance);
        var result = await service.GetLastRoleIdAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SetLastRoleIdAsync_SetsValueInLocalStorage()
    {
        var invocation = JSInterop.SetupVoid("localStorage.setItem", BrowserRoleStorageService.StorageKey, "Admin");
        invocation.SetVoidResult();

        var service = new BrowserRoleStorageService(JSInterop.JSRuntime, NullLogger<BrowserRoleStorageService>.Instance);
        await service.SetLastRoleIdAsync("Admin");

        invocation.VerifyInvoke("localStorage.setItem");
    }

    [Fact]
    public async Task SetLastRoleIdAsync_WhenJsThrowsInvalidOperation_DoesNotThrow()
    {
        JSInterop.SetupVoid("localStorage.setItem", BrowserRoleStorageService.StorageKey, "Admin")
            .SetException(new InvalidOperationException("Prerendering not supported"));

        var service = new BrowserRoleStorageService(JSInterop.JSRuntime, NullLogger<BrowserRoleStorageService>.Instance);
        await service.SetLastRoleIdAsync("Admin");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetLastRoleIdAsync_WithNullOrWhitespace_ThrowsArgumentException(string? invalidRoleId)
    {
        var service = new BrowserRoleStorageService(JSInterop.JSRuntime, NullLogger<BrowserRoleStorageService>.Instance);
        await Assert.ThrowsAnyAsync<ArgumentException>(() => service.SetLastRoleIdAsync(invalidRoleId!).AsTask());
    }
}
