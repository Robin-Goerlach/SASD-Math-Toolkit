# SASD Math Toolkit – C++-Handbuch

Dies ist das eigenständige deutsche Benutzer- und Entwicklerhandbuch für die
moderne C++-Ausgabe des SASD Math Toolkit. Es dokumentiert C++-APIs, CMake,
Ownership, Fehlerbehandlung und Ausführungsverhalten direkt und setzt das
C#/.NET-Handbuch nicht voraus.

## Stand der Ausgabe

Die Handbuchversion `0.4` begleitet CPP-M3. Die stabile
.NET-Ausgabe besitzt bereits einen breiten numerischen Referenzkatalog. Die
C++-Ausgabe enthält Build- und Paketgrundlage, gemeinsames Ergebnismodell,
CPU-Ausführungsrichtlinie, deskriptive Statistik, Polynome, Nullstellensuche,
Interpolation und kubische Splines. Weitere Kapitel entstehen jeweils zusammen
mit den zugehörigen getesteten C++-Algorithmen.

## Inhalt

1. [Erste Schritte](erste-schritte.md)
2. [Diagnostik und parallele Ausführung](parallele-ausfuehrung.md)
3. [Deskriptive Statistik](deskriptive-statistik.md)
4. [Polynome und Nullstellensuche](polynome-nullstellensuche.md)
5. [Interpolation und Splines](interpolation-splines.md)

Später folgen Analysis, dichte und dünnbesetzte lineare Algebra,
Eigenwertmethoden, ODE/BVP, Approximation und Transformationen. Jedes Beispiel
dieses Handbuchs wird als C++20 kompilierbar sein.

## Leitgedanke

Ein Zahlenwert allein ist noch kein vertrauenswürdiges numerisches Ergebnis.
Methode, Eingabevertrag, Toleranz, Terminierungsstatus und eine unabhängige
Qualitätsdiagnose gehören zusammen. Parallelisierung muss diesen Vertrag
erhalten und durch Messungen begründet werden – nicht nur durch viele Threads.
