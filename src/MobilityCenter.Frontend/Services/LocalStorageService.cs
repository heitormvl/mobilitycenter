using System.Text.Json;
using Microsoft.JSInterop;

namespace MobilityCenter.Frontend.Services;

public class LocalStorageService(IJSRuntime js)
{
    public ValueTask SetItemAsync(string key, string value) =>
        js.InvokeVoidAsync("localStorage.setItem", key, value);

    public async Task SetItemAsync<T>(string key, T value) =>
        await js.InvokeVoidAsync("localStorage.setItem", key, JsonSerializer.Serialize(value));

    public ValueTask<string?> GetItemAsync(string key) =>
        js.InvokeAsync<string?>("localStorage.getItem", key);

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await js.InvokeAsync<string?>("localStorage.getItem", key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public ValueTask RemoveItemAsync(string key) =>
        js.InvokeVoidAsync("localStorage.removeItem", key);
}
