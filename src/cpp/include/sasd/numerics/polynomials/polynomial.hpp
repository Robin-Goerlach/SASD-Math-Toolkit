#pragma once

#include <complex>
#include <cstddef>
#include <initializer_list>
#include <span>
#include <vector>

namespace sasd::numerics::polynomials {

struct PolynomialDeflationResult;

/** Value and first two derivatives produced by one extended-Horner pass. */
struct PolynomialEvaluation final {
    std::complex<double> value;
    std::complex<double> first_derivative;
    std::complex<double> second_derivative;
};

/**
 * Immutable polynomial whose coefficients are stored in descending power order.
 *
 * `[a0, a1, ..., an]` represents `a0*x^n + a1*x^(n-1) + ... + an`.
 * Only exact leading zeros are removed: discarding a merely small coefficient
 * would silently change the mathematical degree.
 */
class Polynomial final {
  public:
    /** Constructs a polynomial from finite real coefficients. */
    explicit Polynomial(std::span<const double> coefficients);

    /** Constructs a polynomial from finite complex coefficients. */
    explicit Polynomial(std::span<const std::complex<double>> coefficients);

    /** Convenience constructor for a real coefficient list. */
    Polynomial(std::initializer_list<double> coefficients);

    /** Convenience constructor for a complex coefficient list. */
    Polynomial(std::initializer_list<std::complex<double>> coefficients);

    /** Degree of the canonical polynomial; the zero polynomial has degree zero. */
    [[nodiscard]] std::size_t degree() const noexcept;

    [[nodiscard]] bool is_zero() const noexcept;
    [[nodiscard]] std::complex<double> leading_coefficient() const noexcept;
    [[nodiscard]] std::span<const std::complex<double>> coefficients() const noexcept;

    /** Evaluates the polynomial with Horner's method. */
    [[nodiscard]] std::complex<double> evaluate(std::complex<double> x) const;

    /** Evaluates value, first derivative and second derivative in one pass. */
    [[nodiscard]] PolynomialEvaluation evaluate_with_derivatives(std::complex<double> x) const;

    /** Returns the formal first derivative. */
    [[nodiscard]] Polynomial derivative() const;

    /** Divides by `(x-root)` using synthetic division. */
    [[nodiscard]] PolynomialDeflationResult deflate(std::complex<double> root) const;

  private:
    explicit Polynomial(std::vector<std::complex<double>> coefficients);
    std::vector<std::complex<double>> coefficients_;
};

/** Quotient and remainder produced by synthetic division. */
struct PolynomialDeflationResult final {
    Polynomial quotient;
    std::complex<double> remainder;
};

} // namespace sasd::numerics::polynomials
