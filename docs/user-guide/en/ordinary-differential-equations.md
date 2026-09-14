# Ordinary differential equations

This chapter describes the currently stable C# APIs for first-order initial-value problems. It will grow as the remaining V1 ODE and boundary-value routines are implemented.

## The problem

A first-order initial-value problem specifies a derivative and one starting value:

`y' = f(x, y)` and `y(x0) = y0`.

The toolkit currently offers fixed-step fourth-order Runge-Kutta (RK4) and adaptive Runge-Kutta-Fehlberg 4(5) (RKF45).

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

## Choosing tolerances

Tighter tolerances generally require more derivative evaluations. Do not request extreme tolerances merely because `double` has many printed digits: truncation error, round-off error and the conditioning of the differential equation still matter.

A useful workflow is to solve once with practical tolerances, repeat with stricter tolerances and compare the quantity you actually care about. If the answer changes materially, the first computation was not yet numerically settled.

## Interpreting rejected steps

Rejected steps are not failures by themselves. They are part of normal adaptive integration. A few rejections often mean the controller is learning an appropriate step size. A large rejection count may indicate an aggressive initial step, rapidly changing dynamics or tolerances that are expensive for this method.

`MinimumStepSizeReached` is different: it means RKF45 could not satisfy the requested local tolerance without violating your configured minimum step. Reducing the minimum step may help, but the equation may also need a different numerical method.

## Current limits

The current adaptive implementation is for scalar first-order equations and forward integration. RK4 already supports coupled first-order systems. Higher-order convenience APIs, Adams predictor-corrector methods and linear/nonlinear shooting methods are still V1 work items and will be documented here only after their public APIs exist.
