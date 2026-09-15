# Least-Squares-Approximation

Least-Squares-Verfahren passen ein Modell an Beobachtungen an, die sich normalerweise nicht alle exakt treffen lassen. Das SASD Math Toolkit unterstützt Polynomanpassungen, beliebige lineare Basisfunktionen und die benannten Kurvenmodelle aus dem klassischen Kompatibilitätsfundament.

## Polynomanpassung

Für ein Polynom `y = c0 + c1*x + ... + cn*x^n` wird `FitPolynomial` mit dem gewünschten Grad verwendet.

```csharp
using Sasd.Numerics.Approximation;

double[] x = [-2, -1, 0, 1, 2];
double[] y = [17, 6, 1, 2, 9];

var coefficients = LeastSquares.FitPolynomial(x, y, degree: 2);
var predicted = LeastSquares.EvaluatePolynomial(coefficients, 1.5);
```

Die Koeffizienten werden nach steigender Potenz zurückgegeben. Die Polynom-Komfort-API verlangt weiterhin mindestens `degree + 1` Messwerte; sie wählt bei einem unterbestimmten Polynom nicht stillschweigend irgendeinen Koeffizientenvektor aus.

## Potenz-, Exponential- und logarithmische Modelle

`FitPowerLaw` passt `y = a*x^b` über eine logarithmische Transformation. `FitExponential` passt `y = a*exp(b*x)` ebenfalls über den logarithmischen y-Raum. `FitLogarithmic` passt `y = a + b*ln(x)` und transformiert nur x.

Die jeweiligen Domänenregeln bleiben wichtig: Potenzmodelle benötigen positive x- und y-Werte, Exponentialmodelle positive y-Werte und logarithmische Modelle positive x-Werte.

## Fünfgliedrige Fourier-Anpassung

Für periodische Daten mit bekannter Grundperiode oder Kreisfrequenz steht

`a0 + a1*cos(w*x) + b1*sin(w*x) + a2*cos(2*w*x) + b2*sin(2*w*x)`

zur Verfügung. Die Frequenz wird vom Aufrufer vorgegeben. Dieses Verfahren ist keine FFT und kann auch ungleichmäßig verteilte Messpunkte verwenden.

Der benannte Fourier-Helfer verlangt eine Designmatrix mit vollem Spaltenrang, weil alle fünf benannten Koeffizienten einzeln identifizierbar sein sollen. Degenerierte Phasenwahlen werden deshalb weiterhin zurückgewiesen, obwohl die darunterliegende SVD mathematisch eine Minimalnormlösung aus unendlich vielen äquivalenten Koeffizientenvektoren auswählen könnte.

## Beliebige lineare Basisfunktionen

Lässt sich ein Modell als `c0*f0(x) + c1*f1(x) + ...` formulieren, kann es direkt mit `FitBasis` angepasst werden. Jede Basisfunktion wird genau einmal pro Messpunkt ausgewertet und in der Designmatrix zwischengespeichert.

Die allgemeine Basis-API kann jetzt sowohl den gewöhnlichen Vollrangfall als auch schwierigere Rangstrukturen behandeln:

- quadratische/hohe Designmatrix mit vollem Spaltenrang -> Householder-QR;
- rangdefiziente oder unterbestimmte Designmatrix -> SVD-Minimalnormlösung.

Damit eignet sich `FitBasis` als allgemeiner Regressionsbaustein auch dann, wenn der Koeffizientenvektor nicht eindeutig ist.

## Warum QR der bevorzugte Normalfall bleibt

Die historische Kompatibilitätsimplementierung verwendete Normalgleichungen:

`(A^T A)c = A^T y`.

Durch die explizite Bildung von `A^T A` wird jedoch die Konditionszahl quadriert. Ein nur mäßig schwieriges Regressionsproblem kann dadurch numerisch deutlich problematischer werden.

Für quadratische bzw. hohe Designmatrizen mit vollem Spaltenrang faktorisiert die modernisierte Implementierung die Designmatrix direkt als `A = Q*R` mit Householder-Reflexionen. Nach Anwendung von `Q^T` auf die Beobachtungen bleibt ein oberes Dreieckssystem. QR ist günstiger als eine vollständige SVD und bleibt daher der bevorzugte Standard, solange der Rang unproblematisch ist.

`QrFactorization` steht zusätzlich öffentlich unter `Sasd.Numerics.LinearAlgebra` zur Verfügung, wenn eine Anwendung die Zerlegung selbst benötigt oder für mehrere rechte Seiten wiederverwenden möchte.

## Wann die SVD übernimmt

Meldet QR eine Rangdefizienz oder ist die Designmatrix breit bzw. unterbestimmt, verwendet die allgemeine Basis-Engine `SingularValueDecomposition`.

Die SVD liefert Singulärwerte und einen expliziten toleranzabhängigen numerischen Rang. Komponenten an oder unterhalb der eingestellten Rangschwelle werden abgeschnitten. Der zurückgegebene Koeffizientenvektor ist dann die Minimum-Euklidische-Norm-Lösung für die beibehaltenen Richtungen.

Das ist eine wichtige Unterscheidung: Bei einem rangdefizienten Modell ist der Koeffizientenvektor nicht eindeutig, selbst wenn die Vorhersagen vollständig bestimmt sind. Die SVD macht die Auswahl explizit, statt einen willkürlichen Koeffizientensatz als eindeutig erscheinen zu lassen.

Direkter Zugriff ist ebenfalls möglich:

```csharp
var svd = SingularValueDecomposition.Decompose(design);
Console.WriteLine(svd.EstimatedRank);
Console.WriteLine(svd.ConditionNumber);

var coefficients = svd.SolveLeastSquares(observations);
```

Weitere Details zu Pseudoinverser und Konditionszahl stehen im Kapitel zur modernen dichten linearen Algebra.

## Residuen und Transformationen verstehen

Potenzgesetz- und Exponentialanpassung transformieren y und minimieren deshalb Fehler im logarithmischen y-Raum. Ihre ausgegebenen RSS-/RMSE-Werte werden anschließend im ursprünglichen y-Raum berechnet.

Logarithmische und fünfgliedrige Fourier-Anpassung verändern y nicht. Bei ihnen entspricht `ResidualSumOfSquares` direkt der gewöhnlichen Least-Squares-Zielfunktion in den ursprünglichen y-Einheiten.

## Praktische Prüfung

Ein Fit sollte nicht nur anhand seiner Parameter beurteilt werden. Residuen sollten geprüft oder geplottet werden. Auch mit QR und SVD bleibt eine sinnvolle Skalierung wichtig, und ein numerisch erfolgreicher Fit beweist nicht, dass das gewählte Modell fachlich geeignet ist.

Bei rangempfindlichen Aufgaben sollten Singulärwerte und Rang betrachtet werden, statt einen zurückgegebenen Koeffizientenvektor als Beweis dafür zu verstehen, dass jeder Parameter unabhängig messbar ist. Der historische Modellumfang bleibt erhalten; die weitere Entwicklung konzentriert sich jetzt auf Regressionsdiagnostik, Gewichtung, robuste Verfahren und die breiteren Statistik-Anforderungen der SASD Statistical Workbench.
