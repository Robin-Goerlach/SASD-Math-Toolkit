# SASD Math Toolkit Benutzerhandbuch — C#/.NET

Dies ist das anwenderorientierte Benutzerhandbuch für das SASD Math Toolkit. Es wird speziell für die aktuelle C#/.NET-API geschrieben und wächst parallel zur Qualitätsarbeit an V1.

Das Handbuch ist **keine** Abschrift, Übersetzung oder Neuverpackung des historischen Borland-Pascal-Handbuchs. Das historische Produkt dient für V1 nur als funktionaler Meilenstein. Erklärungen, Beispiele, API-Namen, Hinweise und Arbeitsabläufe dieses Handbuchs werden für das SASD Math Toolkit neu erstellt.

## Zielgruppe

Das Benutzerhandbuch richtet sich an Anwendungsentwickler, Lernende und technisch orientierte Nutzer, die die numerischen Routinen einsetzen möchten, ohne zuerst den Implementierungsquelltext lesen zu müssen. Jedes Kapitel soll vier praktische Fragen beantworten: Welches Problem löst das Verfahren? Wann ist es sinnvoll? Wie wird es in C# aufgerufen? Welche numerischen Grenzen sind zu beachten?

## Kapitelstruktur

1. [Erste Schritte](erste-schritte.md)
2. [Nullstellen von Gleichungen](nullstellen.md)
3. [Interpolation](interpolation.md)
4. [Numerische Differentiation](differentiation.md)
5. [Numerische Integration](integration.md)
6. [Matrizen und lineare Gleichungssysteme](matrizen-lineare-gleichungssysteme.md)
7. [Eigenwerte und Eigenvektoren](eigenwerte-eigenvektoren.md)
8. [Gewöhnliche Differentialgleichungen und Randwertprobleme](differentialgleichungen.md)
9. [Least-Squares-Approximation](least-squares.md)
10. [FFT, Faltung und Korrelation](fft-faltung-korrelation.md)
11. [Numerische Rezepte, Diagnostik und typische Fehlerfälle](diagnostik-fehlerfaelle.md)

Das geplante V1-Benutzerhandbuch ist damit in allen Kapiteln vorhanden. Für V1 verbleibt nun das abschließende repositoryweite Audit, damit öffentliche APIs, Beispiele, technische Dokumentation, Kompatibilitätsaussagen und Release-Metadaten mit der Implementierung übereinstimmen.

## Dokumentationsebenen

Das Benutzerhandbuch konzentriert sich auf Anwendung und Einordnung. Technische Designnotizen liegen weiterhin unter `docs/de/` bzw. `docs/en/`; zusätzlich enthält der Quellcode API-Kommentare. Die Kompatibilitätsmatrix in `docs/en/BORLAND-V1-COMPATIBILITY.md` verfolgt die historische V1-Abdeckung getrennt vom Fortschritt des Benutzerhandbuchs.

## LaTeX-/PDF-Ausgabe

Eine gesetzte deutsch- und englischsprachige PDF-Ausgabe wird aus diesen Markdown-Kapiteln erzeugt. Layout und Build-Pipeline liegen unter [`../LaTeX/`](../LaTeX/). Dadurch bleibt Markdown die redaktionelle Quelle, während LaTeX für Buchsatz, Inhaltsverzeichnis, Kapitelgestaltung und PDF-Ausgabe verwendet wird.
