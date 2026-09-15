# SASD Math Toolkit User Handbook — C#/.NET

This is the user-facing handbook for the SASD Math Toolkit. It is written specifically for the current C#/.NET API and grows in parallel with V1 quality work.

The handbook is **not** a transcription, translation or repackaging of the historical Borland Pascal handbook. The historical product is used only as a functional milestone for V1. Explanations, examples, API names, warnings and workflows in this handbook are newly written for SASD Math Toolkit.

## Intended audience

The handbook is for application developers, students and technically oriented users who want to apply the numerical routines without first reading the implementation source. Each chapter should answer four practical questions: what problem does the method solve, when should it be used, how is it called from C#, and what numerical limitations should be expected?

## Chapter structure

1. [Getting started](getting-started.md)
2. [Roots of equations](root-finding.md)
3. [Interpolation](interpolation.md)
4. [Numerical differentiation](differentiation.md)
5. Numerical integration
6. Matrices and linear systems
7. Eigenvalues and eigenvectors
8. [Ordinary differential equations and boundary-value problems](ordinary-differential-equations.md)
9. [Least-squares approximation](least-squares.md)
10. [FFT, convolution and correlation](fft-convolution-correlation.md)
11. Numerical recipes, diagnostics and common failure modes

Chapters are added or expanded when the corresponding implementation reaches a stable milestone. This keeps examples synchronized with real code instead of documenting planned APIs that do not yet exist.

## Documentation layers

The user handbook emphasizes usage and interpretation. Technical design notes remain under `docs/en/`, while the source code contains XML/API comments. The compatibility matrix in `docs/en/BORLAND-V1-COMPATIBILITY.md` tracks historical V1 coverage separately from handbook completeness.
