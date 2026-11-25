# Exponentielle Least-Squares-Approximation

## Zweck

`LeastSquares.FitExponential` passt das Zweiparametermodell

`y = a * exp(b*x)`

an Beobachtungen mit positiven y-Werten an. Ein positives `b` beschreibt exponentielles Wachstum, ein negatives `b` exponentiellen Zerfall und `b = 0` ein konstantes positives Modell.

Die C#-Implementierung wurde eigenständig erstellt. Die historische Borland-Toolbox dient ausschließlich als funktionales V1-Ziel; historischer Quelltext und Handbuchtext werden nicht übernommen.

## Mathematische Transformation

Durch Logarithmieren entsteht

`ln(y) = ln(a) + b*x`.

Das transformierte Problem ist damit eine lineare Ausgleichsgerade. Die Implementierung verwendet bewusst denselben internen Geradenadapter und denselben öffentlichen `LeastSquares.FitBasis`-Kern wie der Potenzgesetz-Helfer. Es entsteht also kein zweiter unabhängiger Regressionssolver.

Ist `c` der Achsenabschnitt im transformierten Raum, wird der ursprüngliche Skalierungsfaktor durch

`a = exp(c)`

zurückgewonnen.

## Definitionsbereich

Alle x-Werte müssen endlich sein, dürfen aber negativ, null oder positiv sein. Alle y-Beobachtungen müssen endlich und strikt größer als null sein, weil `ln(y)` benötigt wird.

Mindestens zwei Stichproben und mindestens zwei unterschiedliche x-Werte sind erforderlich. Mehrere y-Werte am exakt gleichen x-Wert reichen nicht aus, um eine Exponentialrate zu bestimmen.

## Ergebnismodell

`ExponentialFitResult` stellt bereit:

- `Scale` — angepasstes `a`;
- `Rate` — angepasstes `b`;
- `SampleCount`;
- `ResidualSumOfSquares`;
- `RootMeanSquareError`;
- `Evaluate(x)` für jeden endlichen x-Wert.

Das Ergebnisobjekt ist unveränderlich und prüft seine numerischen Diagnosewerte bei der Erzeugung.

## Fehlerinterpretation

Die Regression minimiert die quadrierten Residuen im **Log-y-Raum**, weil erst die logarithmische Transformation das Modell linear macht. RSS und RMSE werden danach zusätzlich im ursprünglichen y-Raum berechnet, damit ihre Einheiten für Anwendungsprogramme unmittelbar verständlich bleiben.

Diese Diagnosewerte beschreiben den resultierenden Fit, sind aber nicht die minimierte Zielfunktion. Sind additive Fehler in den ursprünglichen y-Einheiten das eigentliche statistische Modell, kann eine nichtlineare Least-Squares-Methode geeigneter sein als dieser historische transformierte Fit.

## Beispiel

```csharp
using Sasd.Numerics.Approximation;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(value => 2.5 * Math.Exp(-0.7 * value)).ToArray();

var fit = LeastSquares.FitExponential(x, y);

Console.WriteLine(fit.Scale);               // ungefähr 2,5
Console.WriteLine(fit.Rate);                // ungefähr -0,7
Console.WriteLine(fit.Evaluate(1.5));
Console.WriteLine(fit.RootMeanSquareError);
```

## Architektur und numerische Hinweise

Die benannten transformierten Modelle liegen in `LeastSquares.TransformedModels.cs`, während der Kern für Polynome und beliebige lineare Basisfunktionen in `LeastSquares.cs` bleibt. So bleibt die öffentliche API unter einem gemeinsamen `LeastSquares`-Typ, ohne dass eine übergroße Implementierungsdatei entsteht.

Der allgemeine Kern verwendet derzeit Normalgleichungen und Gauß-Elimination mit partieller Pivotisierung. Das bleibt für V1 bewusst lesbar und frei von zusätzlichen Abhängigkeiten. Spätere QR-/SVD-Backends können die Konditionierung verbessern, ohne die benannte Exponential-API zu verändern.

Das nächste benannte historische Least-Squares-Modell ist die logarithmische Form. Sie kann denselben Geradenadapter wiederverwenden, transformiert dabei aber die x-Koordinate statt y.
