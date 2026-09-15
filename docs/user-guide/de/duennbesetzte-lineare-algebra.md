# Dünnbesetzte lineare Algebra — CSR, CG und Preconditioning

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
```

Die gemeinsame Sparse-Abbruchregel lautet:

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Damit können spätere Krylov-Solver dieselbe Semantik übernehmen.

Symmetrie wird standardmäßig geprüft. Zusätzlich wird eine strikt positive Diagonale verlangt, weil sie für positive Definitheit notwendig ist. Diese preiswerten Prüfungen können nicht beweisen, dass eine beliebige Sparse-Matrix SPD ist; deshalb meldet CG zusätzlich `NumericalBreakdown`, wenn die Suchrichtungskrümmung `p^T*A*p` nicht positiv oder nicht endlich wird.

### Verifikation des echten Residuums

CG aktualisiert sein Residuum aus Effizienzgründen rekursiv. Floating-Point-Rundung kann dazu führen, dass dieses rekursive Residuum vom echten `b-A*x` abweicht. Vor `Converged` berechnet die SASD-Implementierung deshalb das echte Residuum neu. Liegt es weiterhin oberhalb der Schwelle, startet CG mit dem aktualisierten Residuum neu, statt eine falsche Konvergenz zu melden.

## Preconditioned Conjugate Gradient

Schlechte Skalierung oder ein ungünstiges Spektrum können dazu führen, dass CG trotz einer SPD-Matrix viele Iterationen benötigt. Ein Preconditioner wendet eine angenäherte Inversoperation an:

```text
z = M^-1 * r
```

Dadurch arbeitet die Krylov-Iteration auf einem numerisch günstigeren Problem.

Die öffentliche Schnittstelle `ISparsePreconditioner` ist solverneutral. Die erste Implementierung ist `JacobiPreconditioner`, der die inverse Matrixdiagonale verwendet:

```csharp
var jacobi = JacobiPreconditioner.Create(a);
var result = ConjugateGradientSolver.SolvePreconditioned(
    a,
    rightHandSide,
    jacobi,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

Jacobi ist billig aufzubauen und anzuwenden, erzeugt pro Anwendung keine neuen Arrays und eignet sich besonders gut als Skalierungs-Baseline. Er garantiert nicht bei jedem Problem weniger Iterationen; die Qualität eines Preconditioners ist problemabhängig.

Für PCG muss der Preconditioner selbst SPD sein. `JacobiPreconditioner` besitzt diese Eigenschaft, wenn er aus einer SPD-Matrix erzeugt wird. Führt ein benutzerdefinierter Preconditioner dazu, dass `r^T*M^-1*r` nicht positiv oder nicht endlich ist, meldet der Solver `NumericalBreakdown`, statt mit einer ungültigen Rekursion fortzufahren.

`JacobiPreconditioner.Create` weist fehlende oder unbrauchbare Diagonaleinträge zurück. Die optionale `absoluteDiagonalTolerance` ist bewusst explizit und absolut. Eine positive Schwelle ist eine Modellierungsentscheidung und kein verborgenes universelles Epsilon.

### Solver-Diagnostik

`SparseLinearSolveResult` stellt die beste verfügbare Lösung, `IterationStatus`, Anzahl abgeschlossener Iterationen, Anfangs-/Endresiduum, Norm der rechten Seite, effektive Konvergenzschwelle und relative Residualnorm bereit. Auch ein Ergebnis mit `MaximumIterationsReached` kann damit quantitativ beurteilt werden und wird nicht auf ein einfaches Erfolg/Fehlschlag-Boolean reduziert.

## Was die Sparse-Schicht noch nicht liefert

CSR, CG, die gemeinsame Preconditioner-Abstraktion, Jacobi-Preconditioning und PCG sind jetzt implementiert. Der nächste große Solver ist restarted **GMRES** für allgemeine nichtsymmetrische Systeme, anschließend folgen **BiCGSTAB** und eine Konsolidierungsrunde der Sparse-Architektur. Leistungsfähigere Preconditioner wie Incomplete Cholesky/ILU sowie eine eigene CSC-Struktur bleiben aufgeschoben, bis konkrete Workloads sie rechtfertigen.

Die Trennung bleibt wichtig: Sparse-Speicherung ist eine Datenlayout-Entscheidung, die Solverwahl hängt von der mathematischen Struktur ab, und die Wahl des Preconditioners hängt sowohl vom Solver-Vertrag als auch von der Matrix ab. Ein SPD-System passt gut zu CG/PCG; eine allgemeine nichtsymmetrische Matrix nicht.
