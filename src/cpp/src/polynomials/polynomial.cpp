#include "sasd/numerics/polynomials/polynomial.hpp"

#include <algorithm>
#include <cmath>
#include <stdexcept>
#include <utility>

namespace sasd::numerics::polynomials {
namespace {

[[nodiscard]] bool is_finite(const std::complex<double> value) noexcept {
    return std::isfinite(value.real()) && std::isfinite(value.imag());
}

void require_finite(const std::complex<double> value, const char* description) {
    if (!is_finite(value)) {
        throw std::invalid_argument{description};
    }
}

void require_finite_computation(const std::complex<double> value, const char* description) {
    if (!is_finite(value)) {
        throw std::overflow_error{description};
    }
}

[[nodiscard]] std::vector<std::complex<double>>
convert_real_coefficients(const std::span<const double> coefficients) {
    std::vector<std::complex<double>> converted;
    converted.reserve(coefficients.size());
    for (const double coefficient : coefficients) {
        if (!std::isfinite(coefficient)) {
            throw std::invalid_argument{"Polynomial coefficients must be finite."};
        }
        converted.emplace_back(coefficient, 0.0);
    }
    return converted;
}

} // namespace

Polynomial::Polynomial(const std::span<const double> coefficients)
    : Polynomial(convert_real_coefficients(coefficients)) {}

Polynomial::Polynomial(const std::span<const std::complex<double>> coefficients)
    : Polynomial(std::vector<std::complex<double>>(coefficients.begin(), coefficients.end())) {}

Polynomial::Polynomial(const std::initializer_list<double> coefficients)
    : Polynomial(std::span<const double>{coefficients.begin(), coefficients.size()}) {}

Polynomial::Polynomial(const std::initializer_list<std::complex<double>> coefficients)
    : Polynomial(std::span<const std::complex<double>>{coefficients.begin(), coefficients.size()}) {
}

Polynomial::Polynomial(std::vector<std::complex<double>> coefficients) {
    if (coefficients.empty()) {
        throw std::invalid_argument{"A polynomial requires at least one coefficient."};
    }
    for (const auto coefficient : coefficients) {
        require_finite(coefficient, "Polynomial coefficients must be finite.");
    }

    const auto first_nonzero = std::find_if(coefficients.begin(), coefficients.end() - 1,
                                            [](const auto value) { return value != 0.0; });
    coefficients_.assign(first_nonzero, coefficients.end());
}

std::size_t Polynomial::degree() const noexcept { return coefficients_.size() - 1; }

bool Polynomial::is_zero() const noexcept {
    return degree() == 0 && coefficients_.front() == std::complex<double>{};
}

std::complex<double> Polynomial::leading_coefficient() const noexcept {
    return coefficients_.front();
}

std::span<const std::complex<double>> Polynomial::coefficients() const noexcept {
    return coefficients_;
}

std::complex<double> Polynomial::evaluate(const std::complex<double> x) const {
    require_finite(x, "Polynomial argument must be finite.");

    auto value = coefficients_.front();
    for (std::size_t index = 1; index < coefficients_.size(); ++index) {
        value = (value * x) + coefficients_[index];
        require_finite_computation(value, "Polynomial evaluation produced a non-finite value.");
    }
    return value;
}

PolynomialEvaluation Polynomial::evaluate_with_derivatives(const std::complex<double> x) const {
    require_finite(x, "Polynomial argument must be finite.");

    auto value = coefficients_.front();
    std::complex<double> first_derivative{};
    std::complex<double> second_derivative_accumulator{};

    for (std::size_t index = 1; index < coefficients_.size(); ++index) {
        second_derivative_accumulator = (second_derivative_accumulator * x) + first_derivative;
        first_derivative = (first_derivative * x) + value;
        value = (value * x) + coefficients_[index];

        require_finite_computation(value,
                                   "Extended Horner evaluation produced a non-finite value.");
        require_finite_computation(first_derivative,
                                   "Extended Horner derivative produced a non-finite value.");
        require_finite_computation(
            second_derivative_accumulator,
            "Extended Horner second derivative produced a non-finite value.");
    }

    const auto second_derivative = 2.0 * second_derivative_accumulator;
    require_finite_computation(second_derivative,
                               "Polynomial second derivative produced a non-finite value.");
    return {value, first_derivative, second_derivative};
}

Polynomial Polynomial::derivative() const {
    if (degree() == 0) {
        return Polynomial({0.0});
    }

    std::vector<std::complex<double>> result(degree());
    for (std::size_t index = 0; index < degree(); ++index) {
        result[index] = coefficients_[index] * static_cast<double>(degree() - index);
        require_finite_computation(result[index],
                                   "Polynomial derivative coefficient is not representable.");
    }
    return Polynomial(std::move(result));
}

PolynomialDeflationResult Polynomial::deflate(const std::complex<double> root) const {
    require_finite(root, "Deflation root must be finite.");
    if (degree() < 1) {
        throw std::invalid_argument{"A constant polynomial cannot be deflated."};
    }

    std::vector<std::complex<double>> quotient(degree());
    quotient[0] = coefficients_[0];
    for (std::size_t index = 1; index < quotient.size(); ++index) {
        quotient[index] = coefficients_[index] + (root * quotient[index - 1]);
        require_finite_computation(quotient[index],
                                   "Polynomial deflation produced a non-finite coefficient.");
    }

    const auto remainder = coefficients_.back() + (root * quotient.back());
    require_finite_computation(remainder, "Polynomial deflation produced a non-finite remainder.");
    return {Polynomial(std::move(quotient)), remainder};
}

} // namespace sasd::numerics::polynomials
