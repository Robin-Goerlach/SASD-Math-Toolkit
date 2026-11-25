# Numerische Integration

Numerische Integration nähert ein bestimmtes Integral an, wenn eine Stammfunktion nicht verfügbar, unpraktisch oder nur als ausführbarer Code gegeben ist. Das SASD Math Toolkit bietet feste Newton-Cotes-Verfahren, Romberg-Extrapolation, die Fünf-Punkt-Gauß-Legendre-Quadratur sowie adaptive Varianten.

```csharp
using Sasd.Numerics.Integration;
```

Alle aktuellen eindimensionalen Verfahren verlangen endliche Grenzen mit `a < b`. Wird das Integral in Gegenrichtung benötigt, sollten die Grenzen bewusst vertauscht und das Ergebnis negiert werden.

## Welches Verfahren wähle ich?

Die **zusammengesetzte Trapezregel** eignet sich als einfacher Referenzansatz oder wenn ein stückweise lineares Verhalten naheliegt. **Composite Simpson** ist für glatte Funktionen attraktiv, wenn eine feste gerade Teilintervallzahl in Ordnung ist. **Adaptive Simpson** ist sinnvoll, wenn sich die Schwierigkeit innerhalb des Integrationsintervalls stark ändert und die Verfeinerung dort stattfinden soll, wo sie benötigt wird.

**Romberg** eignet sich für glatte Funktionen, bei denen wiederholte Trapezverfeinerung und Richardson-Extrapolation gut wirken. **Fünf-Punkt-Gauß-Legendre** ist ein hochgradiges Verfahren für ein einzelnes Intervall; in exakter Arithmetik werden Polynome bis Grad neun exakt integriert. Die **adaptive Gauß-Legendre-Variante** ist sinnvoll, wenn ein einzelnes Gauß-Panel für das gesamte Intervall nicht genügt.

## Zusammengesetzte Trapez- und Simpson-Regel

```csharp
var trapezoid = NumericalIntegration.CompositeTrapezoid(
    Math.Sin,
    0.0,
    Math.PI,
    intervals: 200);

var simpson = NumericalIntegration.CompositeSimpson(
    Math.Sin,
    0.0,
    Math.PI,
    intervals: 200);
```

Die Teilintervallzahl gehört zum numerischen Modell. Mehr Panels reduzieren bei hinreichend glatten Funktionen meist den Diskretisierungsfehler, erhöhen aber die Zahl der Funktionsauswertungen und stoßen irgendwann an Gleitkommagrenzen. Für Composite Simpson muss `intervals` gerade sein.

Als Plausibilitätsprüfung sind die Exaktheitseigenschaften hilfreich: Die Trapezregel ist für lineare Funktionen exakt, Simpson in exakter Arithmetik für kubische Polynome.

## Adaptive Simpson mit Diagnosewerten

Die einfache API liefert nur den besten Schätzwert:

```csharp
var integral = NumericalIntegration.AdaptiveSimpson(
    Math.Sin,
    0.0,
    Math.PI,
    tolerance: 1e-10,
    maximumDepth: 20);
```

Wenn eine Anwendung unterscheiden muss, ob die Toleranz wirklich erfüllt oder nur eine Grenze erreicht wurde, sollte die detaillierte API verwendet werden:

```csharp
var result = NumericalIntegration.AdaptiveSimpsonDetailed(
    Math.Sin,
    0.0,
    Math.PI,
    tolerance: 1e-10,
    maximumDepth: 20);

if (!result.Converged)
{
    Console.WriteLine($"Abbruchstatus: {result.Status}");
}

Console.WriteLine(result.Value);
Console.WriteLine(result.EstimatedError);
Console.WriteLine(result.FunctionEvaluations);
```

`AdaptiveIntegrationStatus.Converged` bedeutet, dass jedes akzeptierte Panel sein lokales Kriterium erfüllt hat. `MaximumDepthReached` bedeutet, dass mindestens ein Bereich noch weiter verfeinert werden sollte, als die konfigurierte Rekursionstiefe zulässt. `NumericalResolutionReached` bedeutet, dass sich ein Intervall mit `double` nicht mehr in einen unterschiedlichen Mittelpunkt teilen ließ. In beiden nicht konvergierten Fällen bleibt `Value` der beste verfügbare Schätzwert, darf aber nicht so dargestellt werden, als sei die gewünschte Toleranz garantiert.

Der Simpson-Fehlerindikator basiert auf der Differenz zwischen einem Simpson-Panel und seinen beiden Halbintervallen samt Richardson-Korrektur. Er ist eine Schätzung und kein mathematischer Beweis für den tatsächlichen Fehler.

## Romberg-Integration

Romberg baut aus sukzessiv halbierten Trapezregeln eine Extrapolationstabelle auf:

```csharp
var result = NumericalIntegration.RombergDetailed(
    x => Math.Exp(-x * x),
    0.0,
    1.0,
    tolerance: 1e-10,
    maximumLevels: 12);
```

`result.Value` enthält die beste diagonale Extrapolation. `EstimatedError` ist der Absolutbetrag der Differenz der letzten beiden Diagonaleinträge. `Status` ist `IterationStatus.Converged`, wenn diese Differenz die gewünschte Toleranz erfüllt; sonst wird der letzte verfügbare Wert mit `MaximumIterationsReached` zurückgegeben.

Der Aufwand verdoppelt sich ungefähr mit jeder zusätzlichen Trapezstufe. Die Referenzimplementierung begrenzt `maximumLevels` deshalb auf 30, damit das ganzzahlige Unterteilungsmodell definiert bleibt. Werte in der Nähe dieser Obergrenze können trotzdem sehr teuer sein; normale Anwendungen sollten deutlich weniger Stufen verwenden.

## Fünf-Punkt-Gauß-Legendre

Die feste Fünf-Knoten-Regel kann direkt verwendet werden:

```csharp
var integral = NumericalIntegration.GaussLegendre5(
    x => Math.Exp(x),
    0.0,
    1.0);
```

Knoten und Gewichte werden vom Standardintervall `[-1, 1]` auf `[a, b]` abgebildet. Bei glatten Funktionen kann Gauß-Quadratur mit vergleichsweise wenigen Funktionsauswertungen eine hohe Genauigkeit erreichen.

Die adaptive Variante vergleicht eine Fünf-Punkt-Regel über das gesamte Panel mit der Summe zweier Fünf-Punkt-Regeln über die beiden Hälften:

```csharp
var result = NumericalIntegration.AdaptiveGaussLegendre5Detailed(
    Math.Exp,
    0.0,
    1.0,
    tolerance: 1e-12,
    maximumDepth: 16);
```

`EstimatedError` ist hier die aufsummierte Verfeinerungsdifferenz, die der Algorithmus als Kriterium verwendet. Auch sie ist ein Fehlerindikator und keine formale Schranke.

## Nicht-endliche Werte und singuläre Integranden

Alle Integrationsverfahren weisen `NaN` und Unendlich zurück, wenn der Integrand solche Werte liefert. Das ist beabsichtigt: Ein nicht-endlicher Einzelwert soll nicht still durch eine lange Quadratur fortgepflanzt werden.

Uneigentliche Integrale oder Funktionen mit Rand-Singularitäten benötigen daher eine explizite Transformation oder Intervallbehandlung durch die Anwendung. Die aktuellen V1-Routinen sind Verfahren für endliche Intervalle; unendliche Grenzen oder Hauptwerte werden nicht automatisch interpretiert.

## Genauigkeit ist mehr als eine Toleranzzahl

Eine angegebene Toleranz steuert nur das interne Verfeinerungskriterium. Sie kann keine schlecht skalierte Funktion, Sprungstellen, nicht aufgelöste schmale Spitzen, Auslöschung oder Präzisionsverlust im Benutzer-Callback reparieren. Bei wichtigen Ergebnissen sollte man Methode oder numerische Parameter variieren und prüfen, ob das Ergebnis stabil bleibt.

Die detaillierten adaptiven und Romberg-Ergebnisobjekte existieren deshalb bewusst: Anwendungen sollen nachvollziehen können, wie ein Wert zustande kam, statt nur einen nackten `double` zu speichern.
