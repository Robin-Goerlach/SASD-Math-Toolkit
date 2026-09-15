# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breiter numerischer Vertikalschnitt (historischer V1-Algorithmuskatalog erledigt)

Der historische numerische Methodenkatalog ist in den Bereichen reelle/komplexe Nullstellensuche, Interpolation/Splines, Differentiation, Integration, dichte lineare Algebra, Eigenwerte, RK4/RKF45/Adams, Randwert-Shooting, Least Squares, FFT, Faltung und Korrelation abgedeckt.

Die Implementierung teilt bewusst gemeinsame Grundlagen, statt Formeln für Komfort-APIs zu duplizieren: LU-Faktorisierung ist wiederverwendbar, höhere RK4-Varianten werden auf den gemeinsamen Systemintegrator zurückgeführt, transformierte Least-Squares-Modelle nutzen den allgemeinen Basissolver und Faltung/Korrelation bauen auf dem gemeinsamen komplexen FFT-Kern auf.

Unter `docs/user-guide/` wächst parallel ein eigenes C#/.NET-Benutzerhandbuch. Es begleitet stabile Implementierungsmeilensteine und verwendet nicht das historische Pascal-Handbuch wieder.

## M2 – V1 vollständig (Funktionskatalog erledigt; Release-Härtung läuft)

Die historische Demo-/Grafikrolle wird nun durch die plattformunabhängige Anwendung `Sasd.Math.Toolkit.Sample` abgedeckt. Sie erzeugt einen deterministischen eigenständigen HTML-/SVG-Numerikbericht, ohne UI-Abhängigkeiten in die wiederverwendbare Bibliothek einzubauen.

Damit sind alle Zeilen der `BORLAND-V1-COMPATIBILITY.md` funktional abgedeckt. Die verbleibende V1-Arbeit besteht nicht mehr aus historischen Algorithmen, sondern aus Release-Härtung:

- fehlende Kapitel des Benutzerhandbuchs für stabile V1-Bereiche vervollständigen;
- API-Konsistenz und öffentliche Oberfläche systematisch prüfen;
- XML-Dokumentation und Fehlersemantik aller öffentlichen Algorithmen prüfen;
- ausführbare Beispiele dort ergänzen, wo sie das Handbuch verbessern;
- abschließendes Kompatibilitäts-/Test-Audit sowie Release Notes und Versionierung vorbereiten.

V1 ist erst fertig, wenn jeder öffentliche Algorithmus getestet und dokumentiert ist, die stabilen V1-Bereiche im Benutzerhandbuch beschrieben sind und keine öffentlichen Platzhalter mit `NotImplementedException` existieren.

## M3 – SASD-eigene Erweiterungen

Danach folgen die modernen Anforderungen aus der Produktfamilie: Vektoren/Matrizen/Transformationen für Game und Grafik, Geometrie, Kurven, Statistik-Grundlagen, Simulation/Random, robuste Toleranzwerkzeuge sowie unterstützende Mathematik für Sicherheitssoftware.

## M4 – Weitere Sprachen

Geplante Reihenfolge, sofern kein konkretes Projekt andere Prioritäten erzwingt: C++, Java, JavaScript/TypeScript, Fortran. Ab der zweiten Sprache werden sprachübergreifende Golden-Tests und maschinenlesbare Spezifikationen wichtig.
