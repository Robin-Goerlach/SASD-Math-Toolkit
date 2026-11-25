# Getting started

## Build the library

From the repository root:

```bash
cmake -S src/cpp -B build/cpp -DCMAKE_BUILD_TYPE=Release
cmake --build build/cpp --parallel
ctest --test-dir build/cpp --output-on-failure
```

The first `--parallel` asks the build tool to compile independent translation
units concurrently. It is separate from the numerical execution policy used by
the finished library.

## Use the build-tree target

Projects that include the C++ source tree can link the namespaced CMake target:

```cmake
add_subdirectory(path/to/SASD-Math-Toolkit/src/cpp)
target_link_libraries(my_program PRIVATE SASD::MathToolkit)
```

## Use an installed package

```bash
cmake --install build/cpp --prefix /chosen/prefix
```

```cmake
find_package(SasdMathToolkit 0.1 CONFIG REQUIRED)
target_link_libraries(my_program PRIVATE SASD::MathToolkit)
```

## First program

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

`converged()` reports the algorithm's termination status.
`has_finite_residual()` only reports whether a residual is available. Neither
property replaces application-specific acceptance criteria.
