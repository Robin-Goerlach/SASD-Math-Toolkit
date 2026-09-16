# Release-Candidate-Audit

Dieses Dokument hält das repositoryweite Audit für die erste öffentliche 1.0-Linie fest. Geprüft wird der tatsächliche modernisierte Release Candidate und nicht mehr nur der frühere historische Zwischenstand.

## Release-Grenze

Der Kandidat enthält das abgeschlossene klassische Numerical-Methods-Fundament sowie die modernen Grundlagen M3.1 für dichte und M3.2 für dünnbesetzte lineare Algebra. M3.3 und spätere Modernisierungsschritte bleiben nachgelagerter Umfang, sofern das Audit keinen eng begrenzten notwendigen Fix aufdeckt.

Der Paketbezeichner des Audit-Kandidaten lautet `1.0.0-rc.1`. Der unabhängige Windows-Abnahmelauf wurde erfolgreich abgeschlossen. Ein endgültiges `1.0.0`-Paket/Tag/Release wird bewusst erst nach ausdrücklicher Release-Freigabe erzeugt.

## In Audit-Phase 1 adressierte Findings

- **Paketidentität:** Die bisherige Entwicklungsversionsnummer `0.1.0` wird durch den expliziten Kandidaten `1.0.0-rc.1` ersetzt.
- **NuGet-Metadaten:** Titel, Repository-/Projekt-URLs, Tags, License-Acceptance-Regel, Package-README und Symbolpaket-Einstellungen sind explizit definiert.
- **Paketprüfung:** Die normale CI erzeugt jetzt `.nupkg` und `.snupkg` und prüft das Paket auf Assembly, XML-Dokumentation, README, Package-ID und MIT-Lizenzausdruck.
- **Ausführbarer Smoke-Pfad:** Nach den Tests startet die CI das Sample, prüft SVG-Ausgabe im erzeugten HTML und führt den modernen Sparse-Referenzsolve aus.
- **Workflow-Runtime:** Der normale .NET-Workflow verwendet die beim Audit verifizierten aktuellen Node-24-Action-Major-Versionen (`actions/checkout@v7`, `actions/setup-dotnet@v6`) statt der veralteten Node-20-Generation.
- **Repository-Sauberkeit:** Erzeugte Release-Artefakte unter `artifacts/` werden ignoriert.
- **Implementierungs-Platzhalter:** Die Repository-Suche ergab keine offensichtlichen öffentlichen `NotImplementedException`-/TODO-artigen Implementierungsplatzhalter. Das ist ein Audit-Befund und keine Aussage, dass keine zukünftige Arbeit mehr existiert.
- **Lizenz/Provenienz:** MIT-Lizenzierung und Clean-Room-Regel für historische Referenzen bleiben mit der erklärten Implementierungsstrategie konsistent.

## In Audit-Phase 2 adressierte Findings — Handbuch-/PDF-Gate

- **Handbuch-Synchronisierung:** Der LaTeX-Build enthält jetzt die Markdown-Handbuchkapitel 12 und 13 zu moderner dichter und dünnbesetzter linearer Algebra. Front-/Backmatter kennzeichnen die Ausgabe als Release Candidate `1.0.0-rc.1`.
- **Deterministischer Publikationspfad:** Änderungen an Markdown-Handbuchkapiteln, LaTeX-Quellen oder dem Handbuch-Workflow bauen auf `main` beide Ausgaben neu.
- **PDF-Prüfung:** Die CI prüft vor der Veröffentlichung, dass beide erzeugten PDFs nicht leer und mit `pdfinfo` strukturell lesbar sind.
- **Release-Inspektionsartefakt:** Jeder Handbuch-Build lädt beide erzeugten PDFs zusätzlich als aufbewahrtes GitHub-Actions-Artefakt hoch. Dadurch können exakt die Build-Ausgaben unabhängig von den eingecheckten Kopien geprüft werden.
- **CI-Mutationskontrolle:** Die Handbuch-CI ist nur lesend. Sie baut, prüft und lädt PDFs als Artefakte hoch, schreibt neu erzeugte Binärdateien aber nicht automatisch nach `main`. Producer-Metadaten in PDFs könnten sonst Binäränderungen ohne redaktionelle Änderung erzeugen. Eingecheckte PDF-Snapshots werden deshalb bewusst an RC-/Release-Grenzen nach der Prüfung aktualisiert.
- **Visuelle Prüfung:** Das RC1-Artefakt wurde seitenweise gerendert. Die englische Ausgabe besitzt 81 PDF-Seiten, die deutsche Ausgabe 79 PDF-Seiten. Die Kontaktbogenprüfung sämtlicher gerenderter Seiten sowie die Vollbildprüfung releasekritischer Seiten ergaben keine abgeschnittenen Texte, Überlagerungen, schwarzen Kästchen, defekten Glyphen oder fehlenden Kapitelinhalte. Titelseiten, Inhaltsverzeichnisse, Kapitel 12/13, Anhänge und Herkunftsseiten werden korrekt dargestellt.
- **Eingecheckte Ausgaben:** Die neu erzeugten deutschen und englischen RC1-PDFs sind mit dem aktuellen Markdown-/LaTeX-Stand des Release Candidates synchronisiert.

## In Audit-Phase 3 adressierte Findings — Windows-Abnahme

Die unabhängige Windows-Abnahme wurde am 16.09.2026 gegen den exakten Commit `29deb0922e6384c7ff0592e9156d2250b698cfd7` durchgeführt.

- **Umgebung:** Windows x64, .NET SDK `10.0.303`, .NET Runtime `10.0.11`. Das ergänzt die CI-Abdeckung auf Ubuntu 24.04 mit SDK `10.0.401` und Runtime `10.0.12`.
- **Restore/Build:** `dotnet restore` und der Release-Build wurden erfolgreich abgeschlossen.
- **Tests:** `178/178` Tests bestanden; 0 fehlgeschlagen, 0 übersprungen.
- **Ausführbare Demo:** Der deterministische HTML-/SVG-Report wurde erfolgreich erzeugt. Der moderne Sparse-Referenzlauf schloss mit GMRES nach 17 Iterationen, BiCGSTAB nach 10 Iterationen und einem Residuum von `1.40225E-09` ab.
- **Paketerzeugung:** `dotnet pack --no-build` erzeugte unter Windows erfolgreich sowohl `Sasd.Math.Toolkit.1.0.0-rc.1.nupkg` als auch `Sasd.Math.Toolkit.1.0.0-rc.1.snupkg`.
- **Repository-Sauberkeit:** `git status --short` war nach der Abnahme leer.

Aus dem Windows-Abnahmelauf ergab sich kein releaseblockierendes Finding.

## Explizit nicht blockierende technische Schulden

Eine vollständige CS1591-Erzwingung wird für diesen Release Candidate noch nicht aktiviert. Die modernen öffentlichen APIs der Modernisierungsrunde sind dokumentiert; Teile der älteren öffentlichen Oberfläche stützen sich weiterhin auf Handbuch und technische Dokumentation statt auf lückenlose XML-Kommentare. Die Unterdrückung bleibt deshalb sichtbar im Projekt, statt vollständige Abdeckung vorzutäuschen.

CSC-Speicherung, Incomplete-Cholesky-/ILU-artige Preconditioner, zusätzliche Krylov-Verfahren, benchmarkbasierte Performance-Aussagen und optionale BLAS-/LAPACK-Backends liegen bewusst außerhalb dieser Release-Grenze.

## Noch offene Release-Gates

1. Ausdrückliche Release-Freigabe für den Übergang auf `1.0.0` erteilen.
2. `-rc.1` entfernen und gegen den resultierenden Commit noch einmal Release-Build/Test/Package/Sample sowie die Handbuch-Gates ausführen.
3. Erst nach vollständig grüner finaler CI das `1.0.0`-Tag und GitHub-Release erzeugen.
4. Separat entscheiden, ob und wo das NuGet-Paket veröffentlicht wird.

## Release-Prinzip

Ein grüner Build allein ist kein Release-Kriterium. Der Kandidat benötigt eine grüne Release-Build/Test/Package/Sample-Pipeline, synchronisierte Dokumentation, geprüfte PDFs und eine erfolgreiche Abnahme auf einem unabhängigen Windows-Entwicklungsrechner. RC1 hat diese Kandidaten-Gates jetzt bestanden; die Hochstufung auf `1.0.0` bleibt eine ausdrückliche Release-Entscheidung.
