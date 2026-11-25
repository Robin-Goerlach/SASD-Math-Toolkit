# Sparse-Preconditioning

## Zweck

Preconditioning verändert die numerische Geometrie eines iterativen linearen Lösungsverfahrens, ohne die mathematische Lösung zu ändern. Statt einen Krylov-Solver direkt auf einem schwierig konditionierten System arbeiten zu lassen, wird eine angenäherte Inversoperation angewendet, damit das transformierte Problem günstiger iterierbar wird.

Die SASD-Sparse-Schicht beschreibt diese Operation über `ISparsePreconditioner`:

```csharp
public interface ISparsePreconditioner
{
    int Size { get; }
    void Apply(ReadOnlySpan<double> source, Span<double> destination);
}
```

Die Abstraktion stellt bewusst eine Operation und keine explizite inverse Matrix bereit. Iterative Solver benötigen typischerweise `z = M^-1*r`; eine vollständige Materialisierung von `M^-1` wäre oft teurer und weniger sparse als das direkte Anwenden einer Näherungsinverse.

## Jacobi-Preconditioner

`JacobiPreconditioner` ist die erste konkrete Implementierung. Sie speichert die reziproke Diagonale einer quadratischen CSR-Matrix und wendet

```text
z_i = r_i / A_ii
```

an. Aufbau und Speicherung sind leichtgewichtig, jede Anwendung benötigt O(n). Damit ist Jacobi ein sinnvoller Referenzbaustein, bevor aufwendigere Methoden wie Incomplete Cholesky oder ILU ergänzt werden.

```csharp
var jacobi = JacobiPreconditioner.Create(matrix);
var result = ConjugateGradientSolver.SolvePreconditioned(
    matrix,
    rightHandSide,
    jacobi);
```

Die voreingestellte Diagonalschwelle ist null: Nur exakt fehlende bzw. nullwertige Diagonaleinträge werden zurückgewiesen. Eine positive `absoluteDiagonalTolerance` kann bewusst sehr kleine Diagonaleinträge ablehnen. Die Schwelle ist ausdrücklich absolut, weil ein einziges globales Epsilon nicht für jede physikalische oder numerische Skala geeignet ist.

Würde der Kehrwert eines Diagonaleintrags den endlichen `double`-Bereich überschreiten, schlägt der Aufbau explizit fehl, statt `Infinity` in den Solver einzuschleusen.

## PCG-Vertrag

Preconditioned Conjugate Gradient (PCG) verlangt sowohl für die Systemmatrix als auch für den Preconditioner symmetrische positive Definitheit. Bei einer SPD-Systemmatrix ist der Jacobi-Preconditioner ebenfalls SPD, weil die Diagonaleinträge strikt positiv sind.

Ein eigener `ISparsePreconditioner` kann diesen Vertrag verletzen. Der Solver prüft daher die Rekursionsgröße

```text
r^T * M^-1 * r
```

und meldet `IterationStatus.NumericalBreakdown`, wenn sie nicht positiv oder nicht endlich ist. Die bereits vorhandene CG-Prüfung `p^T*A*p > 0` bleibt ebenfalls erhalten.

Diese Prüfungen beweisen nicht alle mathematischen Eigenschaften eines beliebigen benutzerdefinierten Preconditioners, verhindern aber, dass typische Vertragsverletzungen unbemerkt die Iteration verfälschen.

## Warum die Schnittstelle solverneutral ist

Die Abstraktion heißt bewusst nicht `ICgPreconditioner`. Spätere GMRES- und BiCGSTAB-Implementierungen können denselben Apply-Vertrag wiederverwenden, obwohl sie keinen SPD-Preconditioner verlangen. Solver-spezifische mathematische Anforderungen bleiben an der jeweiligen Solvergrenze dokumentiert und geprüft.

Damit können spätere Implementierungen wie Jacobi, Block-Jacobi, Incomplete Cholesky oder ILU überall dort wiederverwendet werden, wo ihre mathematischen Eigenschaften zum Solver passen.

## Performance-Einordnung

Preconditioner-Anwendungen sind für häufige Wiederverwendung mit caller-seitig bereitgestellten Spans ausgelegt. `JacobiPreconditioner` erzeugt pro Anwendung keine neuen Arrays und unterstützt überlappende Quell-/Zielbereiche, da jede Ausgabekomponente nur von der entsprechenden Eingabekomponente abhängt.

Aufwendigere Preconditioner werden erst mit klaren Speicher-, Breakdown- und Wiederverwendungsverträgen ergänzt. Der nächste Solver-Meilenstein ist restarted GMRES, danach folgen BiCGSTAB und eine Konsolidierungsrunde der Sparse-Architektur.
