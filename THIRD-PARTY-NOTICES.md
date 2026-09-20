# Fremdkomponenten und Lizenzinventar

Stand: 2026-09-20. Dieses Inventar gehört zur MIT-`LICENSE` im Repository-Root
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
| Microsoft.CodeAnalysis.CSharp | 4.14.0 / 4.14.0 |
| Microsoft.Extensions.Configuration, `.Json`, Hosting, Options | 10.0.10 / 10.0.10 |
| ModelContextProtocol.AspNetCore, ModelContextProtocol, ModelContextProtocol.Core | 2.2.0 / 2.2.0 |
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
| AngleSharp 1.8.1 | transitiv in Web.Tests über bUnit; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Copyright beilegen; nicht mit dem Server ausliefern. |
| AngleSharp.Css 1.1.2 | transitiv in Web.Tests über bUnit; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Copyright beilegen; nicht mit dem Server ausliefern. |
| AngleSharp.Diffing 1.1.1 | transitiv in Web.Tests über bUnit; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Copyright beilegen; nicht mit dem Server ausliefern. |
| bunit 2.11.3 | direkt in Web.Tests; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Copyright beilegen; nicht mit dem Server ausliefern. |
| coverlet.collector 10.0.1 | direkt in allen Testprojekten; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Copyright beilegen; nicht mit dem Server ausliefern. |
| Dapper 2.1.66 | direkt in Storage; transitiv in Server/Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Markdig 0.42.0 | direkt in Core; transitiv in übrigen Projekten; P | NuGet.org; `.nuspec`: `expression: BSD-2-Clause` | BSD-2-Clause | Copyright, Bedingungen und Haftungsausschluss beilegen. |
| Microsoft.ApplicationInsights 2.23.0 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Bcl.AsyncInterfaces 1.1.1 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` unverändert mitführen. |
| Microsoft.Bcl.AsyncInterfaces 6.0.0 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen; nicht mit dem Server ausliefern. |
| Microsoft.Bcl.Cryptography 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` unverändert mitführen. |
| Microsoft.CodeAnalysis.Analyzers 3.11.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.CodeAnalysis.Common 4.14.0 | transitiv in Analyzers und Analyzers.Tests; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.CodeAnalysis.CSharp 4.14.0 | direkt in Analyzers und Analyzers.Tests; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
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
| Microsoft.Extensions.Configuration.Json 10.0.10 | direkt in Integrationstests; ASP.NET-Core-Shared-Framework im Server; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Configuration.UserSecrets 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.DependencyInjection 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.DependencyInjection.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Diagnostics 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Diagnostics.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.FileProviders.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.FileProviders.Physical 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.FileSystemGlobbing 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Hosting 10.0.10 | ASP.NET-Core-Shared-Framework im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Hosting.Abstractions 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Abstractions 10.0.10 | direkt in Storage; transitiv in Server/Tests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Configuration 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Console 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.Debug 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.EventLog 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Logging.EventSource 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| Microsoft.Extensions.Options 10.0.10 | ASP.NET-Core-Shared-Framework im Server; transitiv in Storage/Tests; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
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
| Microsoft.NET.Test.Sdk 18.8.1 | direkt in allen Testprojekten; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.NETCore.Platforms 1.1.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `licenseUrl` auf Microsoft-Lizenz | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Playwright 1.62.0 | direkt in BrowserTests; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.SqlServer.Server 1.0.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| Microsoft.Testing.Extensions.Telemetry 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Testing.Extensions.TrxReport.Abstractions 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Testing.Platform 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.Testing.Platform.MSBuild 1.9.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.TestPlatform.ObjectModel 18.8.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| Microsoft.TestPlatform.TestHost 18.8.1 | transitiv; D | NuGet.org; `.nuspec`: `expression: MIT`; `ThirdPartyNotices.txt` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright und `ThirdPartyNotices.txt` mitführen; nicht mit dem Server ausliefern. |
| Microsoft.Win32.Registry 5.0.0 | direkt in KnowHowToAI.TestSupport; zusätzlich transitiv über xunit; D | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen; nicht mit dem Server ausliefern. |
| ModelContextProtocol.AspNetCore 2.2.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| ModelContextProtocol 2.2.0 | direkt in Integrationstests; transitiv im Server; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| ModelContextProtocol.Core 2.2.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| NETStandard.Library 2.0.3 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `licenseUrl` auf .NET-Standard-Lizenz | MIT | Lizenz und .NET-Copyright beilegen; nicht mit dem Server ausliefern. |
| Serilog 4.4.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Extensions.Hosting 9.0.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Extensions.Logging 9.0.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Sinks.Console 6.1.1 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| Serilog.Sinks.File 7.0.0 | direkt im Server; transitiv in Integrationstests; P | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden. |
| System.ClientModel 1.0.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| System.Buffers 4.5.1 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `licenseUrl` auf CoreFX-Lizenz | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Collections.Immutable 9.0.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.ComponentModel.Annotations 5.0.0 | transitiv in BrowserTests; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Configuration.ConfigurationManager 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Diagnostics.EventLog 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Diagnostics.EventLog 10.0.10 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.IdentityModel.Tokens.Jwt 7.5.0 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| System.Memory.Data 1.0.2 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen. |
| System.Memory 4.5.5 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `licenseUrl` auf CoreFX-Lizenz | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Numerics.Vectors 4.5.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `licenseUrl` auf CoreFX-Lizenz | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Reflection.Metadata 9.0.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Runtime.CompilerServices.Unsafe 6.0.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Security.Cryptography.Pkcs 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Security.Cryptography.ProtectedData 9.0.4 | transitiv; P | NuGet.org; `.nuspec`: `expression: MIT`; `LICENSE.TXT`, `THIRD-PARTY-NOTICES.TXT` | MIT plus beigefügte Drittanbieterhinweise | MIT-Lizenz/Copyright sowie `THIRD-PARTY-NOTICES.TXT` mitführen. |
| System.Text.Encoding.CodePages 7.0.0 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `expression: MIT` | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| System.Threading.Tasks.Extensions 4.5.4 | transitiv in Analyzers; D | NuGet.org; `.nuspec`: `licenseUrl` auf CoreFX-Lizenz | MIT | Lizenz und Microsoft-Copyright beilegen; nicht mit dem Server ausliefern. |
| xunit.runner.visualstudio 3.1.5 | direkt in allen Testprojekten; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.assert 3.2.2 | direkt in allen Testprojekten; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.common 3.2.2 | transitiv; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
| xunit.v3.core 3.2.2 | direkt in allen Testprojekten; D | NuGet.org; `.nuspec`: `expression: Apache-2.0` | Apache-2.0 | Lizenz und Copyright beilegen; keine Paket-NOTICE vorhanden; nicht mit dem Server ausliefern. |
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

## npm-Frontend-Abhängigkeiten (M5.1-T3)

Der Restore wurde am 2026-09-20 mit `npm ci --ignore-scripts` aus
`src/KnowHowToAI.Server/Frontend/package-lock.json` gegen die öffentliche
npm-Registry ausgeführt. Das Lockfile enthält 236 `node_modules/*`-Einträge:
211 davon werden auf Windows installiert (direkt: `@milkdown/crepe 7.22.1`,
`esbuild 0.28.2`; alle übrigen Pakete transitiv), 25 sind optionale
esbuild-Plattformpakete für andere Betriebssysteme/Architekturen und werden
auf Windows nicht installiert. Die folgende Inventarliste wurde aus den jeweils
installierten `package.json` und den Paketdateien unter `node_modules`
ermittelt; sie ist absichtlich nach SPDX-Lizenz gruppiert und enthält jede
aufgelöste Name-/Versions-Kombination:

### MIT (232 Lockfile-Einträge; 207 installiert + 25 optional)

| Paket und Version | Paket-Lizenzdatei/NOTICE |
|---|---|
| `@babel/helper-string-parser 7.29.7`; `@babel/helper-validator-identifier 7.29.7`; `@babel/parser 7.29.9`; `@babel/types 7.29.8`; `@codemirror/autocomplete 6.20.3`; `@codemirror/commands 6.11.1`; `@codemirror/lang-angular 0.1.4`; `@codemirror/lang-cpp 6.0.3`; `@codemirror/lang-css 6.3.1`; `@codemirror/lang-go 6.0.1`; `@codemirror/lang-html 6.4.12`; `@codemirror/lang-java 6.0.2`; `@codemirror/lang-javascript 6.2.5`; `@codemirror/lang-jinja 6.0.1`; `@codemirror/lang-json 6.0.2`; `@codemirror/lang-less 6.0.2`; `@codemirror/lang-liquid 6.3.2`; `@codemirror/lang-markdown 6.5.2`; `@codemirror/lang-php 6.0.2`; `@codemirror/lang-python 6.2.1`; `@codemirror/lang-rust 6.0.2`; `@codemirror/lang-sass 6.0.2`; `@codemirror/lang-sql 6.10.0`; `@codemirror/lang-vue 0.1.3`; `@codemirror/lang-wast 6.0.2`; `@codemirror/lang-xml 6.1.0`; `@codemirror/lang-yaml 6.1.3`; `@codemirror/language 6.12.4`; `@codemirror/language-data 6.5.2`; `@codemirror/legacy-modes 6.5.4`; `@codemirror/lint 6.9.7`; `@codemirror/search 6.7.2`; `@codemirror/state 6.7.5`; `@codemirror/theme-one-dark 6.1.3`; `@codemirror/view 6.43.12` | Jeweilige `LICENSE`-Datei; keine zusätzliche NOTICE-Datei. |
| `@esbuild/win32-x64 0.28.2`; `@floating-ui/core 1.8.0`; `@floating-ui/dom 1.8.0`; `@floating-ui/utils 0.2.12`; `@jridgewell/sourcemap-codec 1.6.0`; `@lezer/common 1.5.2`; `@lezer/cpp 1.1.6`; `@lezer/css 1.3.6`; `@lezer/go 1.0.1`; `@lezer/highlight 1.2.3`; `@lezer/html 1.3.13`; `@lezer/java 1.1.4`; `@lezer/javascript 1.5.5`; `@lezer/json 1.0.3`; `@lezer/lr 1.4.10`; `@lezer/markdown 1.7.2`; `@lezer/php 1.0.6`; `@lezer/python 1.1.19`; `@lezer/rust 1.0.3`; `@lezer/sass 1.1.0`; `@lezer/xml 1.0.6`; `@lezer/yaml 1.0.4`; `@marijn/find-cluster-break 1.0.4` | Jeweilige `LICENSE`-Datei; esbuild-Plattformpaket enthält keinen separaten NOTICE-Text. |
| `@milkdown/components 7.22.1`; `@milkdown/core 7.22.1`; `@milkdown/crepe 7.22.1`; `@milkdown/ctx 7.22.1`; `@milkdown/exception 7.22.1`; `@milkdown/kit 7.22.1`; `@milkdown/plugin-block 7.22.1`; `@milkdown/plugin-clipboard 7.22.1`; `@milkdown/plugin-cursor 7.22.1`; `@milkdown/plugin-diff 7.22.1`; `@milkdown/plugin-history 7.22.1`; `@milkdown/plugin-indent 7.22.1`; `@milkdown/plugin-listener 7.22.1`; `@milkdown/plugin-slash 7.22.1`; `@milkdown/plugin-streaming 7.22.1`; `@milkdown/plugin-tooltip 7.22.1`; `@milkdown/plugin-trailing 7.22.1`; `@milkdown/plugin-upload 7.22.1`; `@milkdown/preset-commonmark 7.22.1`; `@milkdown/preset-gfm 7.22.1`; `@milkdown/prose 7.22.1`; `@milkdown/transformer 7.22.1`; `@milkdown/utils 7.22.1`; `@ocavue/utils 1.8.0` | Jeweilige `LICENSE`-Datei; keine zusätzliche NOTICE-Datei. |
| `@types/debug 4.1.13`; `@types/hast 3.0.5`; `@types/katex 0.16.8`; `@types/lodash 4.17.25`; `@types/lodash-es 4.17.12`; `@types/mdast 4.0.4`; `@types/ms 2.1.0`; `@types/trusted-types 2.0.7`; `@types/unist 3.0.3`; `@vue/compiler-core 3.5.43`; `@vue/compiler-dom 3.5.43`; `@vue/compiler-sfc 3.5.43`; `@vue/compiler-ssr 3.5.43`; `@vue/reactivity 3.5.43`; `@vue/runtime-core 3.5.43`; `@vue/runtime-dom 3.5.43`; `@vue/server-renderer 3.5.43`; `@vue/shared 3.5.43` | Jeweilige `LICENSE`-Datei; keine zusätzliche NOTICE-Datei. |
| `bail 2.0.2`; `ccount 2.0.1`; `character-entities 2.0.2`; `clsx 2.1.1`; `codemirror 6.0.2`; `commander 15.0.0`; `commander 8.3.0`; `crelt 1.0.7`; `csstype 3.2.3`; `debug 4.4.3`; `decode-named-character-reference 1.3.0`; `dequal 2.0.3`; `devlop 1.1.0`; `esbuild 0.28.2`; `escape-string-regexp 5.0.0`; `estree-walker 2.0.2`; `extend 3.0.2`; `is-plain-obj 4.1.0`; `katex 0.16.47`; `katex 0.18.7`; `lodash-es 4.18.1`; `longest-streak 3.1.0`; `magic-string 0.30.21`; `markdown-table 3.0.4`; `mdast-util-definitions 6.0.0`; `mdast-util-find-and-replace 3.0.2`; `mdast-util-from-markdown 2.0.3`; `mdast-util-gfm 3.1.0`; `mdast-util-gfm-autolink-literal 2.0.1`; `mdast-util-gfm-footnote 2.1.0`; `mdast-util-gfm-strikethrough 2.0.0`; `mdast-util-gfm-table 2.0.0`; `mdast-util-gfm-task-list-item 2.0.0`; `mdast-util-math 3.0.0`; `mdast-util-phrasing 4.1.0`; `mdast-util-to-markdown 2.1.2`; `mdast-util-to-string 4.0.0` | Jeweilige `LICENSE`/`license`-Datei; esbuild verwendet `LICENSE.md`; keine zusätzliche NOTICE-Datei. |
| `micromark 4.0.2`; `micromark-core-commonmark 2.0.3`; `micromark-extension-gfm 3.0.0`; `micromark-extension-gfm-autolink-literal 2.1.0`; `micromark-extension-gfm-footnote 2.1.0`; `micromark-extension-gfm-strikethrough 2.1.0`; `micromark-extension-gfm-table 2.1.2`; `micromark-extension-gfm-tagfilter 2.0.0`; `micromark-extension-gfm-task-list-item 2.1.0`; `micromark-extension-math 3.1.0`; `micromark-factory-destination 2.0.1`; `micromark-factory-label 2.0.1`; `micromark-factory-space 2.0.1`; `micromark-factory-title 2.0.1`; `micromark-factory-whitespace 2.0.1`; `micromark-util-character 2.1.1`; `micromark-util-chunked 2.0.1`; `micromark-util-classify-character 2.0.1`; `micromark-util-combine-extensions 2.0.1`; `micromark-util-decode-numeric-character-reference 2.0.2`; `micromark-util-decode-string 2.0.1`; `micromark-util-encode 2.0.1`; `micromark-util-html-tag-name 2.0.1`; `micromark-util-normalize-identifier 2.0.1`; `micromark-util-resolve-all 2.0.1`; `micromark-util-sanitize-uri 2.0.1`; `micromark-util-subtokenize 2.1.0`; `micromark-util-symbol 2.0.1`; `micromark-util-types 2.0.2`; `ms 2.1.3`; `nanoid 3.3.19`; `nanoid 6.0.1`; `orderedmap 2.1.1`; `postcss 8.5.28` | Jeweilige `license`/`LICENSE`-Datei; `ms` liefert `license.md`; keine zusätzliche NOTICE-Datei. |
| `prosemirror-changeset 2.4.3`; `prosemirror-commands 1.7.2`; `prosemirror-drop-indicator 0.1.4`; `prosemirror-dropcursor 1.8.3`; `prosemirror-gapcursor 1.4.1`; `prosemirror-history 1.5.0`; `prosemirror-inputrules 1.5.1`; `prosemirror-keymap 1.2.3`; `prosemirror-model 1.25.11`; `prosemirror-safari-ime-span 1.0.2`; `prosemirror-schema-list 1.5.1`; `prosemirror-state 1.4.4`; `prosemirror-tables 1.8.5`; `prosemirror-transform 1.12.1`; `prosemirror-view 1.42.4`; `prosemirror-virtual-cursor 0.4.2`; `remark 15.0.1`; `remark-gfm 4.0.1`; `remark-inline-links 7.0.0`; `remark-math 6.0.0`; `remark-parse 11.0.0`; `remark-stringify 11.0.0`; `rope-sequence 1.3.4`; `style-mod 4.1.4`; `trough 2.2.0`; `unified 11.0.5`; `unist-util-is 6.0.1`; `unist-util-remove-position 5.0.0`; `unist-util-stringify-position 4.0.0`; `unist-util-visit 5.1.0`; `unist-util-visit-parents 6.0.2`; `vfile 6.0.3`; `vfile-message 4.0.3`; `vue 3.5.43`; `w3c-keyname 2.2.8`; `zwitch 2.0.4` | Jeweilige `LICENSE`/`license`-Datei; `remark-math` enthält keinen separaten Dateinamen; keine zusätzliche NOTICE-Datei. |

### Optionale esbuild-Plattformpakete im Lockfile (25, nicht auf Windows installiert)

| Paket und Version | Lizenz-/NOTICE-Einordnung |
|---|---|
| `@esbuild/aix-ppc64 0.28.2`; `@esbuild/android-arm 0.28.2`; `@esbuild/android-arm64 0.28.2`; `@esbuild/android-x64 0.28.2`; `@esbuild/darwin-arm64 0.28.2`; `@esbuild/darwin-x64 0.28.2`; `@esbuild/freebsd-arm64 0.28.2`; `@esbuild/freebsd-x64 0.28.2`; `@esbuild/linux-arm 0.28.2`; `@esbuild/linux-arm64 0.28.2`; `@esbuild/linux-ia32 0.28.2`; `@esbuild/linux-loong64 0.28.2`; `@esbuild/linux-mips64el 0.28.2`; `@esbuild/linux-ppc64 0.28.2`; `@esbuild/linux-riscv64 0.28.2`; `@esbuild/linux-s390x 0.28.2`; `@esbuild/linux-x64 0.28.2`; `@esbuild/netbsd-arm64 0.28.2`; `@esbuild/netbsd-x64 0.28.2`; `@esbuild/openbsd-arm64 0.28.2`; `@esbuild/openbsd-x64 0.28.2`; `@esbuild/openharmony-arm64 0.28.2`; `@esbuild/sunos-x64 0.28.2`; `@esbuild/win32-arm64 0.28.2`; `@esbuild/win32-ia32 0.28.2` | Alle `MIT` laut Lockfile; optionale Plattformpakete enthalten keinen separaten NOTICE-Text. Sie sind für die vollständige Lockfile-Inventur erfasst, werden aber nicht als Windows-Produktdateien verteilt. |

### Weitere SPDX-Lizenzen

| Lizenz | Paket und Version | Paket-Lizenzdatei/NOTICE und Einordnung |
|---|---|---|
| `(MPL-2.0 OR Apache-2.0)` | `dompurify 3.4.15` | `LICENSE` und `LICENSE-MPL`; für die Distribution wird die Apache-2.0-Option verwendet, keine zusätzliche NOTICE-Datei. |
| `BSD-2-Clause` | `entities 7.0.1` | `LICENSE`; BSD-2-Clause ist mit MIT kompatibel, Lizenztext/Copyright bleibt beizulegen. |
| `ISC` | `picocolors 1.1.1` | `LICENSE`; ISC ist mit MIT kompatibel, Lizenztext/Copyright bleibt beizulegen. |
| `BSD-3-Clause` | `source-map-js 1.2.1` | `LICENSE`; BSD-3-Clause ist mit MIT kompatibel, Lizenztext/Copyright bleibt beizulegen. |

Alle 236 Lockfile-Einträge verwenden damit MIT, Apache-2.0 (als zulässige Option
bei DOMPurify), BSD-2-Clause, BSD-3-Clause oder ISC. Keine Paketversion bringt
eine zusätzliche `NOTICE`-Datei mit; die vorhandenen Paket-Lizenzdateien und
Copyright-Hinweise sind bei einer Distribution neben diesem Inventar
beizulegen. Es wurde keine kostenpflichtige, GPL-/LGPL- oder sonstige nicht
kompatible Lizenz aufgenommen. Der esbuild-Build läuft ausschließlich lokal;
die resultierende Datei wird selbst gehostet und enthält keinen CDN- oder
Runtime-Download.

Die Zählung ist reproduzierbar, ohne ein zusätzliches Repository-Skript zu
benötigen: `node -e "const fs=require('fs');const l=JSON.parse(fs.readFileSync('src/KnowHowToAI.Server/Frontend/package-lock.json'));const p=Object.keys(l.packages).filter(x=>x.startsWith('node_modules/'));console.log({lockfileEntries:p.length, optionalEsbuild:p.filter(x=>x.startsWith('node_modules/@esbuild/')&&!x.endsWith('win32-x64')).length});"` ergibt
`{ lockfileEntries: 236, optionalEsbuild: 25 }`. Der anschließende
`npm ci --ignore-scripts`-Restore auf Windows ergibt 211 installierte
`node_modules`-Einträge; die Differenz besteht ausschließlich aus den oben
aufgeführten optionalen Plattformpaketen.
