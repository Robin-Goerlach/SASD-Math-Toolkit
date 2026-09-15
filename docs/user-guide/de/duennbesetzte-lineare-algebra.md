# Dünnbesetzte lineare Algebra — CSR und Conjugate Gradient

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

Diese zweite Form wird von iterativen Sparse-Solvern verwendet, damit wiederholte Matrix-Vektor-Produkte nicht in jeder Iteration neue Ergebnisarrays anlegen.

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

## Conjugate Gradient für SPD-Systeme

`ConjugateGradientSolver` löst Sparse-Systeme, deren Matrix **symmetrisch positiv definit** ist. Er ist kein allgemeiner Ersatz für LU oder einen nichtsymmetrischen Krylov-Solver.

```csharp
var result = ConjugateGradientSolver.Solve(
    a,
    rightHandSide,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });

if (result.Converged)
{
    var solution = result.Solution;
}
```

Die gemeinsame Sparse-Abbruchregel lautet:

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Damit können GMRES und BiCGSTAB später dieselbe Semantik übernehmen.

Symmetrie wird standardmäßig geprüft. Zusätzlich wird eine strikt positive Diagonale verlangt, weil sie für positive Definitheit notwendig ist. Diese preiswerten Prüfungen können nicht beweisen, dass eine beliebige Sparse-Matrix SPD ist; deshalb meldet CG zusätzlich `NumericalBreakdown`, wenn die Suchrichtungskrümmung `p^T*A*p` nicht positiv oder nicht endlich wird.

### Verifikation des echten Residuums

CG aktualisiert sein Residuum aus Effizienzgründen rekursiv. Floating-Point-Rundung kann dazu führen, dass dieses rekursive Residuum vom echten `b-A*x` abweicht. Vor `Converged` berechnet die SASD-Implementierung deshalb das echte Residuum neu. Liegt es weiterhin oberhalb der Schwelle, startet CG mit dem aktualisierten Residuum neu, statt eine falsche Konvergenz zu melden.

### Solver-Diagnostik

`SparseLinearSolveResult` stellt die beste verfügbare Lösung, `IterationStatus`, Anzahl abgeschlossener Iterationen, Anfangs-/Endresiduum, Norm der rechten Seite, effektive Konvergenzschwelle und relative Residualnorm bereit. Auch ein Ergebnis mit `MaximumIterationsReached` kann damit quantitativ beurteilt werden und wird nicht auf ein einfaches Erfolg/Fehlschlag-Boolean reduziert.

## Was die Sparse-Schicht noch nicht liefert

CSR und unpräconditioniertes Conjugate Gradient sind jetzt implementiert. Als nächste Schichten folgen Preconditioning sowie allgemeine nichtsymmetrische Krylov-Verfahren wie GMRES und BiCGSTAB. Eine eigene CSC-Struktur bleibt aufgeschoben, bis dauerhaft spaltenorientierte Workloads eine zweite Sparse-Repräsentation rechtfertigen.

Die Trennung bleibt wichtig: Sparse-Speicherung ist eine Datenlayout-Entscheidung; die Solverwahl hängt von der mathematischen Struktur ab. Ein SPD-System passt gut zu CG, eine allgemeine nichtsymmetrische Matrix nicht.
