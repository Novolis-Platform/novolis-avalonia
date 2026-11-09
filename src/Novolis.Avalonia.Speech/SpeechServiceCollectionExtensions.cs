using Microsoft.Extensions.DependencyInjection;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Speech;

/// <summary>Dependency-injection registration for the application speech front and workspace.</summary>
public static class SpeechServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="SpeechFront"/> and <see cref="SpeechWorkspace"/>.
    /// Optionally register <see cref="IAzureSpeechResourcePicker"/> in the host for Entra lookup.
    /// </summary>
    public static IServiceCollection AddNovolisSpeech(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<SpeechFront>();
        services.AddSingleton(sp =>
        {
            var front = sp.GetRequiredService<SpeechFront>();
            var picker = sp.GetService<IAzureSpeechResourcePicker>();
            return new SpeechWorkspace(front, picker);
        });
        return services;
    }
}
