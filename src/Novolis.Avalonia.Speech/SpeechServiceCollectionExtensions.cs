using System.Text.Json;
using Azure;
using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Novolis.Audio.Voice;
using Novolis.Audio.Voice.AzureSpeech;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Speech;

/// <summary>Dependency-injection registration for the application speech front.</summary>
public static class SpeechServiceCollectionExtensions
{
    /// <summary>Registers <see cref="SpeechFront"/>.</summary>
    public static IServiceCollection AddNovolisSpeech(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<SpeechFront>();
        return services;
    }
}
