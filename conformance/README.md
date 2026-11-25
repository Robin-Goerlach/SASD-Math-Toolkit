# Cross-language conformance

`conformance/` contains machine-readable reference cases that can be consumed
by every SASD Math Toolkit implementation. It complements the behavioral rules
in [`spec/`](../spec/) and must not contain C#, C++ or another language's API
shape.

## Planned organization

Reference vectors will be added with the numerical milestones that need them:

```text
conformance/
├── root-finding/
├── polynomials/
├── interpolation/
├── integration/
├── linear-algebra/
├── sparse/
├── eigen/
├── ode/
├── approximation/
└── transforms/
```

JSON is the preferred format for ordinary scalar, vector and matrix cases.
Specialized text or binary formats require a documented reason and a portable
reader strategy.

## Rules for a reference case

Each case should state:

- the stable algorithm identifier;
- all mathematical inputs and conventions;
- the expected value or invariant;
- an absolute and/or relative comparison tolerance with a reason;
- the expected termination status when iteration is involved;
- provenance for externally sourced constants or datasets.

Floating-point implementations are not required to be bit-identical. A case
passes when it satisfies the documented mathematical contract and tolerance.
No production implementation is allowed to generate its own expected values at
test time.

CPP-M0 established this location without invented numerical golden vectors.
Shared vectors are introduced only when two implementations can execute the
same meaningful cases; CPP-M1 statistics therefore defines its contract and
C++ tests first, with cross-language vectors following during the planned .NET
statistics implementation.
