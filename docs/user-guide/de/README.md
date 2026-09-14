# SASD Math Toolkit Benutzerhandbuch — C#/.NET

Dies ist der Beginn des anwenderorientierten Benutzerhandbuchs für das SASD Math Toolkit. Es wird speziell für die aktuelle C#/.NET-API geschrieben und wächst parallel zur V1-Implementierung.

Das Handbuch ist **keine** Abschrift, Übersetzung oder Neuverpackung des historischen Borland-Pascal-Handbuchs. Das historische Produkt dient für V1 nur als funktionaler Meilenstein. Erklärungen, Beispiele, API-Namen, Hinweise und Arbeitsabläufe dieses Handbuchs werden für das SASD Math Toolkit neu erstellt.

## Zielgruppe

Das Benutzerhandbuch richtet sich an Anwendungsentwickler, Lernende und technisch orientierte Nutzer, die die numerischen Routinen einsetzen möchten, ohne zuerst den Implementierungsquelltext lesen zu müssen. Jedes Kapitel soll vier praktische Fragen beantworten: Welches Problem löst das Verfahren? Wann ist es sinnvoll? Wie wird es in C# aufgerufen? Welche numerischen Grenzen sind zu beachten?

## Geplante Kapitelstruktur

1. [Erste Schritte](erste-schritte.md)
2. Nullstellen von Gleichungen
3. Interpolation
4. [Numerische Differentiation](differentiation.md)
5. Numerische Integration
6. Matrizen und lineare Gleichungssysteme
7. Eigenwerte und Eigenvektoren
8. Gewöhnliche Differentialgleichungen und Randwertprobleme
9. Least-Squares-Approximation
10. FFT, Faltung und Korrelation
11. Numerische Rezepte, Diagnose und typische Fehlerfälle

Kapitel werden ergänzt oder ausgebaut, sobald die zugehörige Implementierung einen stabilen Meilenstein erreicht. Dadurch bleiben Beispiele mit real vorhandenem Code synchron und dokumentieren keine APIs, die nur geplant sind.

## Dokumentationsebenen

Das Benutzerhandbuch konzentriert sich auf Anwendung und Einordnung. Technische Designnotizen liegen weiterhin unter `docs/de/` bzw. `docs/en/`; zusätzlich enthält der Quellcode API-Kommentare. Die Kompatibilitätsmatrix in `docs/en/BORLAND-V1-COMPATIBILITY.md` verfolgt die historische V1-Abdeckung getrennt vom Fortschritt des Benutzerhandbuchs.
