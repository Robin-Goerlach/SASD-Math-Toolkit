# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breite numerische Grundlage (historischer Algorithmuskatalog erledigt)

Implementiert sind Nullstellensuche, Polynom-/Horner-/Deflationsfunktionen, Interpolation/Splines, Differentiation, Integration, lineare Algebra/Eigenwerte, RK4/RKF45/Adams, Least Squares, FFT, Faltung/Korrelation und die Demo-Ebene.

## M2 – Veröffentlichungsqualität für V1 (nur Abschlussaudit offen)

Der historische Kompatibilitätskatalog und alle elf geplanten Kapitel des Benutzerhandbuchs sind vollständig. Die fachbezogenen API-/Test-Audits und die gemeinsame Diagnostikdokumentation sind ebenfalls abgeschlossen.

Vor dem Abschlussaudit wurde eine pragmatische Performance-Runde durchgeführt. Sie entfernt wiederholte geprüfte Matrixzugriffe aus dichten Hot Loops, löst mehrere LU-Rechte-Seiten gemeinsam, cached Least-Squares-Designwerte, spezialisiert die Polynom-Basiserzeugung und vermeidet redundante FFT-/Faltungs-Kopien, ohne die öffentlichen Defensive-Copy- und Validierungsverträge aufzugeben. Details stehen in [`PERFORMANCE.md`](PERFORMANCE.md). Bewusst nicht enthalten sind unsafe/SIMD/Parallel-/Native-Optimierungen ohne repräsentative Benchmarks.

Als verbleibender V1-Meilenstein folgt das **abschließende repositoryweite Release-Audit**. Dabei sollen öffentliche API-Konsistenz, XML-Kommentare, Beispiele, Handbuchlinks, technische Dokumente, Kompatibilitätsaussagen, das Fehlen öffentlicher Platzhalter, Package-/Release-Metadaten, deterministisches Sample-Verhalten und CI-Abdeckung geprüft werden, bevor ein V1-Tag vergeben wird.

V1 ist erst fertig, wenn jeder öffentliche Algorithmus getestet und dokumentiert ist, wichtige Fehler-/Abbruchzustände regressionstestet sind, keine öffentlichen Platzhalter existieren, die Diagnosekonventionen konsistent sind, die pragmatische Performance-Runde abgeschlossen ist und das Abschlussaudit erledigt wurde.

## M3 – SASD-eigene Erweiterungen

Danach folgen moderne Anforderungen aus der Produktfamilie: Vektoren/Matrizen/Transformationen für Game und Grafik, Geometrie, Kurven, Statistik-Grundlagen, Simulation/Random, robuste Toleranzwerkzeuge sowie unterstützende Mathematik für Sicherheitssoftware.

## M4 – Weitere Sprachen

Geplante Reihenfolge, sofern kein konkretes Projekt andere Prioritäten erzwingt: C++, Java, JavaScript/TypeScript, Fortran. Ab der zweiten Sprache werden sprachübergreifende Golden-Tests und maschinenlesbare Spezifikationen wichtig.
