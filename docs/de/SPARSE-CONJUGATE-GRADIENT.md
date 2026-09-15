# Sparse-Conjugate-Gradient-Solver

## Zweck

`ConjugateGradientSolver` ist der erste iterative Solver auf dem kanonischen CSR-Fundament des SASD Math Toolkit. Er löst reelle dünnbesetzte Systeme

```text
A * x = b
```

wenn `A` **symmetrisch positiv definit (SPD)** ist. Die Matrix wird niemals in dichte Speicherung umgewandelt; feste Arbeitsvektoren werden für alle wiederholten Sparse-Matrix-Vektor-Produkte wiederverwendet.

## Konvergenzregel

Sparse-Solver teilen sich `SparseIterativeSolverOptions`. CG akzeptiert Konvergenz, wenn die euklidische Norm des tatsächlich neu berechneten Residuums erfüllt:

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

## Verifikation des echten Residuums

CG aktualisiert das Residuum rekursiv, weil ein vollständiges `A*x` in jeder Iteration den dominierenden Sparse-Matvec-Aufwand praktisch verdoppeln würde. Rekursive Residuen können sich durch Floating-Point-Rundung jedoch vom mathematisch echten Residuum entfernen.

Die SASD-Implementierung berechnet daher `b-A*x` erneut, sobald das rekursive Residuum erstmals Konvergenz behauptet. Nur dieses verifizierte Residuum darf `IterationStatus.Converged` auslösen. Verfehlt es die Schwelle, startet CG mit dem aktualisierten Residuum neu, statt eine falsche Konvergenz zu melden.

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

## Beispiel

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
    [2.0, 4.0, 7.0],
    options: new SparseIterativeSolverOptions
    {
        RelativeTolerance = 1e-10,
        AbsoluteTolerance = 1e-12,
        MaximumIterations = 500
    });

if (result.Converged)
{
    var x = result.Solution;
}
```

## Performance-Einordnung

Die aktuelle Implementierung ist bewusst ein nachvollziehbarer Managed-Referenzsolver. Arbeitsvektoren werden wiederverwendet und dichte Materialisierung vermieden; spekulatives SIMD, parallele Reduktionen oder native Sparse-Backends werden dagegen noch nicht erzwungen. Solche Optimierungen sollen erst nach Benchmarks und bei stabilen Solver-Verträgen folgen.

Die nächsten natürlichen Erweiterungen sind Preconditioning für CG sowie allgemeine nichtsymmetrische Krylov-Solver wie GMRES und BiCGSTAB. Sie sollen dieselben Options- und Result-Semantiken wiederverwenden.
