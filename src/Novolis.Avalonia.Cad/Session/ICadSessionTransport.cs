namespace Novolis.Avalonia.Cad.Session;

public interface ICadSessionTransport
{
    string Kind { get; }

    ValueTask StartAsync(CancellationToken cancellationToken = default);

    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
