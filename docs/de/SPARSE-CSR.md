# Sparse-CSR-Fundament

## Zweck

`CsrMatrix` ist der erste Datenstruktur-Meilenstein der Sparse-Linear-Algebra-Modernisierung 2026. Die Struktur wird bewusst **vor** Conjugate Gradient, GMRES oder Preconditionern festgelegt, damit die Solver auf einem eindeutigen und getesteten Speichervertrag aufbauen und nicht jeweils eigene interne Sparse-Formate erfinden.

Die Implementierung liegt unter `Sasd.Numerics.LinearAlgebra.Sparse` und ist nach dem Aufbau unveränderlich.

## Kanonischer Speichervertrag

Die Matrix verwendet die drei üblichen CSR-Arrays:

- `rowPointers` mit Länge `Rows + 1`;
- `columnIndices` mit Länge `NonZeroCount`;
- `values` mit Länge `NonZeroCount`.

Für SASD gelten zusätzlich folgende kanonische Regeln:

1. Zeiger beginnen bei null, enden bei `NonZeroCount` und sind monoton nicht fallend;
2. Spaltenindizes sind innerhalb jeder Zeile strikt aufsteigend;
3. explizite Nullwerte werden nicht gespeichert;
4. alle gespeicherten Werte sind endlich;
5. öffentliche Fabrikmethoden kopieren aufruferseitige Arrays defensiv.

Damit gibt es für dieselbe Matrix nicht unnötig viele strukturelle Darstellungen, und spätere Solver können Zeilen ohne wiederholte Strukturprüfung durchlaufen.

## Aufbau

### Koordinateneinträge

`FromEntries(rows, columns, entries)` erlaubt beliebige Eingabereihenfolge. Doppelte Koordinaten werden in Enumerationsreihenfolge addiert; eine exakte Aufhebung entfernt den Eintrag. Das endgültige Layout ist nach Zeile und innerhalb der Zeile nach Spalte sortiert.

Dieser Weg eignet sich gut für lokale Beiträge, etwa bei Diskretisierungen. Er ist noch kein spezialisierter Hochdurchsatz-Builder für hunderte Millionen Einfügungen; einen solchen sollten wir erst ergänzen, wenn ein echter Verbraucher ihn benötigt.

### Konvertierung aus Dense

`FromDense` verwendet eine explizite **absolute** Nulltoleranz. Der Standard `0.0` erhält jeden von null verschiedenen Dense-Eintrag. Eine positive Schwelle ist eine bewusst verlustbehaftete Sparsifizierung und muss zur Problemskalierung passen; sie ist kein universelles Epsilon.

### Interoperabilität mit vorhandenem CSR

`FromCompressedRows` übernimmt bereits komprimierte Arrays, prüft jedoch vorher den vollständigen kanonischen Vertrag und kopiert die Daten. Das ist die vorgesehene Grenze für spätere Datei-, Fortran-/C++- oder native Bibliotheksadapter.

## Sparse-Matrix-Vektor-Multiplikation

`Multiply(IReadOnlyList<double>)` ist die bequeme allokierende Variante. Zusätzlich schreibt eine Span-Überladung in aufruferseitigen Speicher, damit iterative Solver ihre Arbeitsvektoren wiederverwenden können:

```csharp
var y = new double[a.Rows];
a.Multiply(x, y);
```

Überlappende Ein-/Ausgabepuffer werden unterstützt, indem die Eingabe vor dem Überschreiben zwischengespeichert wird. Nicht-endliche Produkte oder Überläufe bei der Akkumulation werden explizit gemeldet, statt einen iterativen Solver später still mit NaN/Infinity zu vergiften.

## Transponieren und Konvertieren

`Transpose()` erzeugt ohne Dense-Zwischenmatrix wieder kanonisches CSR. `ToDense()` dient primär Interoperabilität, Tests und Diagnostik; Sparse-Algorithmen sollen intern nicht davon abhängen.

`CopyRowPointers`, `CopyColumnIndices` und `CopyValues` liefern defensive Kopien für Serialisierung oder Adapter, ohne die Unveränderlichkeit aufzugeben.

## Normen

`SparseMatrixNorms` bietet derzeit maximalen Absolutwert, 1-Norm, Unendlich-Norm und Frobenius-Norm ohne Dense-Materialisierung. Die Frobenius-Norm verwendet skalierte Quadratsummen. Die 1-Norm benötigt Speicher proportional zu den tatsächlich nichtleeren Spalten statt zur deklarierten Spaltenzahl.

Eine Sparse-Spektralnorm wird bewusst noch nicht angeboten. Ihre effiziente Berechnung gehört fachlich zu iterativer Sparse-Eigenwert-/Solver-Infrastruktur und nicht in die Speicherprimitive.

## Nächster Solver-Schritt

Als nächstes ist Conjugate Gradient für symmetrisch positiv definite dünnbesetzte Systeme vorgesehen. Der Solver soll:

- die Span-basierte CSR-Matvec wiederverwenden;
- vorhandene `IterationStatus`- und Residualkonventionen übernehmen;
- relative und absolute Abbruchkriterien explizit machen;
- numerischen Zusammenbruch sichtbar melden;
- eine Preconditioner-Abstraktion erst einführen, wenn der Solver sie tatsächlich benötigt.

Eine eigene CSC-Struktur bleibt eine spätere Option, sobald ein spaltenorientierter Verbraucher sie rechtfertigt. Für gelegentliche Spaltenworkflows deckt `Transpose()` bereits viele Fälle ab, ohne die Kern-API vorschnell zu verdoppeln.
