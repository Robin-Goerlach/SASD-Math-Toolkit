# Polynome und Nullstellensuche

CPP-M2 führt das unveränderliche `sasd::numerics::polynomials::Polynomial` und
die Solverfamilie `sasd::numerics::root_finding` ein.

## Öffentliche Algorithmen

- Bisektion, Newton-Raphson und Sekantenverfahren für reelle Funktionen;
- komplexes Muller-Verfahren;
- Newton-Horner und Laguerre für Polynome;
- deterministische Bestimmung aller Wurzeln mit Laguerre, synthetischer
  Deflation und abschließendem Polishing am ursprünglichen Polynom.

Koeffizienten stehen in absteigender Potenzreihenfolge.
`std::complex<double>` wird für komplexe Koeffizienten und Wurzeln verwendet;
für gewöhnliche reelle Polynome gibt es weiterhin reelle Konstruktoren.

## Diagnostik und Fehler

`RootResult`, `ComplexRootResult` und `PolynomialRootsResult` trennen
Terminierungsstatus, Näherung, Iterationszahl, Residuum und optionale Meldung.
Fehlende Klammerung, numerischer Breakdown und ein erreichtes Iterationslimit
sind erwartete numerische Ergebnisse. Leere Callbacks, ungültige Optionen und
nicht endliche Aufrufereingaben sind Vertragsfehler. Nicht endliche
Callbackwerte und übergelaufene Iterierte werden abgewiesen, bevor sie erneut
an Aufrufercode gelangen.

Das Konvergenzkriterium akzeptiert ein ausreichend kleines Residuum oder eine
relativ skalierte Änderung der Iterierten. Anwendungen sollten dennoch das
Residuum prüfen und sicherstellen, dass die gefundene Wurzel fachlich gemeint
war.

## Numerische Schutzmaßnahmen

- `std::midpoint` schützt die Bisektion vor Überlauf bei breiten Intervallen.
- Newton und Sekante verweigern numerisch singuläre Divisionen.
- Muller und Laguerre wählen den betragsmäßig größeren Nenner.
- Nur exakt führende Nullkoeffizienten werden entfernt.
- Deflationsergebnisse werden am ursprünglichen Polynom nachpoliert.
- Das Gesamtergebnis meldet das größte Residuum am Originalpolynom.

Eine einzelne Wurzeliteration ist inhärent seriell. CPP-M2 erzeugt deshalb
keine Threads für abhängige Rekurrenzen. Spätere Batch-APIs für unabhängige
Probleme können die vorhandene CPU-Richtlinie ausdrücklich nutzen.

Das sprachneutrale Verhalten steht in
[`spec/root-finding.md`](../../../spec/root-finding.md); gemeinsame skalare
Referenzfälle liegen unter `conformance/root-finding/`.
