# Release-Audit — 1.0.0

Dieses Dokument hält das repositoryweite Audit für die erste stabile 1.0-Linie fest. Geprüft wurde der tatsächliche modernisierte Release Candidate; das Audit wurde anschließend bis zur finalen Hochstufung auf `1.0.0` fortgeführt.

## Release-Grenze

Das 1.0-Release enthält das abgeschlossene klassische Numerical-Methods-Fundament sowie die modernen Grundlagen M3.1 für dichte und M3.2 für dünnbesetzte lineare Algebra. M3.3 und spätere Modernisierungsschritte bleiben nachgelagerter Umfang, sofern kein eng begrenzter Wartungsfix erforderlich ist.

Der geprüfte Paketbezeichner lautet `1.0.0`. Der vorherige Kandidat war `1.0.0-rc.1`; dieser hatte die unabhängige Windows-Abnahme bereits abgeschlossen, bevor am 16.09.2026 die stabile Freigabe autorisiert wurde.

## In Audit-Phase 1 adressierte Findings — Paket und Repository

- **Paketidentität:** Die Entwicklungsversionsnummer `0.1.0` wurde zunächst durch `1.0.0-rc.1` und nach erfolgreicher Abnahme durch die stabile Version `1.0.0` ersetzt.
- **NuGet-Metadaten:** Titel, Repository-/Projekt-URLs, Tags, License-Acceptance-Regel, Package-README und Symbolpaket-Einstellungen sind explizit definiert.
- **Paketprüfung:** Die normale CI erzeugt `.nupkg` und `.snupkg` und prüft das Paket auf Assembly, XML-Dokumentation, README, Package-ID, exakte Version und MIT-Lizenzausdruck.
- **Ausführbarer Smoke-Pfad:** Nach den Tests startet die CI das Sample, prüft SVG-Ausgabe im erzeugten HTML und führt den modernen Sparse-Referenzsolve aus.
- **Workflow-Runtime:** Der normale .NET-Workflow verwendet die beim Audit verifizierten aktuellen Node-24-Action-Major-Versionen (`actions/checkout@v7`, `actions/setup-dotnet@v6`).
- **Repository-Sauberkeit:** Erzeugte Release-Artefakte unter `artifacts/` werden ignoriert.
- **Implementierungs-Platzhalter:** Die Repository-Suche ergab keine offensichtlichen öffentlichen `NotImplementedException`-/TODO-artigen Implementierungsplatzhalter. Das ist ein Audit-Befund und keine Aussage, dass keine zukünftige Arbeit mehr existiert.
- **Lizenz/Provenienz:** MIT-Lizenzierung und Clean-Room-Regel für historische Referenzen bleiben mit der erklärten Implementierungsstrategie konsistent.

## In Audit-Phase 2 adressierte Findings — Handbuch-/PDF-Gate

- **Handbuch-Synchronisierung:** Der LaTeX-Build enthält die Markdown-Handbuchkapitel 12 und 13 zu moderner dichter und dünnbesetzter linearer Algebra.
- **Redaktionelle Quelle:** Markdown bleibt die laufend gepflegte Quelle der Wahrheit; LaTeX liefert publikationsspezifisches Front-/Backmatter, Kapitelreihenfolge und Satz.
- **PDF-Prüfung:** Die CI prüft, dass beide erzeugten PDFs nicht leer und mit `pdfinfo` strukturell lesbar sind.
- **Inspektionsartefakt:** Handbuch-Builds laden beide PDFs als aufbewahrte GitHub-Actions-Artefakte hoch, sodass die exakten Build-Ausgaben unabhängig von den eingecheckten Kopien geprüft werden können.
- **CI-Mutationskontrolle:** Die normale Handbuch-CI arbeitet nur lesend. PDF-Snapshots werden bewusst nur an geprüften RC-/Release-Grenzen eingecheckt.
- **Visuelle RC1-Prüfung:** Die Release-Candidate-Handbücher wurden seitenweise gerendert und ohne Befund auf abgeschnittene Texte, Überlagerungen, schwarze Kästchen, defekte Glyphen und fehlende releasekritische Inhalte geprüft.

## In Audit-Phase 3 adressierte Findings — Windows-Abnahme

Die unabhängige Windows-Abnahme wurde am 16.09.2026 gegen den exakten RC1-Code-Commit `29deb0922e6384c7ff0592e9156d2250b698cfd7` durchgeführt.

- **Umgebung:** Windows x64, .NET SDK `10.0.303`, .NET Runtime `10.0.11`. Das ergänzte die CI-Abdeckung auf Ubuntu 24.04 mit SDK `10.0.401` und Runtime `10.0.12`.
- **Restore/Build:** `dotnet restore` und der Release-Build wurden erfolgreich abgeschlossen.
- **Tests:** `178/178` Tests bestanden; 0 fehlgeschlagen, 0 übersprungen.
- **Ausführbare Demo:** Der deterministische HTML-/SVG-Report wurde erfolgreich erzeugt. Der moderne Sparse-Referenzlauf schloss mit GMRES nach 17 Iterationen, BiCGSTAB nach 10 Iterationen und einem Residuum von `1.40225E-09` ab.
- **Paketerzeugung:** `dotnet pack --no-build` erzeugte unter Windows erfolgreich RC1-Paket und Symbolpaket.
- **Repository-Sauberkeit:** `git status --short` war nach der Abnahme leer.

Aus dem Windows-Abnahmelauf ergab sich kein releaseblockierendes Finding.

## In Audit-Phase 4 adressierte Findings — stabile 1.0.0-Hochstufung

Die ausdrückliche Release-Freigabe wurde am 16.09.2026 erteilt. Der stabile Promotion-Commit lautet `dc714dc4948fea907f8ad64a86550f33b724aa8a`.

- **Stabile Paketidentität:** Der Suffix `rc.1` wurde entfernt; das Projekt wird nun als `Sasd.Math.Toolkit` Version `1.0.0` gepackt.
- **Finales Linux-Gate:** Der Promotion-Commit bestand Release-Restore/Build/Test/Package/Sample auf Ubuntu 24.04 mit .NET SDK `10.0.401` und Runtime `10.0.12`.
- **Build-Qualität:** Der finale Release-Build wurde mit **0 Warnungen und 0 Fehlern** abgeschlossen.
- **Regressionstests:** **178/178 Tests bestanden**.
- **Finale Pakete:** Die CI erzeugte und prüfte `Sasd.Math.Toolkit.1.0.0.nupkg` und `Sasd.Math.Toolkit.1.0.0.snupkg`, einschließlich einer expliziten nuspec-Prüfung auf Version `1.0.0`. Die Pakete werden als Release-Artefakt aufbewahrt.
- **Finaler Smoke-Pfad:** Das Sample blieb erfolgreich mit GMRES nach 17 Iterationen, BiCGSTAB nach 10 Iterationen und Residuum `1.40225E-09`.
- **Finale Handbücher:** Deutsches und englisches Frontmatter weisen jetzt Handbuchversion 1.0 und Paketversion `1.0.0` aus. Der finale Handbuch-Build war erfolgreich, bestand die strukturelle Prüfung und erzeugte aufbewahrte Release-Artefakte.
- **Finale PDF-Snapshots:** Der Handbuch-Workflow checkte die geprüften finalen PDF-Snapshots mit Commit `618ba1b9ec8d4d0a3d053e8210e01c8bb3cff3b0` ein.
- **Finale PDF-Prüfung:** Die deutsche Ausgabe umfasst 79 Seiten, die englische 81 Seiten. Sämtliche Seiten wurden gerendert und über Kontaktbögen geprüft; releasekritische Titel-/Statusseiten wurden zusätzlich in voller Größe kontrolliert. Es wurden keine abgeschnittenen Inhalte, Überlagerungen, defekten Glyphen oder fehlenden Inhalte gefunden. Der Vergleich mit RC1 zeigte die erwarteten Änderungen an Publikationsmetadaten/Fußzeilen ohne strukturelle Änderung der Seitenzahl.
- **Workflow-Härtung:** Nach der einmaligen Veröffentlichung der finalen Snapshots wird die Handbuch-CI wieder auf ausschließlich lesenden Betrieb zurückgestellt.

## Explizit nicht blockierende technische Schulden

Eine vollständige CS1591-Erzwingung wird für 1.0.0 noch nicht aktiviert. Die modernen öffentlichen APIs der Modernisierungsrunde sind dokumentiert; Teile der älteren öffentlichen Oberfläche stützen sich weiterhin auf Handbuch und technische Dokumentation statt auf lückenlose XML-Kommentare. Die Unterdrückung bleibt sichtbar im Projekt, statt vollständige Abdeckung vorzutäuschen.

CSC-Speicherung, Incomplete-Cholesky-/ILU-artige Preconditioner, zusätzliche Krylov-Verfahren, benchmarkbasierte Performance-Aussagen und optionale BLAS-/LAPACK-Backends liegen bewusst außerhalb der 1.0-Release-Grenze.

## Verbleibende Veröffentlichungsschritte

Die geprüften `1.0.0`-Inhalte sind vollständig. Als verbleibender Repository-Publikationsschritt ist das `v1.0.0`-Git-Tag/GitHub-Release gegen den finalen grünen Release-Finalisierungscommit zu erzeugen. Die Veröffentlichung des NuGet-Pakets ist eine separate Entscheidung und wird durch das GitHub-Release nicht automatisch vorausgesetzt.

## Release-Prinzip

Ein grüner Build allein ist kein Release-Kriterium. Version 1.0.0 erreichte die stabile Grenze erst nach Release-Build/Test/Package/Sample-Validierung, synchronisierter zweisprachiger Dokumentation, struktureller und visueller PDF-Prüfung sowie einer unabhängigen Windows-Abnahme.
