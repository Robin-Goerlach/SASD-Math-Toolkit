# Dünnbesetzte lineare Algebra — CSR, CG/PCG und GMRES

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

## Restarted GMRES für allgemeine quadratische Systeme

`GmresSolver` ist das allgemeine nichtsymmetrische Gegenstück zu CG/PCG. Der Solver setzt weder Symmetrie noch positive Definitheit voraus und minimiert das Residuum über einen Krylov-Unterraum, der mit dem Arnoldi-Prozess aufgebaut wird.

```csharp
var result = GmresSolver.Solve(
    matrix,
    rightHandSide,
    restartLength: 30,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

Vollständiges GMRES speichert pro Iteration einen zusätzlichen Basisvektor. `restartLength` begrenzt dieses Speicherwachstum. An einer Restart-Grenze wird die aktuelle Least-Squares-Korrektur angewendet, das echte Residuum neu berechnet und der nächste Krylov-Zyklus von diesem Residuum aus gestartet. Eine kleine Restart-Länge spart Speicher, kann aber die Konvergenz verlangsamen oder stagnieren lassen; ein größerer Wert bewahrt mehr Krylov-Information.

Die Arnoldi-Basis verwendet zwei Modified-Gram-Schmidt-Pässe, um vermeidbaren Orthogonalitätsverlust zu reduzieren. Givens-Rotationen aktualisieren das kleine obere Hessenberg-Least-Squares-System inkrementell. Das projizierte Residuum dient als günstiger Auslöser, aber `Converged` wird erst nach expliziter Neuberechnung des echten Residuums `b-A*x` gemeldet.

### Right-preconditioned GMRES

Dieselbe solverneutrale Preconditioner-Abstraktion wird über `SolvePreconditioned` wiederverwendet. GMRES wendet den Preconditioner auf der rechten Seite an:

```text
A * M^-1 * y = b
```

Dadurch bleibt das minimierte und gemeldete Residuum das Residuum des ursprünglichen Systems. Anders als PCG verlangt GMRES keinen SPD-Preconditioner.

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = GmresSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi,
    restartLength: 30);
```

Ein abgebrochener Arnoldi-Ausbau wird nicht automatisch als Erfolg gewertet. Kann der Krylov-Raum nicht weiter wachsen und liegt das explizit neu berechnete Residuum noch über der Toleranz, liefert der Solver `NumericalBreakdown` mit der besten verfügbaren Lösung.

### Solver-Diagnostik

`SparseLinearSolveResult` wird von CG, PCG und GMRES gemeinsam verwendet. Es enthält die beste verfügbare Lösung, `IterationStatus`, Anzahl abgeschlossener Iterationen, Anfangs-/Endresiduum, Norm der rechten Seite, effektive Konvergenzschwelle und relative Residualnorm. `MaximumIterationsReached` bleibt damit quantitativ auswertbar und wird nicht auf ein Boolean reduziert.

## Was die Sparse-Schicht noch nicht liefert

CSR, CG/PCG, die gemeinsame Preconditioner-Abstraktion, Jacobi-Preconditioning und restarted GMRES sind jetzt implementiert. Als nächster Solver-Schritt folgt **BiCGSTAB**, danach eine Konsolidierungs- und Test-Runde der Sparse-Architektur vor dem nächsten Release Candidate. Leistungsfähigere Preconditioner wie Incomplete Cholesky/ILU sowie eine eigene CSC-Struktur bleiben aufgeschoben, bis konkrete Workloads sie rechtfertigen.

Die Trennung bleibt wichtig: Sparse-Speicherung ist eine Datenlayout-Entscheidung, die Solverwahl hängt von der mathematischen Struktur ab, und die Wahl des Preconditioners hängt sowohl vom Solver-Vertrag als auch von der Matrix ab. Ein SPD-System passt gut zu CG/PCG; eine allgemeine nichtsymmetrische quadratische Matrix ist ein GMRES-Problem.
