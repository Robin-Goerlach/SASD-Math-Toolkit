# Sparse-BiCGSTAB-Solver

## Zweck

`BiCgStabSolver` ergänzt neben restarted GMRES einen zweiten allgemeinen Krylov-Pfad für nichtsymmetrische Systeme. Der Solver löst reelle quadratische Sparse-Systeme

```text
A * x = b
```

ohne Symmetrie oder positive Definitheit zu verlangen. BiCGSTAB verwendet eine kurze Rekurrenz und hält deshalb eine feste Anzahl von Arbeitsvektoren, statt wie GMRES eine wachsende Arnoldi-Basis zu speichern.

BiCGSTAB ist interessant, wenn der Speicherbedarf einer GMRES-Basis unerwünscht ist und die Rekurrenz für das konkrete Problem gut funktioniert. Es ist kein universeller Ersatz für GMRES: der Residualverlauf kann unruhiger sein und die Rekurrenz besitzt mehr algebraische Breakdown-Punkte.

## Gemeinsamer Konvergenzvertrag

BiCGSTAB verwendet `SparseIterativeSolverOptions` und `SparseLinearSolveResult`. Konvergenz wird ausschließlich über die echte euklidische Residualnorm des ursprünglichen Systems definiert:

```text
||b - A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Die Rekurrenz führt ein günstiges Residuum mit, aber `Converged` wird erst gemeldet, nachdem `b-A*x` explizit neu berechnet wurde. Erreicht nur das rekursive Residuum die Toleranz, das echte Residuum aber nicht, startet der Solver die kurze Rekurrenz vom aktualisierten physikalischen Residuum neu.

## Right Preconditioning

`SolvePreconditioned` verwendet dieselbe solverneutrale `ISparsePreconditioner`-Operation wie GMRES. Suchrichtungen werden vor der Matrixmultiplikation mit `M^-1` transformiert, während das gemeldete Residuum weiterhin zum ursprünglichen Gleichungssystem gehört.

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = BiCgStabSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi,
    options: new SparseIterativeSolverOptions
    {
        AbsoluteTolerance = 1e-12,
        RelativeTolerance = 1e-10,
        MaximumIterations = 1000
    });
```

Anders als PCG verlangt BiCGSTAB keinen SPD-Preconditioner. Die Operation soll innerhalb eines Solve-Laufs trotzdem fest, deterministisch und numerisch endlich bleiben.

## Breakdown-Diagnostik

Die kurze Rekurrenz enthält mehrere skalare Divisionen, die selbst bei endlicher Matrix und rechter Seite undefiniert werden können. Die Implementierung meldet `IterationStatus.NumericalBreakdown`, statt ein scheinbares Ergebnis zu erzeugen, wenn beispielsweise:

- das Skalarprodukt mit dem Shadow-Residual null oder nicht endlich wird;
- der Alpha-Nenner null oder nicht endlich wird;
- der Nenner der Stabilisierung vor Konvergenz verschwindet;
- der Stabilisationskoeffizient `omega` null oder nicht endlich wird;
- Preconditioner oder Sparse-Matvec nichtendliche Werte erzeugen.

Das ist von normaler Nichtkonvergenz zu unterscheiden. `MaximumIterationsReached` bedeutet, dass die Rekurrenz numerisch sinnvoll blieb, aber die geforderte Toleranz im Iterationsbudget nicht erreichte. `NumericalBreakdown` bedeutet, dass die BiCGSTAB-Rekurrenz selbst nicht sicher fortgesetzt werden konnte.

## Speicherbedarf und Solverwahl

BiCGSTAB benötigt O(n) Vektorspeicher unabhängig von der Iterationszahl. Restarted GMRES benötigt für Restart-Länge `m` O(m*n) Basisdaten. Dadurch ist BiCGSTAB für speichersensitive große Systeme attraktiv; GMRES ist jedoch häufig leichter zu beurteilen, weil es ein projiziertes Residuum direkt minimiert und typischerweise einen glatteren Konvergenzverlauf besitzt.

Als erste praktische Auswahlregel gilt deshalb:

- SPD-System: CG/PCG;
- allgemeines nichtsymmetrisches System mit Priorität auf Robustheit: restarted GMRES;
- allgemeines nichtsymmetrisches System mit besonders wichtigem begrenztem Vektorspeicher: BiCGSTAB und Residualdiagnostik beobachten.

## Performance-Einordnung

Die aktuelle Implementierung bleibt ein abhängigkeitfreier Managed-Referenzsolver. Arbeitsarrays werden einmal pro Solve angelegt und über die Iterationen wiederverwendet; Sparse-Matrizen werden niemals dicht materialisiert. SIMD, parallele Reduktionen, ILU-artige Preconditioner und native Sparse-Backends bleiben benchmarkgetriebene spätere Arbeit.
