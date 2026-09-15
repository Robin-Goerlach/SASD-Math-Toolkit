# Sparse-Conjugate-Gradient-Solver

## Zweck

`ConjugateGradientSolver` ist der erste iterative Solver auf dem kanonischen CSR-Fundament des SASD Math Toolkit. Er löst reelle dünnbesetzte Systeme

```text
A * x = b
```

wenn `A` **symmetrisch positiv definit (SPD)** ist. Die Matrix wird niemals in dichte Speicherung umgewandelt; feste Arbeitsvektoren werden für alle wiederholten Sparse-Matrix-Vektor-Produkte wiederverwendet.

Dieselbe Implementierung bietet jetzt sowohl klassisches Conjugate Gradient als auch **Preconditioned Conjugate Gradient (PCG)**. Ohne Preconditioner wird weiterhin `Solve` verwendet; mit einem `ISparsePreconditioner` steht `SolvePreconditioned` bereit.

## Konvergenzregel

Sparse-Solver teilen sich `SparseIterativeSolverOptions`. CG/PCG akzeptiert Konvergenz, wenn die euklidische Norm des tatsächlich neu berechneten Residuums erfüllt:

```text
||b - A*x||2 <= max(absoluteTolerance, relativeTolerance * ||b||2)
```

Das Ergebnis stellt die effektive Schwelle, Anfangs-/Endresiduum und das zu `||b||2` relative Residuum bereit. Anwendungen können damit die reale numerische Abbruchbedingung protokollieren statt nur ein Boolean zu betrachten.

## SPD-Vertrag

CG ist kein allgemeiner Sparse-Solver. Symmetrie wird standardmäßig über `SparseMatrixDiagnostics.IsSymmetric` geprüft. Zusätzlich weist der Solver vor der Iteration eine nichtpositive Diagonale zurück, weil positive Diagonaleinträge für eine SPD-Matrix notwendig sind.

Diese Prüfungen **beweisen** positive Definitheit nicht. Ein vollständiger Beweis wäre deutlich teurer und widerspräche dem Zweck eines leichten iterativen Sparse-Solvers. Während der Iteration prüft CG deshalb die Krümmung der Suchrichtung

```text
p^T * A * p
```

und meldet `IterationStatus.NumericalBreakdown`, wenn sie nicht positiv oder nicht endlich wird. Das ist ein starkes Indiz für einen verletzten SPD-Vertrag oder für einen durch Rundungsfehler zerstörten Rekursionszustand.

## Preconditioned CG

PCG wendet vor der Bildung der Suchrichtung eine angenäherte Inversoperation an:

```text
z = M^-1 * r
```

Die öffentliche `ISparsePreconditioner`-Abstraktion hält diese Operation unabhängig von einer konkreten Speicherform oder Faktorisierung.

```csharp
var jacobi = JacobiPreconditioner.Create(a);
var result = ConjugateGradientSolver.SolvePreconditioned(
    a,
    rightHandSide,
    jacobi,
    options: new SparseIterativeSolverOptions
    {
        RelativeTolerance = 1e-10,
        AbsoluteTolerance = 1e-12,
        MaximumIterations = 500
    });
```

Für PCG muss auch der Preconditioner SPD sein. Der eingebaute `JacobiPreconditioner` erfüllt diese Bedingung, wenn er aus einer SPD-Matrix erzeugt wird. Bei benutzerdefinierten Implementierungen wird der Vertrag indirekt über die PCG-Rekursion geprüft: `r^T*M^-1*r` muss positiv und endlich bleiben. Eine Verletzung führt zu `NumericalBreakdown`, statt die Krylov-Rekursion unbemerkt zu verfälschen.

Jacobi ist bewusst ein einfacher Referenz-Preconditioner und keine Behauptung optimaler Konvergenz. Er ist billig, erzeugt pro Anwendung keine Allokationen und hilft insbesondere bei unterschiedlichen Diagonalskalierungen. Leistungsfähigere Incomplete-Factorization-Verfahren können später hinter derselben Schnittstelle ergänzt werden.

## Verifikation des echten Residuums

CG/PCG aktualisiert das Residuum aus Effizienzgründen rekursiv. Ein vollständiges `A*x` in jeder Iteration würde den dominierenden Sparse-Matvec-Aufwand praktisch verdoppeln. Rekursive Residuen können sich durch Floating-Point-Rundung jedoch vom mathematisch echten Residuum entfernen.

Die SASD-Implementierung berechnet daher `b-A*x` erneut, sobald das rekursive Residuum erstmals Konvergenz behauptet. Nur dieses verifizierte Residuum darf `IterationStatus.Converged` auslösen. Verfehlt es die Schwelle, startet der Algorithmus mit dem aktualisierten Residuum neu und wendet den Preconditioner erneut an.

## Ergebnismodell

`SparseLinearSolveResult` verwendet das gemeinsame `IterationStatus`-Vokabular und liefert:

- beste verfügbare Lösung;
- Anzahl abgeschlossener Iterationen;
- finale und anfängliche euklidische Residualnorm;
- Norm der rechten Seite;
- effektive Konvergenzschwelle;
- relative Residualnorm;
- optionale Diagnose für Numerical Breakdown oder Iterationslimit.

Die gespeicherte Lösung ist gegen externe Änderung geschützt. `Solution` liefert eine defensive Kopie; `GetSolutionValue` und `CopySolutionTo` erlauben kontrollierten Zugriff ohne unnötige vollständige Kopien.

## Unpräconditioniertes Beispiel

```csharp
using Sasd.Numerics.LinearAlgebra.Sparse;

var a = CsrMatrix.FromEntries(
    3,
    3,
    [
        new SparseMatrixEntry(0, 0, 4.0),
        new SparseMatrixEntry(0, 1, -1.0),
        new SparseMatrixEntry(1, 0, -1.0),
        new SparseMatrixEntry(1, 1, 4.0),
        new SparseMatrixEntry(1, 2, -1.0),
        new SparseMatrixEntry(2, 1, -1.0),
        new SparseMatrixEntry(2, 2, 3.0)
    ]);

var result = ConjugateGradientSolver.Solve(
    a,
    [2.0, 4.0, 7.0]);
```

## Performance-Einordnung

Die aktuelle Implementierung ist bewusst ein nachvollziehbarer Managed-Referenzsolver. Arbeitsvektoren werden wiederverwendet und dichte Materialisierung vermieden; spekulatives SIMD, parallele Reduktionen oder native Sparse-Backends werden dagegen noch nicht erzwungen. Solche Optimierungen sollen erst nach Benchmarks und bei stabilen Solver-Verträgen folgen.

Die Preconditioner-Schnittstelle ist bereits solverneutral, sodass spätere GMRES- und BiCGSTAB-Implementierungen sie bei passendem mathematischem Vertrag wiederverwenden können. Restarted GMRES ist der nächste Sparse-Solver-Meilenstein.
