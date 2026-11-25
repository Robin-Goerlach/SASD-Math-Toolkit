# Restarted GMRES

## Zweck

`GmresSolver` löst reelle quadratische Sparse-Systeme

```text
A * x = b
```

ohne Symmetrie oder positive Definitheit vorauszusetzen. Damit ergänzt GMRES die auf SPD-Probleme spezialisierten CG-/PCG-Solver um einen allgemeinen nichtsymmetrischen Krylov-Solver.

Die aktuelle Implementierung ist bewusst eine nachvollziehbare Managed-Referenzimplementierung. Die Sparse-Matrix wird niemals dicht materialisiert; der Krylov-Raum entsteht ausschließlich durch wiederholte CSR-Matrix-Vektor-Produkte.

## Arnoldi-Prozess und Least-Squares-Problem

GMRES baut mit dem Arnoldi-Prozess eine orthonormale Basis eines Krylov-Unterraums auf. Die SASD-Implementierung verwendet Modified Gram-Schmidt und führt bewusst einen zweiten Orthogonalisierungspass aus. Das kostet zusätzliche Skalarprodukte, reduziert aber in der Referenzimplementierung vermeidbaren Verlust der Basis-Orthogonalität.

Das entstehende kleine obere Hessenberg-Least-Squares-Problem wird inkrementell mit Givens-Rotationen aktualisiert. Dadurch steht eine Residualschätzung bereit, ohne in jeder inneren Iteration ein neues dichtes Least-Squares-Problem von Grund auf zu lösen.

## Restarting

Bei vollständigem GMRES wächst die Basis mit jeder Iteration um einen Vektor. Für große Sparse-Systeme kann das zu viel Speicher beanspruchen. Der öffentliche Parameter `restartLength` begrenzt deshalb die Basisgröße:

```csharp
var result = GmresSolver.Solve(
    matrix,
    rightHandSide,
    restartLength: 30,
    options: new SparseIterativeSolverOptions
    {
        RelativeTolerance = 1e-10,
        AbsoluteTolerance = 1e-12,
        MaximumIterations = 1000
    });
```

Am Ende eines Restart-Zyklus wird die aktuelle Least-Squares-Korrektur angewendet, das echte Residuum neu berechnet und eine neue Krylov-Basis von diesem Residuum aus aufgebaut.

Eine kleinere Restart-Länge spart Speicher, kann aber die Konvergenz verlangsamen oder sogar stagnieren lassen. Ein größerer Wert hält mehr Krylov-Information vor, benötigt dafür mehr Basisvektoren. Deshalb bleibt diese Entscheidung explizit.

## Right Preconditioning

`SolvePreconditioned` verwendet **Right Preconditioning**:

```text
A * M^-1 * y = b
x = M^-1 * y
```

Konzeptionell wird die Arnoldi-Basis für `A*M^-1` aufgebaut. Praktisch wird jeder Basisvektor `v_j` zunächst in

```text
z_j = M^-1 * v_j
```

überführt und anschließend `A*z_j` berechnet. Die spätere Korrektur der physikalischen Lösung wird aus den gespeicherten `z_j`-Vektoren zusammengesetzt.

Diese Wahl ist wichtig, weil das minimierte Residuum weiterhin das ursprüngliche Residuum `b-A*x` ist. Die gemeinsame SASD-Abbruchregel bleibt deshalb unverändert:

```text
||b-A*x||2 <= max(AbsoluteTolerance, RelativeTolerance * ||b||2)
```

Anders als bei PCG muss der GMRES-Preconditioner nicht SPD sein. Der übergebene `ISparsePreconditioner` soll sich während eines Solve als feste deterministische lineare angenäherte Inversoperation verhalten.

Beispiel:

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = GmresSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi,
    restartLength: 30);
```

## Residualprüfung und Breakdown

Das durch die Givens-Rotationen aktualisierte projizierte Residuum ist billig zu berechnen und dient dazu, Kandidatenlösungen zu erkennen. `Converged` wird jedoch erst gemeldet, nachdem das echte Residuum `b-A*x` explizit neu berechnet wurde.

`NumericalBreakdown` wird unter anderem gemeldet, wenn:

- ein Matrix-Vektor-Produkt nicht endlich wird;
- die Arnoldi-Orthogonalisierung nicht endliche Werte erzeugt;
- das projizierte Dreieckssystem singulär oder nicht endlich wird;
- der Krylov-Raum endet (`happy breakdown`), das echte Residuum aber noch oberhalb der gewünschten Toleranz liegt;
- ein Preconditioner nicht endliche Ausgaben liefert.

Auch `MaximumIterationsReached` enthält weiterhin die beste gefundene Lösung und Diagnostik des echten Residuums.

## Numerische Designentscheidungen

Die Managed-Referenzimplementierung priorisiert Nachvollziehbarkeit und Diagnostik vor aggressiver Optimierung:

- CSR-Matrix-Vektor-Produkte bleiben sparse;
- die Basisorthogonalisierung verwendet zwei Modified-Gram-Schmidt-Pässe;
- Skalarprodukte verwenden kompensierte Summation;
- euklidische Normen nutzen skalierte Akkumulation gegen vermeidbaren Over-/Underflow;
- Givens-Rotationen verwenden eine skalierungsbewusste Betragsbildung;
- spekulatives SIMD, parallele Reduktionen oder ein nativer Sparse-Backend sind nicht erforderlich.

Damit werden zuerst stabile Semantiken festgelegt. Benchmark-getriebene Optimierungen können später folgen, ohne den öffentlichen Konvergenz- und Ergebnisvertrag zu verändern.

## Einordnung in M3.2

Mit restarted GMRES deckt die Sparse-Schicht jetzt beide großen Strukturklassen ab:

- CG/PCG für SPD-Systeme;
- GMRES für allgemeine quadratische Systeme.

Als nächster Krylov-Schritt ist BiCGSTAB vorgesehen. Danach folgt eine Konsolidierungs- und Release-Readiness-Runde der Sparse-Architektur.
