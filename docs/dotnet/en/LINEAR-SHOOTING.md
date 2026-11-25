# Linear shooting for second-order boundary-value problems

## Purpose

`LinearShooting.Solve` handles scalar linear Dirichlet boundary-value problems of the form

`y'' = p(x) y' + q(x) y + r(x)`

with prescribed endpoint values

`y(x0) = alpha` and `y(xEnd) = beta`.

This is an independent C# implementation of the mathematical shooting method. The historical Borland catalog is used only as a functional V1 target; no historical source code or handbook prose is copied.

## Why shooting is different from an initial-value problem

RK4 normally needs `y(x0)` and `y'(x0)`. A Dirichlet boundary-value problem instead gives `y(x0)` and `y(xEnd)`, so the initial slope is unknown. Shooting turns that missing slope into the quantity that must be chosen so an IVP trajectory lands on the requested right boundary.

For a **linear** equation the dependency on the missing slope is itself linear. SASD therefore needs only two RK4 integrations rather than an iterative root search.

## The two auxiliary problems

The solver computes a particular solution `u`:

`u'' = p u' + q u + r`, `u(x0)=alpha`, `u'(x0)=0`

and a homogeneous sensitivity solution `v`:

`v'' = p v' + q v`, `v(x0)=0`, `v'(x0)=1`.

The final solution is

`y = u + c v`

with

`c = (beta - u(xEnd)) / v(xEnd)`.

Because of the chosen auxiliary initial conditions, `c` is also the reconstructed initial slope `y'(x0)`.

Both auxiliary problems are integrated with `RungeKutta.FourthOrderSecondOrder`; the shooting implementation does not duplicate RK4 stage formulas.

## Result model

`LinearShootingResult` exposes:

- `Points` — immutable trajectory points containing `x`, `y` and `y'`;
- `InitialSlope` — the slope selected by the boundary condition;
- `FinalPoint` — convenient access to the right endpoint;
- `RightBoundaryResidual` — computed `y(xEnd) - beta`;
- `AuxiliaryRightValue` — the value `v(xEnd)` used as the shooting denominator.

The residual is useful when comparing step sizes. It can be very small even when the interior trajectory still has discretization error, so it must not be interpreted as a complete global error estimate.

## Singular or ill-conditioned boundary maps

The correction divides by `v(xEnd)`. If that value is zero, the chosen endpoint condition cannot determine the missing initial slope through this shooting construction. A very small value is numerically dangerous for the same reason.

`singularityTolerance` is therefore an explicit absolute threshold. The default is `1e-12`; applications with unusual scales should choose a threshold appropriate to their problem.

## Example

For

`y'' = 2`, `y(0)=1`, `y(1)=4`,

the exact solution is `y = 1 + 2x + x^2` and the missing initial slope is 2.

```csharp
var result = LinearShooting.Solve(
    _ => 0.0, // p(x)
    _ => 0.0, // q(x)
    _ => 2.0, // r(x)
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 1.0,
    rightValue: 4.0,
    step: 0.05);

Console.WriteLine(result.InitialSlope);            // approximately 2
Console.WriteLine(result.FinalPoint.Y);            // approximately 4
Console.WriteLine(result.RightBoundaryResidual);   // near zero
```

## Scope and limitations

The current API covers scalar, linear, explicit second-order equations with value conditions at both ends. It is not yet a general Robin/Neumann boundary-condition framework and it is not the nonlinear shooting solver.

The next historical V1 boundary-value milestone is nonlinear shooting, where the right-end value depends nonlinearly on the guessed initial slope and an iterative root method is required.
