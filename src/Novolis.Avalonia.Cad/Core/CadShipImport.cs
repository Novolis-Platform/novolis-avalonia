namespace Novolis.Avalonia.Cad.Core;

/// <summary>
/// Copy a generated ship <c>.cadjson</c> (and matching sidecars) into Draft Studio's ship workspace.
/// Source is <c>NOVOLIS_CAD_SHIP_SOURCE</c> or the newest <c>*.cadjson</c> under
/// <c>%LocalAppData%/Novolis/*/generated/</c>. Filenames from the producer are preserved as file metadata.
/// </summary>
public static class CadShipImport
{
    public const string WorkspaceFolderName = "ship-workspace";
    public const string SourceEnvVar = "NOVOLIS_CAD_SHIP_SOURCE";

    public static string DefaultWorkspaceDirectory(string draftStudioDataRoot) =>
        Path.Combine(draftStudioDataRoot, WorkspaceFolderName);

    /// <summary>
    /// Copies the resolved ship document (+ stem-matching sidecars) into the Draft Studio ship workspace
    /// and returns the destination <c>.cadjson</c> path.
    /// </summary>
    public static string ImportIntoWorkspace(string draftStudioDataRoot, string? sourceDirectoryOrCadjson = null)
    {
        var sourceCadjson = ResolveSourceCadjson(sourceDirectoryOrCadjson)
            ?? throw new FileNotFoundException(
                "No ship .cadjson found. Set NOVOLIS_CAD_SHIP_SOURCE to a .cadjson or generate folder, "
                + "or place one under %LocalAppData%/Novolis/*/generated/.");

        var sourceDir = Path.GetDirectoryName(sourceCadjson)!;
        var fileName = Path.GetFileName(sourceCadjson);
        var stem = Path.GetFileNameWithoutExtension(sourceCadjson);

        var destDir = DefaultWorkspaceDirectory(draftStudioDataRoot);
        Directory.CreateDirectory(destDir);

        var destCadjson = Path.Combine(destDir, fileName);
        File.Copy(sourceCadjson, destCadjson, overwrite: true);

        foreach (var src in Directory.EnumerateFiles(sourceDir, stem + ".*"))
        {
            if (string.Equals(src, sourceCadjson, StringComparison.OrdinalIgnoreCase))
                continue;
            File.Copy(src, Path.Combine(destDir, Path.GetFileName(src)), overwrite: true);
        }

        return destCadjson;
    }

    /// <summary>Prefer an explicit path, else env, else newest generated ship under LocalAppData/Novolis.</summary>
    public static string? ResolveSourceCadjson(string? sourceDirectoryOrCadjson = null)
    {
        if (!string.IsNullOrWhiteSpace(sourceDirectoryOrCadjson))
        {
            if (File.Exists(sourceDirectoryOrCadjson)
                && sourceDirectoryOrCadjson.EndsWith(".cadjson", StringComparison.OrdinalIgnoreCase))
                return sourceDirectoryOrCadjson;

            if (Directory.Exists(sourceDirectoryOrCadjson))
            {
                var inDir = NewestCadjson(sourceDirectoryOrCadjson);
                if (inDir is not null)
                    return inDir;
            }
        }

        var env = Environment.GetEnvironmentVariable(SourceEnvVar);
        if (!string.IsNullOrWhiteSpace(env))
        {
            if (File.Exists(env) && env.EndsWith(".cadjson", StringComparison.OrdinalIgnoreCase))
                return env;
            if (Directory.Exists(env))
            {
                var fromEnv = NewestCadjson(env);
                if (fromEnv is not null)
                    return fromEnv;
            }
        }

        return NewestUnderNovolisGenerated();
    }

    public static string? ResolveOpenPath(string draftStudioDataRoot)
    {
        var generated = ResolveSourceCadjson();
        if (generated is not null)
            return generated;

        var workspace = DefaultWorkspaceDirectory(draftStudioDataRoot);
        if (!Directory.Exists(workspace))
            return null;
        return NewestCadjson(workspace);
    }

    private static string? NewestUnderNovolisGenerated()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Novolis");
        if (!Directory.Exists(root))
            return null;

        string? best = null;
        var bestTime = DateTime.MinValue;
        foreach (var generated in Directory.EnumerateDirectories(root, "generated", SearchOption.AllDirectories))
        {
            var candidate = NewestCadjson(generated);
            if (candidate is null)
                continue;
            var t = File.GetLastWriteTimeUtc(candidate);
            if (t > bestTime)
            {
                bestTime = t;
                best = candidate;
            }
        }

        return best;
    }

    private static string? NewestCadjson(string directory)
    {
        if (!Directory.Exists(directory))
            return null;
        return Directory.EnumerateFiles(directory, "*.cadjson")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
