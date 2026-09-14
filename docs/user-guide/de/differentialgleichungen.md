# Gewöhnliche Differentialgleichungen

Dieses Kapitel beschreibt die derzeit stabilen C#-APIs für Anfangswertprobleme erster Ordnung. Es wächst weiter, sobald die noch fehlenden V1-Verfahren für Differential- und Randwertprobleme implementiert sind.

## Das Problem

Ein Anfangswertproblem erster Ordnung besteht aus einer Differentialgleichung und einem Startwert:

`y' = f(x, y)` und `y(x0) = y0`.

Das Toolkit bietet derzeit das klassische Runge-Kutta-Verfahren vierter Ordnung mit fester Schrittweite (RK4) sowie das adaptive Runge-Kutta-Fehlberg-Verfahren 4(5) (RKF45).

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

## Toleranzen wählen

Kleinere Toleranzen benötigen in der Regel mehr Funktionsauswertungen. Extrem kleine Werte sind nicht automatisch besser: Diskretisierungsfehler, Rundungsfehler und die Kondition des Problems bleiben bestehen.

Ein sinnvoller Arbeitsablauf ist deshalb, zunächst mit praxisnahen Toleranzen zu rechnen, anschließend die Toleranzen zu verschärfen und die fachlich relevante Ergebnisgröße zu vergleichen. Ändert sich das Ergebnis noch deutlich, war die erste Rechnung numerisch noch nicht stabil genug.

## Verworfene Schritte verstehen

Verworfene Schritte sind bei einem adaptiven Verfahren zunächst normal. Einige Verwerfungen zeigen oft nur, dass die Schrittweitensteuerung eine geeignete Größe sucht. Sehr viele Verwerfungen können auf einen zu großen Anfangsschritt, stark wechselnde Dynamik oder für RKF45 teure Toleranzen hinweisen.

`MinimumStepSizeReached` ist dagegen ein echter Abbruchzustand: Die gewünschte lokale Toleranz ließ sich nicht einhalten, ohne die konfigurierte minimale Schrittweite zu unterschreiten. Eine kleinere Minimalweite kann helfen; möglicherweise ist für das Problem aber auch ein anderes numerisches Verfahren geeigneter.

## Derzeitige Grenzen

Die adaptive Implementierung behandelt skalare Differentialgleichungen erster Ordnung und integriert vorwärts. RK4 unterstützt bereits gekoppelte Systeme erster Ordnung. Komfort-APIs für höhere Ordnung, Adams-Prädiktor-Korrektor-Verfahren sowie lineare und nichtlineare Shooting-Verfahren sind noch V1-Arbeitspunkte und werden erst dokumentiert, wenn die APIs tatsächlich vorhanden sind.
