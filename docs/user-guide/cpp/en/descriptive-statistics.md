# Descriptive statistics

## Mean and median

The statistics API accepts contiguous C++ data through `std::span` without
taking ownership:

```cpp
#include <sasd/numerics/statistics/descriptive_statistics.hpp>

#include <array>
#include <iostream>

int main() {
    constexpr std::array values{1.0, 2.0, 3.0, 4.0, 5.0};

    std::cout << sasd::numerics::statistics::mean(values) << '\n';   // 3
    std::cout << sasd::numerics::statistics::median(values) << '\n'; // 3
}
```

Empty input, NaN and infinity are contract errors and produce
`std::invalid_argument`. Median copies and sorts the values internally; caller
storage remains unchanged.

## Population and sample variance

Population variance divides by `N`. Sample variance divides by `N-1`:

```cpp
#include <sasd/numerics/statistics/descriptive_statistics.hpp>
#include <sasd/numerics/statistics/variance_mode.hpp>

const double population = sasd::numerics::statistics::variance(values);
const double sample = sasd::numerics::statistics::variance(
    values, sasd::numerics::statistics::VarianceMode::sample);
```

The implementation uses scaled accumulation. It avoids unnecessary
intermediate overflow and throws `std::overflow_error` when the variance itself
cannot fit in a `double`.

## Controlling CPU use

Large mean and variance inputs use the automatic execution policy by default.
Applications can force serial execution or cap parallel work:

```cpp
#include <sasd/numerics/execution/execution_options.hpp>

sasd::numerics::execution::ExecutionOptions options;
options.maximum_threads = 4;

const double average = sasd::numerics::statistics::mean(values, options);
```

Worker chunks are contiguous and combined in a stable order. Select the
sequential policy when an application already parallelizes at a higher level.
