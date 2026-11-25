# RK4 for scalar nth-order ODEs

## Purpose

`RungeKutta.FourthOrderNthOrder` solves scalar initial-value problems of arbitrary order written as

`y^(n) = g(x, y, y', ..., y^(n-1))`.

The caller supplies the initial state

`[y(x0), y'(x0), ..., y^(n-1)(x0)]`.

This closes the historical V1 nth-order RK4 convenience item with a modern C# API. The implementation is new SASD code based on the standard mathematical reduction to a first-order system; it does not copy historical Borland source code or handbook text.

## Companion-system reduction

Define the state components

`z0 = y`

`z1 = y'`

`...`

`z(n-1) = y^(n-1)`.

Then the nth-order scalar equation becomes the first-order system

`z0' = z1`

`z1' = z2`

`...`

`z(n-2)' = z(n-1)`

`z(n-1)' = g(x, z0, z1, ..., z(n-1))`.

`FourthOrderNthOrder` builds exactly this companion system and delegates the numerical work to the same RK4 system core already used by the first- and second-order APIs. There is therefore one RK4 stage implementation to maintain and test.

## API contract

The `highestDerivative` callback receives the current `x` plus a read-only state in the fixed order

`[y, y', y'', ..., y^(n-1)]`.

The number of elements in `initialState` defines `n`. The state must not be empty and every initial value must be finite.

For example, a third-order problem needs

`initialState: [y0, yPrime0, yDoublePrime0]`.

The callback returns only the highest derivative `y'''` for that state.

## Result model

Every returned `NthOrderOdePoint` contains:

- `X` — the independent-variable value;
- `Y` — shorthand for derivative order zero;
- `Order` — the number of stored state components and therefore the ODE order;
- `State` — a read-only snapshot `[y, y', ..., y^(n-1)]`;
- `GetDerivative(k)` — direct access by mathematical derivative order.

The result object copies the state produced by the solver. This prevents later array mutation from silently changing an already returned trajectory.

## Example: third-order sinusoid

Consider

`y''' = -y'`

with

`y(0)=0`, `y'(0)=1`, `y''(0)=0`.

The exact solution is `y=sin(x)`.

```csharp
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

## Step behavior and validation

The method uses the requested fixed nominal RK4 step and shortens only the final step when needed to end exactly at `xEnd`. The shared RK4 core checks that the companion-system derivative dimension remains correct and rejects non-finite intermediate values.

Invalid configuration such as an empty initial state, a non-positive step, non-finite initial values or an end point not greater than the start is reported with ordinary argument exceptions. A non-finite derivative generated during integration is a numerical failure and is reported as an `ArithmeticException`.

## Scope

This is a scalar nth-order convenience API. Coupled first-order systems already use `FourthOrderSystem`; a dedicated convenience API for coupled second-order systems remains a separate V1 milestone. Specialized stiff methods, adaptive nth-order wrappers, event detection and dense output remain future extensions rather than hidden complexity in this reference implementation.
