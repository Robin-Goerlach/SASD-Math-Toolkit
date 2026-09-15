# Least-Squares-Approximation

Least-Squares-Verfahren passen ein Modell an mehr Beobachtungen an, als sich normalerweise exakt treffen lassen. Das SASD Math Toolkit unterstützt bereits Polynomfits und beliebige Modelle, die als lineare Kombination aufruferspezifischer Basisfunktionen formuliert werden können. Im Rahmen der V1-Kompatibilität kommen nun schrittweise komfortable benannte Kurvenmodelle hinzu.

## Polynomanpassung

Für ein Polynom

`y = c0 + c1*x + ... + cn*x^n`

wird `FitPolynomial` mit dem gewünschten Grad verwendet.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [-2, -1, 0, 1, 2];
double[] y = [17, 6, 1, 2, 9];

var coefficients = LeastSquares.FitPolynomial(x, y, degree: 2);
var predicted = LeastSquares.EvaluatePolynomial(coefficients, 1.5);
```

Die Koeffizienten werden nach steigender Potenz zurückgegeben: `c0`, `c1`, `c2` und so weiter.

## Anpassung eines Potenzgesetzes

Ein Potenzgesetz besitzt die Form

`y = a * x^b`.

`FitPowerLaw` passt, wenn beide Variablen positiv sind und ein multiplikativer Skalierungszusammenhang plausibel ist.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0, 16.0];
var y = x.Select(value => 3.0 * Math.Pow(value, 2.5)).ToArray();

var fit = LeastSquares.FitPowerLaw(x, y);

Console.WriteLine(fit.Scale);                // ungefähr 3
Console.WriteLine(fit.Exponent);             // ungefähr 2,5
Console.WriteLine(fit.Evaluate(3.0));
Console.WriteLine(fit.RootMeanSquareError);
```

Der Solver logarithmiert die Daten und passt

`ln(y) = ln(a) + b*ln(x)`

an. Daher müssen sämtliche x- und y-Werte strikt größer als null sein. Gleichzeitig bedeutet dies, dass die Least-Squares-Zielfunktion im Logarithmusraum und nicht direkt in den ursprünglichen y-Werten minimiert wird.

`ResidualSumOfSquares` und `RootMeanSquareError` werden trotzdem im ursprünglichen y-Raum ausgegeben, damit ihre Einheiten unmittelbar verständlich bleiben. Sie sind nützliche Diagnosewerte, ändern aber nicht die beim Fit minimierte Zielfunktion.

## Beliebige lineare Basisfunktionen

Lässt sich ein Modell als

`c0*f0(x) + c1*f1(x) + ...`

formulieren, kann es direkt mit `FitBasis` angepasst werden. Dieser allgemeine Mechanismus bildet auch die gemeinsame Grundlage für mehrere benannte historische Modellhelfer.

## Praktische Prüfung

Ein Fit sollte nicht nur anhand seiner Parameter beurteilt werden. Residuen sollten geplottet oder zumindest geprüft werden; systematische Strukturen sprechen häufig für ein unpassendes Modell. Bei transformierten Modellen wie dem Potenzgesetz ist außerdem wichtig, dass ein guter Fit im Logarithmusraum nicht automatisch dem besten Fit für additive Fehler in den ursprünglichen Einheiten entspricht.

Die aktuelle Referenzimplementierung verwendet Normalgleichungen. Für die V1-Kompatibilität und moderate, vernünftig skalierte Probleme ist das ausreichend. Für schwierigere Regressionsaufgaben sollen später QR-/SVD-Backends ergänzt werden.

## Fortschritt der V1-Modelle

Der Potenzgesetz-Helfer ist jetzt implementiert. Eigene Helfer für Exponential-, Logarithmus- und fünfgliedrige Fouriermodelle folgen noch. Das Verhalten eines fünfgliedrigen Polynoms ist bereits mit `FitPolynomial(..., degree: 4)` verfügbar; ein zusätzlicher Komfortname ist daher optional und keine numerische Voraussetzung.
