namespace GameTeam.Infrastructure.Configuration;

/// <summary>
/// The immutable bundle version label convention <c>config@vN</c> (ADR-005) in one place — used by the
/// builder, store, publisher and snapshot so the string form is never re-spelled inconsistently.
/// </summary>
public static class ConfigVersionLabel
{
    /// <summary>Label prefix.</summary>
    public const string Prefix = "config@v";

    /// <summary>Compose the label for a version number, e.g. <c>1 → "config@v1"</c>.</summary>
    public static string For(int version) => $"{Prefix}{version}";

    /// <summary>Parse the version number out of a label, or <c>0</c> when it is missing/malformed.</summary>
    public static int Number(string? label)
    {
        if (string.IsNullOrEmpty(label) || !label.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return 0;
        }

        return int.TryParse(label.AsSpan(Prefix.Length), out int number) ? number : 0;
    }
}
