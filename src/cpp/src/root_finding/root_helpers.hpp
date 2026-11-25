#pragma once

#include "sasd/numerics/common/numeric_constants.hpp"

#include <algorithm>
#include <cmath>
#include <complex>
#include <stdexcept>

namespace sasd::numerics::root_finding::detail {

[[nodiscard]] inline bool is_finite(const std::complex<double> value) noexcept {
    return std::isfinite(value.real()) && std::isfinite(value.imag());
}

inline void require_finite_complex(const std::complex<double> value, const char* description) {
    if (!is_finite(value)) {
        throw std::overflow_error{description};
    }
}

inline void require_finite_scalar(const double value, const char* description) {
    if (!std::isfinite(value)) {
        throw std::invalid_argument{description};
    }
}

inline void require_finite_callback(const double value) {
    if (!std::isfinite(value)) {
        throw std::domain_error{"The supplied function or derivative returned a non-finite value."};
    }
}

inline void require_finite_callback(const std::complex<double> value) {
    if (!is_finite(value)) {
        throw std::domain_error{"The supplied function returned a non-finite complex value."};
    }
}

[[nodiscard]] inline bool has_converged(const double current, const double previous,
                                        const double function_value,
                                        const double tolerance) noexcept {
    if (std::abs(function_value) <= tolerance) {
        return true;
    }
    const double scale = std::max(1.0, std::abs(previous));
    return std::abs(current - previous) <= tolerance * scale;
}

[[nodiscard]] inline bool has_converged(const std::complex<double> current,
                                        const std::complex<double> previous,
                                        const std::complex<double> function_value,
                                        const double tolerance) noexcept {
    if (std::abs(function_value) <= tolerance) {
        return true;
    }
    const double scale = std::max(1.0, std::abs(previous));
    return std::abs(current - previous) <= tolerance * scale;
}

[[nodiscard]] inline bool is_nearly_zero(const std::complex<double> value) noexcept {
    return std::abs(value) <= common::numeric_constants::nearly_zero;
}

} // namespace sasd::numerics::root_finding::detail
