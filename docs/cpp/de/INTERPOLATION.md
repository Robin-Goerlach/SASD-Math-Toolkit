# Interpolation und kubische Splines

CPP-M3 führt `sasd::numerics::interpolation` als eigenständige
C++20-Implementierung des gemeinsamen Interpolationsvertrags ein.

## Öffentliche API

- `InterpolationAlgorithms::linear` für die Gerade durch zwei verschiedene Punkte;
- direkte Lagrange-Polynominterpolation;
- Berechnung Newtonscher dividierter Differenzen und einmalige Auswertung;
- unveränderlicher `NewtonInterpolant` für wiederholte Einzel- und Batch-Auswertungen;
- unveränderliche natürliche und eingespannte `CubicSpline`-Splines mit Wert,
  erster und zweiter Ableitung.

Polynomstützstellen müssen verschieden, aber nicht sortiert sein. Spline-Knoten
müssen streng aufsteigend vorliegen. Koordinaten, Randableitungen und
Auswertungspunkte müssen endlich sein. Splines werden nur im abgeschlossenen
Knotenintervall ausgewertet; polynomiale und lineare Extrapolation bleibt
mathematisch erlaubt.

## Ownership und numerische Prüfungen

Wiederverwendbare Interpolanten und Splines kopieren ihren fertigen Zustand.
Spätere Änderungen an Eingabevektoren können das Objekt deshalb nicht
ungültig machen. Exakt doppelte Stützstellen werden abgewiesen; nicht endliche
Zwischenergebnisse werden als numerische Fehler gemeldet.

Natürliche und eingespannte Splines verwenden denselben tridiagonalen
Konstruktionspfad. Dadurch entstehen keine zwei schleichend unterschiedlichen
Implementierungen. Öffentlich sichtbar sind nur schreibgeschützte Knoten und
Segmentmetadaten, keine veränderbaren Koeffizienten.

## Parallele Batch-Auswertung

Die Zielpunkte von `evaluate_many` sind voneinander unabhängig. Newton- und
Spline-Batchmethoden beachten deshalb `ExecutionOptions` und erhalten die
Eingabereihenfolge. Spline-Konstruktion und dividierte Differenzen bleiben
wegen ihrer abhängigen Rekurrenzen seriell.

Die Bibliothek legt keinen hardwareunabhängigen, allgemein optimalen
Umschaltpunkt fest. `automatic` verwendet das vom Aufrufer anpassbare
`minimum_work_per_thread`. Anwendungen dürfen außerdem `sequential` wählen,
`parallel` erzwingen oder `maximum_threads` begrenzen. Erzwungene Parallelität
ist eine Ausführungsanforderung, keine Geschwindigkeitsgarantie.

Das sprachneutrale Verhalten steht in
[`spec/interpolation.md`](../../../spec/interpolation.md); gemeinsame analytische
Fälle liegen unter `conformance/interpolation/`.
