# Erste Schritte

## Bibliothek bauen

Aus dem Wurzelverzeichnis des Repositorys:

```bash
cmake -S src/cpp -B build/cpp -DCMAKE_BUILD_TYPE=Release
cmake --build build/cpp --parallel
ctest --test-dir build/cpp --output-on-failure
```

Das erste `--parallel` lässt das Buildwerkzeug unabhängige Übersetzungseinheiten
gleichzeitig kompilieren. Dies ist von der späteren numerischen
Ausführungsrichtlinie der Bibliothek unabhängig.

## Target aus dem Quellbaum verwenden

Ein Projekt kann den C++-Quellbaum einbinden und gegen das namespaced Target
linken:

```cmake
add_subdirectory(pfad/zum/SASD-Math-Toolkit/src/cpp)
target_link_libraries(mein_programm PRIVATE SASD::MathToolkit)
```

## Installiertes Paket verwenden

```bash
cmake --install build/cpp --prefix /gewuenschtes/praefix
```

```cmake
find_package(SasdMathToolkit 0.3 CONFIG REQUIRED)
target_link_libraries(mein_programm PRIVATE SASD::MathToolkit)
```

## Erstes Programm

```cpp
#include <sasd/numerics/common/iterative_result.hpp>

#include <iostream>

int main() {
    using sasd::numerics::common::IterationStatus;
    using sasd::numerics::common::IterativeResult;

    const IterativeResult<double> result{
        0.7390851332151607,
        7,
        IterationStatus::converged,
        2.2e-16};

    if (result.converged() && result.has_finite_residual()) {
        std::cout << result.value() << '\n';
    }
}
```

`converged()` gibt den Terminierungsstatus des Algorithmus wieder.
`has_finite_residual()` sagt nur, ob ein Residuum vorhanden ist. Keine der
Eigenschaften ersetzt anwendungsspezifische Akzeptanzkriterien.
