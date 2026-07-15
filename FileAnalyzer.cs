namespace Byteroom;

public enum Safety { Safe, Caution, Unsafe, Unknown }

/// <summary>Resultatet av en lokal analyse. Tekst hentes fra Loc så den kan bytte språk live.</summary>
public class AnalysisResult
{
    public Safety Safety { get; init; }
    public required string ReasonKey { get; init; }   // f.eks. "log", "cache", "program"
    public string? App { get; init; }                 // Hvilket program filen hører til

    public string Category => Loc.T("Cat_" + ReasonKey);
    public string Explanation => Loc.T("Expl_" + ReasonKey);
    public string Verdict => Loc.T("Verdict_" + Safety);

    public string Glyph => Safety switch
    {
        Safety.Safe => "✓",
        Safety.Caution => "!",
        Safety.Unsafe => "✕",
        _ => "?",
    };

    public string Color => Safety switch
    {
        Safety.Safe => "#1B8A3A",
        Safety.Caution => "#B8860B",
        Safety.Unsafe => "#C62828",
        _ => "#757575",
    };
}

/// <summary>
/// Ren lokal (offline) analyse: gjetter hva en fil er, hvilket program den hører til,
/// og om det er trygt å slette — basert på sti, mappenavn og filtype.
/// </summary>
public static class FileAnalyzer
{
    // Kjente programmer gjenkjent på et mappe-ledd i stien.
    private static readonly (string key, string name)[] KnownApps =
    {
        ("zwift", "Zwift"), ("steam", "Steam"), ("epic games", "Epic Games"), ("epicgames", "Epic Games"),
        ("riot games", "Riot Games"), ("battle.net", "Battle.net"), ("gog", "GOG"),
        ("minecraft", "Minecraft"), ("discord", "Discord"), ("spotify", "Spotify"),
        ("obs-studio", "OBS Studio"), ("obs", "OBS Studio"), ("nvidia", "NVIDIA"), ("amd", "AMD"),
        ("adobe", "Adobe"), ("microsoft teams", "Microsoft Teams"), ("teams", "Microsoft Teams"),
        ("onedrive", "OneDrive"), ("dropbox", "Dropbox"), ("google\\chrome", "Google Chrome"),
        ("chrome", "Google Chrome"), ("mozilla", "Firefox"), ("firefox", "Firefox"),
        ("microsoft\\edge", "Microsoft Edge"), ("edge", "Microsoft Edge"), ("slack", "Slack"),
        ("zoom", "Zoom"), ("unity", "Unity"), ("unreal", "Unreal Engine"), ("jetbrains", "JetBrains"),
        ("visualstudio", "Visual Studio"), ("vscode", "VS Code"), ("code", "VS Code"),
        ("python", "Python"), ("node", "Node.js"), ("docker", "Docker"), ("garmin", "Garmin"),
        ("trainerroad", "TrainerRoad"), ("wahoo", "Wahoo"), ("strava", "Strava"),
    };

    public static AnalysisResult Analyze(FileItem f)
    {
        string lower = f.FullPath.ToLowerInvariant();
        string ext = f.Extension.ToLowerInvariant();
        string name = f.Name.ToLowerInvariant();
        string? app = DetectApp(lower);

        bool Has(string s) => lower.Contains(s);
        AnalysisResult R(Safety s, string reason) => new() { Safety = s, ReasonKey = reason, App = app };

        // 1) Systemkritiske filer som Windows styrer selv
        if (name is "pagefile.sys" or "swapfile.sys" or "hiberfil.sys")
            return R(Safety.Unsafe, "sys_virtualmem");
        if (name is "ntuser.dat" || (ext is ".sys" or ".dll" && Has("\\windows\\")))
            return R(Safety.Unsafe, "sys_file");

        // 2) Papirkurv / systemvolum
        if (Has("$recycle.bin"))
            return R(Safety.Safe, "recyclebin");
        if (Has("system volume information"))
            return R(Safety.Unsafe, "sysrestore");

        // 3) Midlertidige filer
        if (Has("\\temp\\") || Has("\\tmp\\") || Has("\\windows\\temp\\") || Has("\\appdata\\local\\temp\\")
            || ext is ".tmp" or ".temp")
            return R(Safety.Safe, "temp");

        // 4) Buffer / cache
        if (Has("\\cache\\") || Has("\\code cache\\") || Has("gpucache") || Has("shadercache")
            || Has("\\inetcache\\") || Has("\\cache2\\") || Has("\\glcache"))
            return R(Safety.Safe, "cache");

        // 5) Logger og krasjdumper
        if (ext is ".log" || Has("\\logs\\") || Has("\\log\\") || name.Contains("log.txt") || name.StartsWith("log_"))
            return R(Safety.Safe, "log");
        if (ext is ".dmp" or ".mdmp" || Has("crashdumps") || Has("\\minidump"))
            return R(Safety.Safe, "crashdump");

        // 6) Nedlastinger
        if (Has("\\downloads\\") || Has("\\nedlastinger\\"))
        {
            if (ext is ".exe" or ".msi" or ".msix" or ".zip" or ".7z" or ".rar" or ".iso" or ".dmg" or ".pkg")
                return R(Safety.Safe, "download_installer");
            return R(Safety.Caution, "download_file");
        }

        // 7) Installasjonsrester og oppdateringer
        if (ext is ".msi" or ".msp" or ".cab" || Has("\\installer\\") || Has("\\softwaredistribution\\"))
            return R(Safety.Caution, "installer_leftover");

        // 8) Installerte programmer
        if (Has("\\program files\\") || Has("\\program files (x86)\\") || Has("\\programdata\\"))
            return R(Safety.Unsafe, "program");

        // 9) Disk-/systembilder og virtuelle maskiner
        if (ext is ".vhd" or ".vhdx" or ".iso" or ".wim" or ".vmdk")
            return R(Safety.Caution, "diskimage");

        // 10) Sikkerhetskopier / gamle versjoner
        if (ext is ".bak" or ".old" or ".~" || name.EndsWith("~") || name.Contains(".backup"))
            return R(Safety.Safe, "backup");

        // 11) Skysynkronisering
        if (Has("\\onedrive") || Has("\\dropbox") || Has("\\google drive") || Has("\\googledrive"))
            return R(Safety.Caution, "cloud");

        // 12) Mediefiler
        if (ext is ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".mpg" or ".mpeg")
            return R(Safety.Caution, "video");
        if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".heic" or ".raw" or ".cr2" or ".nef")
            return R(Safety.Caution, "image");
        if (ext is ".mp3" or ".flac" or ".wav" or ".aac" or ".m4a" or ".ogg")
            return R(Safety.Caution, "audio");

        // 13) Dokumenter
        bool userDocs = Has("\\documents\\") || Has("\\dokumenter\\") || Has("\\desktop\\") || Has("\\skrivebord\\")
                        || Has("\\pictures\\") || Has("\\bilder\\") || Has("\\videos\\") || Has("\\videoer\\")
                        || Has("\\music\\") || Has("\\musikk\\");
        if (userDocs || ext is ".docx" or ".doc" or ".xlsx" or ".xls" or ".pptx" or ".pdf" or ".txt" or ".csv")
            return R(Safety.Caution, "document");

        // 14) Arkiver
        if (ext is ".zip" or ".7z" or ".rar" or ".gz" or ".tar")
            return R(Safety.Caution, "archive");

        // 15) Ukjent
        return R(Safety.Unknown, "unknown");
    }

    private static string? DetectApp(string lowerPath)
    {
        foreach (var (key, appName) in KnownApps)
            if (lowerPath.Contains(key))
                return appName;
        return null;
    }
}
