# Gewöhnliche Differentialgleichungen und Randwertprobleme

Dieses Kapitel beschreibt die derzeit stabilen C#-APIs für Anfangswert- und Randwertprobleme. Es wächst weiter, sobald die noch fehlenden V1-Verfahren implementiert sind.

## Anfangswertprobleme erster Ordnung

Ein skalares Anfangswertproblem erster Ordnung besteht aus

`y' = f(x, y)` und `y(x0) = y0`.

Das Toolkit bietet derzeit das klassische Runge-Kutta-Verfahren vierter Ordnung mit fester Schrittweite (RK4), das adaptive Runge-Kutta-Fehlberg-Verfahren 4(5) (RKF45) sowie einen Adams-Bashforth-/Adams-Moulton-Prädiktor-Korrektor vierter Ordnung.

## RK4 mit fester Schrittweite

RK4 ist sinnvoll, wenn bewusst eine feste nominale Schrittweite verwendet werden soll und eine geeignete Auflösung bereits bekannt ist.

```csharp
using Sasd.Numerics.DifferentialEquations;

var points = RungeKutta.FourthOrder(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    step: 0.01);

Console.WriteLine(points[^1].Y); // ungefähr e
```

Nur der letzte Schritt wird bei Bedarf verkürzt, damit die Folge genau bei `xEnd` endet.

## Gleichungen zweiter Ordnung mit RK4

Ein skalares Anfangswertproblem zweiter Ordnung hat die Form

`y'' = g(x, y, y')`

und benötigt zwei Anfangswerte: `y(x0)` und `y'(x0)`. Ein solches Problem lässt sich immer manuell als System aus zwei Gleichungen erster Ordnung formulieren. `FourthOrderSecondOrder` übernimmt diese Standardtransformation und liefert sowohl `y` als auch `y'` zurück.

```csharp
using Sasd.Numerics.DifferentialEquations;

// Harmonischer Oszillator: y'' = -y, y(0)=0, y'(0)=1.
var points = RungeKutta.FourthOrderSecondOrder(
    (_, y, _) => -y,
    x0: 0.0,
    y0: 0.0,
    firstDerivative0: 1.0,
    xEnd: Math.PI / 2.0,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.Y);               // ungefähr 1
Console.WriteLine(final.FirstDerivative); // ungefähr 0
```

Das ist eine Komfort-API und keine zweite unabhängige RK4-Implementierung. Intern wird die Gleichung zu `y'=v`, `v'=g(x,y,v)` umgeformt und über denselben getesteten System-RK4-Kern integriert.

## Gleichungen n-ter Ordnung mit RK4

Für ein skalares Problem der Ordnung `n` wird die Gleichung so geschrieben, dass die höchste Ableitung isoliert ist:

`y^(n) = g(x, y, y', ..., y^(n-1))`.

`FourthOrderNthOrder` erhält die niedrigeren Ableitungen als geordneten Anfangszustand. Element null ist `y`, Element eins ist `y'` und so weiter. Die Anzahl der Elemente bestimmt die Ordnung der Gleichung.

```csharp
using Sasd.Numerics.DifferentialEquations;

// y''' = -y', mit y(0)=0, y'(0)=1, y''(0)=0.
var points = RungeKutta.FourthOrderNthOrder(
    (_, state) => -state[1],
    x0: 0.0,
    initialState: [0.0, 1.0, 0.0],
    xEnd: Math.PI / 2.0,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.Y);                // ungefähr 1
Console.WriteLine(final.GetDerivative(1)); // ungefähr 0
Console.WriteLine(final.GetDerivative(2)); // ungefähr -1
```

Jeder `NthOrderOdePoint` speichert einen schreibgeschützten Snapshot `[y, y', ..., y^(n-1)]`. `GetDerivative(0)` liefert denselben Wert wie `Y`. Der Solver bildet intern das mathematische Begleitsystem erster Ordnung und verwendet danach denselben RK4-Systemkern; für jede mögliche Ordnung wird also keine eigene Runge-Kutta-Formel dupliziert.

Diese API ist besonders praktisch, wenn die ursprüngliche Gleichung natürlich in höherer Ordnung formuliert ist. Besteht das Modell bereits aus mehreren wechselwirkenden Variablen erster Ordnung, ist `FourthOrderSystem` die passendere direkte Schnittstelle.

## Gekoppelte Systeme zweiter Ordnung mit RK4

Viele physikalische Modelle besitzen mehrere Variablen zweiter Ordnung, die sich gegenseitig beeinflussen. Ein solches System wird als

`Y'' = G(x, Y, Y')`

geschrieben. `FourthOrderSecondOrderSystem` hält Werte und erste Ableitungen an der öffentlichen API bewusst in getrennten Vektoren und reduziert das Problem intern auf ein gewöhnliches System erster Ordnung.

```csharp
using Sasd.Numerics.DifferentialEquations;

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

var final = points[^1];
Console.WriteLine(final.GetValue(0));
Console.WriteLine(final.GetFirstDerivative(0));
```

Das Beispiel beschreibt zwei gekoppelte Oszillatoren. Beide Anfangsvektoren müssen dieselbe Dimension besitzen, und der Callback muss genau eine zweite Ableitung pro Gleichung zurückgeben. `SecondOrderSystemOdePoint` kopiert seine Vektoren, sodass spätere Änderungen an aufrufereigenen Arrays die berechnete Trajektorie nicht verändern.

Diese API passt besonders zu Modellen, die natürlich als Gruppe von Gleichungen zweiter Ordnung formuliert sind, etwa gekoppelte mechanische Koordinaten. Liegt das Modell bereits als allgemeiner Zustandsvektor erster Ordnung vor, ist `FourthOrderSystem` die direktere Schnittstelle.

## Lineare Randwertprobleme mit Shooting

Bei einem Randwertproblem kann die Lösung an **beiden** Intervallenden vorgegeben sein, statt eine Anfangsableitung zu kennen. Die aktuelle lineare Shooting-API behandelt

`y'' = p(x)y' + q(x)y + r(x)`

mit `y(x0)=alpha` und `y(xEnd)=beta`.

```csharp
using Sasd.Numerics.DifferentialEquations;

// y'' = 2, y(0)=1, y(1)=4 -> y = 1 + 2x + x^2.
var result = LinearShooting.Solve(
    _ => 0.0, // p(x)
    _ => 0.0, // q(x)
    _ => 2.0, // r(x)
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 1.0,
    rightValue: 4.0,
    step: 0.05);

Console.WriteLine(result.InitialSlope);          // ungefähr 2
Console.WriteLine(result.FinalPoint.Y);          // ungefähr 4
Console.WriteLine(result.RightBoundaryResidual); // nahe null
```

Das lineare Shooting integriert zwei Anfangswertprobleme zweiter Ordnung mit RK4: eine partikuläre Lösung und eine homogene Sensitivitätslösung. Ihre Linearkombination wird so gewählt, dass der rechte Randwert erfüllt wird. Deshalb ist für das lineare Problem noch keine iterative Folge von Steigungsraten notwendig.

`AuxiliaryRightValue` gibt den Sensitivitätsnenner der Konstruktion zurück. Ist sein Betrag kleiner als `singularityTolerance`, wird die Randabbildung als singulär oder numerisch schlecht konditioniert behandelt, statt durch einen instabil kleinen Wert zu teilen.

Das ausgegebene Randresiduum beschreibt, wie gut der **konstruierte Endpunkt** die gewünschte Randbedingung trifft. Es ist keine globale Fehlerschätzung für alle inneren Punkte. Wenn die numerische Genauigkeit wichtig ist, sollte wie bei gewöhnlichem RK4 mit kleinerer Schrittweite gegengeprüft werden.

## Adaptives RKF45

RKF45 ist besonders nützlich, wenn sich die Lösung innerhalb des Intervalls unterschiedlich schnell ändert oder wenn lieber eine Fehlertoleranz vorgegeben werden soll als eine einzige feste Schrittweite.

```csharp
using Sasd.Numerics.DifferentialEquations;

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

if (result.Completed)
{
    Console.WriteLine(result.FinalPoint.Y);
    Console.WriteLine($"Akzeptiert: {result.AcceptedSteps}, verworfen: {result.RejectedSteps}");
}
else
{
    Console.WriteLine($"Abbruch mit {result.Status}: {result.Message}");
}
```

Der adaptive Solver berechnet für jeden Versuchsschritt zwei verwandte Näherungen. Ihre Differenz dient als lokale Fehlerschätzung. Erfüllt sie die kombinierte absolute und relative Toleranz, wird der Schritt akzeptiert; andernfalls wird er verworfen und mit kleinerer Schrittweite wiederholt.

## Adams-Prädiktor-Korrektor

Das Adams-Verfahren ist interessant, wenn ein regelmäßiges Ausgabegitter erwünscht ist und die Wiederverwendung früherer Ableitungswerte Vorteile bringt. Die aktuelle Implementierung kombiniert den Adams-Bashforth-Prädiktor vierter Ordnung mit dem Adams-Moulton-Korrektor vierter Ordnung.

```csharp
using Sasd.Numerics.DifferentialEquations;

var points = AdamsBashforthMoulton.Integrate(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    maximumStep: 0.1);

Console.WriteLine(points[^1].Y); // ungefähr e
```

Ein Mehrschrittverfahren benötigt eine Historie. Das SASD Math Toolkit berechnet deshalb zunächst drei Intervalle mit RK4 und wechselt anschließend auf AB4/AM4. Standardmäßig erfolgt ein Adams-Moulton-Korrekturdurchlauf. Mit `correctorIterations` können weitere feste Korrekturdurchläufe angefordert werden; mehr Durchläufe sind jedoch nicht für jedes Problem automatisch besser.

### Warum `maximumStep` nicht immer die tatsächliche Schrittweite ist

Die AB4/AM4-Koeffizienten setzen gleiche Abstände voraus. Teilt `maximumStep` das Intervall nicht exakt, wird daher **kein** verkürzter letzter Adams-Schritt angehängt. Stattdessen wählt der Solver die kleinste ganze Schrittzahl, die den Maximalabstand einhält, und verteilt diese Schritte gleichmäßig auf das vollständige Intervall.

Von 0 bis 1 mit `maximumStep: 0.3` entstehen beispielsweise vier Intervalle mit Schrittweite 0,25. Die Mehrschritt-Historie bleibt dadurch mathematisch gültig und der Endpunkt wird trotzdem exakt erreicht.

## Wahl zwischen den Verfahren

RK4 eignet sich als einfache feste Referenzrechnung. RKF45 ist sinnvoll, wenn automatische lokale Fehlerkontrolle und variable Schrittweiten wichtiger sind. Adams-Bashforth/Moulton passt gut, wenn ein regelmäßiges Gitter und die Wiederverwendung der Ableitungshistorie erwünscht sind. Für skalare Gleichungen zweiter oder höherer Ordnung sowie gekoppelte Systeme zweiter Ordnung sind die passenden RK4-Komfort-APIs meist klarer, als den entsprechenden Zustand erster Ordnung selbst zu packen, sofern die allgemeine Systemschnittstelle nicht ausdrücklich benötigt wird. Lineares Shooting passt, wenn die Gleichung linear ist, aber an beiden Enden Werte statt eines vollständigen Anfangszustands vorgegeben sind.

Keines dieser Verfahren ist automatisch die richtige Wahl für steife Differentialgleichungen. Ändert sich das Ergebnis stark, wenn Schrittweite oder Toleranz verschärft werden, sollte die numerische Stabilität untersucht werden, statt zusätzliche Nachkommastellen mit zusätzlicher Genauigkeit gleichzusetzen.

## RKF-Toleranzen wählen

Kleinere Toleranzen benötigen in der Regel mehr Funktionsauswertungen. Extrem kleine Werte sind nicht automatisch besser: Diskretisierungsfehler, Rundungsfehler und die Kondition des Problems bleiben bestehen.

Ein sinnvoller Arbeitsablauf ist deshalb, zunächst mit praxisnahen Toleranzen zu rechnen, anschließend die Toleranzen zu verschärfen und die fachlich relevante Ergebnisgröße zu vergleichen. Ändert sich das Ergebnis noch deutlich, war die erste Rechnung numerisch noch nicht stabil genug.

## Verworfene RKF-Schritte verstehen

Verworfene Schritte sind bei einem adaptiven Verfahren zunächst normal. Einige Verwerfungen zeigen oft nur, dass die Schrittweitensteuerung eine geeignete Größe sucht. Sehr viele Verwerfungen können auf einen zu großen Anfangsschritt, stark wechselnde Dynamik oder für RKF45 teure Toleranzen hinweisen.

`MinimumStepSizeReached` ist dagegen ein echter Abbruchzustand: Die gewünschte lokale Toleranz ließ sich nicht einhalten, ohne die konfigurierte minimale Schrittweite zu unterschreiten. Eine kleinere Minimalweite kann helfen; möglicherweise ist für das Problem aber auch ein anderes numerisches Verfahren geeigneter.

## Derzeitige Grenzen

Die adaptive Implementierung behandelt skalare Differentialgleichungen erster Ordnung und integriert vorwärts. RK4 deckt skalare Gleichungen erster, zweiter und n-ter Ordnung sowie gekoppelte Systeme erster und zweiter Ordnung ab. Adams ist skalar und arbeitet mit fester Schrittweite. Das lineare Shooting behandelt derzeit skalare lineare Dirichlet-Randwertprobleme zweiter Ordnung. Als wesentlicher V1-Punkt im Randwertbereich verbleibt das nichtlineare Shooting; allgemeinere Neumann-/Robin-Randbedingungen liegen außerhalb der aktuellen V1-Komfort-API.
