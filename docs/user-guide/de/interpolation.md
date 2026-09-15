# Interpolation

Interpolation konstruiert eine Funktion, die durch bekannte Datenpunkte verläuft. Sie ist sinnvoll, wenn Werte nur an diskreten Stellen bekannt sind, aber dazwischen Werte benötigt werden.

SASD Math Toolkit V1 stellt vier eng verwandte Varianten bereit:

- direkte Lagrange-Polynominterpolation,
- Newton-Interpolation mit dividierten Differenzen,
- natürliche kubische Splines,
- geklemmte bzw. eingespannte kubische Splines.

Alle vier sind Interpolationsverfahren: An den vorgegebenen Stützstellen reproduzieren sie die vorgegebenen Werte bis auf Gleitkomma-Rundung. Sie sind keine Regressionsverfahren und glätten kein Messrauschen.

## Welches Verfahren wählen?

**Lagrange** eignet sich für kleine Datenmengen und einzelne Auswertungen, wenn die direkte Formel praktisch ist. **Newton mit dividierten Differenzen** ist nützlich, wenn dasselbe Interpolationspolynom konzeptionell mehrfach verwendet oder seine Koeffizientenform benötigt wird. Ein **kubischer Spline** ist meist geeigneter, wenn viele Stützstellen vorliegen und eine stückweise glatte Kurve einem einzigen Polynom hohen Grades vorzuziehen ist.

Natürliche und geklemmte Splines unterscheiden sich durch die Randbedingungen. Beim natürlichen Spline wird die zweite Ableitung an beiden Enden auf null gesetzt. Beim geklemmten Spline werden die bekannten ersten Ableitungen an beiden Enden vorgegeben.

Globale Interpolationspolynome hohen Grades können stark oszillieren, besonders an den Rändern und bei ungünstig verteilten Stützstellen. Mehr Datenpunkte machen ein globales Interpolationspolynom nicht automatisch besser.

## Lagrange-Interpolation

Die Punkte `(0,1)`, `(1,4)` und `(2,9)` liegen beispielsweise auf `(x+1)^2`.

```csharp
using Sasd.Numerics.Interpolation;

double[] x = [0.0, 1.0, 2.0];
double[] y = [1.0, 4.0, 9.0];

var value = InterpolationAlgorithms.Lagrange(x, y, 1.5);
Console.WriteLine(value); // ungefähr 6.25
```

Die x-Werte müssen verschieden sein, müssen für die Polynominterpolation aber nicht sortiert sein. Eingabedaten und Auswertungspunkt müssen endlich sein.

`Lagrange` wertet die Basisformel direkt aus. Dadurch bleibt die Referenzimplementierung leicht nachvollziehbar, führt die O(n^2)-Basisarbeit aber bei jedem Aufruf erneut aus. Für V1 ist diese Verständlichkeit bewusst wichtiger als vorzeitige Optimierung.

## Newton und dividierte Differenzen

Dasselbe Interpolationspolynom lässt sich darstellen als

`a0 + a1(x-x0) + a2(x-x0)(x-x1) + ...`

Die Koeffizienten können getrennt berechnet werden:

```csharp
var coefficients = InterpolationAlgorithms.NewtonDividedDifferenceCoefficients(x, y);
```

Für das obige Beispiel sind sie in der durch die x-Reihenfolge definierten Newton-Basis ungefähr `[1, 3, 1]`.

Für eine einzelne Auswertung gibt es die Komfortmethode:

```csharp
var value = InterpolationAlgorithms.NewtonDividedDifference(x, y, 1.5);
```

Das zurückgegebene Koeffizientenarray ist von den Eingabearrays unabhängig. Spätere Änderungen an den Eingaben verändern die bereits berechneten Koeffizienten nicht.

## Natürlicher kubischer Spline

Ein kubischer Spline verwendet auf jedem Intervall ein eigenes kubisches Polynom und erzwingt an inneren Stützstellen Stetigkeit der Funktion sowie ihrer ersten und zweiten Ableitung.

```csharp
double[] x = [0.0, 1.0, 2.0, 3.0];
double[] y = [0.0, 1.0, 0.0, 1.0];

var spline = InterpolationAlgorithms.NaturalCubicSpline(x, y);

var value = spline.Evaluate(1.5);
var slope = spline.FirstDerivative(1.5);
var curvature = spline.SecondDerivative(1.5);
```

Der natürliche Spline ergänzt die Randbedingungen

`S''(x0) = 0` und `S''(xn) = 0`.

Das ist eine neutrale mathematische Randannahme und keine Behauptung, dass die reale zugrunde liegende Funktion dort tatsächlich keine Krümmung besitzt.

## Geklemmter kubischer Spline

Sind die Steigungen an den beiden Rändern bekannt, können sie vorgegeben werden:

```csharp
static double F(double x) => x * x * x;
static double Df(double x) => 3.0 * x * x;

double[] x = [0.0, 1.0, 2.0, 3.0];
var y = x.Select(F).ToArray();

var spline = InterpolationAlgorithms.ClampedCubicSpline(
    x,
    y,
    leftDerivative: Df(x[0]),
    rightDerivative: Df(x[^1]));
```

Mit exakten Randableitungen reproduziert ein geklemmter kubischer Spline ein kubisches Polynom bis auf Rundungsfehler exakt. Bei Messdaten sollten Randableitungen nur vorgegeben werden, wenn sie fachlich sinnvoll bekannt sind; ungenaue Steigungsbedingungen können gerade den Randbereich verzerren.

## Definitionsintervall und Unveränderlichkeit

Spline-Stützstellen müssen streng aufsteigend sein. Das Spline-Objekt stellt zur Verfügung:

```csharp
spline.Knots
spline.IntervalStart
spline.IntervalEnd
spline.SegmentCount
```

`Knots` ist schreibgeschützt, und der Spline besitzt seine internen Koeffizientendaten selbst. Er kann nach der Konstruktion also wiederverwendet werden, ohne durch spätere Änderungen an Eingabearrays verändert zu werden.

Spline-Auswertung extrapoliert bewusst **nicht**. Aufrufe außerhalb von `[IntervalStart, IntervalEnd]` sowie NaN oder Unendlich führen zu `ArgumentOutOfRangeException`. Wenn Extrapolation gewünscht ist, sollte sie eine ausdrückliche Modellierungsentscheidung sein und kein unbeabsichtigter Nebeneffekt der Interpolation.

Die globalen Verfahren `Lagrange` und `NewtonDividedDifference` können dagegen mathematisch auch außerhalb des gegebenen x-Bereichs ausgewertet werden. Das bedeutet nicht, dass eine solche Polynomextrapolation zuverlässig ist.

## Ungültige Daten und numerische Probleme

Doppelte x-Werte sind bei der Polynominterpolation ungültig, weil sonst durch null dividiert werden müsste. Bei Splines müssen die Stützstellen zusätzlich sortiert und streng aufsteigend sein. Nicht-endliche Eingangsdaten werden abgelehnt.

Auch endliche Daten können so extrem skaliert sein, dass Zwischenrechnungen überlaufen oder das Spline-Gleichungssystem numerisch unbrauchbar wird. In solchen Fällen wirft die Referenzimplementierung `ArithmeticException`, statt still NaN oder Unendlich zurückzugeben.

Bei schwierigen Datensätzen sollte man zunächst die Größenordnungen prüfen, x-Werte gegebenenfalls mathematisch sinnvoll verschieben oder normieren und unnötig hohe globale Polynomgrade vermeiden.

## Interpolation ist keine Approximation

Interpolation zwingt die resultierende Kurve durch jeden vorgegebenen Wert. Least-Squares-Approximation erlaubt dagegen bewusst Residuen, um ein Modell an verrauschte oder überbestimmte Daten anzupassen. Die Wahl sollte von der Bedeutung der Daten abhängen und nicht nur davon, welche Kurve optisch glatt aussieht.
