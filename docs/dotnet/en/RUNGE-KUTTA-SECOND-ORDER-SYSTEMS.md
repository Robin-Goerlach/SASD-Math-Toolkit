# RK4 for coupled second-order ODE systems

## Purpose

`RungeKutta.FourthOrderSecondOrderSystem` solves coupled systems of the form

`Y'' = G(x, Y, Y')`

where `Y` is a vector of dependent variables. Initial values for both `Y(x0)` and `Y'(x0)` are required.

This closes the historical V1 coupled-second-order RK4 convenience item. The implementation is independent new C# code based on the standard mathematical reduction to first order; it does not copy historical Borland source code or handbook text.

## Reduction to a first-order system

For `m` second-order equations, introduce the first-derivative vector `V = Y'`. The problem becomes

`Y' = V`

`V' = G(x, Y, V)`.

Internally the solver packs the state as

`[Y0, ..., Y(m-1), V0, ..., V(m-1)]`

and delegates integration to the same tested first-order system RK4 core used by the other RK4 convenience APIs. The packing is deliberately hidden from callers.

## Callback model

The callback receives three arguments:

- `x`;
- a read-only vector containing the current equation values;
- a read-only vector containing the matching first derivatives.

It returns one second derivative per equation. The returned dimension must match the initial vectors. The callback receives copies of the value and derivative groups so accidental mutation cannot corrupt an RK4 stage state.

## Result model

Each `SecondOrderSystemOdePoint` stores an immutable snapshot with:

- `X`;
- `Values`;
- `FirstDerivatives`;
- `Dimension`;
- indexed helpers `GetValue(i)` and `GetFirstDerivative(i)`.

The constructor copies both vectors, so later mutation of caller-owned arrays cannot modify an already returned trajectory.

## Example: two coupled oscillators

```csharp
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
```

The equations are `y0'' = -(y0-y1)` and `y1'' = +(y0-y1)`. This example is genuinely coupled because each acceleration depends on both current values.

## Validation and numerical failure

Initial value and derivative vectors must be non-empty, finite and equal in dimension. The acceleration callback must return the same dimension. Non-finite intermediate values are rejected by the shared RK4 system core rather than silently propagating `NaN` or infinity.

## Scope

This is a fixed-step explicit RK4 convenience API. It is not intended as a specialized solver for stiff mechanical systems, constrained dynamics, symplectic integration or differential-algebraic equations. Those are possible post-V1 extensions and should not complicate the reference implementation prematurely.
