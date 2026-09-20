# Leitfaden: GitHub Binary Releases und Lizenz-Compliance

**Status:** Leitfaden und Vorbereitung für künftige Releases. Verwandt mit `THIRD-PARTY-NOTICES.md` und `LICENSE`.

## Ausgangslage

Das Projekt KnowHowToAI soll auf GitHub veröffentlicht und künftig über **GitHub Binary Releases** als vorkompilierte Binärpakete (z. B. ZIP-Archive mit Server-Executable für Windows/Linux) bereitgestellt werden.

Dieses Dokument fasst zusammen:
1. Warum und unter welchen Bedingungen das rechtlich zulässig ist.
2. Welche Pflichten bei der Weitergabe von Binärdateien zwingend erfüllt werden müssen.
3. Welche konkreten Schritte und Checklisten vor und während eines Releases abzuarbeiten sind.

---

## 1. Rechtliche Bewertung: Warum die Veröffentlichung erlaubt ist

Alle im Projekt verwendeten Drittkomponenten erlauben die Weitergabe sowohl als Quellcode als auch in kompilierter Form (Binaries):

- **Permissive Open-Source-Lizenzen (MIT, Apache-2.0, BSD-2/3-Clause, ISC):**  
  Der überwiegende Teil der .NET- und npm-Abhängigkeiten (z. B. Dapper, Markdig, Serilog, Milkdown, CodeMirror) erlaubt die Weitergabe kostenlos, weltweit und auch für kommerzielle Zwecke.
- **Keine Copyleft-Falle (GPL / AGPL):**  
  Es werden keine Bibliotheken unter GPL/AGPL verwendet. Der eigene Quellcode muss daher nicht zwingend unter einer Copyleft-Lizenz offengelegt werden, und es besteht keine Gefahr einer Lizenzinfektion.
- **Proprietäre Microsoft-EULA (`Microsoft.Data.SqlClient.SNI.runtime 6.0.2`):**  
  Dieses Paket unterliegt den *Microsoft Software License Terms*. Diese erlauben die Weitergabe von Objektcode ausdrücklich, solange dies **als integraler Bestandteil der eigenen Anwendung** geschieht (keine isolierte Distribution der DLL).

---

## 2. Zwingende Pflichten bei der Binär-Distribution

Wenn ein Anwender ein Release-ZIP herunterlädt, sieht er das Git-Repository in der Regel nicht direkt. Daher greifen bei der Binär-Weitergabe folgende Pflichten:

### Pflicht 1: Lizenz- und Copyright-Hinweise beilegen
Die Lizenzen (MIT, Apache-2.0, BSD) verlangen universell:
> *„The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.“*

- **Konsequenz:** In jedes Release-ZIP **müssen** die eigene `LICENSE` sowie die `THIRD-PARTY-NOTICES.md` (oder als `.txt`) beigelegt werden.
- **Rechtliches Risiko bei Unterlassung:** Werden Lizenz- und Urheberhinweise nicht beigelegt, erlischt das Nutzungs- und Verbreitungsrecht der Fremdkomponenten automatisch. Die Weitergabe wird zur Urheberrechtsverletzung (§ 97 UrhG).

### Pflicht 2: Microsoft SNI-Bedingungen einhalten
Für `Microsoft.Data.SqlClient.SNI.runtime` gilt:
- Nur im Verbund mit der Server-Anwendung ausliefern.
- Die eigene MIT-Lizenz schließt bereits Gewährleistung und Haftung aus („AS IS“) – dies erfüllt Microsofts Anforderung an mindestens gleichwertige Schutzbedingungen für Endnutzer.
- Keine Verwendung von Microsoft-Marken oder -Logos für eigene Zwecke.
- Exportkontrollbestimmungen beachten.

### Pflicht 3: Frontend-Bundle berücksichtigen
Das mit `esbuild` erzeugte Web-Bundle (`@milkdown/*`, `@codemirror/*` etc.) wird vom Server an den Browser ausgeliefert. Da `THIRD-PARTY-NOTICES.md` auch alle npm-Komponenten vollständig inventarisiert, ist dieser Aspekt durch die Beilage der Datei ebenfalls abgedeckt.

### Pflicht 4: Keine Secrets im Release
- Keine echten Connection-Strings, Kennwörter oder API-Keys in Konfigurationsdateien (`appsettings.json`, `appsettings.Production.json`).
- Release-Builds dürfen nur Vorlagen mit Dummy-Werten enthalten.

---

## 3. Checkliste für den Release-Workflow

| Phase | Schritt | Beschreibung / Prüfung |
|---|---|---|
| **Vorbereitung** | **1. Inventar prüfen** | `THIRD-PARTY-NOTICES.md` auf Übereinstimmung mit aktuellen `PackageReference`- und `package-lock.json`-Ständen prüfen. |
| **Vorbereitung** | **2. Secrets prüfen** | Sicherstellen, dass keine Passwörter, Connection-Strings oder Token in `appsettings*.json` stehen. |
| **Build** | **3. Publish ausführen** | `dotnet publish src/KnowHowToAI.Server/KnowHowToAI.Server.csproj -c Release -r win-x64 --self-contained false -o publish/` (bzw. zielgerichtetes Target). |
| **Paketierung** | **4. Lizenzen beilegen** | `LICENSE` und `THIRD-PARTY-NOTICES.md` direkt in das Publish-Ausgabeverzeichnis kopieren. |
| **Paketierung** | **5. ZIP schnüren** | Das Verzeichnis mitsamt `LICENSE` und `THIRD-PARTY-NOTICES.md` als `.zip` komprimieren (z. B. `KnowHowToAI-Server-v1.0.0-win-x64.zip`). |
| **Release** | **6. GitHub Release** | Release-Tag auf GitHub erstellen, Release-Notes schreiben und die ZIP-Datei als Release-Asset anhängen. |

---

## 4. Automatisierungsidee (künftige Pipeline)

Für künftige Versionen empfiehlt sich ein PowerShell-Skript oder ein GitHub-Actions-Workflow:

```powershell
# Beispiel: scripts/package-release.ps1
param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64"
)

$PublishDir = "temp/publish/$Runtime"
$ZipFile = "temp/KnowHowToAI-Server-$Version-$Runtime.zip"

dotnet publish src/KnowHowToAI.Server/KnowHowToAI.Server.csproj -c Release -r $Runtime --self-contained false -o $PublishDir
Copy-Item "LICENSE" -Destination $PublishDir
Copy-Item "THIRD-PARTY-NOTICES.md" -Destination $PublishDir

Compress-Archive -Path "$PublishDir/*" -DestinationPath $ZipFile -Force
Write-Host "Release-Archiv erstellt: $ZipFile"
```
