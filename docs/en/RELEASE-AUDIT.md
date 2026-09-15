# Release-candidate audit

This document records the repository-wide audit for the first public 1.0 line. The audit is performed against the real modernization release candidate rather than the earlier historical-only baseline.

## Release boundary

The candidate contains the completed classical numerical-methods foundation plus the M3.1 modern dense and M3.2 sparse foundations. M3.3 and later modernization work remains post-1.0 scope unless an audit finding requires a narrowly targeted fix.

The package identifier for the audit candidate is `1.0.0-rc.1`. A final `1.0.0` tag/release is intentionally deferred until an independent Windows acceptance run is complete.

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
- **Visual inspection:** the RC1 artifact was rendered page-by-page. The English edition contains 81 PDF pages and the German edition 79 PDF pages. Contact-sheet review of every rendered page plus full-size inspection of release-critical pages found no clipped text, overlaps, black squares, broken glyphs or missing chapter content. Title pages, contents, chapters 12/13, appendices and provenance pages render correctly.
- **Committed editions:** the regenerated English and German RC1 PDFs are synchronized with the current Markdown/LaTeX release-candidate content.

## Explicitly non-blocking technical debt

Complete CS1591 enforcement is not enabled for this release candidate. Modern public APIs added during the modernization work are documented, while some older public surface still relies on the existing handbook/technical documentation rather than complete XML comments. The suppression remains visible in the project file instead of pretending that coverage is complete.

CSC storage, incomplete Cholesky/ILU-class preconditioners, additional Krylov methods, benchmark-derived performance claims and optional BLAS/LAPACK backends are intentionally outside this release boundary.

## Remaining release gates

1. Run the final Windows acceptance procedure against the exact release-candidate commit after all audit fixes are merged.
2. Resolve any findings from that acceptance run.
3. Only with explicit release authorization: remove the `-rc.1` suffix, create the final tag/release and decide whether/where to publish the NuGet package.

## Release principle

A green build alone is not the release criterion. The candidate must have a green Release build/test/package/sample pipeline, synchronized documentation, independently checked PDFs and a successful consumer-machine acceptance run.
