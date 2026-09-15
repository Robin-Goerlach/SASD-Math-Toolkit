# SASD Math Toolkit User Handbook — C#/.NET

The first eleven chapters form the completed classical V1 handbook. New chapters now document the 2026 modernization layer.

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
12. [Modern dense linear algebra — 2026 additions](modern-dense-linear-algebra.md)
13. [Sparse linear algebra — CSR foundation](sparse-linear-algebra.md)

## Documentation source and PDF edition

**Markdown is the continuously maintained editorial source of truth.** New algorithms, examples and API explanations are written here first during development.

The committed V1.0 PDFs are generated from Markdown through `../LaTeX/`. The LaTeX directory supplies typesetting, front/back matter and chapter ordering; it is not a second independent copy of the technical prose. During active modernization the PDFs may intentionally lag behind Markdown. At the next release-candidate boundary the LaTeX chapter order will be synchronized and both PDF editions will be regenerated and visually checked.
