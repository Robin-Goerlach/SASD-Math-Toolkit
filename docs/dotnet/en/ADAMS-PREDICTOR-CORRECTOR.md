# Adams-Bashforth / Adams-Moulton predictor-corrector

## Purpose

`AdamsBashforthMoulton.Integrate` solves scalar first-order initial-value problems

`y' = f(x, y),   y(x0) = y0`

with the classical fourth-order Adams predictor-corrector pair. This closes the historical V1 Adams-Bashforth/Adams-Moulton item with a C# API that keeps the numerical method explicit and inspectable.

The implementation is independent clean-room code based on the published mathematical method. Historical Borland/Pascal source and handbook prose are not copied.

## Why a multistep method is different

Runge-Kutta methods construct each new point mainly from derivative evaluations within the current step. Adams methods reuse derivative values from previous accepted points. After startup, this can reduce the number of new derivative evaluations needed per step, but it also means the method depends on a regular history grid.

The implementation uses the four-step Adams-Bashforth predictor

`y(n+1)^p = y(n) + h/24 * [55 f(n) - 59 f(n-1) + 37 f(n-2) - 9 f(n-3)]`

followed by the fourth-order Adams-Moulton corrector

`y(n+1) = y(n) + h/24 * [9 f(n+1) + 19 f(n) - 5 f(n-1) + f(n-2)]`.

The predicted value supplies the first estimate of `f(n+1)`. By default one correction pass is performed. Additional fixed-point correction passes can be requested with `correctorIterations`.

## RK4 startup

A four-step Adams formula cannot start from one initial point because it needs derivative history. The first three intervals are therefore generated with classical RK4. The scalar RK4 implementation exposes an internal single-step primitive so the startup and the public RK4 routine share the same formula rather than maintaining two copies.

If the requested interval contains fewer than four steps, the solver naturally remains in the RK4 startup phase because an AB4 history does not yet exist.

## Uniform grid and the meaning of `maximumStep`

Multistep coefficients assume equal spacing. A shortened last step would invalidate the fixed AB4/AM4 coefficients. Therefore `maximumStep` is treated as a maximum requested spacing rather than an exact spacing.

For interval length `L`, the solver chooses

`N = ceil(L / maximumStep)`

and then uses the uniform step

`h = L / N`.

This guarantees `h <= maximumStep`, preserves a uniform history, and reaches `xEnd` exactly without a special short final Adams step.

## Numerical behavior and validation

The reference implementation deliberately favors clear textbook structure over optimization. It validates finite scalar inputs, positive step size and correction count, and reports non-finite derivative or solution values with `ArithmeticException` rather than continuing with `NaN` data.

The method is fixed-step and does not provide an embedded local-error estimator. When error control is more important than a regular grid, `RungeKuttaFehlberg.Integrate` is generally the better current API.

## Future extensions

Possible later extensions include system-valued Adams methods, variable-order/variable-step formulas, reusable derivative-history objects, convergence-controlled implicit correction, and stiff multistep methods. Those are intentionally outside the V1 compatibility implementation.
