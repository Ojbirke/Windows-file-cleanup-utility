using System.ComponentModel;
using System.Globalization;

namespace Byteroom;

public enum Lang { No, En }

/// <summary>
/// Enkel lokalisering. Bind tekster i XAML slik:
///   Text="{Binding [Col_Name], Source={x:Static local:Loc.I}}"
/// og hent i kode med Loc.T("key"). Språkbytte oppdaterer alle bindinger live.
/// </summary>
public class Loc : INotifyPropertyChanged
{
    public static Loc I { get; } = new Loc();

    public Lang Current { get; private set; } = DetectDefault();

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Indeksering brukt av XAML-bindinger: {Binding [Key], Source=...}.</summary>
    public string this[string key]
        => (Current == Lang.No ? No : En).TryGetValue(key, out var v) ? v : key;

    public static string T(string key) => I[key];

    public void Toggle() => SetLang(Current == Lang.No ? Lang.En : Lang.No);

    public void SetLang(Lang lang)
    {
        if (lang == Current) return;
        Current = lang;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
    }

    private static Lang DetectDefault()
    {
        var t = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return t is "nb" or "nn" or "no" ? Lang.No : Lang.En;
    }

    private static readonly Dictionary<string, string> No = new()
    {
        // Verktøylinje
        ["Toolbar_Folder"] = "Mappe:",
        ["Btn_Browse"] = "Bla gjennom…",
        ["Toolbar_Min"] = "Minst:",
        ["Min_All"] = "Alle",
        ["Btn_Scan"] = "🔍  Skann",
        ["Btn_Stop"] = "Stopp",
        ["Btn_Analyze"] = "🔬  Analyser",
        ["SwitchLang"] = "English",
        // Statuslinje
        ["Status_Ready"] = "Klar. Velg en mappe eller disk og trykk Skann.",
        ["Link_Website"] = "Nettside",
        ["Link_Donate"] = "❤ Støtt utvikleren",
        // Disker
        ["ThisPC"] = "Denne PC-en",
        ["Drive_LocalDisk"] = "Lokal disk",
        ["Drive_Free"] = "{0} ledig av {1}",
        // Filter
        ["Filter_Show"] = "Vis:",
        ["Filter_All"] = "Alle filer",
        ["Filter_Safe"] = "Bare trygge",
        ["Filter_Caution"] = "Vær forsiktig",
        ["Filter_Unsafe"] = "Bør ikke slettes",
        ["Filter_Unknown"] = "Usikre",
        ["Count_Files"] = "{0} filer",
        ["Count_Showing"] = "Viser {0} av {1} filer",
        // Kolonner
        ["Col_Name"] = "Navn",
        ["Col_Size"] = "Størrelse",
        ["Col_Type"] = "Type",
        ["Col_Modified"] = "Endret",
        ["Col_Folder"] = "Mappe",
        // Kontekstmeny
        ["Ctx_Open"] = "Åpne mappe i Utforsker",
        ["Ctx_Copy"] = "Kopier full sti",
        ["Ctx_Delete"] = "Slett til papirkurv",
        // Detaljpanel
        ["Detail_Title"] = "Analyse",
        ["Detail_Hint"] = "Velg en fil i lista (eller klikk «Analyser») for å se hva den er og om det er trygt å slette.",
        ["Detail_Name"] = "Navn",
        ["Detail_Size"] = "Størrelse",
        ["Detail_Program"] = "Program",
        ["Detail_Assessment"] = "Vurdering",
        ["Detail_Location"] = "Plassering",
        ["Detail_Open"] = "📂  Åpne i Utforsker",
        ["Detail_Delete"] = "🗑  Slett",
        ["Detail_Disclaimer"] = "Vurderingen er en lokal beste-gjetning ut fra plassering og filtype — sjekk alltid selv før du sletter noe du er usikker på.",
        // Skanning / meldinger
        ["Scan_Starting"] = "Skanner… starter opp",
        ["Scan_Progress"] = "Skanner… {0} mapper gjennomsøkt · {1} store filer funnet · {2}",
        ["Done_Empty"] = "Ferdig på {0} s — ingen filer her. Prøv en lavere «Minst»-verdi.",
        ["Done_Empty_Limited"] = "Ferdig på {0} s — ingen filer over {1} her. Prøv en lavere «Minst»-verdi.",
        ["Done"] = "Ferdig på {0} s — {1}, til sammen {2}.",
        ["Shown_Top"] = "Viser de {0} største av {1}",
        ["Shown_Count"] = "{0} filer",
        ["Stopped"] = "Stoppet — {0} filer vist, til sammen {1}.",
        ["Deleted"] = "Slettet til papirkurv — frigjorde {0}.",
        ["Delete_Partial"] = "Sletting ble avbrutt eller delvis fullført.",
        // Dialoger
        ["Dlg_Analyze_Title"] = "Analyser",
        ["Dlg_Analyze_Msg"] = "Velg en fil i lista først, så analyserer jeg den.",
        ["Dlg_Delete_Title"] = "Bekreft sletting",
        ["Dlg_Delete_One"] = "Slett denne fila til papirkurven?\n\n{0}\n{1}",
        ["Dlg_Delete_Many"] = "Slett {0} filer til papirkurven?\n\nFrigjør {1}.",
        ["Dlg_InvalidPath_Msg"] = "Velg en gyldig mappe eller disk å skanne.",
        ["Browse_Desc"] = "Velg mappe eller disk som skal skannes",
        // Filtype
        ["Type_File"] = "Fil",
        ["Type_Suffix"] = "{0}-fil",
        // Trygghet
        ["Verdict_Safe"] = "Trygg å slette",
        ["Verdict_Caution"] = "Vær forsiktig",
        ["Verdict_Unsafe"] = "Bør ikke slettes",
        ["Verdict_Unknown"] = "Usikker",
        // Analyse: kategorier
        ["Cat_sys_virtualmem"] = "Systemfil (virtuelt minne)",
        ["Cat_sys_file"] = "Systemfil",
        ["Cat_recyclebin"] = "Allerede i papirkurven",
        ["Cat_sysrestore"] = "Systemgjenoppretting",
        ["Cat_temp"] = "Midlertidig fil",
        ["Cat_cache"] = "Buffer / cache",
        ["Cat_log"] = "Loggfil",
        ["Cat_crashdump"] = "Krasjdump",
        ["Cat_download_installer"] = "Nedlastet installasjonsfil",
        ["Cat_download_file"] = "Nedlastet fil",
        ["Cat_installer_leftover"] = "Installasjons-/oppdateringsfil",
        ["Cat_program"] = "Del av et installert program",
        ["Cat_diskimage"] = "Disk-/systembilde",
        ["Cat_backup"] = "Sikkerhetskopi / gammel versjon",
        ["Cat_cloud"] = "Skysynkronisert fil",
        ["Cat_video"] = "Videofil",
        ["Cat_image"] = "Bildefil",
        ["Cat_audio"] = "Lydfil",
        ["Cat_document"] = "Ditt dokument",
        ["Cat_archive"] = "Arkivfil",
        ["Cat_unknown"] = "Ukjent filtype",
        // Analyse: forklaringer
        ["Expl_sys_virtualmem"] = "Denne styres av Windows for virtuelt minne / dvalemodus. Ikke slett den manuelt — juster den heller via Systeminnstillinger hvis du vil frigjøre plass.",
        ["Expl_sys_file"] = "Del av Windows. Å slette den kan gjøre systemet ustabilt.",
        ["Expl_recyclebin"] = "Ligger allerede i papirkurven. Tøm papirkurven i Utforsker for å frigjøre plassen helt.",
        ["Expl_sysrestore"] = "Brukes av Windows til gjenopprettingspunkter og skyggekopier. Rydd heller via Diskopprydding.",
        ["Expl_temp"] = "Midlertidig fil. Slikt kan nesten alltid slettes trygt — programmer lager det på nytt ved behov.",
        ["Expl_cache"] = "Bufferdata som lagres for å gjøre et program raskere. Trygt å slette — det bygges opp igjen automatisk (programmet kan starte litt tregere første gang etterpå).",
        ["Expl_log"] = "Loggfil som beskriver hva et program har gjort. Brukes stort sett bare til feilsøking — trygt å slette hvis alt fungerer.",
        ["Expl_crashdump"] = "Feilsøkingsdump laget da et program krasjet. Trygt å slette med mindre du aktivt feilsøker en krasj.",
        ["Expl_download_installer"] = "Installasjons- eller arkivfil i Nedlastinger. Er programmet allerede installert, trengs ikke denne — trygg å slette. (Ta vare på den hvis du vil kunne installere på nytt uten å laste ned igjen.)",
        ["Expl_download_file"] = "En fil du har lastet ned. Sjekk om du fortsatt trenger den før du sletter.",
        ["Expl_installer_leftover"] = "Installasjons- eller oppdateringsdata for Windows/programmer. Ofte trygt via Diskopprydding, men å slette manuelt kan gi problemer ved reparasjon/avinstallering.",
        ["Expl_program"] = "Ligger inne i mappen til et installert program. Å slette enkeltfiler her kan ødelegge programmet. Vil du frigjøre plass: avinstaller hele programmet via Innstillinger i stedet.",
        ["Expl_diskimage"] = "Stort disk- eller systembilde (virtuell maskin, ISO e.l.). Kan være trygt å slette, men bare hvis du er sikker på at du ikke trenger innholdet.",
        ["Expl_backup"] = "Ser ut som en sikkerhetskopi eller gammel versjon. Vanligvis trygt å slette hvis originalen finnes og fungerer.",
        ["Expl_cloud"] = "Ligger i en skymappe. Sletter du den her, forsvinner den også fra skyen på alle enhetene dine. Vær sikker først.",
        ["Expl_video"] = "Videofil — ofte det som tar mest plass. Dette er sannsynligvis ditt eget innhold, så sjekk at du ikke trenger den før du sletter.",
        ["Expl_image"] = "Bilde — sannsynligvis ditt eget. Sjekk før du sletter.",
        ["Expl_audio"] = "Lyd-/musikkfil — sannsynligvis din egen. Sjekk før du sletter.",
        ["Expl_document"] = "Ser ut som et personlig dokument. Slett bare hvis du er sikker på at du ikke trenger innholdet.",
        ["Expl_archive"] = "Komprimert arkiv. Kan inneholde hva som helst — pakk ut eller sjekk innholdet før du sletter.",
        ["Expl_unknown"] = "Klarte ikke å fastslå sikkert hva denne fila er. Åpne mappa i Utforsker og undersøk før du eventuelt sletter.",
    };

    private static readonly Dictionary<string, string> En = new()
    {
        // Toolbar
        ["Toolbar_Folder"] = "Folder:",
        ["Btn_Browse"] = "Browse…",
        ["Toolbar_Min"] = "At least:",
        ["Min_All"] = "All",
        ["Btn_Scan"] = "🔍  Scan",
        ["Btn_Stop"] = "Stop",
        ["Btn_Analyze"] = "🔬  Analyze",
        ["SwitchLang"] = "Norsk",
        // Status bar
        ["Status_Ready"] = "Ready. Pick a folder or drive and click Scan.",
        ["Link_Website"] = "Website",
        ["Link_Donate"] = "❤ Support the developer",
        // Drives
        ["ThisPC"] = "This PC",
        ["Drive_LocalDisk"] = "Local disk",
        ["Drive_Free"] = "{0} free of {1}",
        // Filter
        ["Filter_Show"] = "Show:",
        ["Filter_All"] = "All files",
        ["Filter_Safe"] = "Safe only",
        ["Filter_Caution"] = "Use caution",
        ["Filter_Unsafe"] = "Don't delete",
        ["Filter_Unknown"] = "Uncertain",
        ["Count_Files"] = "{0} files",
        ["Count_Showing"] = "Showing {0} of {1} files",
        // Columns
        ["Col_Name"] = "Name",
        ["Col_Size"] = "Size",
        ["Col_Type"] = "Type",
        ["Col_Modified"] = "Modified",
        ["Col_Folder"] = "Folder",
        // Context menu
        ["Ctx_Open"] = "Open folder in Explorer",
        ["Ctx_Copy"] = "Copy full path",
        ["Ctx_Delete"] = "Delete to Recycle Bin",
        // Detail panel
        ["Detail_Title"] = "Analysis",
        ["Detail_Hint"] = "Select a file in the list (or click “Analyze”) to see what it is and whether it's safe to delete.",
        ["Detail_Name"] = "Name",
        ["Detail_Size"] = "Size",
        ["Detail_Program"] = "Program",
        ["Detail_Assessment"] = "Assessment",
        ["Detail_Location"] = "Location",
        ["Detail_Open"] = "📂  Open in Explorer",
        ["Detail_Delete"] = "🗑  Delete",
        ["Detail_Disclaimer"] = "This is a local best guess based on location and file type — always check for yourself before deleting anything you're unsure about.",
        // Scanning / messages
        ["Scan_Starting"] = "Scanning… starting up",
        ["Scan_Progress"] = "Scanning… {0} folders searched · {1} large files found · {2}",
        ["Done_Empty"] = "Done in {0} s — no files here. Try a lower “At least” value.",
        ["Done_Empty_Limited"] = "Done in {0} s — no files over {1} here. Try a lower “At least” value.",
        ["Done"] = "Done in {0} s — {1}, {2} in total.",
        ["Shown_Top"] = "Showing the {0} largest of {1}",
        ["Shown_Count"] = "{0} files",
        ["Stopped"] = "Stopped — {0} files shown, {1} in total.",
        ["Deleted"] = "Deleted to Recycle Bin — freed {0}.",
        ["Delete_Partial"] = "Deletion was cancelled or only partly completed.",
        // Dialogs
        ["Dlg_Analyze_Title"] = "Analyze",
        ["Dlg_Analyze_Msg"] = "Select a file in the list first, then I'll analyze it.",
        ["Dlg_Delete_Title"] = "Confirm deletion",
        ["Dlg_Delete_One"] = "Delete this file to the Recycle Bin?\n\n{0}\n{1}",
        ["Dlg_Delete_Many"] = "Delete {0} files to the Recycle Bin?\n\nFrees {1}.",
        ["Dlg_InvalidPath_Msg"] = "Pick a valid folder or drive to scan.",
        ["Browse_Desc"] = "Pick a folder or drive to scan",
        // File type
        ["Type_File"] = "File",
        ["Type_Suffix"] = "{0} file",
        // Safety
        ["Verdict_Safe"] = "Safe to delete",
        ["Verdict_Caution"] = "Use caution",
        ["Verdict_Unsafe"] = "Don't delete",
        ["Verdict_Unknown"] = "Uncertain",
        // Analysis: categories
        ["Cat_sys_virtualmem"] = "System file (virtual memory)",
        ["Cat_sys_file"] = "System file",
        ["Cat_recyclebin"] = "Already in the Recycle Bin",
        ["Cat_sysrestore"] = "System restore",
        ["Cat_temp"] = "Temporary file",
        ["Cat_cache"] = "Cache",
        ["Cat_log"] = "Log file",
        ["Cat_crashdump"] = "Crash dump",
        ["Cat_download_installer"] = "Downloaded installer",
        ["Cat_download_file"] = "Downloaded file",
        ["Cat_installer_leftover"] = "Installer / update file",
        ["Cat_program"] = "Part of an installed program",
        ["Cat_diskimage"] = "Disk / system image",
        ["Cat_backup"] = "Backup / old version",
        ["Cat_cloud"] = "Cloud-synced file",
        ["Cat_video"] = "Video file",
        ["Cat_image"] = "Image file",
        ["Cat_audio"] = "Audio file",
        ["Cat_document"] = "Your document",
        ["Cat_archive"] = "Archive file",
        ["Cat_unknown"] = "Unknown file type",
        // Analysis: explanations
        ["Expl_sys_virtualmem"] = "Windows manages this for virtual memory / hibernation. Don't delete it manually — adjust it via System settings if you want to free space.",
        ["Expl_sys_file"] = "Part of Windows. Deleting it can make the system unstable.",
        ["Expl_recyclebin"] = "Already in the Recycle Bin. Empty it in Explorer to fully reclaim the space.",
        ["Expl_sysrestore"] = "Used by Windows for restore points and shadow copies. Clean it via Disk Cleanup instead.",
        ["Expl_temp"] = "A temporary file. Almost always safe to delete — programs recreate it as needed.",
        ["Expl_cache"] = "Cache data stored to make a program faster. Safe to delete — it rebuilds automatically (the program may start a bit slower the first time after).",
        ["Expl_log"] = "A log file recording what a program did. Mostly for troubleshooting — safe to delete if everything works.",
        ["Expl_crashdump"] = "A debug dump created when a program crashed. Safe to delete unless you're actively debugging a crash.",
        ["Expl_download_installer"] = "An installer or archive in Downloads. If the program is already installed you don't need this — safe to delete. (Keep it if you want to reinstall without downloading again.)",
        ["Expl_download_file"] = "A file you downloaded. Check whether you still need it before deleting.",
        ["Expl_installer_leftover"] = "Install or update data for Windows/programs. Often safe via Disk Cleanup, but deleting it manually can cause problems with repair/uninstall.",
        ["Expl_program"] = "Located inside an installed program's folder. Deleting individual files here can break the program. To free space, uninstall the whole program via Settings instead.",
        ["Expl_diskimage"] = "A large disk or system image (virtual machine, ISO, etc.). Can be safe to delete, but only if you're sure you don't need the contents.",
        ["Expl_backup"] = "Looks like a backup or old version. Usually safe to delete if the original exists and works.",
        ["Expl_cloud"] = "Located in a cloud folder. If you delete it here it also disappears from the cloud on all your devices. Be sure first.",
        ["Expl_video"] = "Video file — often the biggest space user. This is probably your own content, so check you don't need it before deleting.",
        ["Expl_image"] = "An image — probably your own. Check before deleting.",
        ["Expl_audio"] = "Audio/music file — probably your own. Check before deleting.",
        ["Expl_document"] = "Looks like a personal document. Delete only if you're sure you don't need the contents.",
        ["Expl_archive"] = "A compressed archive. Could contain anything — extract or check the contents before deleting.",
        ["Expl_unknown"] = "Couldn't determine for sure what this file is. Open the folder in Explorer and investigate before deleting.",
    };
}
