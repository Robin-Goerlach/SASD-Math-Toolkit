# Dünnbesetzte lineare Algebra — CSR-Fundament

Dichte Matrizen reservieren Speicher für jede Matrixposition. Das ist für viele kleine und mittlere Probleme sinnvoll, wird aber verschwenderisch, wenn eine große Matrix pro Zeile nur wenige von null verschiedene Koeffizienten enthält. `CsrMatrix` ist die erste SASD-Sparse-Repräsentation für solche Workloads.

## Sparse-Matrix aufbauen

Für einen koordinatenorientierten Aufbau:

```csharp
using Sasd.Numerics.LinearAlgebra.Sparse;

var a = CsrMatrix.FromEntries(
    4,
    4,
    [
        new SparseMatrixEntry(0, 0, 4.0),
        new SparseMatrixEntry(0, 1, -1.0),
        new SparseMatrixEntry(1, 0, -1.0),
        new SparseMatrixEntry(1, 1, 4.0),
        new SparseMatrixEntry(1, 2, -1.0),
        new SparseMatrixEntry(2, 1, -1.0),
        new SparseMatrixEntry(2, 2, 4.0),
        new SparseMatrixEntry(2, 3, -1.0),
        new SparseMatrixEntry(3, 2, -1.0),
        new SparseMatrixEntry(3, 3, 4.0)
    ]);
```

Nach dem Aufbau ist die Matrix unveränderlich. Die Einträge liegen in kanonischem CSR vor: sortierte Spalten je Zeile, keine doppelten Koordinaten und keine explizit gespeicherten Nullwerte.

Doppelte Koordinaten werden in `FromEntries` addiert. Das ist nützlich, wenn mehrere lokale Beiträge denselben globalen Matrixkoeffizienten verändern. Heben sie sich exakt auf, verschwindet der Eintrag aus der komprimierten Speicherung.

## Matrix-Vektor-Multiplikation

Der bequeme Standardaufruf erzeugt einen neuen Ergebnisvektor:

```csharp
var y = a.Multiply(x);
```

Iterative Algorithmen sollten Arbeitsbereiche dagegen wiederverwenden:

```csharp
var y = new double[a.Rows];
a.Multiply(x, y);
```

Genau deshalb wurde CSR vor dem ersten iterativen Sparse-Solver aufgebaut: Conjugate Gradient kann später wiederholt Matrix-Vektor-Produkte berechnen, ohne in jeder Iteration einen neuen Ergebnisvektor anzulegen.

## Dense-Konvertierung und Sparsifizierung

`CsrMatrix.FromDense(dense)` speichert jeden von null verschiedenen Dense-Eintrag. Eine positive `absoluteZeroTolerance` verwirft bewusst kleinere Koeffizienten:

```csharp
var sparse = CsrMatrix.FromDense(dense, absoluteZeroTolerance: 1e-10);
```

Diese Schwelle ist absolut. Ihre Wahl gehört zum numerischen Modell; ein einziges Epsilon ist nicht automatisch für Matrizen mit völlig unterschiedlichen physikalischen Skalen geeignet.

`ToDense()` eignet sich für Inspektion, Interoperabilität und Tests. Sparse-Algorithmen sollten aber nicht fortlaufend in dichte Speicherung zurückkonvertieren.

## Transponieren

`Transpose()` liefert wieder kanonisches CSR für die transponierte Matrix, ohne zunächst eine dichte Matrix anzulegen. Eine eigene CSC-Struktur kann später folgen, wenn dauerhaft spaltenorientierte Workloads sie rechtfertigen.

## Sparse-Normen

`SparseMatrixNorms` berechnet preiswerte Normen direkt aus der komprimierten Speicherung:

```csharp
var one = SparseMatrixNorms.OneNorm(a);
var infinity = SparseMatrixNorms.InfinityNorm(a);
var frobenius = SparseMatrixNorms.FrobeniusNorm(a);
```

Die Frobenius-Berechnung ist skalierungsbewusst. Große, aber endliche Einträge laufen deshalb nicht nur deshalb über, weil eine naive Zwischenrechnung sie zuerst quadriert.

## Was CSR noch nicht liefert

Dieser Meilenstein legt Datenrepräsentation und Arithmetik fest, noch kein vollständiges Sparse-Paket. Conjugate Gradient, GMRES, BiCGSTAB und eine Preconditioner-API folgen erst darauf. Sie sollen die vorhandenen Iterationsstatus- und Residualkonventionen des Toolkits übernehmen.

Die Trennung ist wichtig: Sparse-Speicherung ist eine Datenlayout-Entscheidung; die Solverwahl hängt von der mathematischen Struktur ab. Ein symmetrisch positiv definites Sparse-System benötigt später ein anderes iteratives Verfahren als ein allgemeines nichtsymmetrisches System.
