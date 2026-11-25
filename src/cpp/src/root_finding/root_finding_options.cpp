#include "sasd/numerics/root_finding/root_finding_options.hpp"

#include <cmath>
#include <stdexcept>

namespace sasd::numerics::root_finding {

void RootFindingOptions::validate() const {
    if (!std::isfinite(tolerance) || tolerance <= 0.0) {
        throw std::invalid_argument{"Root-finding tolerance must be finite and positive."};
    }
    if (maximum_iterations == 0) {
        throw std::invalid_argument{"Root finding requires at least one iteration."};
    }
}

} // namespace sasd::numerics::root_finding
