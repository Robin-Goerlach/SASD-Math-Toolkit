#pragma once

#include "sasd/numerics/common/iteration_status.hpp"

#include <cmath>
#include <complex>
#include <cstddef>
#include <optional>
#include <span>
#include <string>
#include <utility>
#include <vector>

namespace sasd::numerics::root_finding {

/** Outcome of a scalar real root-finding iteration. */
class RootResult final {
  public:
    RootResult(double root, double function_value, std::size_t iterations,
               common::IterationStatus status, std::optional<std::string> message = std::nullopt)
        : root_(root), function_value_(function_value), iterations_(iterations), status_(status),
          message_(std::move(message)) {}

    [[nodiscard]] double root() const noexcept { return root_; }
    [[nodiscard]] double function_value() const noexcept { return function_value_; }
    [[nodiscard]] std::size_t iterations() const noexcept { return iterations_; }
    [[nodiscard]] common::IterationStatus status() const noexcept { return status_; }
    [[nodiscard]] const std::optional<std::string>& message() const noexcept { return message_; }
    [[nodiscard]] bool converged() const noexcept {
        return status_ == common::IterationStatus::converged;
    }
    [[nodiscard]] double residual() const noexcept { return std::abs(function_value_); }

  private:
    double root_;
    double function_value_;
    std::size_t iterations_;
    common::IterationStatus status_;
    std::optional<std::string> message_;
};

/** Outcome of one complex root-finding iteration. */
class ComplexRootResult final {
  public:
    ComplexRootResult(std::complex<double> root, std::complex<double> function_value,
                      std::size_t iterations, common::IterationStatus status,
                      std::optional<std::string> message = std::nullopt)
        : root_(root), function_value_(function_value), iterations_(iterations), status_(status),
          message_(std::move(message)) {}

    [[nodiscard]] std::complex<double> root() const noexcept { return root_; }
    [[nodiscard]] std::complex<double> function_value() const noexcept { return function_value_; }
    [[nodiscard]] std::size_t iterations() const noexcept { return iterations_; }
    [[nodiscard]] common::IterationStatus status() const noexcept { return status_; }
    [[nodiscard]] const std::optional<std::string>& message() const noexcept { return message_; }
    [[nodiscard]] bool converged() const noexcept {
        return status_ == common::IterationStatus::converged;
    }
    [[nodiscard]] double residual() const noexcept { return std::abs(function_value_); }

  private:
    std::complex<double> root_;
    std::complex<double> function_value_;
    std::size_t iterations_;
    common::IterationStatus status_;
    std::optional<std::string> message_;
};

/** Result of repeated polynomial root search and deflation. */
class PolynomialRootsResult final {
  public:
    PolynomialRootsResult(std::vector<std::complex<double>> roots, std::size_t iterations,
                          common::IterationStatus status, double maximum_residual,
                          std::optional<std::string> message = std::nullopt)
        : roots_(std::move(roots)), iterations_(iterations), status_(status),
          maximum_residual_(maximum_residual), message_(std::move(message)) {}

    [[nodiscard]] std::span<const std::complex<double>> roots() const noexcept { return roots_; }
    [[nodiscard]] std::size_t iterations() const noexcept { return iterations_; }
    [[nodiscard]] common::IterationStatus status() const noexcept { return status_; }
    [[nodiscard]] double maximum_residual() const noexcept { return maximum_residual_; }
    [[nodiscard]] const std::optional<std::string>& message() const noexcept { return message_; }
    [[nodiscard]] bool converged() const noexcept {
        return status_ == common::IterationStatus::converged;
    }

  private:
    std::vector<std::complex<double>> roots_;
    std::size_t iterations_;
    common::IterationStatus status_;
    double maximum_residual_;
    std::optional<std::string> message_;
};

} // namespace sasd::numerics::root_finding
