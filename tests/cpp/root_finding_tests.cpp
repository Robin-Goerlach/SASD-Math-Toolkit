#include "sasd/numerics/common/iteration_status.hpp"
#include "sasd/numerics/polynomials/polynomial.hpp"
#include "sasd/numerics/root_finding/complex_root_solvers.hpp"
#include "sasd/numerics/root_finding/polynomial_root_solvers.hpp"
#include "sasd/numerics/root_finding/root_solvers.hpp"

#include <algorithm>
#include <cmath>
#include <complex>
#include <fstream>
#include <iostream>
#include <limits>
#include <sstream>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

namespace {

using Complex = std::complex<double>;
using sasd::numerics::common::IterationStatus;
using sasd::numerics::polynomials::Polynomial;
using sasd::numerics::root_finding::ComplexRootSolvers;
using sasd::numerics::root_finding::PolynomialRootSolvers;
using sasd::numerics::root_finding::RootFindingOptions;
using sasd::numerics::root_finding::RootSolvers;

int failures = 0;
int checks = 0;

void expect(const bool condition, const std::string_view description) {
    ++checks;
    if (!condition) {
        std::cerr << "FAILED: " << description << '\n';
        ++failures;
    }
}

template <typename Exception, typename Function>
void expect_throws(Function&& function, const std::string_view description) {
    bool observed = false;
    try {
        function();
    } catch (const Exception&) {
        observed = true;
    }
    expect(observed, description);
}

void expect_complex_near(const Complex expected, const Complex actual, const double tolerance,
                         const std::string_view description) {
    expect(std::abs(actual - expected) <= tolerance, description);
}

void test_polynomial_contract() {
    const Polynomial polynomial{0.0, 1.0, -6.0, 11.0, -6.0};
    expect(polynomial.degree() == 3, "exact leading zeros are canonicalized");
    expect(polynomial.coefficients().size() == 4, "canonical coefficients are exposed read-only");
    expect(polynomial.leading_coefficient() == Complex{1.0, 0.0},
           "leading coefficient is retained");
    expect(!polynomial.is_zero(), "nonzero polynomial is identified");

    const auto evaluation = polynomial.evaluate_with_derivatives(Complex{2.0, 0.0});
    expect_complex_near({}, evaluation.value, 1.0e-14, "extended Horner evaluates the polynomial");
    expect_complex_near({-1.0, 0.0}, evaluation.first_derivative, 1.0e-14,
                        "extended Horner evaluates the first derivative");
    expect_complex_near({}, evaluation.second_derivative, 1.0e-14,
                        "extended Horner evaluates the second derivative");
    expect_complex_near({}, polynomial.evaluate({3.0, 0.0}), 1.0e-14,
                        "ordinary Horner evaluation finds a known root");

    const auto derivative = polynomial.derivative();
    expect(derivative.degree() == 2, "formal derivative reduces degree");
    expect_complex_near({3.0, 0.0}, derivative.coefficients()[0], 1.0e-14,
                        "formal derivative leading coefficient is correct");

    const auto deflated = polynomial.deflate({2.0, 0.0});
    expect_complex_near({}, deflated.remainder, 1.0e-14,
                        "synthetic division reports a small remainder");
    expect(deflated.quotient.degree() == 2, "synthetic division reduces degree");
    expect_complex_near({-4.0, 0.0}, deflated.quotient.coefficients()[1], 1.0e-14,
                        "synthetic quotient coefficient is correct");

    const Polynomial zero{0.0, 0.0, 0.0};
    expect(zero.degree() == 0 && zero.is_zero(), "zero polynomial has canonical degree zero");
    expect(zero.derivative().is_zero(), "derivative of a constant is zero");
    expect_throws<std::invalid_argument>([&] { (void)zero.deflate({0.0, 0.0}); },
                                         "constant polynomial cannot be deflated");

    const std::vector<double> empty;
    expect_throws<std::invalid_argument>([&] { (void)Polynomial{std::span{empty}}; },
                                         "empty coefficient sequence is rejected");
    const std::vector<double> non_finite{1.0, std::numeric_limits<double>::infinity()};
    expect_throws<std::invalid_argument>([&] { (void)Polynomial{std::span{non_finite}}; },
                                         "non-finite coefficient is rejected");
    const Polynomial overflowing{std::numeric_limits<double>::max(), 1.0};
    expect_throws<std::overflow_error>(
        [&] { (void)overflowing.evaluate(std::numeric_limits<double>::max()); },
        "non-finite Horner intermediate is reported");
}

[[nodiscard]] double evaluate_reference_problem(const std::string& problem, const double x) {
    if (problem == "cos_minus_x") {
        return std::cos(x) - x;
    }
    if (problem == "square_minus_two") {
        return (x * x) - 2.0;
    }
    if (problem == "cubic_minus_two_x_minus_five") {
        return (x * x * x) - (2.0 * x) - 5.0;
    }
    throw std::runtime_error{"Unknown scalar conformance problem: " + problem};
}

void test_scalar_conformance_cases() {
    const std::string path =
        std::string{SASD_MATH_TOOLKIT_CONFORMANCE_DIR} + "/root-finding/scalar-reference.csv";
    std::ifstream input{path};
    expect(input.good(), "scalar conformance file is available");

    std::string line;
    std::getline(input, line); // header
    std::size_t cases = 0;
    while (std::getline(input, line)) {
        std::stringstream row{line};
        std::vector<std::string> fields;
        std::string field;
        while (std::getline(row, field, ',')) {
            fields.push_back(field);
        }
        if (fields.size() != 5) {
            throw std::runtime_error{"Malformed scalar conformance row."};
        }

        const std::string& problem = fields[0];
        const double left = std::stod(fields[1]);
        const double right = std::stod(fields[2]);
        const double expected = std::stod(fields[3]);
        const double tolerance = std::stod(fields[4]);
        const auto function = [&](const double x) {
            return evaluate_reference_problem(problem, x);
        };
        const auto result = RootSolvers::bisection(function, left, right);

        expect(result.converged(), "conformance bisection converges");
        expect(std::abs(result.root() - expected) <= tolerance,
               "conformance bisection matches reference root");
        expect(result.residual() <= 2.0e-11, "conformance bisection has a small true residual");
        ++cases;
    }
    expect(cases == 3, "all scalar conformance cases were executed");
}

void test_scalar_solvers_and_failures() {
    constexpr double expected = 0.7390851332151607;
    const auto function = [](const double x) { return std::cos(x) - x; };
    const auto derivative = [](const double x) { return -std::sin(x) - 1.0; };

    const auto bisection = RootSolvers::bisection(function, 0.0, 1.0);
    const auto newton = RootSolvers::newton_raphson(function, derivative, 0.5);
    const auto secant = RootSolvers::secant(function, 0.0, 1.0);
    expect(std::abs(bisection.root() - expected) <= 1.0e-12, "bisection finds cos(x)=x");
    expect(newton.converged() && newton.residual() <= 1.0e-12, "Newton-Raphson finds cos(x)=x");
    expect(secant.converged() && secant.residual() <= 1.0e-12, "secant iteration finds cos(x)=x");

    const auto not_bracketed =
        RootSolvers::bisection([](const double x) { return (x * x) + 1.0; }, -1.0, 1.0);
    expect(not_bracketed.status() == IterationStatus::not_bracketed,
           "same-sign endpoints report not_bracketed");
    expect(std::isnan(not_bracketed.root()) && std::isnan(not_bracketed.residual()),
           "missing bracket has no invented root or residual");

    const auto newton_breakdown = RootSolvers::newton_raphson(
        [](const double x) { return (x * x) + 1.0; }, [](const double x) { return 2.0 * x; }, 0.0);
    expect(newton_breakdown.status() == IterationStatus::numerical_breakdown,
           "zero derivative reports numerical breakdown");
    const auto secant_breakdown = RootSolvers::secant([](double) { return 1.0; }, 0.0, 1.0);
    expect(secant_breakdown.status() == IterationStatus::numerical_breakdown,
           "flat secant slope reports numerical breakdown");

    const RootFindingOptions one_iteration{1.0e-30, 1};
    const auto limited = RootSolvers::bisection(function, 0.0, 1.0, one_iteration);
    expect(limited.status() == IterationStatus::maximum_iterations_reached,
           "iteration budget exhaustion is explicit");

    const double maximum = std::numeric_limits<double>::max();
    const auto wide = RootSolvers::bisection([](const double x) { return x; }, -maximum, maximum);
    expect(wide.converged() && wide.root() == 0.0,
           "overflow-safe midpoint handles the widest finite bracket");

    expect_throws<std::invalid_argument>([&] { (void)RootSolvers::bisection({}, 0.0, 1.0); },
                                         "empty function callback is rejected");
    expect_throws<std::invalid_argument>([&] { (void)RootSolvers::bisection(function, 1.0, 0.0); },
                                         "reversed interval is rejected");
    expect_throws<std::invalid_argument>(
        [&] { (void)RootSolvers::bisection(function, 0.0, 1.0, {0.0, 10}); },
        "non-positive tolerance is rejected");
    expect_throws<std::domain_error>(
        [&] { (void)RootSolvers::bisection([](double) { return std::nan(""); }, 0.0, 1.0); },
        "non-finite callback result is rejected");

    bool received_non_finite = false;
    const auto huge_function = [&](const double x) {
        received_non_finite = received_non_finite || !std::isfinite(x);
        return 1.0e308;
    };
    expect_throws<std::overflow_error>(
        [&] {
            (void)RootSolvers::newton_raphson(huge_function, [](double) { return 2.0e-15; }, 0.0);
        },
        "overflowed Newton iterate is rejected");
    expect(!received_non_finite, "overflowed iterate is not passed back to caller code");
}

void test_complex_and_polynomial_solvers() {
    const auto muller = ComplexRootSolvers::muller(
        [](const Complex x) { return (x * x) + Complex{1.0, 0.0}; }, {}, {1.0, 0.0}, {2.0, 0.0});
    expect(muller.converged() && muller.residual() <= 1.0e-10, "Muller finds a root of z^2+1");
    expect(std::abs(muller.root().real()) <= 1.0e-10 &&
               std::abs(std::abs(muller.root().imag()) - 1.0) <= 1.0e-10,
           "Muller may leave the real axis");

    const auto muller_breakdown = ComplexRootSolvers::muller(
        [](const Complex x) { return (x * x) + 1.0; }, {}, {}, {2.0, 0.0});
    expect(muller_breakdown.status() == IterationStatus::numerical_breakdown,
           "duplicate Muller points report breakdown");
    expect_throws<std::domain_error>(
        [&] {
            (void)ComplexRootSolvers::muller([](Complex) { return Complex{std::nan(""), 0.0}; }, {},
                                             {1.0, 0.0}, {2.0, 0.0});
        },
        "Muller rejects a non-finite callback value");

    const Polynomial cubic{1.0, 0.0, -2.0, -5.0};
    const auto newton_horner = PolynomialRootSolvers::newton_horner(cubic, {2.0, 0.0});
    expect(newton_horner.converged(), "Newton-Horner converges");
    expect(std::abs(newton_horner.root().real() - 2.0945514815423265) <= 1.0e-10 &&
               std::abs(newton_horner.root().imag()) <= 1.0e-12,
           "Newton-Horner finds the expected real polynomial root");

    const Polynomial quartic_plus_one{1.0, 0.0, 0.0, 0.0, 1.0};
    const auto laguerre = PolynomialRootSolvers::laguerre(quartic_plus_one, {1.0, 0.0});
    expect(laguerre.converged() && laguerre.residual() <= 1.0e-9,
           "Laguerre finds a complex quartic root");
    expect(std::abs(std::abs(laguerre.root()) - 1.0) <= 1.0e-9,
           "Laguerre root has the expected magnitude");

    const Polynomial quartic_minus_one{1.0, 0.0, 0.0, 0.0, -1.0};
    const auto all_roots = PolynomialRootSolvers::find_all_roots_laguerre(quartic_minus_one);
    expect(all_roots.converged() && all_roots.roots().size() == 4,
           "all-roots Laguerre returns every quartic root");
    expect(all_roots.maximum_residual() <= 1.0e-8,
           "all-roots residual is measured against the original polynomial");
    const std::vector<Complex> expected_roots{{-1.0, 0.0}, {0.0, -1.0}, {0.0, 1.0}, {1.0, 0.0}};
    for (const auto expected_root : expected_roots) {
        const bool found =
            std::any_of(all_roots.roots().begin(), all_roots.roots().end(), [&](const auto actual) {
                return std::abs(actual - expected_root) <= 1.0e-8;
            });
        expect(found, "all-roots Laguerre contains an expected root");
    }

    const Polynomial repeated{1.0, -4.0, 6.0, -4.0, 1.0}; // (x-1)^4
    const auto repeated_roots = PolynomialRootSolvers::find_all_roots_laguerre(repeated);
    expect(repeated_roots.converged() && repeated_roots.roots().size() == 4,
           "all-roots Laguerre retains repeated roots");
    expect(repeated_roots.maximum_residual() <= 1.0e-8,
           "repeated roots retain a bounded original-polynomial residual");

    const Polynomial linear{2.0, -8.0};
    const auto linear_root = PolynomialRootSolvers::laguerre(linear, {});
    expect(linear_root.converged() && linear_root.root() == Complex{4.0, 0.0},
           "linear polynomial uses the exact direct path");
    const Polynomial constant{42.0};
    expect_throws<std::invalid_argument>(
        [&] { (void)PolynomialRootSolvers::newton_horner(constant, {}); },
        "constant polynomial is not rootable");
}

} // namespace

int main() {
    test_polynomial_contract();
    test_scalar_conformance_cases();
    test_scalar_solvers_and_failures();
    test_complex_and_polynomial_solvers();

    if (failures != 0) {
        std::cerr << failures << " C++ root-finding test(s) failed.\n";
        return 1;
    }
    std::cout << "All " << checks << " C++ polynomial/root-finding checks passed.\n";
    return 0;
}
