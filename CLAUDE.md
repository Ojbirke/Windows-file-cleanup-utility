# Byteroom — prosjektkontekst for Claude Code

**Byteroom** er et Windows-verktøy (WPF, .NET 9) som ser ut som Utforskeren og hjelper
brukeren finne store filer å slette for å frigjøre diskplass. Fargekodet lokal
trygghetsanalyse (🟢🟡🔴), tospråklig NO/EN, sletter til papirkurv.

Laget av **Birkeland Digital** (birkelanddigital.no). Live på **https://byteroom.org**.

> Mappe/csproj heter fortsatt `StoreFiler` av historiske grunner; **produkt og
> assembly heter Byteroom**. Namespace er `Byteroom`. Ikke forvirre disse.

## Nøkkelfakta

| | |
|---|---|
| GitHub | github.com/Ojbirke/Windows-file-cleanup-utility (branch `main`) |
| Nettside | https://byteroom.org (Hetzner, se DEPLOY-seksjon) |
| Nedlastingsside-kilde | `landing/index.html` (tospråklig, `data-i18n` + JS) |
| Donasjon | paypal.me/ojbirke |
| Nåværende versjon | **1.0.1** (tospråklig + oppstartsfiks) |
| gh CLI | installert + innlogget som `Ojbirke` |

## Prosjektstruktur (kildefiler)

- `MainWindow.xaml(.cs)` — hovedvindu, skann-logikk, filliste, detaljpanel, hurtigfilter
- `FileAnalyzer.cs` — lokal trygghetsanalyse, returnerer `ReasonKey` (språknøytral)
- `Loc.cs` — lokalisering (NO/EN ordbøker), `Loc.I` singleton, XAML-bindinger
  `{Binding [Key], Source={x:Static local:Loc.I}}`. **Run.Text-bindinger MÅ ha
  `Mode=OneWay`** (ellers TwoWay → krasj mot skrivebeskyttet indexer, se 1.0.1-fiks).
- `FileItem.cs`, `SizeConverter.cs`, `HexBrushConverter.cs`, `NativeMethods.cs` (papirkurv),
  `AppLinks.cs` (lenker), `icon.ico`, `make-icon.ps1`

## Bygg & release (kjør fra `C:\Claude\Filsystem\StoreFiler`)

Lukk appen først (den låser exe-en).

```powershell
# Portabel selvstendig exe (Byteroom.exe, ~71 MB, ingen .NET kreves)
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
# Installer (Byteroom-Setup-<ver>.exe, ~66 MB)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" StoreFiler.iss
```

Ved ny versjon: bump `<Version>` i `StoreFiler.csproj`, `Version` i `AppLinks.cs`,
`MyAppVersion` i `StoreFiler.iss`, og `1.0.1`/filnavn i `landing/index.html`,
`README.md`, `RELEASE_NOTES.md`.

## ⚠️ Opplasting til GitHub Releases — VIKTIG

Utviklerens **hjemme-uplink kollapser under vedvarende full-fart-opplasting**
(bufferbloat). `git push` går fint (små data), men 60–70 MB exe-filer staller til
~3 kB/s eller dør via gh/curl/nettleser. To veier som VIRKER:

1. **Rate-limit fra hjemme:** `scp -l 400` (=50 kB/s) holder stabilt.
2. **Best — last opp FRA Hetzner-serveren** (datasenterfart, ~5s per fil). Filene
   speiles på serveren i `/home/ole/devportal/docker/byteroom/download/`:
   ```bash
   TOKEN=$(gh auth token); RID=$(gh api repos/Ojbirke/Windows-file-cleanup-utility/releases/tags/v<VER> --jq .id)
   ssh -i ~/.ssh/qr_clean ole@65.21.1.29 "export TOKEN='$TOKEN' RID='$RID'; bash -s" <<'E'
   cd /home/ole/devportal/docker/byteroom/download
   curl --http1.1 -X POST -H "Authorization: token $TOKEN" -H "Content-Type: application/octet-stream" \
     --data-binary @Byteroom.exe \
     "https://uploads.github.com/repos/Ojbirke/Windows-file-cleanup-utility/releases/$RID/assets?name=Byteroom.exe"
   E
   ```
   (På den faste PC-en i Bergen med skikkelig linje går vanlig `gh release upload` fint.)

## Deploy nettsiden til byteroom.org

Se `../memory` ([[byteroom-server-deploy]]) for full server-kontekst. Kort:
`landing/index.html` er et Artifact-fragment. Deploy = scp til serveren, wrap i
standalone HTML (awk legger til `<head>`/charset/viewport), skriv til
`/home/ole/devportal/docker/byteroom/index.html`. nginx-config + TLS + volum er
allerede på plass. Alltid `docker exec docker-nginx-1 nginx -t` før reload.
Server: `ssh -i ~/.ssh/qr_clean ole@65.21.1.29`. **Delt produksjonsserver** — ikke
rør andre apper (managemyapps, teamup, dailyclarity, qr).

Etter deploy: republiser Claude-artefakten (samme fil) og `git push`.

## Status & mulige neste steg

- ✅ v1.0.1 live: tospråklig app + nettside, begge filer på GitHub, TLS auto-fornyes
- ✅ Nettside: sikkerhetsnotis, favicon, «Om Birkeland Digital»-seksjon
- Åpne idéer: versjonsløse nedlastingslenker (`/releases/latest`) + cache-header,
  OG-delingsbilde, kodesignering mot SmartScreen, Microsoft Store, klikkbare app-chips
- Urelatert: `qr-qr-api` på serveren crash-looper (Prisma/OpenSSL) — egen sak
