# Release-candidate audit

This document records the repository-wide audit for the first public 1.0 line. The audit is performed against the real modernization release candidate rather than the earlier historical-only baseline.

## Release boundary

The candidate contains the completed classical numerical-methods foundation plus the M3.1 modern dense and M3.2 sparse foundations. M3.3 and later modernization work remains post-1.0 scope unless an audit finding requires a narrowly targeted fix.

The package identifier for the audit candidate is `1.0.0-rc.1`. The independent Windows acceptance run has completed successfully. A final `1.0.0` package/tag/release is intentionally deferred until explicit release authorization.

## Findings addressed in audit phase 1

- **Package identity:** the development-only `0.1.0` package version is replaced by the explicit `1.0.0-rc.1` candidate identity.
- **NuGet metadata:** title, repository/project URLs, tags, license acceptance policy, package README and symbol-package settings are explicit.
- **Package verification:** normal CI now builds the `.nupkg`/`.snupkg` and checks the package for the assembly, XML documentation, README, package ID and MIT license expression.
- **Executable smoke path:** CI runs the sample after tests, verifies that the generated HTML contains SVG output and that the modern sparse reference solve executes.
- **Workflow runtime:** the normal .NET workflow uses the current Node-24-generation action majors verified during the audit (`actions/checkout@v7`, `actions/setup-dotnet@v6`) instead of the deprecated Node-20-generation majors.
- **Repository cleanliness:** generated release artifacts are ignored under `artifacts/`.
- **Implementation placeholders:** the audit found no obvious `NotImplementedException`/TODO-style public implementation placeholders in the repository search. This is a release review observation, not a proof that future work is exhausted.
- **License/provenance:** MIT licensing and the clean-room historical-reference policy remain consistent with the repository's stated implementation strategy.

## Findings addressed in audit phase 2 — handbook/PDF gate

- **Handbook synchronization:** the LaTeX build now includes Markdown handbook chapters 12 and 13, covering modern dense and sparse linear algebra, and the release-facing front/back matter identifies the `1.0.0-rc.1` candidate.
- **Deterministic publication path:** changes to Markdown handbook chapters, LaTeX sources or the handbook workflow rebuild both editions on `main`.
- **PDF validation:** CI verifies that both generated PDF files are non-empty and structurally readable with `pdfinfo` before publication.
- **Release inspection artifact:** each handbook build uploads both generated PDFs as a retained GitHub Actions artifact so the exact build outputs can be inspected independently of the committed copies.
- **CI mutation control:** handbook CI is read-only. It builds, validates and uploads PDFs but does not auto-commit regenerated binaries to `main`; PDF producer metadata can otherwise create binary churn without editorial changes. Committed PDF snapshots are updated deliberately at RC/release boundaries after inspection.
- **Visual inspection:** the RC1 artifact was rendered page-by-page. The English edition contains 81 PDF pages and the German edition 79 PDF pages. Contact-sheet review of every rendered page plus full-size inspection of release-critical pages found no clipped text, overlaps, black squares, broken glyphs or missing chapter content. Title pages, contents, chapters 12/13, appendices and provenance pages render correctly.
- **Committed editions:** the regenerated English and German RC1 PDFs are synchronized with the current Markdown/LaTeX release-candidate content.

## Findings addressed in audit phase 3 — Windows acceptance

The independent Windows acceptance was performed against exact commit `29deb0922e6384c7ff0592e9156d2250b698cfd7` on 2026-09-16.

- **Environment:** Windows x64, .NET SDK `10.0.303`, .NET runtime `10.0.11`. This complements CI coverage on Ubuntu 24.04 with SDK `10.0.401` and runtime `10.0.12`.
- **Restore/build:** `dotnet restore` and the Release build completed successfully.
- **Tests:** `178/178` tests passed, with zero failed and zero skipped tests.
- **Executable demo:** the deterministic HTML/SVG report was generated successfully and the modern sparse reference solve completed with GMRES in 17 iterations, BiCGSTAB in 10 iterations and residual `1.40225E-09`.
- **Package production:** Windows `dotnet pack --no-build` produced both `Sasd.Math.Toolkit.1.0.0-rc.1.nupkg` and `Sasd.Math.Toolkit.1.0.0-rc.1.snupkg` successfully.
- **Repository cleanliness:** `git status --short` was empty after the acceptance procedure.

No release-blocking finding was produced by the Windows acceptance run.

## Explicitly non-blocking technical debt

Complete CS1591 enforcement is not enabled for this release candidate. Modern public APIs added during the modernization work are documented, while some older public surface still relies on the existing handbook/technical documentation rather than complete XML comments. The suppression remains visible in the project file instead of pretending that coverage is complete.

CSC storage, incomplete Cholesky/ILU-class preconditioners, additional Krylov methods, benchmark-derived performance claims and optional BLAS/LAPACK backends are intentionally outside this release boundary.

## Remaining release gates

1. Obtain explicit release authorization for the final `1.0.0` transition.
2. Remove the `-rc.1` suffix and run the final Release build/test/package/sample and handbook gates against the resulting commit.
3. Create the final `1.0.0` tag/GitHub release only after that final CI run is green.
4. Decide separately whether and where the NuGet package is published.

## Release principle

A green build alone is not the release criterion. The candidate must have a green Release build/test/package/sample pipeline, synchronized documentation, independently checked PDFs and a successful consumer-machine acceptance run. RC1 has now passed those candidate gates; promotion to `1.0.0` remains an explicit release decision.
