#pragma once

namespace sasd::numerics::statistics {

/** Selects the denominator used by a variance calculation. */
enum class VarianceMode {
    /** Divide by N; the values represent the complete population. */
    population,

    /** Divide by N-1; the values are a sample of a larger population. */
    sample
};

} // namespace sasd::numerics::statistics
