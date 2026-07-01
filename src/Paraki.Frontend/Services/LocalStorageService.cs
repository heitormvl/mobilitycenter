using System.Text.Json;
using Microsoft.JSInterop;
using Paraki.RazorLib.Interfaces;

namespace Paraki.Frontend.Services;

/// <summary>
/// Implementação WASM de <see cref="ILocalStorageService"/> usando
/// <c>window.localStorage</c> via JS interop.
/// </summary>
public class LocalStorageService(IJSRuntime js) : ILocalStorageService
{
    public Task SetItemAsync(string key, string value) =>
        js.InvokeVoidAsync("localStorage.setItem", key, value).AsTask();

    public Task SetItemAsync<T>(string key, T value) =>
        js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value)).AsTask();

    public async Task<string?> GetItemAsync(string key) =>
        await js.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public Task RemoveItemAsync(string key) =>
        js.InvokeVoidAsync("localStorage.removeItem", key).AsTask();
}
