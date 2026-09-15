# Least-Squares-Approximation

Least-Squares-Verfahren passen ein Modell an mehr Beobachtungen an, als sich normalerweise exakt treffen lassen. Das SASD Math Toolkit unterstützt Polynomanpassungen und beliebige Modelle, die als lineare Kombination aufruferspezifischer Basisfunktionen formuliert werden können. Im Rahmen der V1-Kompatibilität kommen schrittweise komfortable benannte Kurvenmodelle hinzu.

## Polynomanpassung

Für ein Polynom `y = c0 + c1*x + ... + cn*x^n` wird `FitPolynomial` mit dem gewünschten Grad verwendet.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [-2, -1, 0, 1, 2];
double[] y = [17, 6, 1, 2, 9];

var coefficients = LeastSquares.FitPolynomial(x, y, degree: 2);
var predicted = LeastSquares.EvaluatePolynomial(coefficients, 1.5);
```

Die Koeffizienten werden nach steigender Potenz zurückgegeben.

## Anpassung eines Potenzgesetzes

`FitPowerLaw` passt `y = a * x^b`. Beide Variablen müssen positiv sein, weil intern `ln(y) = ln(a) + b*ln(x)` angepasst wird.

## Exponentielle Anpassung

`FitExponential` passt `y = a * exp(b*x)`. x darf jeder endliche reelle Wert sein, y muss wegen der Transformation `ln(y) = ln(a) + b*x` positiv bleiben.

## Logarithmische Anpassung

`FitLogarithmic` passt `y = a + b * ln(x)`. x muss positiv sein; y darf negativ, null oder positiv sein, solange es endlich ist.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0];
double[] y = [2.0, 3.1, 4.0, 5.2];

var fit = LeastSquares.FitLogarithmic(x, y);

Console.WriteLine(fit.Intercept);
Console.WriteLine(fit.LogCoefficient);
Console.WriteLine(fit.Evaluate(3.0));
```

## Fünfgliedrige Fourier-Anpassung

Für periodische Daten mit bekannter Grundperiode oder Kreisfrequenz steht das Modell

`a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`

zur Verfügung.

```csharp
using Sasd.Numerics.Approximation;

const double period = 4.0;
var omega = 2.0 * Math.PI / period;
var x = Enumerable.Range(0, 20).Select(i => i * 0.2).ToArray();
var y = x.Select(value =>
    1.5
    + 2.0 * Math.Cos(omega * value)
    - 0.5 * Math.Sin(omega * value)
    + 0.75 * Math.Cos(2.0 * omega * value)
    + 1.25 * Math.Sin(2.0 * omega * value)).ToArray();

var fit = LeastSquares.FitFiveTermFourierForPeriod(x, y, period);

Console.WriteLine(fit.ConstantTerm);
Console.WriteLine(fit.FundamentalCosineCoefficient);
Console.WriteLine(fit.SecondHarmonicSineCoefficient);
Console.WriteLine(fit.Evaluate(1.25));
```

Die Frequenz wird von diesem Verfahren nicht mitgeschätzt. Entweder wird `w` direkt an `FitFiveTermFourier` übergeben oder die Periode an `FitFiveTermFourierForPeriod`. Mindestens fünf Messwerte sind notwendig; ihre Phasen müssen außerdem genug unabhängige Information enthalten, um alle fünf Koeffizienten bestimmen zu können.

Die Fourier-Anpassung ist keine FFT. Sie bestimmt ein kleines periodisches Modell aus Messwerten, die auch ungleichmäßig verteilt sein dürfen. Eine FFT analysiert dagegen Frequenzbins einer regelmäßig abgetasteten Folge.

## Residuen und Transformationen verstehen

Potenzgesetz- und Exponentialanpassung transformieren y und minimieren deshalb Fehler im logarithmischen y-Raum. Ihre ausgegebenen RSS-/RMSE-Werte werden anschließend im ursprünglichen y-Raum berechnet.

Logarithmische und fünfgliedrige Fourier-Anpassung verändern y nicht. Bei ihnen entspricht `ResidualSumOfSquares` deshalb unmittelbar der gewöhnlichen Least-Squares-Zielfunktion in den ursprünglichen y-Einheiten.

## Beliebige lineare Basisfunktionen

Lässt sich ein Modell als `c0*f0(x) + c1*f1(x) + ...` formulieren, kann es direkt mit `FitBasis` angepasst werden. Dieser allgemeine Mechanismus bildet auch die gemeinsame numerische Grundlage für Polynome, Fouriermodelle und mehrere benannte transformierte Modelle.

## Praktische Prüfung

Ein Fit sollte nicht nur anhand seiner Parameter beurteilt werden. Residuen sollten geplottet oder zumindest geprüft werden. Bei periodischen Modellen sollte zusätzlich fachlich begründet werden, warum die gewählte Grundperiode sinnvoll ist; auch eine falsche Periode kann numerische Koeffizienten liefern, die das reale Verhalten aber schlecht beschreiben.

Die aktuelle Referenzimplementierung verwendet Normalgleichungen. Für die V1-Kompatibilität und moderate, vernünftig skalierte Probleme ist das ausreichend. Für schwierigere Regressionsaufgaben sollen später QR-/SVD-Backends ergänzt werden.

## Fortschritt der V1-Modelle

Der historische V1-Least-Squares-Modellbereich ist jetzt abgedeckt: allgemeine lineare Basis, Polynom-, Potenz-, Exponential-, logarithmische und fünfgliedrige Fourier-Anpassung besitzen aufrufbare APIs. Weitere Arbeiten können sich damit auf numerische Robustheit und die breiteren Statistik-Anforderungen der SASD-Produktfamilie konzentrieren.
