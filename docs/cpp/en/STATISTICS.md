# Descriptive statistics

CPP-M1 introduces `sasd::numerics::statistics` with arithmetic mean, median and
population/sample variance over `std::span<const double>`.

## Numerical contract

- input must be non-empty and finite;
- sample variance requires at least two values;
- mean normalizes values by their largest magnitude before compensated summation, avoiding the
  naive overflow of summing `{max, max}` first;
- an even median uses `std::midpoint`, avoiding overflow in `(a + b) / 2`;
- variance uses a scaled sum of squared deviations and reports
  `std::overflow_error` if the mathematical result is not representable;
- input storage is never modified.

Mean and variance accept `ExecutionOptions`. Automatic mode uses multiple
deterministic chunks only when the input is large enough. Median remains
sequential because sorting one value sequence is not currently a measured
parallel win in the dependency-free reference implementation.
