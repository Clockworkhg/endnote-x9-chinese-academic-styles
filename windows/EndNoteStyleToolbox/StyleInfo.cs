using System.Text.Json.Serialization;

namespace EndNoteStyleToolbox;

internal sealed class StyleInfo
{
    [JsonPropertyName("number")]
    public int Number { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("display_name")]
    public string DisplayName { get; init; } = string.Empty;

    [JsonPropertyName("filename")]
    public string Filename { get; init; } = string.Empty;

    [JsonPropertyName("family")]
    public string Family { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; init; } = string.Empty;

    [JsonPropertyName("source_url")]
    public string SourceUrl { get; init; } = string.Empty;

    [JsonPropertyName("examples")]
    public string[] Examples { get; init; } = [];

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;
}

internal enum InstalledState
{
    NotInstalled,
    Installed,
    DifferentVersion
}

internal sealed record OperationResult(string Message, string? BackupDirectory = null);
