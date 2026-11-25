#pragma once

#include <algorithm>
#include <cmath>
#include <span>
#include <stdexcept>
#include <vector>

namespace sasd::numerics::interpolation::detail {

inline void require_finite(const double value, const char* description) {
    if (!std::isfinite(value)) {
        throw std::invalid_argument{description};
    }
}

inline void require_finite_computation(const double value, const char* description) {
    if (!std::isfinite(value)) {
        throw std::overflow_error{description};
    }
}

inline void validate_points(const std::span<const double> abscissas,
                            const std::span<const double> ordinates,
                            const std::size_t minimum_count) {
    if (abscissas.size() != ordinates.size()) {
        throw std::invalid_argument{"Interpolation coordinate arrays must have equal length."};
    }
    if (abscissas.size() < minimum_count) {
        throw std::invalid_argument{"Too few interpolation points were supplied."};
    }
    for (std::size_t index = 0; index < abscissas.size(); ++index) {
        if (!std::isfinite(abscissas[index]) || !std::isfinite(ordinates[index])) {
            throw std::invalid_argument{"Interpolation coordinates must be finite."};
        }
    }
}

inline void require_distinct(const std::span<const double> abscissas) {
    std::vector<double> sorted{abscissas.begin(), abscissas.end()};
    std::sort(sorted.begin(), sorted.end());
    for (std::size_t index = 1; index < sorted.size(); ++index) {
        if (sorted[index] == sorted[index - 1]) {
            throw std::invalid_argument{"Interpolation abscissas must be distinct."};
        }
    }
}

inline void require_strictly_increasing(const std::span<const double> abscissas) {
    for (std::size_t index = 1; index < abscissas.size(); ++index) {
        if (abscissas[index] <= abscissas[index - 1]) {
            throw std::invalid_argument{"Spline abscissas must be strictly increasing."};
        }
    }
}

[[nodiscard]] inline double finite_difference(const double left, const double right,
                                              const char* description) {
    const double difference = left - right;
    require_finite_computation(difference, description);
    return difference;
}

} // namespace sasd::numerics::interpolation::detail
