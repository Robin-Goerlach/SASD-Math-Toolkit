# Interpolation und Splines

Interpolation konstruiert aus bekannten Stützpunkten eine Funktion. CPP-M3
bietet einfache Geradeninterpolation, Polynomverfahren und stückweise kubische
Splines. Die Wahl ist wichtig: Ein Polynom hohen Grades wirkt global, während
eine Änderung in einem Spline vor allem benachbarte Segmente betrifft.

## Lineare Interpolation

```cpp
#include <sasd/numerics/interpolation/interpolation_algorithms.hpp>

using sasd::numerics::interpolation::InterpolationAlgorithms;

const double value = InterpolationAlgorithms::linear(
    2.0, 10.0,
    6.0, 18.0,
    4.0); // 14.0
```

Die beiden Abszissen müssen verschieden sein. Außerhalb ihres Intervalls wird
linear extrapoliert. Die Anwendung muss selbst entscheiden, ob dieses Modell
dort fachlich sinnvoll ist.

## Lagrange- und Newton-Polynome

```cpp
#include <sasd/numerics/interpolation/interpolation_algorithms.hpp>

#include <array>

constexpr std::array x{0.0, 1.0, 2.0};
constexpr std::array y{1.0, 4.0, 9.0};

const double lagrange = InterpolationAlgorithms::lagrange(x, y, 1.5);
const double newton =
    InterpolationAlgorithms::newton_divided_difference(x, y, 1.5);
// Beide ergeben 6.25 für y=(x+1)^2.
```

Lagrange ist für eine einzelne Auswertung direkt und verständlich. Newtonsche
dividierte Differenzen eignen sich besser, wenn derselbe Datensatz wiederholt
ausgewertet wird.

```cpp
#include <sasd/numerics/interpolation/newton_interpolant.hpp>

using sasd::numerics::interpolation::NewtonInterpolant;

const NewtonInterpolant interpolant{x, y};
const double first = interpolant.evaluate(1.5);
const double second = interpolant.evaluate(2.5);
```

Der Interpolant kopiert Stützstellen und Koeffizienten. Spätere Änderungen an
den ursprünglichen Arrays verändern ihn nicht.

## Natürliche und eingespannte kubische Splines

```cpp
#include <sasd/numerics/interpolation/interpolation_algorithms.hpp>

constexpr std::array knots{0.0, 1.0, 2.0, 3.0};
constexpr std::array values{0.0, 1.0, 0.0, 1.0};

const auto natural =
    InterpolationAlgorithms::natural_cubic_spline(knots, values);

const double value = natural.evaluate(1.5);
const double slope = natural.first_derivative(1.5);
const double curvature = natural.second_derivative(1.5);
```

Bei einem natürlichen Spline ist die zweite Ableitung an beiden Endpunkten
null. Ein eingespannter Spline verwendet stattdessen bekannte Randsteigungen:

```cpp
const auto clamped = InterpolationAlgorithms::clamped_cubic_spline(
    knots, values, 1.0, 2.0);
```

Splines lehnen Extrapolation ab. `interval_start()`, `interval_end()` und
`knots()` beschreiben das gültige abgeschlossene Intervall.

## Batch-Auswertung und CPU-Richtlinie

Die Zielpunkte eines Batches sind unabhängig und können deshalb die gemeinsame
Ausführungsrichtlinie verwenden:

```cpp
#include <sasd/numerics/execution/execution_options.hpp>

#include <vector>

using sasd::numerics::execution::ExecutionOptions;
using sasd::numerics::execution::ExecutionPolicy;

const std::vector<double> targets{0.0, 0.25, 0.5, 1.0, 1.5, 2.0};
ExecutionOptions execution{
    ExecutionPolicy::automatic,
    4,    // höchstens vier Worker
    1024  // mindestens so viele Zielpunkte je Worker
};

const auto results = interpolant.evaluate_many(targets, execution);
```

Es gibt keine Batch-Größe, die auf jeder CPU und in jeder Anwendung optimal
ist. Thread-Erzeugung, Polynomgrad, Cache-Verhalten und eine übergeordnete
Parallelisierung beeinflussen den Umschaltpunkt. Passe
`minimum_work_per_thread` an die Anwendung an, verwende `sequential`, wenn die
Anwendung bereits größere Aufgaben verteilt, oder `parallel`, wenn Worker
ausdrücklich verlangt werden.

Spline-Konstruktion und Berechnung der Newton-Koeffizienten bleiben seriell,
weil ihre Rekurrenzen von vorherigen Schritten abhängen. Parallelisiert werden
nur unabhängige Zielpunkte; ihre Reihenfolge bleibt erhalten.

## Fehlerverhalten

- Koordinaten und Auswertungspunkte müssen endlich sein;
- Koordinatenarrays müssen gleich lang sein;
- Polynomstützstellen müssen verschieden sein;
- Spline-Knoten müssen streng aufsteigend sein;
- Splines benötigen mindestens zwei Knoten und lehnen Punkte außerhalb ihres Intervalls ab;
- nicht endliche Zwischenergebnisse werden gemeldet und nicht zurückgegeben.
