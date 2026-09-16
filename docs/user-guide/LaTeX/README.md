# SASD Math Toolkit - LaTeX handbook build

This directory contains the typesetting layer for the **SASD Math Toolkit C#/.NET Handbook** in German and English.

## Design goal

The editorial source remains the existing Markdown handbook in `../de/` and `../en/`. Pandoc converts those chapters into temporary LaTeX fragments under `build/`; LuaLaTeX then applies the shared SASD book design. This avoids two independent copies of the same technical text.

The interior design deliberately recalls a few visual ideas from the historical 1987 *Turbo Pascal Numerical Methods Toolbox* manual - especially the restrained chapter opener with a large chapter number, horizontal rules and an italic chapter title - while all text, examples, APIs and diagnostics document the independently implemented SASD C#/.NET library.

## Current publication status

The two PDFs committed one directory above are the stable **V1.0 handbook editions**. They contain the classical chapters plus the modern dense and sparse linear-algebra chapters included in the 1.0 release boundary.

The publication workflow is:

1. maintain and audit the Markdown chapters in both languages;
2. synchronize the German and English LaTeX chapter order/front matter at a publication boundary;
3. rebuild both PDFs from the same Markdown sources;
4. structurally validate and visually inspect the generated books;
5. commit the refreshed PDF snapshots deliberately at the RC/release boundary.

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

The V1.0 Makefile outputs are:

- `SASD-Math-Toolkit-Handbuch-V1.0-DE.pdf`
- `SASD-Math-Toolkit-Handbook-V1.0-EN.pdf`

Release-candidate or release version/name changes are made deliberately in the LaTeX/front-matter/build configuration as part of a publication milestone rather than ad hoc during feature development.

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
