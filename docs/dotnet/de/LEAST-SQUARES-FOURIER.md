# Fünfgliedrige Fourier-Least-Squares-Approximation

## Zweck

`LeastSquares.FitFiveTermFourier` passt das periodische Modell mit fünf Koeffizienten

`y = a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`

an. Damit wird der historische Punkt „fünfgliedrige Fourier-Least-Squares-Anpassung“ der V1-Kompatibilitätsliste geschlossen. Die Implementierung ist eigenständig in C# geschrieben; das historische Borland-Produkt dient nur als funktionale Zielreferenz und nicht als Quelle für Code oder Handbuchtext.

## Bedeutung von „fünfgliedrig“

Die fünf linearen Basisfunktionen sind:

1. `1`
2. `cos(w*x)`
3. `sin(w*x)`
4. `cos(2*w*x)`
5. `sin(2*w*x)`

Die Kreisfrequenz `w` ist vor dem Fit bekannt. Bestimmt werden ausschließlich die fünf Amplituden. Eine gleichzeitig unbekannte Frequenz würde eine nichtlineare Optimierungsaufgabe erzeugen und gehört bewusst nicht zu diesem V1-Helfer.

Mit dem Standardwert `w = 1` erhält man die klassische Bogenmaß-Basis `1, cos(x), sin(x), cos(2x), sin(2x)`. Ist eine Periode `T` natürlicher, kann `FitFiveTermFourierForPeriod` verwendet werden. Der Helfer berechnet `w = 2*pi/T` und verwendet anschließend denselben Fitpfad.

## Architektur

Bei festem `w` ist das Modell bezüglich aller fünf unbekannten Koeffizienten linear. Die Implementierung erzeugt deshalb lediglich die fünf Fourier-Basisfunktionen und delegiert die eigentliche Koeffizientenbestimmung an `LeastSquares.FitBasis`.

Das ist eine bewusste Architekturentscheidung: Es gibt keinen zweiten Fourier-spezifischen Least-Squares-Solver. Validierung und Modellaufbau gehören zum Fourier-Helfer; die lineare Regression bleibt im gemeinsamen Least-Squares-Kern. Auch die Residuenkennzahlen verwenden den bereits gemeinsamen Diagnosepfad im ursprünglichen y-Raum.

Die aktuelle V1-Implementierung von `FitBasis` verwendet Normalgleichungen mit Gauß-Elimination und partieller Pivotisierung. Das hält die Referenzimplementierung klein und nachvollziehbar. QR oder SVD bleiben spätere Robustheits- beziehungsweise Performance-Erweiterungen.

## Ergebnismodell

`FiveTermFourierFitResult` stellt bereit:

- `ConstantTerm` (`a0`)
- `FundamentalCosineCoefficient` (`a1`)
- `FundamentalSineCoefficient` (`b1`)
- `SecondHarmonicCosineCoefficient` (`a2`)
- `SecondHarmonicSineCoefficient` (`b2`)
- `FundamentalAngularFrequency`
- `Period`
- `SampleCount`
- `ResidualSumOfSquares`
- `RootMeanSquareError`
- `Evaluate(x)`

Die tatsächlich verwendete Frequenz ist Bestandteil des Ergebnisses. Dadurch kann die spätere Auswertung nicht versehentlich mit einer anderen Periode erfolgen.

## Beispiel

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
Console.WriteLine(fit.Period);
Console.WriteLine(fit.Evaluate(1.25));
```

## Validierung und Rang

Mindestens fünf Beobachtungen sind notwendig, weil fünf Koeffizienten unbekannt sind. Fünf Punkte sind jedoch nicht automatisch ausreichend: Die Abtastphasen müssen fünf linear unabhängige Basisspalten erzeugen. Werden beispielsweise immer wieder dieselben Phasen gemessen, lassen sich die harmonischen Koeffizienten nicht bestimmen; der gemeinsame Solver meldet dann ein singuläres oder numerisch singuläres System.

Alle Stichprobenwerte und die Kreisfrequenz müssen endlich sein. Die Frequenz muss positiv sein und eine endliche zweite Harmonische sowie eine endliche Periode erlauben. Kombinationen aus extrem großen x-Werten und Frequenzen, die die Phasenberechnung überlaufen lassen, werden ausdrücklich abgewiesen.

## Abgrenzung zur FFT

Dieser Helfer ist **keine FFT**. Eine Fourier-Least-Squares-Anpassung darf beliebige Abtaststellen verwenden, erhält eine vorgegebene Grundfrequenz und bestimmt Modellkoeffizienten. Eine FFT transformiert dagegen eine regelmäßig abgetastete Folge in diskrete Frequenzbins. Beide Verfahren verwenden Sinus- und Kosinusfunktionen, beantworten aber unterschiedliche Fragen und bleiben deshalb getrennte APIs.
