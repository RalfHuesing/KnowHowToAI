# Fremdkomponenten und Lizenzinventar

Stand: 2026-09-18. Dieses Inventar gehört zur MIT-`LICENSE` im Repository-Root
(`Copyright (c) 2026 Ralf Hüsing`). Die Lizenzdatei wurde bei dieser Prüfung
nicht verändert.

## Aktualisierung und Reproduktion

Dieses Inventar ist bei **jeder** Änderung einer `PackageReference`, einer
zentralen Paketversion, eines Ziel-Frameworks oder eines Restore-Inputs im
selben Commit zu aktualisieren. Die Auflösung wird nicht aus einer
Paketwebseite geraten, sondern nach einem frischen Restore aus den Assets und
den genau dazu restaurierten Paketmetadaten ermittelt:

```powershell
dotnet restore KnowHowToAI.slnx
dotnet list KnowHowToAI.slnx package --include-transitive
Get-ChildItem -Recurse -Filter project.assets.json |
  ForEach-Object { Get-Content -Raw $_.FullName | ConvertFrom-Json }
dotnet nuget list source
```

Für jeden in den `libraries` der `project.assets.json` genannten Eintrag wird
die restaurierte `<Paketwurzel>\<id>\<version>\*.nuspec` gelesen; die konkrete
Restore-Quelle steht in deren `.nupkg.metadata`. Beigefügte `LICENSE*`,
`NOTICE*`, `THIRD-PARTY-NOTICES*` und `Copyright*` werden in derselben
Paketwurzel geprüft. Beim vorliegenden Restore ist die Paketwurzel
`%USERPROFILE%\.nuget\packages`, und alle inventarisierten `.nupkg.metadata`
nennen `https://api.nuget.org/v3/index.json`. Die NuGet-Quellenliste enthielt
zusätzlich lokale DevExpress- und Visual-Studio-Offline-Quellen; keines der
untenstehenden Pakete stammt von ihnen.

Die Spalte „Quelle/Nachweis“ bedeutet daher jeweils: NuGet.org als tatsächlich
verwendete Restore-Quelle, Lizenzfeld im genannten, restaurierten `.nuspec`.
Eine `NOTICE`-Datei ist nur dann zusätzlich aufzuführen, wenn sie in der
Paketversion vorhanden ist. „Lizenz und Copyright beilegen“ bedeutet, den
vollständigen jeweiligen Lizenztext einschließlich Copyright-Hinweisen bei
einer Weitergabe zu erhalten. Apache-2.0 verlangt zusätzlich eine vorhandene
`NOTICE`-Datei weiterzugeben; keine der untenstehenden Apache-Paketversionen
enthält eine solche Datei.

## Einordnung des Scopes

`P` bedeutet Produktionsumfang: Das Paket wird von `Core`, `Storage.SqlServer`
oder `Server` direkt oder transitiv benötigt und kann mit der Serveranwendung
ausgeliefert werden. `D` bedeutet ausschließlich Entwicklungs-/Test-/Buildumfang
und ist nicht Teil der Serverdistribution. `SF` bezeichnet das .NET Shared
Framework, nicht ein NuGet-Paket: alle Projekte zielen auf `net10.0`; die
geprüfte Host-Runtime ist `Microsoft.NETCore.App 10.0.11` (MIT, .NET Runtime
Installation). Sie erscheint folgerichtig nicht in `project.assets.json` und
nicht in der NuGet-Tabelle.

„Direkt“ nennt mindestens eine Projekt-`PackageReference`; „transitiv“ gilt
ansonsten. Ein Paket kann durch verschiedene Projekte sowohl direkt als auch
transitiv erreichbar sein und wird trotzdem nur einmal je tatsächlich
aufgelöster Version aufgeführt.

## Zentrale Versionen gegen Restore-Auflösung

`ManagePackageVersionsCentrally` und
`CentralPackageTransitivePinningEnabled` sind aktiv. Alle zentral deklarierten
Versionen wurden in den Assets exakt aufgelöst; es gibt keine Abweichung.

| Zentral deklarierte Pakete | Zentral / aufgelöst |
|---|---|
| Markdig | 0.42.0 / 0.42.0 |
| Microsoft.Extensions.Logging, Microsoft.Extensions.Logging.Abstractions | 10.0.10 / 10.0.10 |
| Dapper | 2.1.66 / 2.1.66 |
| Microsoft.Data.SqlClient | 6.0.2 / 6.0.2 |
| Microsoft.Extensions.Configuration, `.Json`, Hosting, Options | 10.0.10 / 10.0.10 |
| ModelContextProtocol | 2.2.0 / 2.2.0 |
| Serilog, Serilog.Extensions.Hosting | 4.4.0 / 4.4.0; 9.0.0 / 9.0.0 |
| Serilog.Sinks.Console, Serilog.Sinks.File | 6.1.1 / 6.1.1; 7.0.0 / 7.0.0 |
| coverlet.collector | 10.0.1 / 10.0.1 |
| Microsoft.NET.Test.Sdk | 18.8.1 / 18.8.1 |
| xunit.runner.visualstudio | 3.1.5 / 3.1.5 |
| xunit.v3.assert, xunit.v3.core | 3.2.2 / 3.2.2 |

Nicht in `Directory.Packages.props` deklarierte Versionen sind vollständig
transitive Restore-Ergebnisse; sie werden nicht als zentrale Vorgabe ausgegeben.

## Inventar

| Paket und aufgelöste Version | Verwendung und Scope | Quelle/Nachweis | SPDX/Lizenztyp | Konkrete Pflicht bei Weitergabe |
|---|---|---|---|---|
| Azure.Core 1.38.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Azure.Identity 1.11.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| coverlet.collector 10.0.1 | direkt in beiden Testprojekten; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Copyright beilegen; nicht mit dem Server ausliefern. |
| Dapper 2.1.66 | direkt in Storage; transitiv in Server/Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Markdig 0.42.0 | direkt in Core; transitiv in übrigen Projekten; P | NuGet.org; `.nuspec`: `expression: BSD-2-Clause` | BSD-2-Clause | Copyright, Bedingungen und Haftungsausschluss beilegen. |
| Microsoft.ApplicationInsights 2.23.0 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Bcl.AsyncInterfaces 1.1.1 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` unverändert mitführen. |
| Microsoft.Bcl.AsyncInterfaces 6.0.0 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen; nicht mit dem Server ausliefern. |
| Microsoft.Bcl.Cryptography 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` unverändert mitführen. |
| Microsoft.CodeCoverage 18.8.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT`; `ThirdPartyNotices.txt` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright und `ThirdPartyNotices.txt` mitführen; nicht mit dem Server ausliefern. |
| Microsoft.Data.SqlClient 6.0.2 | direkt in Storage; transitiv in Server/Integrationstests; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.Data.SqlClient.SNI.runtime 6.0.2 | transitiv; P | NuGet.org; `.nuspec`: `file: LICENSE.txt`; beigefügte `LICENSE.txt` | Microsoft Software License Terms (kein SPDX-Ausdruck, nicht permissiv) | Nur Objektcode als Teil einer Anwendung verteilen, Endnutzer-/Distributorbedingungen mindestens gleich schützend verlangen, Microsoft freistellen, Marken nicht verwenden, Exportregeln einhalten und Drittanbieterhinweise beachten. Die ausdrückliche Annahme dieser Restriktionen ist unten festgehalten. |
| Microsoft.Extensions.AI.Abstractions 10.8.3 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.Extensions.Caching.Abstractions 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Caching.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Caching.Memory 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration 10.0.10 | direkt in Integrationstests; transitiv im Server; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.Binder 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.CommandLine 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.EnvironmentVariables 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.FileExtensions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.Json 10.0.10 | direkt in Server/Integrationstests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.UserSecrets 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.DependencyInjection 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.DependencyInjection.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Diagnostics 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Diagnostics.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.FileProviders.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.FileProviders.Physical 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.FileSystemGlobbing 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Hosting 10.0.10 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Hosting.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Abstractions 10.0.10 | direkt in Storage; transitiv in Server/Tests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Configuration 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Console 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Debug 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.EventLog 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.EventSource 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Options 10.0.10 | direkt im Server; transitiv in Storage/Tests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Options.ConfigurationExtensions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Primitives 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Identity.Client 4.61.3 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.Identity.Client.Extensions.Msal 4.61.3 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.IdentityModel.Abstractions 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.IdentityModel.JsonWebTokens 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.IdentityModel.Logging 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.IdentityModel.Protocols 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.IdentityModel.Protocols.OpenIdConnect 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.IdentityModel.Tokens 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.NET.Test.Sdk 18.8.1 | direkt in beiden Testprojekten; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.SqlServer.Server 1.0.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.Testing.Extensions.Telemetry 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Testing.Extensions.TrxReport.Abstractions 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Testing.Platform 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Testing.Platform.MSBuild 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.TestPlatform.ObjectModel 18.8.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.TestPlatform.TestHost 18.8.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT`; `ThirdPartyNotices.txt` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright und `ThirdPartyNotices.txt` mitführen; nicht mit dem Server ausliefern. |
| Microsoft.Win32.Registry 5.0.0 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen; nicht mit dem Server ausliefern. |
| ModelContextProtocol 2.2.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| ModelContextProtocol.Core 2.2.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog 4.4.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Extensions.Hosting 9.0.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Extensions.Logging 9.0.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Sinks.Console 6.1.1 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Sinks.File 7.0.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| System.ClientModel 1.0.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| System.Configuration.ConfigurationManager 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Diagnostics.EventLog 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Diagnostics.EventLog 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.IdentityModel.Tokens.Jwt 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| System.Memory.Data 1.0.2 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| System.Security.Cryptography.Pkcs 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Security.Cryptography.ProtectedData 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| xunit.runner.visualstudio 3.1.5 | direkt in beiden Testprojekten; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.assert 3.2.2 | direkt in beiden Testprojekten; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.common 3.2.2 | transitiv; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.core 3.2.2 | direkt in beiden Testprojekten; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.core.mtp-v1 3.2.2 | transitiv; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.extensibility.core 3.2.2 | transitiv; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.runner.common 3.2.2 | transitiv; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.runner.inproc.console 3.2.2 | transitiv; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |

## Ergebnis und explizite Benutzerentscheidung

Mit Ausnahme von `Microsoft.Data.SqlClient.SNI.runtime 6.0.2` verwenden alle
inventarisierten Pakete die permissiven SPDX-Ausdrücke MIT, Apache-2.0 oder
BSD-2-Clause. Für das SNI-Runtimepaket liefert der restaurierte Paketinhalt
jedoch die oben dokumentierten Microsoft Software License Terms. Diese sind
kostenlos nutzbar, aber keine permissive Open-Source-Lizenz und enthalten
zusätzliche Weitergabe-, Freistellungs- und Exportpflichten.

Am 2026-09-18 hat der Benutzer die Microsoft Software License Terms für
`Microsoft.Data.SqlClient.SNI.runtime` `6.0.2` ausdrücklich angenommen. Die
Annahme beschränkt sich auf dieses Paket und diese Version sowie die im
Inventar genannten Restriktionen: Objektcode nur als Teil der Anwendung
verteilen, gleichwertig schützende Endnutzer-/Distributorbedingungen verlangen,
Microsoft freistellen, Marken nicht verwenden, Exportregeln einhalten und
Drittanbieterhinweise beachten. Sie erklärt die Lizenz weder zu MIT noch zu
einer permissiven Open-Source-Lizenz und hebt keine der genannten Pflichten auf.
Es wurden keinerlei Paketversionen geändert.
