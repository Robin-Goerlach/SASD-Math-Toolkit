# Adaptive Runge-Kutta-Fehlberg 4(5)

## Purpose

`RungeKuttaFehlberg.Integrate` solves scalar first-order initial-value problems

`y' = f(x, y),   y(x0) = y0`

with an adaptive step size. This closes the historical V1 Runge-Kutta-Fehlberg item while exposing a modern C# result model and explicit configuration.

The implementation is an independent implementation of the classical mathematical method. It does not copy historical Borland/Pascal source code or handbook text.

## Embedded 4(5) pair

Each trial step evaluates six derivative stages shared by a fourth-order and a fifth-order Runge-Kutta estimate. The absolute difference between those two estimates is used as a local error estimate. Accepted steps retain the fifth-order estimate.

The local acceptance threshold combines absolute and relative tolerances:

`absoluteTolerance + relativeTolerance * max(|y_old|, |y_new|)`

This keeps values near zero protected by an absolute floor while scaling the permitted local error for larger solution magnitudes.

## Step-size controller

The error estimate behaves approximately like the fifth power of the step size. The controller therefore uses a fifth-root correction, multiplied by a safety factor. Step changes are clamped by `MinimumScaleFactor` and `MaximumScaleFactor` so one unusually easy or difficult trial cannot change the step size too violently.

`RungeKuttaFehlbergOptions` exposes:

- initial, minimum and maximum step sizes;
- absolute and relative local-error tolerances;
- maximum accepted plus rejected step attempts;
- safety, minimum-scale and maximum-scale factors.

The implementation deliberately favors explicit textbook structure and diagnostics over premature optimization.

## Result and failure reporting

`AdaptiveOdeResult` stores the initial point and every accepted `OdePoint`, plus accepted/rejected step counts. Expected adaptive termination conditions do not throw:

- `Completed` — the requested end point was reached;
- `MinimumStepSizeReached` — the tolerance would require a smaller permitted step;
- `MaximumStepAttemptsReached` — the configured work limit was exhausted;
- `NumericalBreakdown` — an intermediate value became non-finite or floating-point resolution prevented progress.

Invalid arguments and invalid option combinations still use ordinary .NET exceptions because they are programming/input errors rather than numerical outcomes.

## Current scope

The V1 implementation is intentionally scalar and forward in `x`, matching the first-order RKF milestone. System-valued adaptive RKF, dense output, event detection, stiffness detection and specialized stiff solvers are future extensions rather than hidden complexity in this first reference implementation.
