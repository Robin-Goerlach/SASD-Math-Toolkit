# Deskriptive Statistik

CPP-M1 führt `sasd::numerics::statistics` mit arithmetischem Mittelwert, Median
sowie Populations- und Stichprobenvarianz über `std::span<const double>` ein.

## Numerischer Vertrag

- Die Eingabe muss mindestens einen endlichen Wert enthalten.
- Die Stichprobenvarianz benötigt mindestens zwei Werte.
- Der Mittelwert normiert jeden Wert vor der kompensierten Summation mit dem größten Betrag
  und vermeidet damit den naiven Überlauf beim Summieren von `{max, max}`.
- Ein geradzahliger Median verwendet `std::midpoint` statt `(a + b) / 2`.
- Die Varianz nutzt eine skalierte Quadratsumme und meldet
  `std::overflow_error`, wenn das mathematische Ergebnis nicht darstellbar ist.
- Der Eingabespeicher wird nicht verändert.

Mittelwert und Varianz akzeptieren `ExecutionOptions`. Der Automatikmodus nutzt
mehrere deterministische Bereiche nur bei ausreichend großen Eingaben. Der
Median bleibt seriell, weil paralleles Sortieren in der abhängigkeitsfreien
Referenzimplementierung noch keinen gemessenen Vorteil besitzt.
