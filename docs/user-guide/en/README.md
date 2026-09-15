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
5. [Numerical integration](integration.md)
6. [Matrices and linear systems](matrices-linear-systems.md)
7. [Eigenvalues and eigenvectors](eigenvalues-eigenvectors.md)
8. [Ordinary differential equations and boundary-value problems](ordinary-differential-equations.md)
9. [Least-squares approximation](least-squares.md)
10. [FFT, convolution and correlation](fft-convolution-correlation.md)
11. [Numerical recipes, diagnostics and common failure modes](diagnostics-common-failure-modes.md)

The V1 handbook now covers all planned chapters. The remaining V1 work is a final cross-repository audit to make sure public APIs, examples, technical documentation, compatibility claims and release metadata agree with the implementation.

## Documentation layers

The user handbook emphasizes usage and interpretation. Technical design notes remain under `docs/en/`, while the source code contains XML/API comments. The compatibility matrix in `docs/en/BORLAND-V1-COMPATIBILITY.md` tracks historical V1 coverage separately from handbook completeness.
