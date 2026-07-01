using System.Text.Json;
using Paraki.RazorLib.Interfaces;

namespace Paraki.Maui.Services;

/// <summary>
/// Implementação MAUI de <see cref="ILocalStorageService"/>. Chaves sensíveis
/// (tokens) vão para o SecureStorage (Keystore/Keychain); o restante para
/// Preferences (armazenamento leve chave-valor).
/// </summary>
public class MauiLocalStorageService : ILocalStorageService
{
    private static readonly HashSet<string> SecureKeys =
        new(StringComparer.Ordinal) { "authToken", "refreshToken" };

    public async Task SetItemAsync(string key, string value)
    {
        if (SecureKeys.Contains(key))
            await SecureStorage.Default.SetAsync(key, value);
        else
            Preferences.Default.Set(key, value);
    }

    public Task SetItemAsync<T>(string key, T value) =>
        SetItemAsync(key, JsonSerializer.Serialize(value));

    public async Task<string?> GetItemAsync(string key)
    {
        if (SecureKeys.Contains(key))
            return await SecureStorage.Default.GetAsync(key);

        return Preferences.Default.ContainsKey(key)
            ? Preferences.Default.Get(key, string.Empty)
            : null;
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var json = await GetItemAsync(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public Task RemoveItemAsync(string key)
    {
        if (SecureKeys.Contains(key))
            SecureStorage.Default.Remove(key);
        else
            Preferences.Default.Remove(key);

        return Task.CompletedTask;
    }
}
