# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breite numerische Grundlage (historischer Algorithmuskatalog erledigt)

Implementiert sind reelle und komplexe Nullstellensuche, wiederverwendbare Polynom-/Horner-/Deflationsfunktionen, Interpolation/Splines, der historische V1-Differentiationsbereich, Integration, lineare Algebra mit LU-Faktorisierung, der historische Eigenwertbereich, RK4/RKF45/Adams-Verfahren, Least Squares und FFT.

Die RK4-Komfortfamilie deckt skalare Gleichungen erster, zweiter und n-ter Ordnung sowie gekoppelte Systeme erster und zweiter Ordnung über einen gemeinsamen System-RK4-Kern ab. Hinzu kommen adaptives RKF45, Adams-Bashforth/Adams-Moulton, lineares und nichtlineares Shooting, die vollständige historische Least-Squares-Modellgruppe sowie komplexe/reelle FFT, kompaktes Realspektrum, Faltung und Kreuzkorrelation.

## M2 – Veröffentlichungsqualität für V1 (nur Abschlussaudit offen)

Der historische Kompatibilitätskatalog einschließlich grafischer Demo-Ebene ist implementiert. Publication-Quality-Handbuch-/API-Audits sind für Nullstellensuche, Interpolation, numerische Integration, Matrizen/lineare Gleichungssysteme sowie Eigenwerte/Eigenvektoren abgeschlossen.

Auch der fachübergreifende Diagnostik-Meilenstein ist abgeschlossen. Das Handbuch beschreibt jetzt Exception-vs.-Status-Semantik, Residuen gegenüber Vorwärtsfehlern, Fehlerschätzungen, Skalierung von Toleranzen, numerische Breakdowns, Konditionierung, unabhängige Prüfungen und Reproduzierbarkeit. Der gemeinsame Vertrag `IterativeResult<T>` besitzt nun ausdrücklich `HasFiniteResidual`; `IterationStatus` und `NumericConstants` dokumentieren ihre beabsichtigte Semantik in den öffentlichen API-Kommentaren.

Alle elf geplanten V1-Kapitel des Benutzerhandbuchs sind jetzt in Englisch und Deutsch vorhanden.

Als verbleibender V1-Meilenstein folgt ein **abschließendes repositoryweites Release-Audit**. Dabei sollen öffentliche API-Konsistenz, XML-Kommentare, Beispiele, Handbuchlinks, technische Dokumente, Kompatibilitätsaussagen, das Fehlen öffentlicher Platzhalter, Package-/Release-Metadaten, deterministisches Sample-Verhalten und CI-Abdeckung geprüft werden, bevor ein V1-Tag vergeben wird.

V1 ist erst fertig, wenn jeder öffentliche Algorithmus getestet und dokumentiert ist, wichtige Fehler-/Abbruchzustände regressionstestet sind, die stabilen V1-Bereiche im Benutzerhandbuch beschrieben sind, keine öffentlichen Platzhalter mit `NotImplementedException` existieren, die Diagnosekonventionen konsistent sind und das Abschlussaudit erledigt ist.

## M3 – SASD-eigene Erweiterungen

Danach folgen die modernen Anforderungen aus der Produktfamilie: Vektoren/Matrizen/Transformationen für Game und Grafik, Geometrie, Kurven, Statistik-Grundlagen, Simulation/Random, robuste Toleranzwerkzeuge sowie unterstützende Mathematik für Sicherheitssoftware.

## M4 – Weitere Sprachen

Geplante Reihenfolge, sofern kein konkretes Projekt andere Prioritäten erzwingt: C++, Java, JavaScript/TypeScript, Fortran. Ab der zweiten Sprache werden sprachübergreifende Golden-Tests und maschinenlesbare Spezifikationen wichtig.
