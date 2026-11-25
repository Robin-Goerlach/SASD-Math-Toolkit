# Migration from `sasd-toolbox-dotnet`

The Math Toolkit is the canonical home for reusable numerical functionality.
The source repository remains untouched until this checklist is complete; it
must not be deleted merely because an item has a similarly named replacement.

| Source feature | Canonical decision | C++ status | Later .NET status |
| --- | --- | --- | --- |
| `Guard` | Do not expose a general utility API. Absorb numerical precondition checks into each implementation. | Foundation validation only | Existing internal `NumericGuard` remains canonical |
| Mean, median, variance | Migrate with stronger overflow handling, finite-input validation and explicit sample/population semantics. | CPP-M1 | Port after the C++ contract stabilizes |
| Two-point linear interpolation | Add to the interpolation domain without duplicating Lagrange interpolation. | CPP-M3 implementation complete | Planned after C++ |
| Bisection root finder | Do not copy the narrower toolbox implementation. Use the Math Toolkit's richer status-bearing contract. | CPP-M2 implementation complete | Existing implementation is canonical |
| Demo and repository scaffolding | Do not migrate; they are not reusable mathematics. | Not applicable | Not applicable |

Before the source repository can be retired:

1. every desired feature has a mapped implementation, tests and documentation;
2. C++ and subsequent .NET conformance cases agree on the public mathematics;
3. package consumers and repository links no longer depend on the source;
4. license and attribution records remain intact;
5. the repository is archived first and deleted only by an explicit maintainer decision.

This is a semantic migration, not a line-by-line source port. Implementations
are derived from the mathematical definitions and the canonical SASD contracts.
