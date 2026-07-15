using System.IO;

namespace StoreFiler;

public enum Safety { Safe, Caution, Unsafe, Unknown }

/// <summary>Resultatet av en lokal analyse av én fil.</summary>
public class AnalysisResult
{
    public Safety Safety { get; init; }
    public required string Category { get; init; }   // Hva slags fil (kort)
    public string? App { get; init; }                // Hvilket program den hører til
    public required string Explanation { get; init; } // Forklaring + anbefaling

    /// <summary>Enkelt symbol som rendres godt på farget bakgrunn.</summary>
    public string Glyph => Safety switch
    {
        Safety.Safe => "✓",
        Safety.Caution => "!",
        Safety.Unsafe => "✕",
        _ => "?",
    };

    public string Verdict => Safety switch
    {
        Safety.Safe => "Trygg å slette",
        Safety.Caution => "Vær forsiktig",
        Safety.Unsafe => "Bør ikke slettes",
        _ => "Usikker",
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
        string path = f.FullPath;
        string lower = path.ToLowerInvariant();
        string ext = f.Extension.ToLowerInvariant();
        string name = f.Name.ToLowerInvariant();
        string? app = DetectApp(lower);
        string belongs = app is null ? "" : $" fra {app}";

        // Hjelpere
        bool Has(string s) => lower.Contains(s);
        AnalysisResult R(Safety s, string cat, string expl) => new()
        { Safety = s, Category = cat, App = app, Explanation = expl };

        // 1) Systemkritiske filer som Windows styrer selv
        if (name is "pagefile.sys" or "swapfile.sys" or "hiberfil.sys")
            return R(Safety.Unsafe, "Systemfil (virtuelt minne)",
                "Denne styres av Windows for virtuelt minne / dvalemodus. Ikke slett den manuelt — juster den heller via Systeminnstillinger hvis du vil frigjøre plass.");
        if (name is "ntuser.dat" || ext is ".sys" or ".dll" && Has("\\windows\\"))
            return R(Safety.Unsafe, "Systemfil", "Del av Windows. Å slette den kan gjøre systemet ustabilt.");

        // 2) Papirkurv / systemvolum
        if (Has("$recycle.bin"))
            return R(Safety.Safe, "Allerede i papirkurven",
                "Ligger allerede i papirkurven. Tøm papirkurven i Utforsker for å frigjøre plassen helt.");
        if (Has("system volume information"))
            return R(Safety.Unsafe, "Systemgjenoppretting",
                "Brukes av Windows til gjenopprettingspunkter og skyggekopier. Rydd heller via Diskopprydding.");

        // 3) Midlertidige filer
        if (Has("\\temp\\") || Has("\\tmp\\") || Has("\\windows\\temp\\") || Has("\\appdata\\local\\temp\\")
            || ext is ".tmp" or ".temp")
            return R(Safety.Safe, "Midlertidig fil",
                $"Midlertidig fil{belongs}. Slikt kan nesten alltid slettes trygt — programmer lager det på nytt ved behov.");

        // 4) Buffer / cache
        if (Has("\\cache\\") || Has("\\code cache\\") || Has("gpucache") || Has("shadercache")
            || Has("\\inetcache\\") || Has("\\cache2\\") || Has("\\glcache"))
            return R(Safety.Safe, "Buffer / cache",
                $"Bufferdata{belongs} som lagres for å gjøre programmet raskere. Trygt å slette — det bygges opp igjen automatisk (programmet kan starte litt tregere første gang etterpå).");

        // 5) Logger og krasjdumper
        if (ext is ".log" || Has("\\logs\\") || Has("\\log\\") || name.Contains("log.txt") || name.StartsWith("log_"))
            return R(Safety.Safe, "Loggfil",
                $"Loggfil{belongs} som beskriver hva programmet har gjort. Brukes stort sett bare til feilsøking — trygt å slette hvis alt fungerer.");
        if (ext is ".dmp" or ".mdmp" || Has("crashdumps") || Has("\\minidump"))
            return R(Safety.Safe, "Krasjdump",
                $"Feilsøkingsdump{belongs} laget da et program krasjet. Trygt å slette med mindre du aktivt feilsøker en krasj.");

        // 6) Nedlastinger (installasjonsfiler kan ofte fjernes etter bruk)
        if (Has("\\downloads\\") || Has("\\nedlastinger\\"))
        {
            if (ext is ".exe" or ".msi" or ".msix" or ".zip" or ".7z" or ".rar" or ".iso" or ".dmg" or ".pkg")
                return R(Safety.Safe, "Nedlastet installasjonsfil",
                    "Installasjons- eller arkivfil i Nedlastinger. Er programmet allerede installert, trengs ikke denne — trygg å slette. (Ta vare på den hvis du vil kunne installere på nytt uten å laste ned igjen.)");
            return R(Safety.Caution, "Nedlastet fil",
                "En fil du har lastet ned. Sjekk om du fortsatt trenger den før du sletter.");
        }

        // 7) Installasjonsrester og oppdateringer
        if (ext is ".msi" or ".msp" or ".cab" || Has("\\installer\\") || Has("\\softwaredistribution\\"))
            return R(Safety.Caution, "Installasjons-/oppdateringsfil",
                "Installasjons- eller oppdateringsdata for Windows/programmer. Ofte trygt via Diskopprydding, men å slette manuelt kan gi problemer ved reparasjon/avinstallering.");

        // 8) Installerte programmer
        if (Has("\\program files\\") || Has("\\program files (x86)\\") || Has("\\programdata\\"))
            return R(Safety.Unsafe, "Del av et installert program",
                $"Ligger i programmappen{(app is null ? "" : $" til {app}")}. Å slette enkeltfiler her kan ødelegge programmet. Vil du frigjøre plass: avinstaller hele programmet via Innstillinger i stedet.");

        // 9) Disk-/systembilder og virtuelle maskiner
        if (ext is ".vhd" or ".vhdx" or ".iso" or ".wim" or ".vmdk")
            return R(Safety.Caution, "Disk-/systembilde",
                "Stort disk- eller systembilde (virtuell maskin, ISO e.l.). Kan være trygt å slette, men bare hvis du er sikker på at du ikke trenger innholdet.");

        // 10) Sikkerhetskopier / gamle versjoner
        if (ext is ".bak" or ".old" or ".~" || name.EndsWith("~") || name.Contains(".backup"))
            return R(Safety.Safe, "Sikkerhetskopi / gammel versjon",
                "Ser ut som en sikkerhetskopi eller gammel versjon. Vanligvis trygt å slette hvis originalen finnes og fungerer.");

        // 11) Skysynkronisering
        if (Has("\\onedrive") || Has("\\dropbox") || Has("\\google drive") || Has("\\googledrive"))
            return R(Safety.Caution, "Skysynkronisert fil",
                $"Ligger i en skymappe{belongs}. Sletter du den her, forsvinner den også fra skyen på alle enhetene dine. Vær sikker først.");

        // 12) Mediefiler i dine egne mapper
        bool userDocs = Has("\\documents\\") || Has("\\dokumenter\\") || Has("\\desktop\\") || Has("\\skrivebord\\")
                        || Has("\\pictures\\") || Has("\\bilder\\") || Has("\\videos\\") || Has("\\videoer\\")
                        || Has("\\music\\") || Has("\\musikk\\");
        if (ext is ".mp4" or ".mkv" or ".avi" or ".mov" or ".wmv" or ".flv" or ".mpg" or ".mpeg")
            return R(userDocs ? Safety.Caution : Safety.Caution, "Videofil",
                "Videofil — ofte det som tar mest plass. Dette er sannsynligvis ditt eget innhold, så sjekk at du ikke trenger den før du sletter.");
        if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".heic" or ".raw" or ".cr2" or ".nef")
            return R(Safety.Caution, "Bildefil", "Bilde — sannsynligvis ditt eget. Sjekk før du sletter.");
        if (ext is ".mp3" or ".flac" or ".wav" or ".aac" or ".m4a" or ".ogg")
            return R(Safety.Caution, "Lydfil", "Lyd-/musikkfil — sannsynligvis din egen. Sjekk før du sletter.");

        // 13) Dine dokumenter
        if (userDocs || ext is ".docx" or ".doc" or ".xlsx" or ".xls" or ".pptx" or ".pdf" or ".txt" or ".csv")
            return R(Safety.Caution, "Ditt dokument",
                "Ser ut som et personlig dokument. Slett bare hvis du er sikker på at du ikke trenger innholdet.");

        // 14) Arkiver utenfor Nedlastinger
        if (ext is ".zip" or ".7z" or ".rar" or ".gz" or ".tar")
            return R(Safety.Caution, "Arkivfil",
                "Komprimert arkiv. Kan inneholde hva som helst — pakk ut eller sjekk innholdet før du sletter.");

        // 15) Ukjent
        return R(Safety.Unknown, string.IsNullOrEmpty(ext) ? "Ukjent filtype" : $"{ext.TrimStart('.').ToUpperInvariant()}-fil",
            $"Klarte ikke å fastslå sikkert hva denne fila er{belongs}. Åpne mappa i Utforsker og undersøk før du eventuelt sletter.");
    }

    private static string? DetectApp(string lowerPath)
    {
        foreach (var (key, appName) in KnownApps)
            if (lowerPath.Contains(key))
                return appName;
        return null;
    }
}
