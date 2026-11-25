# Least-Squares-Anpassung eines Potenzgesetzes

## Zweck

`LeastSquares.FitPowerLaw` passt das zweiparametrige Modell

`y = a * x^b`

an Mess- oder Beobachtungsdaten an. Es ist der erste gezielte historische Least-Squares-Modellhelfer auf Basis der bereits vorhandenen allgemeinen Polynom- und Basisfunktionsanpassung des Toolkits.

Die C#-Implementierung ist eigenständig erstellt. Das historische Borland-Toolbox-Produkt dient nur zur Bestimmung des funktionalen V1-Ziels; historischer Quelltext und Handbuchformulierungen werden nicht übernommen.

## Mathematische Transformation

Ein Potenzgesetz ist im Exponenten `b` nichtlinear. Durch den natürlichen Logarithmus wird es jedoch linear:

`ln(y) = ln(a) + b * ln(x)`.

Die Implementierung transformiert deshalb alle Datenpunkte nach `(ln(x), ln(y))` und verwendet anschließend `LeastSquares.FitBasis` mit den Basisfunktionen `1` und `ln(x)`. Ist der angepasste Achsenabschnitt `c`, wird der Skalierungsfaktor rekonstruiert als

`a = exp(c)`.

Damit bleibt der Code klein und nachvollziehbar und verwendet den allgemeinen Least-Squares-Kern, statt einen zweiten unabhängigen Regressionsalgorithmus einzuführen.

## Definitionsbereich

Wegen der logarithmischen Transformation müssen sämtliche x- und y-Werte endlich und strikt größer als null sein. Außerdem werden mindestens zwei Messpunkte und mindestens zwei unterschiedliche x-Werte benötigt; andernfalls lässt sich der Exponent nicht bestimmen.

Diese Bedingungen entstehen aus dem mathematischen Modell und sind keine willkürlichen API-Einschränkungen. Daten mit null oder negativen Werten benötigen ein anderes Modell oder ein anderes Anpassungsverfahren.

## Diagnosewerte

`PowerLawFitResult` stellt bereit:

- `Scale` — den angepassten Faktor `a`;
- `Exponent` — den angepassten Exponenten `b`;
- `SampleCount`;
- `ResidualSumOfSquares`;
- `RootMeanSquareError`;
- `Evaluate(x)` für positive x-Werte.

Die Regression selbst minimiert die quadrierten Residuen im **Logarithmusraum**, weil das Modell dort linear ist. Residuenquadratsumme und RMSE werden anschließend im ursprünglichen y-Raum berechnet, damit sie in den Einheiten der Eingangsdaten interpretierbar sind. Diese beiden Sachverhalte dürfen nicht verwechselt werden: Die Diagnosewerte beschreiben die resultierende Kurve, sind aber nicht die Zielfunktion der transformierten Regression.

## Beispiel

```csharp
using Sasd.Numerics.Approximation;

double[] x = [1.0, 2.0, 4.0, 8.0];
double[] y = [3.0, 16.970562748, 96.0, 543.058007951];

var fit = LeastSquares.FitPowerLaw(x, y);

Console.WriteLine(fit.Scale);
Console.WriteLine(fit.Exponent);
Console.WriteLine(fit.Evaluate(3.0));
Console.WriteLine(fit.RootMeanSquareError);
```

## Numerische Hinweise

Der allgemeine Least-Squares-Kern verwendet derzeit Normalgleichungen und Gauß-Elimination mit partieller Pivotisierung. Das ist bewusst eine verständliche, abhängigkeitfreie Referenzimplementierung und nicht die endgültige Lösung für stark schlecht konditionierte oder sehr große Regressionsprobleme. Später kann ein QR- oder SVD-Backend ergänzt werden, ohne die konzeptionelle Potenzgesetz-API zu verändern.

Auch die logarithmische Transformation verändert das statistische Fehlermodell. Wenn additive Fehler im ursprünglichen y-Raum entscheidend sind, kann ein nichtlineares Least-Squares-Verfahren, das dort die Residuen minimiert, passender sein. Das liegt außerhalb dieses historischen V1-Helfers.
