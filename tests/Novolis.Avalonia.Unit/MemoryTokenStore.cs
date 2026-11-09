using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Unit;

sealed class MemoryTokenStore : ISecureTokenStore
{
    readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> Values => _values.Values;

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_values.GetValueOrDefault(key));

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _values[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _values.Remove(key);
        return Task.CompletedTask;
    }
}
