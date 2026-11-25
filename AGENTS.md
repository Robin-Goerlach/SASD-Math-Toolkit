# SASD Math Toolkit - Codex instructions

## Mission

SASD Math Toolkit is a long-lived mathematical foundation for SASD projects. The C#/.NET implementation is currently the reference implementation, but the repository is intentionally language-neutral so later implementations can coexist under `src/<language>/`, `tests/<language>/`, and `samples/<language>/`.

Work autonomously on normal development, review, debugging, tests, and documentation. Prefer small, complete, reviewable changes over broad speculative rewrites. Do not ask for approval for routine implementation details when the repository and task make the intended direction clear.

## Stable release boundary

- `v1.0.0` is an immutable historical release boundary.
- The released commit is `e4eb8e4fb63803af060569b305daece87c9cfe7a`.
- Never move or rewrite `v1.0.0`, replace its release assets, rewrite published history, force-push, or publish packages/releases without explicit user authorization.
- New functionality belongs to post-1.0 development. Maintenance fixes must preserve the published 1.0 contract unless the task explicitly authorizes a breaking change.

## Clean-room rule

Historical Borland/Turbo Pascal numerical-toolbox material may be used only as a high-level functional catalog or historical orientation.

Do not copy or port historical source code. Do not reproduce substantial historical manual text. Implement algorithms independently from mathematical definitions, primary references, and the existing SASD contracts. Preserve this separation in code comments and documentation.

## Repository map

- `src/dotnet/Sasd.Math.Toolkit/` - C#/.NET reference implementation.
- `src/cpp/` - independent modern C++ implementation and CMake package.
- `tests/dotnet/Sasd.Math.Toolkit.Tests/` - executable numerical regression tests.
- `tests/cpp/` - C++/CTest verification and installed-package consumer.
- `samples/dotnet/Sasd.Math.Toolkit.Sample/` - demonstrations and smoke paths; no reusable library logic.
- `samples/cpp/` - C++ examples; no reusable library logic.
- `spec/` - language-neutral contracts/reference data as they are introduced.
- `conformance/` - language-neutral machine-readable reference cases.
- `docs/dotnet/en/`, `docs/dotnet/de/` - .NET technical documentation.
- `docs/cpp/en/`, `docs/cpp/de/` - independent C++ technical documentation.
- `docs/user-guide/dotnet/en/`, `docs/user-guide/dotnet/de/` - continuously maintained .NET Markdown handbook sources.
- `docs/user-guide/LaTeX/<language>/` - language-specific publication/typesetting layers; regenerate committed PDFs only at deliberate RC/release boundaries unless the task explicitly requests otherwise.
- `eng/codex-verify.sh` - canonical local verification entry point for Codex.
- `eng/cpp-verify.sh` - focused C++ build/test/install verification entry point.

## Read before editing

At the start of a development task:

1. Inspect `git status -sb` and the current branch. Preserve all pre-existing user changes; never discard or overwrite unrelated work.
2. Read the root `README.md` plus the relevant sections of `docs/dotnet/en/ROADMAP.md`, `docs/dotnet/en/MODERNIZATION-2026.md`, and `docs/dotnet/en/ARCHITECTURE.md`.
3. Inspect the relevant implementation, tests, and corresponding German/English documentation before designing the change.
4. Search for existing abstractions and terminology before introducing new public types.
5. Treat the current repository as truth when older prompts or notes disagree with it.

If the working tree already contains unrelated changes, work around them. If the requested task overlaps those changes and cannot be completed safely without interpreting the user's unfinished work, stop and report the conflict instead of resetting anything.

## Numerical engineering rules

For numerical algorithms, explicitly consider as applicable:

- finite-input validation and dimension validation;
- NaN/Infinity from callbacks and intermediate computations;
- overflow/underflow and scaled formulations;
- conditioning and scale-aware thresholds;
- explicit absolute/relative tolerances rather than mutable global accuracy state;
- convergence criteria, iteration limits, and breakdown states;
- independently recomputed quality metrics such as true residuals, reconstruction errors, orthogonality defects, or known analytic solutions;
- deterministic behavior where practical;
- defensive ownership/copies for completed result state;
- preservation of mathematical invariants.

Expected numerical termination should normally use status-bearing result types. Programmer errors and invalid API contracts should normally use ordinary .NET exceptions. Reuse the existing `IterationStatus` / result-model conventions when they fit; do not force unrelated domain-specific termination states into one oversized enum.

Do not claim a performance improvement unless it was measured. Prefer readable managed reference implementations, good data layout, reuse of decompositions/buffers, and reduced avoidable allocation before unsafe code, SIMD, parallelism, or native backends.

## API and implementation style

- Target .NET 10 and keep nullable analysis, warnings-as-errors, and deterministic builds working.
- Keep public API changes deliberate and minimal; public APIs are expensive to change after release.
- Public APIs need useful XML documentation. Complex numerical code also needs normal comments explaining *why* a formulation/check exists.
- Use existing namespaces and sibling-domain structure. Do not mix application/UI/file I/O into numerical library code.
- Keep the managed implementation functional if optional native backends are introduced later.
- Do not silently weaken validation, immutability, defensive-copy, or residual semantics for speed.
- Avoid new dependencies unless they provide clear mathematical/engineering value and are justified in the change.

## Tests

Every new algorithm or substantial behavior change should cover the cases that apply:

- happy path and a known reference solution;
- independent numerical quality validation;
- boundary/degenerate cases;
- invalid dimensions and malformed inputs;
- NaN/Infinity behavior;
- breakdown/non-convergence and iteration limits;
- defensive ownership where result state exposes arrays/matrices;
- at least one meaningful deterministic case beyond toy 2x2/3x3 examples when scale matters.

A test should validate the mathematical result, not merely reproduce the implementation's internal recurrence.

## Documentation

- Update Markdown documentation with the code in the same development task.
- Keep German and English technical/user documentation aligned when user-facing behavior changes.
- Markdown is the editorial source of truth during active development.
- Do not regenerate/commit handbook PDFs for ordinary feature commits.
- Keep language-specific code/documentation separate. Language-neutral contracts belong in `spec/`.
- The active implementation sequence is C#/.NET reference, then C++, then Fortran and JavaScript. Do not start the latter two before the C++ edition is complete unless the maintainer changes that decision.

## Verification workflow

During implementation, run:

```bash
./eng/codex-verify.sh quick
```

Before declaring a coding task complete or pushing a normal development commit, run:

```bash
./eng/codex-verify.sh full
```

If a verification step fails, investigate the root cause, fix it, and rerun the failed/full gate. Do not report a milestone as complete with a red gate. Do not suppress compiler warnings to make the gate green.

For documentation/config-only changes that cannot affect compiled behavior, at minimum run the relevant syntax/format checks and `git diff --check`; use the full gate when there is reasonable doubt.

## Git workflow and autonomy

The maintainer permits normal project development directly on `main`.

- Do not create a branch unless the user asks for one or the execution environment requires a worktree/branch.
- Never force-push, rewrite history, amend published commits, or move release tags.
- Keep commits small and coherent with clear messages.
- For an implementation task, continue through implementation, tests, documentation, verification, commit, and normal push to `origin/main` without asking for approval at each intermediate step, unless the user explicitly requested read-only/planning work.
- Before committing/pushing, inspect the diff and make sure no unrelated user changes are included.
- After committing/pushing, verify the resulting HEAD and check CI when GitHub access is available.

Do not perform release/tag creation, NuGet/registry publication, release-asset replacement, destructive repository administration, license changes, or other externally consequential publication steps without explicit authorization.

## When to ask instead of guessing

Stop and ask only when a decision is genuinely consequential and the repository/task does not resolve it, especially for:

- incompatible public API choices with no clear existing convention;
- a mathematical definition or acceptance criterion whose ambiguity changes behavior;
- destructive data/history changes;
- licensing/provenance uncertainty;
- credentials/secrets or broad permission changes;
- release/tag/package publication;
- changes to the immutable 1.0 release boundary.

For ordinary local design choices, make the most conservative coherent decision, document it where useful, and keep moving.

## Completion report

For a substantive development step, report concisely:

- what changed and why;
- important numerical/API decisions;
- tests and documentation added/updated;
- exact verification result and total test count when available;
- CI status when checked;
- exact final HEAD SHA;
- the next sensible step.
