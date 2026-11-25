# Numerische Differentiation

Numerische Differentiation schätzt Ableitungen, wenn keine analytische Ableitung verfügbar oder praktisch nutzbar ist. Das SASD Math Toolkit bietet dafür drei Wege: eine aufrufbare Funktion differenzieren, Tabellenwerte differenzieren oder einen aus Tabellenwerten erzeugten kubischen Spline differenzieren.

## Eine Funktion differenzieren

```csharp
using Sasd.Numerics.Differentiation;

var first = NumericalDifferentiation.FirstDerivative(Math.Sin, 0.3);
var second = NumericalDifferentiation.SecondDerivative(Math.Sin, 0.3);
```

Die Standardroutinen verwenden zentrale Differenzen mit Richardson-Verfeinerung. Zusätzlich stehen explizite Fünfpunktverfahren zur Verfügung.

## Tabellenwerte differenzieren

Angenommen, eine Messreihe enthält steigende x-Werte und dazugehörige y-Werte:

```csharp
var x = new[] { 0.0, 0.5, 1.1, 2.0, 3.0 };
var y = x.Select(value => value * value).ToArray();

var first = TabularDifferentiation.FirstDerivativeThreePoint(x, y, 2);
var second = TabularDifferentiation.SecondDerivativeFivePoint(x, y, 2);
```

Das letzte Argument ist der Index des Tabellenwertes, an dem die Ableitung gesucht wird. Die x-Werte dürfen ungleichmäßig verteilt sein, müssen aber endlich und streng steigend sein.

Zur Verfügung stehen Zwei-, Drei- und Fünfpunktverfahren für die erste sowie Drei- und Fünfpunktverfahren für die zweite Ableitung.

Am Anfang oder Ende der Tabelle verschiebt die Bibliothek den lokalen Stützstellenbereich automatisch, statt außerhalb der Daten zuzugreifen. Bei glatten Daten kann eine größere Punktzahl den Diskretisierungsfehler verringern. Bei verrauschten Messungen verstärkt Differentiation jedoch häufig das Rauschen; mehr Stützstellen sind deshalb nicht automatisch besser.

## Einen Spline differenzieren

Ein Spline ist sinnvoll, wenn die Daten zunächst durch eine glatte stückweise kubische Kurve beschrieben werden sollen. Für wiederholte Auswertungen wird der Spline einmal erzeugt:

```csharp
using Sasd.Numerics.Interpolation;

var spline = InterpolationAlgorithms.NaturalCubicSpline(x, y);
var slope = spline.FirstDerivative(1.4);
var curvature = spline.SecondDerivative(1.4);
```

Für eine einzelne Berechnung gibt es Komfortfunktionen:

```csharp
var slope = SplineDifferentiation.NaturalFirstDerivative(x, y, 1.4);
```

Sind die Randableitungen bekannt, kann ein geklemmter Spline verwendet werden:

```csharp
var slope = SplineDifferentiation.ClampedFirstDerivative(
    x,
    y,
    leftDerivative: 0.0,
    rightDerivative: 6.0,
    point: 1.4);
```

Spline-Auswertungen sind auf das Interpolationsintervall beschränkt. Das SASD Math Toolkit extrapoliert nicht still über die bereitgestellten Daten hinaus.

## Welches Verfahren wählen?

Direkte Funktionsdifferentiation eignet sich, wenn die Funktion in der Umgebung des Zielpunkts frei ausgewertet werden kann. Tabellarische Differentiation passt, wenn die vorhandenen Datenpunkte selbst untersucht werden. Spline-Differentiation ist sinnvoll, wenn eine glatte Interpolationskurve ein vertretbares Modell darstellt und Ableitungen an beliebigen Stellen innerhalb des Intervalls benötigt werden.

Kein Verfahren kann Information rekonstruieren, die in den Daten nicht enthalten ist. Ableitungen aus verrauschten Messwerten müssen deshalb vorsichtiger interpretiert werden als Ableitungen glatter mathematischer Funktionen.
