#pragma once

#include "sasd/numerics/interpolation/cubic_spline.hpp"

#include <span>
#include <vector>

namespace sasd::numerics::interpolation {

/** Scalar interpolation algorithms and cubic-spline construction. */
class InterpolationAlgorithms final {
  public:
    /** Interpolates or extrapolates on the line through two distinct points. */
    [[nodiscard]] static double linear(double first_x, double first_y, double second_x,
                                       double second_y, double point);

    /** Evaluates the Lagrange form through distinct finite abscissas. */
    [[nodiscard]] static double lagrange(std::span<const double> abscissas,
                                         std::span<const double> ordinates, double point);

    /** Builds reusable Newton divided-difference coefficients. */
    [[nodiscard]] static std::vector<double>
    newton_divided_difference_coefficients(std::span<const double> abscissas,
                                           std::span<const double> ordinates);

    /** Builds and evaluates the Newton form for one point. */
    [[nodiscard]] static double newton_divided_difference(std::span<const double> abscissas,
                                                          std::span<const double> ordinates,
                                                          double point);

    /** Constructs a natural spline whose endpoint second derivatives are zero. */
    [[nodiscard]] static CubicSpline natural_cubic_spline(std::span<const double> abscissas,
                                                          std::span<const double> ordinates);

    /** Constructs a spline with prescribed endpoint first derivatives. */
    [[nodiscard]] static CubicSpline clamped_cubic_spline(std::span<const double> abscissas,
                                                          std::span<const double> ordinates,
                                                          double left_derivative,
                                                          double right_derivative);

  private:
    [[nodiscard]] static CubicSpline build_cubic_spline(std::span<const double> abscissas,
                                                        std::span<const double> ordinates,
                                                        bool clamped, double left_derivative,
                                                        double right_derivative);

    InterpolationAlgorithms() = delete;
};

} // namespace sasd::numerics::interpolation
