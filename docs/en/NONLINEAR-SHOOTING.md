# Nonlinear shooting for second-order boundary-value problems

## Purpose

`NonlinearShooting.Solve` handles scalar nonlinear Dirichlet boundary-value problems

`y'' = g(x, y, y')`

with prescribed values

`y(x0) = alpha` and `y(xEnd) = beta`.

This is independent C# code based on the mathematical shooting method. The historical Borland toolbox is used only as a functional V1 reference; no historical source or handbook prose is copied.

## Shooting as a root problem

RK4 needs an initial slope, but a Dirichlet boundary-value problem supplies the solution value at the right endpoint instead. Let `s` denote a trial initial slope and integrate

`y(x0)=alpha`, `y'(x0)=s`.

The resulting boundary residual is

`R(s) = y(xEnd; s) - beta`.

The desired slope is therefore a root of `R`. For a nonlinear differential equation this map is generally nonlinear, so the direct linear combination used by `LinearShooting` is no longer available.

SASD uses the secant method for the slope search. Two initial slope estimates are required, but no derivative of the boundary map is needed.

## Reuse of existing numerical building blocks

The implementation deliberately composes existing toolkit algorithms:

- `RungeKutta.FourthOrderSecondOrder` integrates every trial slope;
- `RootSolvers.Secant` updates the unknown slope;
- `NonlinearShooting` owns only the transformation between boundary residuals and slope guesses.

This avoids maintaining a second RK4 or secant implementation inside the boundary-value module.

## Result and termination status

`NonlinearShootingResult` contains the final trajectory, `InitialSlope`, `RightBoundaryResidual`, `Iterations`, `Status`, `Message` and the convenience property `Converged`.

Unlike invalid input, failure to converge is an expected numerical outcome. The solver therefore returns `MaximumIterationsReached` or `NumericalBreakdown` instead of throwing merely because the secant iteration did not find a satisfactory slope. The returned trajectory corresponds to the last usable slope estimate and can be inspected for diagnostics.

A reported `Converged` status additionally requires the final boundary residual to satisfy `BoundaryTolerance`. This prevents a tiny change in slope from being mistaken for success while the requested endpoint is still missed.

## Options

`NonlinearShootingOptions` currently exposes:

- `BoundaryTolerance` — tolerance used by the secant search and explicitly checked against the final boundary residual;
- `MaximumIterations` — maximum number of secant corrections.

The defaults follow the shared SASD numerical defaults. The RK4 integration step remains an explicit argument because discretization error and shooting convergence are different concerns.

## Example with an exact nonlinear solution

The function `y = 1 / (1 - x)` satisfies `y'' = 2 y^3`. On `[0, 0.5]`, `y(0)=1`, `y(0.5)=2`, and the missing initial slope is exactly 1.

```csharp
var result = NonlinearShooting.Solve(
    (_, y, _) => 2.0 * y * y * y,
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 0.5,
    rightValue: 2.0,
    step: 0.005,
    firstSlopeGuess: 0.5,
    secondSlopeGuess: 1.5,
    options: new NonlinearShootingOptions(
        BoundaryTolerance: 1e-10,
        MaximumIterations: 25));

if (result.Converged)
{
    Console.WriteLine(result.InitialSlope);          // approximately 1
    Console.WriteLine(result.FinalPoint.Y);          // approximately 2
    Console.WriteLine(result.RightBoundaryResidual); // close to zero
}
```

## Numerical interpretation

A small right-boundary residual proves only that the final RK4 trajectory lands close to the requested endpoint for the chosen discretization. It is not a global error estimate. Repeat the calculation with a smaller `step` and compare the interior trajectory when accuracy matters.

Secant shooting is also sensitive to the two starting slopes. Poor guesses can converge slowly, reach a numerical breakdown when two residuals become indistinguishable, or find a different solution when the nonlinear boundary-value problem has multiple solutions. The current API intentionally exposes these outcomes instead of silently hiding them.

## Scope

The V1 API covers scalar explicit second-order equations with value conditions at both endpoints. It does not yet generalize shooting to Neumann/Robin conditions, multiple shooting, continuation or stiff BVP solvers. Those are potential post-V1 extensions rather than requirements for historical compatibility.
