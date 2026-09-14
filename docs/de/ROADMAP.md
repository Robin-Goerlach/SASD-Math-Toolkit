# Roadmap

## M0 – Repository-Grundlage (erledigt)

Repository-Struktur, .NET-10-Projekt, Tests, Sample, CI, englische/deutsche Dokumentation und Clean-Room-Regeln.

## M1 – Breiter numerischer Vertikalschnitt (gestartet und erweitert)

Bereits implementiert sind wichtige Verfahren aus nahezu allen V1-Bereichen: reelle und komplexe Nullstellensuche, wiederverwendbare Polynom-/Horner-/Deflationsfunktionen, Interpolation/Splines, der vollständige historische V1-Differentiationsbereich, Integration, lineare Algebra mit wiederverwendbarer LU-Faktorisierung, der vollständige historische V1-Eigenwertbereich, RK4/RKF45/Adams-Verfahren, Least Squares und FFT.

Der V1-Bereich der Nullstellensuche ist vollständig abgedeckt. Auch direkte LU-Faktorisierung, Eigenwertbereich und Differentiation sind vollständig. Im ODE-Bereich stehen festes skalares RK4, adaptives RKF45, der Adams-Bashforth-/Adams-Moulton-Prädiktor-Korrektor vierter Ordnung, RK4 für Systeme erster Ordnung, eine Komfort-API für skalare Gleichungen zweiter Ordnung und nun auch eine allgemeine skalare Komfort-API für Gleichungen n-ter Ordnung zur Verfügung. Beide höheren Ordnungen verwenden bewusst denselben Begleitsystem-/System-RK4-Kern.

Unter `docs/user-guide/` wächst parallel ein eigenes C#/.NET-Benutzerhandbuch. Es begleitet stabile Implementierungsmeilensteine und verwendet nicht das historische Pascal-Handbuch wieder.

Damit wird früh geprüft, ob die Architektur für die ganze Produktfamilie trägt, statt erst ein Kapitel vollständig zu perfektionieren und später grundlegende Entscheidungen wieder ändern zu müssen.

## M2 – V1 vollständig

Alle noch offenen Punkte der englischen Kompatibilitätsmatrix schließen: insbesondere lineare/nichtlineare Shooting-Verfahren und die verbleibenden FFT-Hilfsfunktionen. Zusätzlich müssen die noch mit `partial` gekennzeichneten Komfort-APIs, vor allem die Hilfe für gekoppelte Systeme zweiter Ordnung sowie benannte Least-Squares-Modelle, abgeschlossen oder bewusst als durch allgemeinere APIs ersetzt dokumentiert werden.

V1 ist erst fertig, wenn jeder öffentliche Algorithmus getestet und dokumentiert ist, die stabilen V1-Bereiche im Benutzerhandbuch beschrieben sind und keine öffentlichen Platzhalter mit `NotImplementedException` existieren.

## M3 – SASD-eigene Erweiterungen

Danach folgen die modernen Anforderungen aus der Produktfamilie: Vektoren/Matrizen/Transformationen für Game und Grafik, Geometrie, Kurven, Statistik-Grundlagen, Simulation/Random, robuste Toleranzwerkzeuge sowie unterstützende Mathematik für Sicherheitssoftware.

## M4 – Weitere Sprachen

Geplante Reihenfolge, sofern kein konkretes Projekt andere Prioritäten erzwingt: C++, Java, JavaScript/TypeScript, Fortran. Ab der zweiten Sprache werden sprachübergreifende Golden-Tests und maschinenlesbare Spezifikationen wichtig.
