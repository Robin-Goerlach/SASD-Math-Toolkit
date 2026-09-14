# RK4 for scalar second-order ODEs

## Purpose

`RungeKutta.FourthOrderSecondOrder` solves scalar second-order initial-value problems of the form

`y'' = g(x, y, y')`

with initial conditions for both `y(x0)` and `y'(x0)`.

This closes the historical V1 second-order RK4 convenience item while keeping a modern C# API. The implementation is independent new code based on the mathematical method; it does not copy historical Borland source or handbook text.

## Reduction to a first-order system

A second-order equation does not require a different Runge-Kutta formula. Introduce

`v = y'`.

Then

`y' = v`

`v' = g(x, y, v)`.

The SASD API deliberately performs exactly this transformation and delegates to the existing first-order system RK4 core. This is an architectural choice: there is one RK4 system implementation to test and maintain, while the second-order method is a type-safe convenience layer.

## Result model

Every returned `SecondOrderOdePoint` contains:

- `X` — the independent-variable value;
- `Y` — the solution value `y`;
- `FirstDerivative` — the simultaneously integrated value `y'`.

Returning the derivative is important because it is part of the state of a second-order initial-value problem and is often needed for physical models such as velocity, angular velocity or current.

## Step behavior

The method uses the caller's fixed nominal RK4 step. If the remaining distance to `xEnd` is shorter, only the final step is shortened so the trajectory reaches the requested end point exactly.

This differs intentionally from the Adams multistep implementation, where uniform spacing is mathematically required and the whole grid is adjusted instead.

## Validation and numerical failure

Initial values, end points and step size are validated explicitly. The shared RK4 system core also verifies that derivative vectors have the correct dimension and that intermediate values remain finite. A non-finite derivative or state is reported instead of allowing `NaN` or infinity to silently contaminate the remainder of the trajectory.

## Example

For the harmonic oscillator

`y'' = -y`, `y(0)=0`, `y'(0)=1`,

the exact solution is `y=sin(x)` and `y'=cos(x)`.

```csharp
var points = RungeKutta.FourthOrderSecondOrder(
    (_, y, _) => -y,
    x0: 0.0,
    y0: 0.0,
    firstDerivative0: 1.0,
    xEnd: Math.PI / 2.0,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.Y);               // approximately 1
Console.WriteLine(final.FirstDerivative); // approximately 0
```

## Scope

This API covers one scalar second-order equation. The general nth-order convenience API and a dedicated convenience API for coupled second-order systems remain separate V1 milestones. They will reuse the same first-order system foundation rather than introduce duplicate RK4 implementations.
