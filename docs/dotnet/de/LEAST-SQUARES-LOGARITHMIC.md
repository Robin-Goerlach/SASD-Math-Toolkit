# Logarithmische Least-Squares-Approximation

## Zweck

`LeastSquares.FitLogarithmic` passt das zweiparametrige Modell

`y = a + b * ln(x)`

an Beobachtungen mit strikt positiven x-Werten an. Damit wird der benannte logarithmische Least-Squares-Punkt des historischen V1-Kompatibilitätskatalogs geschlossen, während die öffentliche API modern und C#-typisch bleibt.

Die Implementierung ist eigenständig geschrieben. Das historische Borland-Produkt dient nur als funktionale Referenz für die V1-Abdeckung; historischer Quelltext und Handbuchtext werden nicht übernommen.

## Mathematische Form

Mit der transformierten erklärenden Variablen

`u = ln(x)`

wird das Modell zur gewöhnlichen Geraden

`y = a + b*u`.

Die Implementierung validiert und transformiert deshalb x und delegiert die eigentliche Regression anschließend an denselben gemeinsamen Geraden-Fit, den auch die anderen benannten transformierten Modelle verwenden. Dieser Pfad basiert wiederum auf dem allgemeinen `LeastSquares.FitBasis`-Kern.

Diese Trennung ist beabsichtigt: Modellspezifischer Code beschreibt Transformation und Wertebereich; der numerische Least-Squares-Kern bleibt zentralisiert und separat testbar.

## Wichtiger Unterschied zu Potenz- und Exponential-Fits

Beim Potenzmodell werden x und y logarithmiert, beim Exponentialmodell y. In beiden Fällen liegt die angepasste Zielfunktion daher in transformierten y-Koordinaten.

Das logarithmische Modell transformiert **nur x**. Die y-Beobachtungen bleiben unverändert. Die gewöhnliche Least-Squares-Zielfunktion ist deshalb bereits die Summe der quadrierten Residuen in den ursprünglichen y-Einheiten.

`ResidualSumOfSquares` und `RootMeanSquareError` beschreiben somit genau denselben Residuenraum, den der logarithmische Fit minimiert.

## Wertebereich

- jeder x-Wert muss endlich und strikt größer als null sein, damit `ln(x)` reell definiert ist;
- y darf negativ, null oder positiv sein, muss aber endlich sein;
- mindestens zwei Beobachtungen sind erforderlich;
- mindestens zwei verschiedene x-Werte werden benötigt, damit der logarithmische Koeffizient bestimmbar ist.

## Ergebnismodell

`LogarithmicFitResult` stellt bereit:

- `Intercept` — angepasstes `a`;
- `LogCoefficient` — angepasstes `b` vor `ln(x)`;
- `SampleCount`;
- `ResidualSumOfSquares`;
- `RootMeanSquareError`;
- `Evaluate(x)` für endliche positive x-Werte.

Der ausdrückliche Name `LogCoefficient` vermeidet ein missverständliches allgemeines `Slope`: Die Steigung bezieht sich auf `ln(x)` und nicht direkt auf x.

## Beispiel

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

## Numerische Hinweise

Der gemeinsame lineare V1-Least-Squares-Kern bildet derzeit Normalgleichungen und löst sie mit Gauß-Elimination und partieller Pivotisierung. Das ist bewusst verständlich und abhängigkeitsfrei. Für moderate, vernünftig skalierte Probleme eignet sich diese Referenzimplementierung gut; bei schwierigen oder schlecht konditionierten Regressionsproblemen sollen später QR- oder SVD-Verfahren ergänzt werden.

Eine logarithmische Kurve ist eine Modellannahme und nicht nur ein numerischer Kunstgriff. Residuenplots und Fachwissen sollten weiterhin genutzt werden, um zu beurteilen, ob `a + b ln(x)` für die Daten plausibel ist.
