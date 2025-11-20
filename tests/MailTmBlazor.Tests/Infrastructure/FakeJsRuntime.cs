using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace MailTmBlazor.Tests.Infrastructure;

internal sealed class FakeJsRuntime : IJSRuntime
{
    private readonly ConcurrentDictionary<string, string?> _storage = new();

    public IReadOnlyDictionary<string, string?> Storage => _storage;

    public void Seed(string key, string? value) => _storage[key] = value;

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        switch (identifier)
        {
            case "localStorage.getItem":
                var getKey = GetKey(args);
                _storage.TryGetValue(getKey, out var stored);
                return ValueTask.FromResult(Cast<TValue>(stored));

            case "localStorage.setItem":
                var setKey = GetKey(args);
                var value = args is { Length: > 1 } ? args![1]?.ToString() : null;
                _storage[setKey] = value;
                return ValueTask.FromResult(default(TValue)!);

            case "localStorage.removeItem":
                var removeKey = GetKey(args);
                _storage.TryRemove(removeKey, out _);
                return ValueTask.FromResult(default(TValue)!);

            default:
                throw new NotSupportedException(identifier);
        }
    }

    private static string GetKey(object?[]? args) =>
        args is { Length: > 0 } && args[0] is not null
            ? args[0]!.ToString()!
            : string.Empty;

    private static TValue Cast<TValue>(string? value)
    {
        if (value is null)
        {
            return default!;
        }

        return (TValue)(object)value;
    }
}
