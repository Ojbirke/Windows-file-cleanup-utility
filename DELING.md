# Byteroom — release & deploy-runbook

Prosjektet er **live**: kode på GitHub, nettside på byteroom.org, v1.0.1 publisert.
Denne fila er oppskriften for å lage en **ny release** og oppdatere nettsiden.
Full prosjektkontekst ligger i [CLAUDE.md](CLAUDE.md).

## Gjeldende oppsett

| Ting | Verdi |
|------|-------|
| Repo | github.com/Ojbirke/Windows-file-cleanup-utility |
| Nettside | https://byteroom.org |
| Siste versjon | 1.0.1 |
| Filer | `Byteroom-Setup-<ver>.exe` (installer), `Byteroom.exe` (portabel) |

## Ny release — steg for steg

**1. Bump versjon** i alle fem: `StoreFiler.csproj` (`<Version>`), `AppLinks.cs`
(`Version`), `StoreFiler.iss` (`MyAppVersion`), og filnavn/`v1.0.x` i
`landing/index.html`, `README.md`, `RELEASE_NOTES.md`.

**2. Bygg** (lukk appen først — den låser exe-en):
```powershell
cd C:\Claude\Filsystem\StoreFiler
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" StoreFiler.iss
Copy-Item .\bin\Release\net9.0-windows\win-x64\publish\Byteroom.exe .\dist\ -Force
```

**3. Commit + push:** `git add -A; git commit -m "..."; git push`

**4. Lag release + last opp filene:**
```powershell
gh release create v<VER> --title "..." --notes-file RELEASE_NOTES.md --latest
```
Så last opp `dist\Byteroom.exe` og `dist\Byteroom-Setup-<VER>.exe`.
**⚠️ Opplastingen staller fra hjemme-uplinken** — se «Opplasting» i
[CLAUDE.md](CLAUDE.md) for løsningen (rate-limit eller last opp fra Hetzner).
Nedlastingsknappene på siden peker på `releases/latest/download/...`, så de virker
automatisk når filene er oppe.

## Oppdatere nettsiden (byteroom.org)

Rediger `landing/index.html` → scp til serveren → wrap til standalone → skriv til
`/home/ole/devportal/docker/byteroom/index.html`. Detaljert kommando i CLAUDE.md.
Deretter: republiser Claude-artefakten (samme fil) + `git push`.

## SmartScreen

Usignert exe → «Windows beskyttet PC-en» første gang (Mer info → Kjør likevel).
Normalt for nye hobbyprogrammer; forklart i sikkerhetsnotisen på siden og i
RELEASE_NOTES. Fjernes helt ved kodesignering (EV = umiddelbart) eller over tid.

## Cache-felle (kjent)

Nedlastingslenkene har fast filnavn (`Byteroom-Setup-1.0.1.exe`). Cachet gammel
side kan peke på en slettet versjon → 404. Bruker fikser med Ctrl+F5.
Varig fiks: lenk til `/releases/latest` + cache-header (åpen oppgave).
