# Deskriptive Statistik

## Mittelwert und Median

Die Statistik-API übernimmt zusammenhängende C++-Daten als `std::span`, ohne
deren Besitz zu übernehmen:

```cpp
#include <sasd/numerics/statistics/descriptive_statistics.hpp>

#include <array>
#include <iostream>

int main() {
    constexpr std::array werte{1.0, 2.0, 3.0, 4.0, 5.0};

    std::cout << sasd::numerics::statistics::mean(werte) << '\n';   // 3
    std::cout << sasd::numerics::statistics::median(werte) << '\n'; // 3
}
```

Leere Eingaben, NaN und Unendlich sind Vertragsfehler und führen zu
`std::invalid_argument`. Der Median kopiert und sortiert intern; der Speicher
des Aufrufers bleibt unverändert.

## Populations- und Stichprobenvarianz

Die Populationsvarianz teilt durch `N`, die Stichprobenvarianz durch `N-1`:

```cpp
#include <sasd/numerics/statistics/descriptive_statistics.hpp>
#include <sasd/numerics/statistics/variance_mode.hpp>

const double population = sasd::numerics::statistics::variance(werte);
const double stichprobe = sasd::numerics::statistics::variance(
    werte, sasd::numerics::statistics::VarianceMode::sample);
```

Die Implementierung summiert skaliert. Sie vermeidet unnötige
Zwischenüberläufe und wirft `std::overflow_error`, wenn die Varianz selbst nicht
in einem `double` darstellbar ist.

## CPU-Nutzung steuern

Große Mittelwert- und Varianzeingaben verwenden standardmäßig die automatische
Ausführungsrichtlinie. Anwendungen können serielles Rechnen erzwingen oder die
Parallelität begrenzen:

```cpp
#include <sasd/numerics/execution/execution_options.hpp>

sasd::numerics::execution::ExecutionOptions optionen;
optionen.maximum_threads = 4;

const double mittelwert = sasd::numerics::statistics::mean(werte, optionen);
```

Worker-Bereiche sind zusammenhängend und werden in stabiler Reihenfolge
zusammengeführt. Wenn die Anwendung bereits außen parallelisiert, sollte sie
innerhalb der Bibliothek die serielle Richtlinie wählen.
