namespace Paraki.RazorLib.Interfaces;

/// <summary>
/// Abstração de armazenamento chave-valor persistente. No WASM é implementada
/// via <c>window.localStorage</c>; no MAUI via SecureStorage/Preferences.
/// </summary>
public interface ILocalStorageService
{
    Task SetItemAsync(string key, string value);
    Task SetItemAsync<T>(string key, T value);
    Task<string?> GetItemAsync(string key);
    Task<T?> GetItemAsync<T>(string key);
    Task RemoveItemAsync(string key);
}
