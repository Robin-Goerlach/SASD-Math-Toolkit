# Least-Squares-Approximation

Least-Squares-Verfahren passen ein Modell an mehr Beobachtungen an, als sich normalerweise exakt treffen lassen. Das SASD Math Toolkit unterstützt Polynomanpassungen und beliebige Modelle, die als lineare Kombination aufruferspezifischer Basisfunktionen formuliert werden können. Im Rahmen der V1-Kompatibilität kommen schrittweise komfortable benannte Kurvenmodelle hinzu.

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

Der Solver logarithmiert beide Koordinaten und passt

`ln(y) = ln(a) + b*ln(x)`

an. Daher müssen sämtliche x- und y-Werte strikt größer als null sein.

## Exponentielle Anpassung

Der Exponential-Helfer passt

`y = a * exp(b*x)`

an. Das eignet sich für Verläufe, die näherungsweise exponentiell von x abhängen, zum Beispiel einfache Wachstums- oder Zerfallsmodelle. x darf jeder endliche reelle Wert sein; y muss positiv sein, weil intern die Gerade

`ln(y) = ln(a) + b*x`

angepasst wird.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(value => 2.5 * Math.Exp(-0.7 * value)).ToArray();

var fit = LeastSquares.FitExponential(x, y);

Console.WriteLine(fit.Scale); // ungefähr 2,5
Console.WriteLine(fit.Rate);  // ungefähr -0,7
Console.WriteLine(fit.Evaluate(1.5));
```

Ein positives `Rate` beschreibt Wachstum, ein negatives Zerfall und null ein konstantes positives Modell. Mindestens zwei unterschiedliche x-Werte sind notwendig, damit die Rate bestimmbar ist.

## Logarithmische Anpassung

Der logarithmische Helfer passt

`y = a + b * ln(x)`

an. Er eignet sich, wenn x positiv bleiben muss und sich die Antwort näherungsweise linear mit dem Logarithmus von x verändert. Anders als beim Potenzgesetz- und Exponential-Helfer wird y selbst **nicht** transformiert. y darf deshalb negativ, null oder positiv sein, solange der Wert endlich ist.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0];
double[] y = [2.0, 3.1, 4.0, 5.2];

var fit = LeastSquares.FitLogarithmic(x, y);

Console.WriteLine(fit.Intercept);
Console.WriteLine(fit.LogCoefficient);
Console.WriteLine(fit.Evaluate(3.0));
Console.WriteLine(fit.RootMeanSquareError);
```

`Intercept` entspricht dem angepassten Wert bei `x = 1`, weil `ln(1) = 0`. `LogCoefficient` beschreibt die Änderung des Modells pro Einheit von `ln(x)` und ist nicht die gewöhnliche Steigung bezüglich x.

## Residuen und Transformationen verstehen

Potenzgesetz- und Exponentialanpassung transformieren y vor dem Geraden-Fit. Sie minimieren daher quadrierte Residuen in einem logarithmischen y-Raum. Ihre Werte `ResidualSumOfSquares` und `RootMeanSquareError` werden anschließend trotzdem im ursprünglichen y-Raum berechnet, damit sie leichter interpretierbar sind.

Beim logarithmischen Helfer ist es anders: Nur x wird transformiert. Die y-Werte bleiben unverändert, sodass seine ausgegebene Residuenquadratsumme im ursprünglichen y-Raum zugleich die von Least Squares minimierte Zielfunktion ist.

Diese Unterscheidung ist bei der Modellauswahl wichtig. Eine nach Transformation von y optimale Kurve ist nicht zwingend diejenige, die additive Fehler in den ursprünglichen Einheiten minimiert.

## Beliebige lineare Basisfunktionen

Lässt sich ein Modell als

`c0*f0(x) + c1*f1(x) + ...`

formulieren, kann es direkt mit `FitBasis` angepasst werden. Dieser allgemeine Mechanismus bildet auch die gemeinsame numerische Grundlage für mehrere benannte historische Modellhelfer.

## Praktische Prüfung

Ein Fit sollte nicht nur anhand seiner Parameter beurteilt werden. Residuen sollten geplottet oder zumindest geprüft werden; systematische Strukturen sprechen häufig für ein unpassendes Modell. Bei transformierten Modellen sollte zusätzlich geprüft werden, ob Transformation und implizite Fehlerstruktur fachlich sinnvoll sind.

Die aktuelle Referenzimplementierung verwendet Normalgleichungen. Für die V1-Kompatibilität und moderate, vernünftig skalierte Probleme ist das ausreichend. Für schwierigere Regressionsaufgaben sollen später QR-/SVD-Backends ergänzt werden.

## Fortschritt der V1-Modelle

Potenzgesetz-, Exponential- und logarithmische Helfer sind jetzt implementiert. Der dedizierte fünfgliedrige Fourier-Helfer fehlt noch. Das Verhalten eines fünfgliedrigen Polynoms ist bereits mit `FitPolynomial(..., degree: 4)` verfügbar; ein zusätzlicher Komfortname ist daher optional und keine numerische Voraussetzung.
