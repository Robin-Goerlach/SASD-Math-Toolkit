# Ordinary differential equations and boundary-value problems

This chapter describes the currently stable C# APIs for initial-value and boundary-value problems. It grows as the remaining V1 routines are implemented.

## First-order initial-value problems

A scalar first-order problem specifies

`y' = f(x, y)` and `y(x0) = y0`.

The toolkit currently offers fixed-step fourth-order Runge-Kutta (RK4), adaptive Runge-Kutta-Fehlberg 4(5) (RKF45), and a fourth-order Adams-Bashforth/Adams-Moulton predictor-corrector method.

## Fixed-step RK4

Use RK4 when you deliberately want a predictable, fixed nominal step size and already know a suitable resolution.

```csharp
using Sasd.Numerics.DifferentialEquations;

var points = RungeKutta.FourthOrder(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    step: 0.01);

Console.WriteLine(points[^1].Y); // approximately e
```

The solver shortens only the final step when necessary so the trajectory ends exactly at `xEnd`.

## Second-order equations with RK4

A scalar second-order problem has the form

`y'' = g(x, y, y')`

and needs two initial values: `y(x0)` and `y'(x0)`. You can always rewrite such a problem manually as a two-component first-order system. `FourthOrderSecondOrder` performs that standard transformation for you and returns both `y` and `y'`.

```csharp
using Sasd.Numerics.DifferentialEquations;

// Harmonic oscillator: y'' = -y, y(0)=0, y'(0)=1.
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

This is a convenience API, not a second independent RK4 implementation. Internally the equation is transformed to `y'=v`, `v'=g(x,y,v)` and passed through the same tested system RK4 core.

## Nth-order equations with RK4

For a scalar problem of order `n`, write the equation so the highest derivative is isolated:

`y^(n) = g(x, y, y', ..., y^(n-1))`.

`FourthOrderNthOrder` accepts the lower derivatives as one ordered initial state. Element zero is `y`, element one is `y'`, and so on. The number of elements defines the equation order.

```csharp
using Sasd.Numerics.DifferentialEquations;

// y''' = -y', with y(0)=0, y'(0)=1, y''(0)=0.
var points = RungeKutta.FourthOrderNthOrder(
    (_, state) => -state[1],
    x0: 0.0,
    initialState: [0.0, 1.0, 0.0],
    xEnd: Math.PI / 2.0,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.Y);                // approximately 1
Console.WriteLine(final.GetDerivative(1)); // approximately 0
Console.WriteLine(final.GetDerivative(2)); // approximately -1
```

Each `NthOrderOdePoint` stores a read-only snapshot `[y, y', ..., y^(n-1)]`. `GetDerivative(0)` is the same value as `Y`. The solver performs the standard companion-system transformation and then reuses the same RK4 system core; there is no separate Runge-Kutta formula for every possible order.

Use this API when the original equation is naturally written in higher-order form. If your model is already a set of interacting first-order variables, use `FourthOrderSystem` directly.

## Coupled second-order systems with RK4

Many physical models contain several second-order variables that influence one another. Write the system as

`Y'' = G(x, Y, Y')`.

`FourthOrderSecondOrderSystem` keeps values and first derivatives in separate vectors at the API boundary, while internally reducing the problem to one ordinary first-order RK4 system.

```csharp
using Sasd.Numerics.DifferentialEquations;

var end = Math.PI / (2.0 * Math.Sqrt(2.0));
var points = RungeKutta.FourthOrderSecondOrderSystem(
    (_, values, _) =>
    {
        var difference = values[0] - values[1];
        return [-difference, difference];
    },
    x0: 0.0,
    initialValues: [1.0, -1.0],
    initialFirstDerivatives: [0.0, 0.0],
    xEnd: end,
    step: 0.01);

var final = points[^1];
Console.WriteLine(final.GetValue(0));
Console.WriteLine(final.GetFirstDerivative(0));
```

Both initial vectors must have the same dimension, and the callback must return exactly one second derivative per equation. `SecondOrderSystemOdePoint` copies its vectors, so later changes to caller-owned arrays cannot alter the computed trajectory.

## Linear boundary-value problems with shooting

A boundary-value problem can prescribe the solution at **both** ends instead of supplying an initial derivative. The linear shooting API handles

`y'' = p(x)y' + q(x)y + r(x)`

with `y(x0)=alpha` and `y(xEnd)=beta`.

```csharp
using Sasd.Numerics.DifferentialEquations;

// y'' = 2, y(0)=1, y(1)=4 -> y = 1 + 2x + x^2.
var result = LinearShooting.Solve(
    _ => 0.0,
    _ => 0.0,
    _ => 2.0,
    x0: 0.0,
    leftValue: 1.0,
    xEnd: 1.0,
    rightValue: 4.0,
    step: 0.05);

Console.WriteLine(result.InitialSlope);          // approximately 2
Console.WriteLine(result.FinalPoint.Y);          // approximately 4
Console.WriteLine(result.RightBoundaryResidual); // near zero
```

Linear shooting integrates a particular and a homogeneous sensitivity solution with RK4. Because the dependence on the unknown initial slope is linear, the final correction can be computed directly rather than by iteration.

`AuxiliaryRightValue` reports the sensitivity denominator. If its absolute value falls below `singularityTolerance`, the boundary map is treated as singular or numerically ill-conditioned instead of dividing by an unstable small number.

## Nonlinear boundary-value problems with shooting

For a nonlinear problem

`y'' = g(x, y, y')`

with `y(x0)=alpha` and `y(xEnd)=beta`, the right-end value generally depends nonlinearly on the unknown initial slope. `NonlinearShooting.Solve` therefore tries two initial slopes and uses the secant method to drive the boundary residual

`R(s) = y(xEnd; s) - beta`

toward zero.

```csharp
using Sasd.Numerics.DifferentialEquations;

// Exact solution y = 1/(1-x): y'' = 2y^3,
// y(0)=1 and y(0.5)=2, with exact initial slope 1.
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
else
{
    Console.WriteLine($"{result.Status}: {result.Message}");
}
```

A nonlinear shooting failure is not automatically an invalid input. `MaximumIterationsReached` means that the allowed secant corrections were exhausted; `NumericalBreakdown` can occur when the two residuals become too similar for a stable secant update. In both cases the result still exposes the final usable trajectory for diagnosis.

The solver verifies the actual right-boundary residual before reporting `Converged`. This matters because a slope sequence can stagnate even while the requested endpoint is still missed.

The two starting slopes matter. Nonlinear BVPs can have multiple solutions, and different guesses can lead to different roots of the shooting residual. For important calculations, document the guesses and repeat with alternative starting values when multiple solutions are plausible.

## Adaptive RKF45

Use RKF45 when the solution changes at different rates over the interval or when you prefer to state an error tolerance instead of manually choosing one fixed step size.

```csharp
using Sasd.Numerics.DifferentialEquations;

var result = RungeKuttaFehlberg.Integrate(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    new RungeKuttaFehlbergOptions
    {
        InitialStep = 0.25,
        MinimumStep = 1e-8,
        MaximumStep = 0.5,
        AbsoluteTolerance = 1e-10,
        RelativeTolerance = 1e-10
    });

if (result.Completed)
{
    Console.WriteLine(result.FinalPoint.Y);
    Console.WriteLine($"Accepted: {result.AcceptedSteps}, rejected: {result.RejectedSteps}");
}
else
{
    Console.WriteLine($"Stopped with {result.Status}: {result.Message}");
}
```

The adaptive solver computes two related estimates for every trial step. Their difference estimates local error. A trial is accepted when the estimate satisfies the configured absolute-plus-relative tolerance; otherwise the trial is rejected and retried with a smaller step.

## Adams predictor-corrector

The Adams method is useful when you want a regular output grid and the derivative is expensive enough that reusing earlier derivative values is attractive. The current implementation uses the fourth-order Adams-Bashforth predictor followed by the fourth-order Adams-Moulton corrector.

```csharp
using Sasd.Numerics.DifferentialEquations;

var points = AdamsBashforthMoulton.Integrate(
    (_, y) => y,
    x0: 0.0,
    y0: 1.0,
    xEnd: 1.0,
    maximumStep: 0.1);

Console.WriteLine(points[^1].Y); // approximately e
```

A multistep formula needs history. SASD Math Toolkit therefore calculates the first three intervals with RK4 and then switches to the AB4/AM4 pair. One Adams-Moulton correction pass is the default.

### Why `maximumStep` is not always the actual step

The AB4/AM4 coefficients require equal spacing. If `maximumStep` does not divide the interval exactly, the solver does **not** append a shortened final step. Instead it selects the smallest integer step count that respects the maximum and spreads those steps evenly over the complete interval.

## Choosing between the methods

Use RK4 when a simple fixed-step reference calculation is desirable. Use RKF45 when automatic local error control and variable steps are more important. Use Adams-Bashforth/Moulton when a regular grid and derivative-history reuse fit the problem well. Use linear shooting for a linear equation with values prescribed at both endpoints. Use nonlinear shooting when the equation itself is nonlinear and the missing initial slope must be found iteratively.

None of these methods is a universal answer for stiff differential equations. If results change strongly when the step or tolerance is tightened, investigate numerical stability rather than assuming more printed digits imply more accuracy.

## Boundary residual versus global accuracy

For both shooting solvers, a tiny `RightBoundaryResidual` only means that the final discrete trajectory lands close to the requested endpoint. It is not an error bound for the interior solution. Repeat the computation with a smaller RK4 `step` and compare the quantities you care about.

For nonlinear shooting, `BoundaryTolerance` controls the slope search while `step` controls RK4 discretization. Tightening only one of them does not automatically improve the other error source.

## Current limits

RK4 covers scalar first-order, scalar second-order, scalar nth-order, coupled first-order and coupled second-order systems. RKF45 is currently scalar and forward-only, and Adams is scalar with fixed spacing. The historical V1 linear and nonlinear shooting routines for scalar second-order Dirichlet boundary-value problems are now both implemented. More general Neumann/Robin conditions, multiple shooting, continuation methods and dedicated stiff BVP solvers remain outside the current V1 compatibility target.
