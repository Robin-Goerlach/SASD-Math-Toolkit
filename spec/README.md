# Language-neutral specification

`spec/` is the contract layer for the C#/.NET, C++, Fortran and JavaScript
implementations.

This directory describes mathematical behavior rather than language syntax.
For example, a dimension mismatch is invalid input and must be rejected; the
contract does not require every language to use the same exception class.

Planned machine-readable material:

- algorithm identifiers and capability metadata
- common status/error semantics
- tolerance rules for conformance tests
- serialization format for matrices, vectors, complex values and sampled signals

Machine-readable inputs and expected outputs belong in [`../conformance/`](../conformance/).
Keeping the contract and the executable data separate makes it possible to
explain a rule once while providing several focused datasets for it.

The English C# compatibility matrix remains the human-readable source of truth
for the V1 algorithm catalog until each subject has a dedicated neutral
contract. Language-specific APIs, ownership rules and exception types belong in
that language's documentation, not here.

Dedicated contracts introduced by the C++ implementation:

- [Root finding and polynomials](root-finding.md)
- [Interpolation](interpolation.md)
