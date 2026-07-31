using System.Security.Cryptography;
using System.Text;
using Android.Content;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Novolis.Avalonia.Mobile;

namespace Novolis.Avalonia.Mobile.Android;

/// <summary>
/// Encrypts secrets with an AES key in the Android Keystore and stores ciphertext
/// in private SharedPreferences.
/// </summary>
public sealed class AndroidSecureTokenStore : ISecureTokenStore
{
    const string PrefsName = "novolis_secure_tokens";
    const string KeyAlias = "novolis.mobile.token.aes";
    const string AndroidKeyStore = "AndroidKeyStore";

    readonly Context _context;
    readonly Lazy<ISharedPreferences> _prefs;

    /// <summary>Creates a store using <see cref="Application.Context"/>.</summary>
    public AndroidSecureTokenStore()
        : this(global::Android.App.Application.Context
               ?? throw new InvalidOperationException("Android Application.Context is not available."))
    {
    }

    /// <summary>Creates a store for the given context.</summary>
    public AndroidSecureTokenStore(Context context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context.ApplicationContext ?? context;
        _prefs = new Lazy<ISharedPreferences>(() =>
            _context.GetSharedPreferences(PrefsName, FileCreationMode.Private)
            ?? throw new InvalidOperationException("GetSharedPreferences returned null."));
        EnsureKey();
    }

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var packed = _prefs.Value.GetString(key, null);
        if (string.IsNullOrEmpty(packed))
            return Task.FromResult<string?>(null);
        return Task.FromResult<string?>(Decrypt(packed));
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _prefs.Value.Edit()?.PutString(key, Encrypt(value))?.Apply();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _prefs.Value.Edit()?.Remove(key)?.Apply();
        return Task.CompletedTask;
    }

    static void EnsureKey()
    {
        var ks = KeyStore.GetInstance(AndroidKeyStore)
            ?? throw new InvalidOperationException("AndroidKeyStore unavailable.");
        ks.Load(null);
        if (ks.ContainsAlias(KeyAlias))
            return;

        var generator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, AndroidKeyStore)
            ?? throw new InvalidOperationException("AES KeyGenerator unavailable.");
        var spec = new KeyGenParameterSpec.Builder(
                KeyAlias,
                KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
            .SetBlockModes(KeyProperties.BlockModeGcm)
            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
            .SetKeySize(256)
            .Build();
        generator.Init(spec);
        generator.GenerateKey();
    }

    static IKey GetSecretKey()
    {
        var ks = KeyStore.GetInstance(AndroidKeyStore)
            ?? throw new InvalidOperationException("AndroidKeyStore unavailable.");
        ks.Load(null);
        return ks.GetKey(KeyAlias, null)
            ?? throw new InvalidOperationException($"Keystore key '{KeyAlias}' missing.");
    }

    static string Encrypt(string plaintext)
    {
        var cipher = Cipher.GetInstance("AES/GCM/NoPadding")
            ?? throw new InvalidOperationException("AES/GCM cipher unavailable.");
        cipher.Init(Javax.Crypto.CipherMode.EncryptMode, GetSecretKey());
        var iv = cipher.GetIV() ?? throw new InvalidOperationException("GCM IV missing.");
        var cipherBytes = cipher.DoFinal(Encoding.UTF8.GetBytes(plaintext))
            ?? throw new InvalidOperationException("Encrypt returned null.");
        return Convert.ToBase64String(iv) + ":" + Convert.ToBase64String(cipherBytes);
    }

    static string Decrypt(string packed)
    {
        var parts = packed.Split(':', 2);
        if (parts.Length != 2)
            throw new CryptographicException("Invalid packed ciphertext.");
        var iv = Convert.FromBase64String(parts[0]);
        var cipherBytes = Convert.FromBase64String(parts[1]);
        var cipher = Cipher.GetInstance("AES/GCM/NoPadding")
            ?? throw new InvalidOperationException("AES/GCM cipher unavailable.");
        cipher.Init(Javax.Crypto.CipherMode.DecryptMode, GetSecretKey(), new GCMParameterSpec(128, iv));
        var plain = cipher.DoFinal(cipherBytes)
            ?? throw new InvalidOperationException("Decrypt returned null.");
        return Encoding.UTF8.GetString(plain);
    }
}
