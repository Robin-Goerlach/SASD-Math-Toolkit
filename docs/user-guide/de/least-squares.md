# Least-Squares-Approximation

Least-Squares-Verfahren passen ein Modell an mehr Beobachtungen an, als sich normalerweise exakt treffen lassen. Das SASD Math Toolkit unterstützt Polynomanpassungen, beliebige lineare Basisfunktionen und die benannten Kurvenmodelle aus dem klassischen Kompatibilitätsfundament.

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

## Potenz-, Exponential- und logarithmische Modelle

`FitPowerLaw` passt `y = a*x^b` über eine logarithmische Transformation. `FitExponential` passt `y = a*exp(b*x)` ebenfalls über den logarithmischen y-Raum. `FitLogarithmic` passt `y = a + b*ln(x)` und transformiert nur x.

Die jeweiligen Domänenregeln bleiben wichtig: Potenzmodelle benötigen positive x- und y-Werte, Exponentialmodelle positive y-Werte und logarithmische Modelle positive x-Werte.

## Fünfgliedrige Fourier-Anpassung

Für periodische Daten mit bekannter Grundperiode oder Kreisfrequenz steht

`a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`

zur Verfügung. Die Frequenz wird vom Aufrufer vorgegeben. Dieses Verfahren ist keine FFT und kann auch ungleichmäßig verteilte Messpunkte verwenden.

## Beliebige lineare Basisfunktionen

Lässt sich ein Modell als `c0*f0(x) + c1*f1(x) + ...` formulieren, kann es direkt mit `FitBasis` angepasst werden. Jede Basisfunktion wird genau einmal pro Messpunkt ausgewertet; anschließend wird die Designmatrix mit Householder-QR gelöst.

## Warum QR jetzt der Standard ist

Die historische Kompatibilitätsimplementierung verwendete Normalgleichungen:

`(A^T A)c = A^T y`.

Durch die explizite Bildung von `A^T A` wird jedoch die Konditionszahl quadriert. Ein nur mäßig schwieriges Regressionsproblem kann dadurch numerisch deutlich problematischer werden.

Die modernisierte Implementierung faktorisiert die Designmatrix deshalb direkt als `A = Q*R` mit Householder-Reflexionen. Nach Anwendung von `Q^T` auf die Beobachtungen bleibt nur ein oberes Dreieckssystem. Das ist für allgemeines dichtes Least Squares ein wesentlich besserer Standard und bleibt trotzdem abhängigkeitfrei und nachvollziehbar.

`QrFactorization` steht zusätzlich öffentlich unter `Sasd.Numerics.LinearAlgebra` zur Verfügung, wenn eine Anwendung die Zerlegung selbst benötigt oder für mehrere rechte Seiten wiederverwenden möchte.

Rangdefiziente und unterbestimmte Regression wird bewusst nicht mit irgendeiner willkürlichen Lösung kaschiert. Solche Fälle melden derzeit einen Fehler und werden später von der geplanten SVD-Schicht mit Singulärwerten, robuster Rangdiagnostik und Pseudoinverser übernommen.

## Residuen und Transformationen verstehen

Potenzgesetz- und Exponentialanpassung transformieren y und minimieren deshalb Fehler im logarithmischen y-Raum. Ihre ausgegebenen RSS-/RMSE-Werte werden anschließend im ursprünglichen y-Raum berechnet.

Logarithmische und fünfgliedrige Fourier-Anpassung verändern y nicht. Bei ihnen entspricht `ResidualSumOfSquares` direkt der gewöhnlichen Least-Squares-Zielfunktion in den ursprünglichen y-Einheiten.

## Praktische Prüfung

Ein Fit sollte nicht nur anhand seiner Parameter beurteilt werden. Residuen sollten geprüft oder geplottet werden. Auch mit QR bleibt eine sinnvolle Skalierung wichtig, und ein numerisch erfolgreicher Fit beweist nicht, dass das gewählte Modell fachlich geeignet ist.

Der historische Modellumfang bleibt erhalten; die weitere Entwicklung konzentriert sich jetzt auf SVD, Rang-/Konditionsdiagnostik und die breiteren Statistik-Anforderungen der SASD Statistical Workbench statt auf historische Feature-Parität.
