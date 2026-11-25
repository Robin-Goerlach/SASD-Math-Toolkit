#pragma once

#include "sasd/numerics/polynomials/polynomial.hpp"
#include "sasd/numerics/root_finding/root_finding_options.hpp"
#include "sasd/numerics/root_finding/root_result.hpp"

#include <complex>

namespace sasd::numerics::root_finding {

/** Root-finding algorithms specialized for immutable polynomials. */
class PolynomialRootSolvers final {
  public:
    /** Newton iteration using one extended-Horner evaluation per step. */
    [[nodiscard]] static ComplexRootResult newton_horner(const polynomials::Polynomial& polynomial,
                                                         std::complex<double> initial_guess,
                                                         const RootFindingOptions& options = {});

    /** Finds one real or complex root with Laguerre's method. */
    [[nodiscard]] static ComplexRootResult laguerre(const polynomials::Polynomial& polynomial,
                                                    std::complex<double> initial_guess,
                                                    const RootFindingOptions& options = {});

    /**
     * Finds every root by deterministic Laguerre searches, synthetic
     * deflation and final polishing against the original polynomial.
     */
    [[nodiscard]] static PolynomialRootsResult
    find_all_roots_laguerre(const polynomials::Polynomial& polynomial,
                            const RootFindingOptions& options = {});

  private:
    PolynomialRootSolvers() = delete;
};

} // namespace sasd::numerics::root_finding
