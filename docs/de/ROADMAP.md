# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breite numerische Grundlage (historischer Algorithmuskatalog erledigt)

Implementiert sind reelle und komplexe Nullstellensuche, wiederverwendbare Polynom-/Horner-/Deflationsfunktionen, Interpolation/Splines, der historische V1-Differentiationsbereich, Integration, lineare Algebra mit LU-Faktorisierung, der historische Eigenwertbereich, RK4/RKF45/Adams-Verfahren, Least Squares und FFT.

Die RK4-Komfortfamilie deckt skalare Gleichungen erster, zweiter und n-ter Ordnung sowie gekoppelte Systeme erster und zweiter Ordnung über einen gemeinsamen System-RK4-Kern ab. Hinzu kommen adaptives RKF45, Adams-Bashforth/Adams-Moulton, lineares und nichtlineares Shooting, die vollständige historische Least-Squares-Modellgruppe sowie komplexe/reelle FFT, kompaktes Realspektrum, Faltung und Kreuzkorrelation.

## M2 – Veröffentlichungsqualität für V1 (in Arbeit)

Der historische Kompatibilitätskatalog einschließlich grafischer Demo-Ebene ist implementiert. Die Arbeit konzentriert sich deshalb jetzt auf die Qualität einer veröffentlichbaren V1 statt auf weitere historische Algorithmen.

Das eigenständige C#/.NET-Benutzerhandbuch unter `docs/user-guide/` wird Fachbereich für Fachbereich vervollständigt. Nullstellensuche, Interpolation, numerische Integration sowie Matrizen/lineare Gleichungssysteme besitzen jetzt ausführliche Handbuchkapitel und API-/Test-Audits. Beim linearen Algebra-Audit wurden insbesondere die Endlichkeitsinvariante von Matrizen, direkte Solver-Validierung, LU-Diagnosezustand, Residualprüfung und Gauss-Seidel-Breakdown-Erkennung gehärtet, ohne die Referenzimplementierung unnötig zu verkomplizieren.

Als Nächstes folgen die Eigenwerte. Anschließend kommen das fachübergreifende Diagnosekapitel und das abschließende V1-Audit.

V1 ist erst fertig, wenn jeder öffentliche Algorithmus getestet und dokumentiert ist, wichtige Fehler-/Abbruchzustände regressionstestet sind, die stabilen V1-Bereiche im Benutzerhandbuch beschrieben sind und keine öffentlichen Platzhalter mit `NotImplementedException` existieren.

## M3 – SASD-eigene Erweiterungen

Danach folgen die modernen Anforderungen aus der Produktfamilie: Vektoren/Matrizen/Transformationen für Game und Grafik, Geometrie, Kurven, Statistik-Grundlagen, Simulation/Random, robuste Toleranzwerkzeuge sowie unterstützende Mathematik für Sicherheitssoftware.

## M4 – Weitere Sprachen

Geplante Reihenfolge, sofern kein konkretes Projekt andere Prioritäten erzwingt: C++, Java, JavaScript/TypeScript, Fortran. Ab der zweiten Sprache werden sprachübergreifende Golden-Tests und maschinenlesbare Spezifikationen wichtig.
