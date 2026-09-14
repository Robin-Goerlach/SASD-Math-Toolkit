# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breiter numerischer Vertikalschnitt (gestartet und erweitert)

Bereits implementiert sind wichtige Verfahren aus nahezu allen V1-Bereichen: reelle und komplexe Nullstellensuche, wiederverwendbare Polynom-/Horner-/Deflationsfunktionen, Interpolation/Splines, der vollständige historische V1-Differentiationsbereich, Integration, lineare Algebra mit wiederverwendbarer LU-Faktorisierung, der vollständige historische V1-Eigenwertbereich, RK4 und nun auch adaptives RKF45, Least Squares und FFT.

Nullstellensuche, Differentiation und Eigenwertbereich sind für den historischen V1-Umfang vollständig. Die direkte LU-Faktorisierung ist ebenfalls umgesetzt. Im ODE-Bereich stehen nun sowohl klassisches RK4 als auch Runge-Kutta-Fehlberg 4(5) mit expliziten Toleranzen, Diagnose verworfener Schritte und begrenzter adaptiver Schrittweite zur Verfügung.

Unter `docs/user-guide/` wächst parallel ein eigenes C#/.NET-Benutzerhandbuch. Das neue Kapitel zu Differentialgleichungen erklärt bereits RK4 und RKF45, ohne das historische Pascal-Handbuch wiederzuverwenden.

Damit wird früh geprüft, ob die Architektur für die ganze Produktfamilie trägt, statt erst ein Kapitel vollständig zu perfektionieren und später grundlegende Entscheidungen wieder ändern zu müssen.

## M2 – V1 vollständig

Alle noch offenen Punkte der englischen Kompatibilitätsmatrix schließen: insbesondere Adams-Prädiktor-Korrektor-Verfahren, Shooting-Verfahren und die verbleibenden FFT-Hilfsfunktionen. Zusätzlich müssen die mit `partial` gekennzeichneten Komfort-APIs abgeschlossen oder bewusst als durch allgemeinere APIs ersetzt dokumentiert werden.

V1 ist erst fertig, wenn jeder öffentliche Algorithmus getestet und dokumentiert ist, die stabilen V1-Bereiche im Benutzerhandbuch beschrieben sind und keine öffentlichen Platzhalter mit `NotImplementedException` existieren.

## M3 – SASD-eigene Erweiterungen

Danach folgen die modernen Anforderungen aus der Produktfamilie: Vektoren/Matrizen/Transformationen für Game und Grafik, Geometrie, Kurven, Statistik-Grundlagen, Simulation/Random, robuste Toleranzwerkzeuge sowie unterstützende Mathematik für Sicherheitssoftware.

## M4 – Weitere Sprachen

Geplante Reihenfolge, sofern kein konkretes Projekt andere Prioritäten erzwingt: C++, Java, JavaScript/TypeScript, Fortran. Ab der zweiten Sprache werden sprachübergreifende Golden-Tests und maschinenlesbare Spezifikationen wichtig.
