# SASD Math Toolkit - LaTeX handbook build

This directory contains the typesetting layer for the **SASD Math Toolkit C#/.NET Handbook** in German and English.

## Design goal

The editorial source remains the existing Markdown handbook in `../de/` and `../en/`. Pandoc converts those chapters into temporary LaTeX fragments under `build/`; LuaLaTeX then applies the shared SASD book design. This avoids two independent copies of the same technical text.

The interior design deliberately recalls a few visual ideas from the historical 1987 *Turbo Pascal Numerical Methods Toolbox* manual - especially the restrained chapter opener with a large chapter number, horizontal rules and an italic chapter title - while all text, examples, APIs and diagnostics document the independently implemented SASD C#/.NET library.

## Current publication status

The two PDFs currently committed one directory above are the stable **V1.0 classical handbook editions**. Active 2026 modernization chapters are written and reviewed in Markdown first and may intentionally be newer than those PDFs while development is moving quickly.

At the next release-candidate boundary, the workflow is:

1. finish and audit the current Markdown chapters in both languages;
2. synchronize the German and English LaTeX chapter order/front matter;
3. rebuild both PDFs from the same Markdown sources;
4. visually inspect the generated books and their table of contents, code blocks, tables and cross references;
5. commit the refreshed PDFs together with the corresponding LaTeX source changes.

This keeps the checked-in PDFs meaningful release artifacts instead of regenerating large binary files after every numerical-development commit.

## Requirements

- Pandoc
- LuaLaTeX / TeX Live
- latexmk
- TeX packages used by `sasd-handbook.sty` (`fontspec`, `microtype`, `titlesec`, `tcolorbox`, `listings`, `longtable`, `hyperref`, ...)

## Build

```bash
cd docs/user-guide/LaTeX
make de
make en
# or
make all
```

The current V1.0 Makefile outputs are:

- `SASD-Math-Toolkit-Handbuch-V1.0-DE.pdf`
- `SASD-Math-Toolkit-Handbook-V1.0-EN.pdf`

Release-candidate version/name changes should be made deliberately in the LaTeX/front-matter/build configuration as part of the publication milestone rather than ad hoc during feature development.

Temporary Pandoc/LaTeX artifacts are written below `build/` and can be removed with:

```bash
make clean
```

## Files

- `sasd-handbook.sty` - shared typography, chapter styling, code blocks, notes and PDF metadata.
- `de/handbook.tex` - German front matter, chapter order and appendices.
- `en/handbook.tex` - English front matter, chapter order and appendices.
- `Makefile` - Markdown -> LaTeX -> PDF build pipeline.

## Maintenance rule

Change numerical explanations, examples and API documentation in the Markdown handbook first. Change only presentation, PDF-specific front/back matter or build behavior here. The PDF build is a publication step; Markdown remains the technical source of truth throughout normal development.
