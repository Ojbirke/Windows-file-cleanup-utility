using System.IO;

namespace StoreFiler;

/// <summary>En fil funnet under skanning, med rådata for sortering.</summary>
public class FileItem
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public required string Directory { get; init; }
    public long Size { get; init; }
    public DateTime Modified { get; init; }
    public string Extension { get; init; } = "";

    /// <summary>Lokal trygghetsanalyse, fylt inn under skanning.</summary>
    public AnalysisResult? Analysis { get; set; }

    public string TypeDisplay => string.IsNullOrEmpty(Extension) ? "Fil" : Extension.TrimStart('.').ToUpperInvariant() + "-fil";

    public static FileItem FromInfo(FileInfo fi) => new()
    {
        Name = fi.Name,
        FullPath = fi.FullName,
        Directory = fi.DirectoryName ?? "",
        Size = fi.Length,
        Modified = fi.LastWriteTime,
        Extension = fi.Extension,
    };
}
