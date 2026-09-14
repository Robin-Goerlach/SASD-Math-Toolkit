# Nichtlineares Shooting für Randwertprobleme zweiter Ordnung

## Zweck

`NonlinearShooting.Solve` behandelt skalare nichtlineare Dirichlet-Randwertprobleme

`y'' = g(x, y, y')`

mit vorgegebenen Randwerten

`y(x0) = alpha` und `y(xEnd) = beta`.

Die Implementierung ist eigenständiger C#-Code auf Grundlage des mathematischen Shooting-Verfahrens. Die historische Borland-Toolbox dient nur als funktionale V1-Referenz; historischer Quellcode oder Handbuchtext wird nicht übernommen.

## Shooting als Nullstellenproblem

RK4 benötigt eine Anfangssteigung. Ein Dirichlet-Randwertproblem liefert stattdessen den Lösungswert am rechten Rand. Sei `s` eine versuchsweise Anfangssteigung. Dann wird `y(x0)=alpha`, `y'(x0)=s` integriert. Das Randresiduum lautet

`R(s) = y(xEnd; s) - beta`.

Die gesuchte Anfangssteigung ist also eine Nullstelle von `R`. Bei einer nichtlinearen Differentialgleichung ist diese Abbildung im Allgemeinen ebenfalls nichtlinear; die direkte Linearkombination des `LinearShooting` steht daher nicht zur Verfügung.

SASD verwendet für die Steigungssuche das Sekantenverfahren. Es benötigt zwei Anfangsschätzungen, aber keine Ableitung der Randabbildung.

## Wiederverwendung vorhandener Bausteine

Die Implementierung kombiniert bewusst bereits vorhandene Algorithmen:

- `RungeKutta.FourthOrderSecondOrder` integriert jeden Versuch;
- `RootSolvers.Secant` korrigiert die unbekannte Anfangssteigung;
- `NonlinearShooting` verantwortet nur die Abbildung zwischen Steigung und Randresiduum.

Damit entstehen weder eine zweite RK4- noch eine zweite Sekantenimplementierung im Randwertmodul.

## Ergebnis und Abbruchstatus

`NonlinearShootingResult` enthält die finale Trajektorie, `InitialSlope`, `RightBoundaryResidual`, `Iterations`, `Status`, `Message` und die Komforteigenschaft `Converged`.

Nichtkonvergenz ist im Gegensatz zu ungültigen Eingaben ein erwartbares numerisches Ergebnis. Deshalb werden `MaximumIterationsReached` oder `NumericalBreakdown` zurückgegeben, statt allein wegen einer erfolglosen Sekanteniteration eine Exception auszulösen. Die ausgegebene Trajektorie gehört zur letzten noch verwendbaren Steigung und kann zur Diagnose untersucht werden.

Ein Status `Converged` wird zusätzlich nur akzeptiert, wenn das tatsächliche Randresiduum `BoundaryTolerance` einhält. So wird eine stagnierende Steigungsfolge nicht fälschlich als erfolgreicher Randtreffer gemeldet.

## Optionen

`NonlinearShootingOptions` bietet derzeit `BoundaryTolerance` für die Sekantensuche und die abschließende Randprüfung sowie `MaximumIterations` für die maximale Anzahl der Steigungskorrekturen. Die RK4-Schrittweite bleibt ein eigener Parameter, weil Diskretisierungsfehler und Konvergenz der Shooting-Suche zwei verschiedene Aspekte sind.

## Beispiel mit exakter nichtlinearer Lösung

Die Funktion `y = 1 / (1 - x)` erfüllt `y'' = 2 y^3`. Auf `[0, 0.5]` gilt `y(0)=1`, `y(0.5)=2`, und die unbekannte Anfangssteigung ist exakt 1.

```csharp
var result = NonlinearShooting.Solve(
    (_, y, _) => 2.0 * y * y * y,
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 0.5,
    rightValue: 2.0,
    step: 0.005,
    firstSlopeGuess: 0.5,
    secondSlopeGuess: 1.5,
    options: new NonlinearShootingOptions(
        BoundaryTolerance: 1e-10,
        MaximumIterations: 25));

if (result.Converged)
{
    Console.WriteLine(result.InitialSlope);          // ungefähr 1
    Console.WriteLine(result.FinalPoint.Y);          // ungefähr 2
    Console.WriteLine(result.RightBoundaryResidual); // nahe null
}
```

## Numerische Interpretation

Ein kleines Randresiduum beweist nur, dass die finale RK4-Trajektorie bei der gewählten Diskretisierung nahe am geforderten rechten Randwert endet. Es ist keine globale Fehlerschätzung. Für relevante Genauigkeitsanforderungen sollte mit kleinerer `step`-Schrittweite wiederholt und auch die innere Trajektorie verglichen werden.

Das Sekanten-Shooting hängt außerdem von den beiden Startsteigungen ab. Schlechte Schätzungen können langsam konvergieren, bei nahezu gleichen Residuen in einen numerischen Abbruch laufen oder bei Randwertproblemen mit mehreren Lösungen zu einer anderen Lösung führen. Die API macht solche Ergebnisse bewusst sichtbar.

## Umfang

Die V1-API behandelt skalare explizite Gleichungen zweiter Ordnung mit Wertbedingungen an beiden Enden. Neumann-/Robin-Bedingungen, Multiple Shooting, Continuation und spezielle steife BVP-Solver gehören nicht zum historischen V1-Ziel und sind mögliche spätere Erweiterungen.
