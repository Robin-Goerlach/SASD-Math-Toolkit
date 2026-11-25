# Gewöhnliche Differentialgleichungen und Randwertprobleme

Dieses Kapitel beschreibt die derzeit stabilen C#-APIs für Anfangswert- und Randwertprobleme. Es wächst weiter, sobald die noch fehlenden V1-Verfahren implementiert sind.

## Anfangswertprobleme erster Ordnung

Ein skalares Anfangswertproblem erster Ordnung besteht aus

`y' = f(x, y)` und `y(x0) = y0`.

Das Toolkit bietet derzeit das klassische Runge-Kutta-Verfahren vierter Ordnung mit fester Schrittweite (RK4), das adaptive Runge-Kutta-Fehlberg-Verfahren 4(5) (RKF45) sowie einen Adams-Bashforth-/Adams-Moulton-Prädiktor-Korrektor vierter Ordnung.

## RK4 mit fester Schrittweite

```csharp
using Sasd.Numerics.DifferentialEquations;

var points = RungeKutta.FourthOrder(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    step: 0.01);
```

Nur der letzte Schritt wird bei Bedarf verkürzt, damit die Folge genau bei `xEnd` endet.

## Gleichungen zweiter Ordnung mit RK4

Ein skalares Anfangswertproblem zweiter Ordnung hat die Form `y'' = g(x, y, y')` und benötigt `y(x0)` sowie `y'(x0)`.

```csharp
var points = RungeKutta.FourthOrderSecondOrder(
    (_, y, _) => -y,
    x0: 0.0,
    y0: 0.0,
    firstDerivative0: 1.0,
    xEnd: Math.PI / 2.0,
    step: 0.01);
```

Intern wird die Gleichung zu `y'=v`, `v'=g(x,y,v)` umgeformt und über denselben getesteten System-RK4-Kern integriert.

## Gleichungen n-ter Ordnung mit RK4

Für `y^(n) = g(x, y, y', ..., y^(n-1))` erhält `FourthOrderNthOrder` die niedrigeren Ableitungen als geordneten Anfangszustand `[y, y', ..., y^(n-1)]`.

```csharp
var points = RungeKutta.FourthOrderNthOrder(
    (_, state) => -state[1],
    x0: 0.0,
    initialState: [0.0, 1.0, 0.0],
    xEnd: Math.PI / 2.0,
    step: 0.01);
```

Jeder `NthOrderOdePoint` speichert einen schreibgeschützten Snapshot des Zustands. Die eigentliche RK4-Logik bleibt im gemeinsamen Systemkern.

## Gekoppelte Systeme zweiter Ordnung mit RK4

Mehrere gekoppelte Variablen zweiter Ordnung werden als `Y'' = G(x, Y, Y')` formuliert.

```csharp
var end = Math.PI / (2.0 * Math.Sqrt(2.0));
var points = RungeKutta.FourthOrderSecondOrderSystem(
    (_, values, _) =>
    {
        var difference = values[0] - values[1];
        return [-difference, difference];
    },
    x0: 0.0,
    initialValues: [1.0, -1.0],
    initialFirstDerivatives: [0.0, 0.0],
    xEnd: end,
    step: 0.01);
```

Die öffentlichen Werte- und Ableitungsvektoren bleiben getrennt, intern wird wieder auf ein System erster Ordnung reduziert.

## Lineare Randwertprobleme mit Shooting

Das lineare Shooting behandelt

`y'' = p(x)y' + q(x)y + r(x)`

mit `y(x0)=alpha` und `y(xEnd)=beta`.

```csharp
var result = LinearShooting.Solve(
    _ => 0.0,
    _ => 0.0,
    _ => 2.0,
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 1.0,
    rightValue: 4.0,
    step: 0.05);

Console.WriteLine(result.InitialSlope);
Console.WriteLine(result.RightBoundaryResidual);
```

Das Verfahren integriert eine partikuläre und eine homogene Sensitivitätslösung. Wegen der Linearität kann die fehlende Anfangssteigung direkt aus beiden Lösungen kombiniert werden. `AuxiliaryRightValue` macht dabei eine singuläre oder schlecht konditionierte Randabbildung sichtbar.

## Nichtlineare Randwertprobleme mit Shooting

Für

`y'' = g(x, y, y')`

mit `y(x0)=alpha` und `y(xEnd)=beta` hängt der rechte Randwert im Allgemeinen nichtlinear von der unbekannten Anfangssteigung ab. `NonlinearShooting.Solve` verwendet deshalb zwei Startsteigungen und sucht mit dem Sekantenverfahren eine Nullstelle des Randresiduums

`R(s) = y(xEnd; s) - beta`.

```csharp
// Exakte Lösung y=1/(1-x): y''=2y^3,
// y(0)=1, y(0.5)=2 und y'(0)=1.
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
    Console.WriteLine(result.RightBoundaryResidual); // nahe null
}
else
{
    Console.WriteLine($"{result.Status}: {result.Message}");
}
```

Nichtkonvergenz ist ein erwartbares numerisches Ergebnis und wird über `Status` gemeldet. `MaximumIterationsReached` bedeutet, dass die erlaubten Sekantenkorrekturen ausgeschöpft wurden. `NumericalBreakdown` kann auftreten, wenn zwei Randresiduen für eine stabile Sekantenkorrektur zu ähnlich werden. Die letzte noch nutzbare Trajektorie bleibt im Ergebnis erhalten.

Ein Status `Converged` wird nur akzeptiert, wenn das tatsächliche Randresiduum die angeforderte `BoundaryTolerance` erfüllt. Die beiden Startsteigungen sind fachlich relevant: Bei mehreren Lösungen können unterschiedliche Startwerte zu unterschiedlichen Lösungen des Randwertproblems führen.

## Adaptives RKF45

RKF45 ist sinnvoll, wenn automatische lokale Fehlerkontrolle und variable Schrittweiten wichtiger sind als eine feste Schrittweite.

```csharp
var result = RungeKuttaFehlberg.Integrate(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    new RungeKuttaFehlbergOptions
    {
        InitialStep = 0.25,
        MinimumStep = 1e-8,
        MaximumStep = 0.5,
        AbsoluteTolerance = 1e-10,
        RelativeTolerance = 1e-10
    });
```

Verworfene Versuchsschritte sind bei einem adaptiven Verfahren normal. `MinimumStepSizeReached` bedeutet dagegen, dass die gewünschte lokale Toleranz mit der erlaubten Minimalweite nicht eingehalten werden konnte.

## Adams-Prädiktor-Korrektor

Das Adams-Verfahren verwendet einen Adams-Bashforth-Prädiktor vierter Ordnung und einen Adams-Moulton-Korrektor vierter Ordnung. Die ersten drei Intervalle werden mit RK4 erzeugt, weil das Mehrschrittverfahren zunächst eine Historie benötigt.

```csharp
var points = AdamsBashforthMoulton.Integrate(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    maximumStep: 0.1);
```

Die AB4/AM4-Koeffizienten verlangen gleichmäßige Abstände. Deshalb wird das gesamte Gitter angepasst, wenn `maximumStep` das Intervall nicht exakt teilt.

## Wahl zwischen den Verfahren

RK4 eignet sich als einfache feste Referenzrechnung. RKF45 bietet automatische lokale Fehlerkontrolle. Adams passt gut zu einem regelmäßigen Gitter und zur Wiederverwendung früherer Ableitungen. Lineares Shooting ist für lineare Gleichungen mit Wertbedingungen an beiden Enden gedacht; nichtlineares Shooting ergänzt diesen Fall, wenn die fehlende Anfangssteigung iterativ bestimmt werden muss.

Keines dieser Verfahren ist automatisch die richtige Wahl für steife Differentialgleichungen.

## Randresiduum und globale Genauigkeit

Bei beiden Shooting-Verfahren bedeutet ein sehr kleines `RightBoundaryResidual` nur, dass die finale diskrete Trajektorie den verlangten Endwert gut trifft. Es ist keine Fehlerschranke für die inneren Punkte. Für belastbare Ergebnisse sollte mit kleinerer RK4-Schrittweite wiederholt und die fachlich wichtige Ergebnisgröße verglichen werden.

Beim nichtlinearen Shooting steuert `BoundaryTolerance` die Steigungssuche, während `step` den RK4-Diskretisierungsfehler beeinflusst. Nur eine dieser Größen zu verschärfen verbessert nicht automatisch die andere Fehlerquelle.

## Derzeitige Grenzen

RK4 deckt skalare Gleichungen erster, zweiter und n-ter Ordnung sowie gekoppelte Systeme erster und zweiter Ordnung ab. RKF45 ist derzeit skalar und vorwärtsgerichtet, Adams skalar mit festem Gitter. Die historischen V1-Verfahren für lineares und nichtlineares Shooting bei skalaren Dirichlet-Randwertproblemen zweiter Ordnung sind nun beide implementiert. Allgemeinere Neumann-/Robin-Bedingungen, Multiple Shooting, Continuation-Verfahren und spezielle steife BVP-Solver liegen außerhalb des aktuellen V1-Kompatibilitätsziels.
