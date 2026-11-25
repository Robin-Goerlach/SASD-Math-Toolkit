#include "sasd/numerics/common/iteration_status.hpp"

namespace sasd::numerics::common {

std::string_view to_string(const IterationStatus status) noexcept {
    switch (status) {
    case IterationStatus::converged:
        return "converged";
    case IterationStatus::maximum_iterations_reached:
        return "maximum_iterations_reached";
    case IterationStatus::invalid_input:
        return "invalid_input";
    case IterationStatus::numerical_breakdown:
        return "numerical_breakdown";
    case IterationStatus::not_bracketed:
        return "not_bracketed";
    }

    // A defensive fallback keeps diagnostic/logging code noexcept even when a
    // value was produced by an unsafe cast or by data from a newer version.
    return "unknown";
}

} // namespace sasd::numerics::common
