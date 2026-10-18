using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Novolis.Avalonia.Speech;

/// <summary>Protects manual Azure Speech credentials for portable backups.</summary>
public static class AzureSpeechCredentialBundle
{
    public const string Schema = "novolis.azure-speech-credential-bundle";
    public const int Version = 1;
    public const int MinimumPassphraseLength = 8;

    const string KeyDerivation = "PBKDF2-SHA256";
    const int Iterations = 600_000;
    const int SaltBytes = 16;
    const int NonceBytes = 12;
    const int KeyBytes = 32;
    const int TagBytes = 16;

    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    /// <summary>Writes an encrypted manual subscription-key bundle.</summary>
    public static async Task WriteAsync(
        Stream destination,
        AzureSpeechSetup setup,
        string passphrase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(setup);
        ValidatePassphrase(passphrase);
        setup.Validate();
        if (setup.EffectiveCredentialSource != AzureSpeechCredentialSource.Manual
            || setup.AuthenticationMode != AzureSpeechAuthenticationMode.ApiKey
            || string.IsNullOrWhiteSpace(setup.ApiKey))
        {
            throw new InvalidOperationException(
                "Only a manual subscription-key setup can be exported as credentials.");
        }

        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new Payload
            {
                Authentication = "subscriptionKey",
                Endpoint = setup.Endpoint.AbsoluteUri,
                SubscriptionKey = setup.ApiKey,
                VoiceName = setup.VoiceName,
                Locale = setup.Locale,
            },
            JsonOptions);
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var key = DeriveKey(passphrase, salt, Iterations);
        var ciphertext = new byte[payload.Length];
        var tag = new byte[TagBytes];
        using (var aes = new AesGcm(key, TagBytes))
        {
            aes.Encrypt(nonce, payload, ciphertext, tag, AssociatedData());
        }

        var envelope = new Envelope
        {
            Schema = Schema,
            Version = Version,
            KeyDerivation = KeyDerivation,
            Iterations = Iterations,
            Salt = Convert.ToBase64String(salt),
            Nonce = Convert.ToBase64String(nonce),
            Tag = Convert.ToBase64String(tag),
            Ciphertext = Convert.ToBase64String(ciphertext),
        };
        await JsonSerializer.SerializeAsync(
                destination,
                envelope,
                JsonOptions,
                cancellationToken)
            .ConfigureAwait(false);
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads and decrypts a manual subscription-key bundle.</summary>
    public static async Task<AzureSpeechSetup> ReadAsync(
        Stream source,
        string passphrase,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        ValidatePassphrase(passphrase);

        Envelope? envelope;
        try
        {
            envelope = await JsonSerializer.DeserializeAsync<Envelope>(
                    source,
                    JsonOptions,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The encrypted Azure credentials file is not valid JSON.",
                exception);
        }

        if (envelope is null
            || !string.Equals(envelope.Schema, Schema, StringComparison.Ordinal)
            || envelope.Version != Version
            || !string.Equals(envelope.KeyDerivation, KeyDerivation, StringComparison.Ordinal)
            || envelope.Iterations is < 100_000 or > 2_000_000)
        {
            throw new InvalidOperationException(
                "The encrypted Azure credentials file is unsupported.");
        }

        byte[] salt;
        byte[] nonce;
        byte[] tag;
        byte[] ciphertext;
        try
        {
            salt = Convert.FromBase64String(envelope.Salt ?? string.Empty);
            nonce = Convert.FromBase64String(envelope.Nonce ?? string.Empty);
            tag = Convert.FromBase64String(envelope.Tag ?? string.Empty);
            ciphertext = Convert.FromBase64String(envelope.Ciphertext ?? string.Empty);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "The encrypted Azure credentials file is incomplete.",
                exception);
        }

        if (salt.Length != SaltBytes
            || nonce.Length != NonceBytes
            || tag.Length != TagBytes
            || ciphertext.Length == 0)
        {
            throw new InvalidOperationException(
                "The encrypted Azure credentials file is incomplete.");
        }

        var plaintext = new byte[ciphertext.Length];
        try
        {
            var key = DeriveKey(passphrase, salt, envelope.Iterations);
            using var aes = new AesGcm(key, TagBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData());
        }
        catch (CryptographicException exception)
        {
            throw new InvalidOperationException(
                "The credentials could not be decrypted. Check the export passphrase.",
                exception);
        }

        Payload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<Payload>(plaintext, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The decrypted Azure credentials are invalid.",
                exception);
        }

        if (payload is null
            || !string.Equals(
                payload.Authentication,
                "subscriptionKey",
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The encrypted bundle does not contain a manual subscription key.");
        }

        try
        {
            var setup = new AzureSpeechSetup
            {
                Endpoint = new Uri(payload.Endpoint ?? string.Empty, UriKind.Absolute),
                CredentialSource = AzureSpeechCredentialSource.Manual,
                AuthenticationMode = AzureSpeechAuthenticationMode.ApiKey,
                ApiKey = payload.SubscriptionKey,
                VoiceName = payload.VoiceName ?? "en-US-AvaMultilingualNeural",
                Locale = payload.Locale ?? "en-US",
            };
            setup.Validate();
            return setup;
        }
        catch (UriFormatException exception)
        {
            throw new InvalidOperationException(
                "The decrypted Azure credentials contain an invalid endpoint.",
                exception);
        }
    }

    static void ValidatePassphrase(string passphrase)
    {
        if (string.IsNullOrWhiteSpace(passphrase)
            || passphrase.Length < MinimumPassphraseLength)
        {
            throw new InvalidOperationException(
                $"Use an export passphrase with at least {MinimumPassphraseLength} characters.");
        }
    }

    static byte[] DeriveKey(string passphrase, byte[] salt, int iterations) =>
        Rfc2898DeriveBytes.Pbkdf2(
            passphrase,
            salt,
            iterations,
            HashAlgorithmName.SHA256,
            KeyBytes);

    static byte[] AssociatedData() =>
        Encoding.UTF8.GetBytes($"{Schema}:{Version}");

    sealed class Envelope
    {
        [JsonPropertyName("schema")]
        public string? Schema { get; init; }

        [JsonPropertyName("version")]
        public int Version { get; init; }

        [JsonPropertyName("keyDerivation")]
        public string? KeyDerivation { get; init; }

        [JsonPropertyName("iterations")]
        public int Iterations { get; init; }

        [JsonPropertyName("salt")]
        public string? Salt { get; init; }

        [JsonPropertyName("nonce")]
        public string? Nonce { get; init; }

        [JsonPropertyName("tag")]
        public string? Tag { get; init; }

        [JsonPropertyName("ciphertext")]
        public string? Ciphertext { get; init; }
    }

    sealed class Payload
    {
        [JsonPropertyName("authentication")]
        public string? Authentication { get; init; }

        [JsonPropertyName("endpoint")]
        public string? Endpoint { get; init; }

        [JsonPropertyName("subscriptionKey")]
        public string? SubscriptionKey { get; init; }

        [JsonPropertyName("voiceName")]
        public string? VoiceName { get; init; }

        [JsonPropertyName("locale")]
        public string? Locale { get; init; }
    }
}
