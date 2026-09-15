# V1 Numerical Methods compatibility matrix

Status legend: **done** = callable implementation exists now; **partial** = domain exists but historical variants remain; **planned** = V1 work item.

| Historical area / method | SASD V1 status | Current SASD API / plan |
|---|---:|---|
| Bisection root finding | done | `RootSolvers.Bisection` |
| Newton-Raphson root finding | done | `RootSolvers.NewtonRaphson` |
| Secant root finding | done | `RootSolvers.Secant` |
| Newton-Horner with deflation | done | `PolynomialRootSolvers.NewtonHorner` + `Polynomial.Deflate` |
| Muller complex root method | done | `ComplexRootSolvers.Muller` |
| Laguerre polynomial roots + deflation | done | `PolynomialRootSolvers.Laguerre` / `FindAllRootsLaguerre` |
| Lagrange interpolation | done | `InterpolationAlgorithms.Lagrange` |
| Newton divided differences | done | `NewtonDividedDifference` |
| Free/natural cubic spline | done | `NaturalCubicSpline` |
| Clamped cubic spline | done | `ClampedCubicSpline` |
| First derivative from tabular data (2/3/5 point) | done | `TabularDifferentiation.FirstDerivativeTwoPoint/ThreePoint/FivePoint` |
| Second derivative from tabular data (3/5 point) | done | `TabularDifferentiation.SecondDerivativeThreePoint/FivePoint` |
| Differentiation via cubic spline interpolant | done | `CubicSpline.FirstDerivative/SecondDerivative` + `SplineDifferentiation` helpers |
| First derivative of a function | done | Richardson-refined central formula |
| Second derivative of a function | done | Richardson-refined central formula |
| Composite Simpson integration | done | `CompositeSimpson` |
| Composite trapezoid integration | done | `CompositeTrapezoid` |
| Adaptive Simpson integration | done | `AdaptiveSimpson` |
| Adaptive Gaussian quadrature | done | `AdaptiveGaussLegendre5` |
| Romberg integration | done | `Romberg` |
| Determinant | done | `LinearSystemSolvers.Determinant` |
| Matrix inverse | done | `LinearSystemSolvers.Inverse` (LU-backed) |
| Gaussian elimination | done | `SolveGaussian(... partialPivoting:false)` |
| Gaussian elimination + partial pivoting | done | `SolveGaussian(... partialPivoting:true)` |
| Direct factoring / LU decompose + solve | done | `LuFactorization` / `LinearSystemSolvers.FactorizeLu` |
| Gauss-Seidel iterative solve | done | `GaussSeidel` |
| Dominant eigenpair / power method | done | `EigenSolvers.PowerMethod` |
| Inverse power method | done | `EigenSolvers.InversePowerMethod` (reuses LU factorization) |
| Power method + Wielandt deflation | done | `WielandtDeflation` / `EigenSolvers.WielandtSecondEigenpair` |
| Cyclic Jacobi symmetric eigensystem | done | `EigenSolvers.CyclicJacobi` / `SymmetricEigendecomposition` |
| RK4 first-order ODE | done | `RungeKutta.FourthOrder` |
| Runge-Kutta-Fehlberg first-order ODE | done | `RungeKuttaFehlberg.Integrate` with adaptive RKF45 step control |
| Adams-Bashforth/Adams-Moulton predictor-corrector | done | `AdamsBashforthMoulton.Integrate` (AB4 predictor + AM4 corrector, RK4 startup) |
| RK4 second-order ODE | done | `RungeKutta.FourthOrderSecondOrder` + `SecondOrderOdePoint` |
| RK4 nth-order ODE | done | `RungeKutta.FourthOrderNthOrder` + `NthOrderOdePoint` companion-system API |
| Coupled first-order ODE system | done | `FourthOrderSystem` |
| Coupled second-order ODE system | done | `RungeKutta.FourthOrderSecondOrderSystem` + `SecondOrderSystemOdePoint` |
| Nonlinear shooting + RK for second-order BVP | done | `NonlinearShooting.Solve` + secant slope search + RK4 trial integration |
| Linear shooting + RK for second-order BVP | done | `LinearShooting.Solve` + `LinearShootingResult`, RK4-backed Dirichlet BVP |
| General linear-basis least squares | done | `LeastSquares.FitBasis` |
| Polynomial least squares | done | `LeastSquares.FitPolynomial`; five-term polynomial is degree 4 |
| Power least-squares model | done | `LeastSquares.FitPowerLaw` + `PowerLawFitResult` |
| Exponential least-squares model | done | `LeastSquares.FitExponential` + `ExponentialFitResult` |
| Logarithmic least-squares model | planned | named transformed-model helper next |
| Five-term Fourier least-squares model | partial | representable through `FitBasis`; dedicated historical-style helper/result planned |
| Complex FFT | done | radix-2 `Forward` / `Inverse` |
| Real FFT | done | `ForwardReal` (compact real-spectrum API planned) |
| Complex convolution | planned | transform domain exists; public helper planned |
| Real convolution | done | `ConvolveReal` |
| Complex cross-correlation | planned | public helper planned |
| Real cross-correlation | done | `CrossCorrelateReal` |
| Graphics demo applications | planned | samples/visualization after numerical V1 core |

The historical catalog is a minimum compatibility target. SASD APIs may expose safer defaults or more general forms rather than reproducing old procedure signatures literally.
