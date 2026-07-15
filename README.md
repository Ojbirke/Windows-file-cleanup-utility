# Store Filer 🔍

Et Windows-verktøy som ser ut som Utforskeren, men som hjelper deg å finne
**store filer du kan slette** for å frigjøre plass på PC-en.

![Windows 10/11](https://img.shields.io/badge/Windows-10%20%7C%2011-0067C0)
![.NET 9](https://img.shields.io/badge/.NET-9-512BD4)

## Funksjoner

- **Største først** — rekursiv skann av mappe eller hel disk, sortert etter størrelse, med live fremdrift.
- **Fargekodet trygghet** 🟢🟡🔴 — hver fil får en vurdering av om det er trygt å slette. Kjenner igjen apper (Zwift, Steam, …), cache, logger, systemfiler og dine egne dokumenter.
- **Analyse per fil** — klikk en fil og se hva den er, hvilket program den hører til og en anbefaling. Alt lokalt, ingen data forlater PC-en.
- **Trygg sletting** — alt går til papirkurven, aldri rett i søpla.
- **Hurtigfilter** — vis «bare trygge» med ett klikk.

## Last ned

Se [Releases](https://github.com/Ojbirke/Windows-file-cleanup-utility/releases/latest):

- **Installer** (`StoreFiler-Setup-1.0.0.exe`) — installerer som et vanlig program.
- **Portabel** (`StoreFiler.exe`) — ingen installasjon, bare dobbeltklikk.

Begge kjører på Windows 10/11 (64-bit) uten at .NET må installeres.

## Bygge selv

```powershell
# Vanlig build
dotnet build -c Release

# Portabel selvstendig exe
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true

# Installer (krever Inno Setup 6)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" StoreFiler.iss
```

Se [DELING.md](DELING.md) for full utgivelses- og publiseringsguide.

## Lisens

Gratis å bruke. Liker du programmet? [Doner gjerne ☕](https://www.paypal.me/DITTNAVN)
