# Ordinary differential equations

This chapter describes the currently stable C# APIs for initial-value problems. It grows as the remaining V1 ODE and boundary-value routines are implemented.

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

This API is especially useful when the original equation is naturally written in higher-order form. If your model is already a set of interacting first-order variables, use `FourthOrderSystem` directly instead of forcing it into an nth-order scalar representation.

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

A multistep formula needs history. SASD Math Toolkit therefore calculates the first three intervals with RK4 and then switches to the AB4/AM4 pair. One Adams-Moulton correction pass is the default. More passes can be requested with `correctorIterations`, although extra passes are not automatically better for every problem.

### Why `maximumStep` is not always the actual step

The AB4/AM4 coefficients require equal spacing. If `maximumStep` does not divide the interval exactly, the solver does **not** append a shortened final step. Instead it selects the smallest integer step count that respects the maximum and spreads those steps evenly over the complete interval.

For example, integrating from 0 to 1 with `maximumStep: 0.3` produces four intervals of 0.25. This keeps the multistep history valid and still reaches 1 exactly.

## Choosing between the methods

Use RK4 when a simple fixed-step reference calculation is desirable. Use RKF45 when automatic local error control and variable steps are more important. Use Adams-Bashforth/Moulton when a regular grid and derivative-history reuse fit the problem well. For scalar second- or higher-order equations, the typed RK4 convenience APIs are clearer than manually packing derivatives into a generic state array unless you specifically need the general system interface.

None of these methods is a universal answer for stiff differential equations. If results change strongly when the step or tolerance is tightened, investigate numerical stability rather than assuming more printed digits imply more accuracy.

## Choosing RKF tolerances

Tighter tolerances generally require more derivative evaluations. Do not request extreme tolerances merely because `double` has many printed digits: truncation error, round-off error and the conditioning of the differential equation still matter.

A useful workflow is to solve once with practical tolerances, repeat with stricter tolerances and compare the quantity you actually care about. If the answer changes materially, the first computation was not yet numerically settled.

## Interpreting rejected RKF steps

Rejected steps are not failures by themselves. They are part of normal adaptive integration. A few rejections often mean the controller is learning an appropriate step size. A large rejection count may indicate an aggressive initial step, rapidly changing dynamics or tolerances that are expensive for this method.

`MinimumStepSizeReached` is different: it means RKF45 could not satisfy the requested local tolerance without violating your configured minimum step. Reducing the minimum step may help, but the equation may also need a different numerical method.

## Current limits

The adaptive implementation is for scalar first-order equations and forward integration. RK4 supports scalar first-order, scalar second-order, scalar nth-order and coupled first-order systems. The Adams implementation is scalar and fixed-step. A dedicated convenience API for coupled second-order systems and linear/nonlinear shooting methods are still V1 work items and will be documented here only after their public APIs exist.
