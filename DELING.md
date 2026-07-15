# Store Filer — dele og publisere

Alt er bygget. Her er hva som finnes, hva du må fylle inn, og hvordan du legger det ut.

## Ferdige filer

| Fil | Sti | Størrelse |
|-----|-----|-----------|
| **Installer** | `dist\StoreFiler-Setup-1.0.0.exe` | 66 MB |
| **Portabel exe** | `bin\Release\net9.0-windows\win-x64\publish\StoreFiler.exe` | 71 MB |
| **Nedlastingsside** | `landing\index.html` | — |

Begge exe-ene kjører på Windows 10/11 (64-bit) **uten** at .NET er installert.

## 1. Bytt ut plassholderne (søk etter `DITTNAVN`)

| Fil | Hva du endrer |
|-----|---------------|
| `AppLinks.cs` | PayPal-brukernavn + nettside-URL (vises inne i appen) |
| `StoreFiler.iss` | `MyAppURL` (vises i installer/avinstaller) |
| `landing\index.html` | PayPal-lenke + GitHub-lenker (4 steder) |

PayPal: `https://www.paypal.me/DITTNAVN` → ditt faktiske PayPal.me-navn.
GitHub: `DITTNAVN/store-filer` → din bruker og repo-navn.

## 2. Bygg på nytt etter endringer

```powershell
cd C:\Claude\Filsystem\StoreFiler

# Vanlig build
dotnet build -c Release

# Portabel selvstendig exe
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true

# Installer (Inno Setup)
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" StoreFiler.iss
```

## 3. Legg ut på GitHub

1. Opprett et repo på github.com (f.eks. `store-filer`).
2. Push kildekoden (valgfritt, men fint hvis du vil dele koden).
3. Lag en **Release** (Releases → Draft a new release), tag `v1.0.0`.
4. Last opp begge filene som «assets»:
   - `StoreFiler-Setup-1.0.0.exe`
   - `StoreFiler.exe`
   Da fungerer nedlastingsknappene på siden automatisk (de peker på
   `releases/latest/download/...`).

## 4. Publiser nedlastingssiden

**Alternativ A — GitHub Pages (gratis):**
Legg `landing\index.html` i repoet (gjerne som `docs/index.html`),
og slå på Pages under Settings → Pages → kilde `docs/`. Siden blir da
`https://DITTNAVN.github.io/store-filer`.

**Alternativ B — Claude-artefakt:**
Allerede publisert som en privat, delbar lenke. Åpne den og bruk «Share»
for å dele. (Nedlastingsknappene trenger fortsatt GitHub Release i steg 3.)

## Merk om Windows SmartScreen

Siden exe-en ikke er signert med et (dyrt) kodesigneringssertifikat, vil
Windows kunne vise «Windows beskyttet PC-en» første gang. Brukeren klikker
«Mer info» → «Kjør likevel». Dette forsvinner om du senere kjøper et
signeringssertifikat.
