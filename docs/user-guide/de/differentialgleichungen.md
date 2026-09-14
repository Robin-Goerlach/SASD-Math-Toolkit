# Gewöhnliche Differentialgleichungen

Dieses Kapitel beschreibt die derzeit stabilen C#-APIs für Anfangswertprobleme erster Ordnung. Es wächst weiter, sobald die noch fehlenden V1-Verfahren für Differential- und Randwertprobleme implementiert sind.

## Das Problem

Ein Anfangswertproblem erster Ordnung besteht aus einer Differentialgleichung und einem Startwert:

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

RK4 eignet sich als einfache feste Referenzrechnung. RKF45 ist sinnvoll, wenn automatische lokale Fehlerkontrolle und variable Schrittweiten wichtiger sind. Adams-Bashforth/Moulton passt gut, wenn ein regelmäßiges Gitter und die Wiederverwendung der Ableitungshistorie erwünscht sind.

Keines dieser Verfahren ist automatisch die richtige Wahl für steife Differentialgleichungen. Ändert sich das Ergebnis stark, wenn Schrittweite oder Toleranz verschärft werden, sollte die numerische Stabilität untersucht werden, statt zusätzliche Nachkommastellen mit zusätzlicher Genauigkeit gleichzusetzen.

## RKF-Toleranzen wählen

Kleinere Toleranzen benötigen in der Regel mehr Funktionsauswertungen. Extrem kleine Werte sind nicht automatisch besser: Diskretisierungsfehler, Rundungsfehler und die Kondition des Problems bleiben bestehen.

Ein sinnvoller Arbeitsablauf ist deshalb, zunächst mit praxisnahen Toleranzen zu rechnen, anschließend die Toleranzen zu verschärfen und die fachlich relevante Ergebnisgröße zu vergleichen. Ändert sich das Ergebnis noch deutlich, war die erste Rechnung numerisch noch nicht stabil genug.

## Verworfene RKF-Schritte verstehen

Verworfene Schritte sind bei einem adaptiven Verfahren zunächst normal. Einige Verwerfungen zeigen oft nur, dass die Schrittweitensteuerung eine geeignete Größe sucht. Sehr viele Verwerfungen können auf einen zu großen Anfangsschritt, stark wechselnde Dynamik oder für RKF45 teure Toleranzen hinweisen.

`MinimumStepSizeReached` ist dagegen ein echter Abbruchzustand: Die gewünschte lokale Toleranz ließ sich nicht einhalten, ohne die konfigurierte minimale Schrittweite zu unterschreiten. Eine kleinere Minimalweite kann helfen; möglicherweise ist für das Problem aber auch ein anderes numerisches Verfahren geeigneter.

## Derzeitige Grenzen

Die adaptive Implementierung behandelt skalare Differentialgleichungen erster Ordnung und integriert vorwärts. RK4 unterstützt bereits gekoppelte Systeme erster Ordnung. Die Adams-Implementierung ist ebenfalls skalar und arbeitet mit fester Schrittweite. Komfort-APIs für höhere Ordnung sowie lineare und nichtlineare Shooting-Verfahren sind noch V1-Arbeitspunkte und werden erst dokumentiert, wenn die APIs tatsächlich vorhanden sind.
