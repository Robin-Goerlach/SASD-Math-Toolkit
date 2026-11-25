#pragma once

#include "sasd/numerics/common/numeric_constants.hpp"

#include <cstddef>

namespace sasd::numerics::root_finding {

/** Common convergence options for real and complex root solvers. */
struct RootFindingOptions final {
    /** Positive finite residual/relative-step tolerance. */
    double tolerance = common::numeric_constants::default_tolerance;

    /** Positive maximum number of iterations for one root search. */
    std::size_t maximum_iterations = common::numeric_constants::default_maximum_iterations;

    /** Throws std::invalid_argument when an option violates its contract. */
    void validate() const;
};

} // namespace sasd::numerics::root_finding
