namespace Novolis.Avalonia.AssetStudio;

/// <summary>
/// Identifies a button action from the Asset Studio action bar.
/// </summary>
public sealed class AssetStudioActionRequestedEventArgs : EventArgs
{
    public AssetStudioActionRequestedEventArgs(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            throw new ArgumentException(
                "An action command is required.",
                nameof(command));
        }

        Command = command.Trim();
    }

    public string Command { get; }
}
