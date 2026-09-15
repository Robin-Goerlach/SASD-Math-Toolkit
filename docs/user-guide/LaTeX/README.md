# SASD Math Toolkit - LaTeX handbook build

This directory contains the typesetting layer for the **SASD Math Toolkit C#/.NET Handbook, Version 1.0** in German and English.

## Design goal

The editorial source remains the existing Markdown handbook in `../de/` and `../en/`. Pandoc converts those chapters into temporary LaTeX fragments under `build/`; LuaLaTeX then applies the shared SASD book design. This avoids two independent copies of the same technical text.

The interior design deliberately recalls a few visual ideas from the historical 1987 *Turbo Pascal Numerical Methods Toolbox* manual - especially the restrained chapter opener with a large chapter number, horizontal rules and an italic chapter title - while all text, examples, APIs and diagnostics document the independently implemented SASD C#/.NET library.

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

Outputs:

- `SASD-Math-Toolkit-Handbuch-V1.0-DE.pdf`
- `SASD-Math-Toolkit-Handbook-V1.0-EN.pdf`

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

Change numerical explanations, examples and API documentation in the Markdown handbook first. Change only presentation, PDF-specific front/back matter or build behavior here.
