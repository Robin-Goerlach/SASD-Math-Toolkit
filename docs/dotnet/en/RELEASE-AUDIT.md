# Release audit — 1.0.0

This document records the repository-wide audit for the first stable 1.0 line. The audit was performed against the real modernization release candidate and then carried through the final `1.0.0` promotion.

## Release boundary

The 1.0 release contains the completed classical numerical-methods foundation plus the M3.1 modern dense and M3.2 sparse foundations. M3.3 and later modernization work remains post-1.0 scope unless a narrowly targeted maintenance fix is required.

The audited package identifier is `1.0.0`. The preceding candidate was `1.0.0-rc.1`; it completed the independent Windows acceptance before the stable promotion was authorized on 2026-09-16.

## Findings addressed in audit phase 1 — package and repository

- **Package identity:** the development-only `0.1.0` package version was first replaced by `1.0.0-rc.1`, then promoted to stable `1.0.0` after acceptance.
- **NuGet metadata:** title, repository/project URLs, tags, license acceptance policy, package README and symbol-package settings are explicit.
- **Package verification:** normal CI builds the `.nupkg`/`.snupkg` and checks the package for the assembly, XML documentation, README, package ID, exact version and MIT license expression.
- **Executable smoke path:** CI runs the sample after tests, verifies that the generated HTML contains SVG output and that the modern sparse reference solve executes.
- **Workflow runtime:** the normal .NET workflow uses the current Node-24-generation action majors verified during the audit (`actions/checkout@v7`, `actions/setup-dotnet@v6`).
- **Repository cleanliness:** generated release artifacts are ignored under `artifacts/`.
- **Implementation placeholders:** the audit found no obvious `NotImplementedException`/TODO-style public implementation placeholders in the repository search. This is a release-review observation, not a proof that future work is exhausted.
- **License/provenance:** MIT licensing and the clean-room historical-reference policy remain consistent with the repository's stated implementation strategy.

## Findings addressed in audit phase 2 — handbook/PDF gate

- **Handbook synchronization:** the LaTeX build includes Markdown handbook chapters 12 and 13, covering modern dense and sparse linear algebra.
- **Editorial source:** Markdown remains the continuously maintained source of truth; LaTeX supplies publication-specific front/back matter, chapter order and typesetting.
- **PDF validation:** CI verifies that both generated PDF files are non-empty and structurally readable with `pdfinfo`.
- **Inspection artifact:** handbook builds upload both PDFs as retained GitHub Actions artifacts so exact build outputs can be inspected independently of committed copies.
- **CI mutation control:** ordinary handbook CI is read-only. Generated PDF snapshots are committed deliberately only at reviewed RC/release boundaries.
- **RC1 visual inspection:** the release-candidate books were rendered page-by-page and reviewed without finding clipping, overlaps, black squares, broken glyphs or missing release-critical content.

## Findings addressed in audit phase 3 — Windows acceptance

The independent Windows acceptance was performed against exact RC1 code commit `29deb0922e6384c7ff0592e9156d2250b698cfd7` on 2026-09-16.

- **Environment:** Windows x64, .NET SDK `10.0.303`, .NET runtime `10.0.11`. This complemented CI coverage on Ubuntu 24.04 with SDK `10.0.401` and runtime `10.0.12`.
- **Restore/build:** `dotnet restore` and the Release build completed successfully.
- **Tests:** `178/178` tests passed, with zero failed and zero skipped tests.
- **Executable demo:** the deterministic HTML/SVG report was generated successfully and the modern sparse reference solve completed with GMRES in 17 iterations, BiCGSTAB in 10 iterations and residual `1.40225E-09`.
- **Package production:** Windows `dotnet pack --no-build` produced both RC1 package and symbol package successfully.
- **Repository cleanliness:** `git status --short` was empty after the acceptance procedure.

No release-blocking finding was produced by the Windows acceptance run.

## Findings addressed in audit phase 4 — stable 1.0.0 promotion

Explicit release authorization was received on 2026-09-16. The stable promotion commit is `dc714dc4948fea907f8ad64a86550f33b724aa8a`.

- **Stable package identity:** the `rc.1` suffix was removed; the project now packs as `Sasd.Math.Toolkit` version `1.0.0`.
- **Final Linux gate:** the promotion commit passed Release restore/build/test/package/sample CI on Ubuntu 24.04 with .NET SDK `10.0.401` and runtime `10.0.12`.
- **Build quality:** the final Release build completed with **0 warnings and 0 errors**.
- **Regression suite:** **178/178 tests passed**.
- **Final packages:** CI produced and verified `Sasd.Math.Toolkit.1.0.0.nupkg` and `Sasd.Math.Toolkit.1.0.0.snupkg`, including an explicit nuspec version check for `1.0.0`. The packages are retained as a release artifact.
- **Final smoke path:** the sample remained successful with GMRES in 17 iterations, BiCGSTAB in 10 iterations and residual `1.40225E-09`.
- **Final handbooks:** the German and English front matter now identify handbook version 1.0 and package version `1.0.0`. The final handbook build completed successfully, passed structural validation and produced retained release artifacts.
- **Final PDF snapshots:** the handbook workflow committed the reviewed final PDF snapshots in `618ba1b9ec8d4d0a3d053e8210e01c8bb3cff3b0`.
- **Final PDF inspection:** the German edition has 79 pages and the English edition 81 pages. All pages were rendered and contact-sheet reviewed; release-critical title/status pages were also inspected at full size. No clipping, overlap, broken glyphs or missing content was found. Comparison with RC1 showed the expected publication-metadata/footer changes without structural page-count changes.
- **Workflow hardening:** after the one-time final snapshot publication, handbook CI is restored to read-only operation.

## Explicitly non-blocking technical debt

Complete CS1591 enforcement is not enabled for 1.0.0. Modern public APIs added during the modernization work are documented, while some older public surface still relies on the existing handbook/technical documentation rather than complete XML comments. The suppression remains visible in the project file instead of pretending that coverage is complete.

CSC storage, incomplete Cholesky/ILU-class preconditioners, additional Krylov methods, benchmark-derived performance claims and optional BLAS/LAPACK backends are intentionally outside the 1.0 release boundary.

## Remaining publication steps

The audited `1.0.0` contents are complete. The remaining repository-publication step is to create the `v1.0.0` Git tag/GitHub Release against the final green release-finalization commit. Publication of the NuGet package is a separate decision and is not implied by the GitHub release.

## Release principle

A green build alone is not the release criterion. Version 1.0.0 reached the stable boundary only after Release build/test/package/sample validation, synchronized bilingual documentation, structural and visual PDF inspection, and an independent Windows acceptance run.
