# SASD Math Toolkit C++ handbook build

This is the independent LaTeX publication layer for the German and English C++
handbooks. It reads only the C++ Markdown sources below
`docs/user-guide/cpp/`; it does not import C#/.NET handbook content.

## Requirements

- Pandoc
- LuaLaTeX / TeX Live
- latexmk
- DejaVu fonts and the packages loaded by `sasd-cpp-handbook.sty`

## Build

```bash
cd docs/user-guide/LaTeX/cpp
make de
make en
# or
make all
```

Development outputs are:

- `SASD-Math-Toolkit-Cpp-Handbuch-V0.4-DE.pdf`
- `SASD-Math-Toolkit-Cpp-Handbook-V0.4-EN.pdf`

Markdown remains the editorial source. Generated PDFs are inspected and
committed only at deliberate release-candidate or release boundaries. Temporary
fragments and LaTeX files live below `build/` and are removed with `make clean`.

The C++ project keeps its style file locally so its handbook can be built and
released independently. A future neutral SASD publication package may replace
the duplicated visual conventions after both editions have stable requirements.
