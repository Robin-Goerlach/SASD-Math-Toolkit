PANDOC ?= pandoc
LATEXMK ?= latexmk

BUILD_DIR := build
DE_MD_DIR := ../de
EN_MD_DIR := ../en

DE_CHAPTERS := erste-schritte nullstellen interpolation differentiation integration matrizen-lineare-gleichungssysteme eigenwerte-eigenvektoren differentialgleichungen least-squares fft-faltung-korrelation diagnostik-fehlerfaelle moderne-dichte-lineare-algebra duennbesetzte-lineare-algebra
EN_CHAPTERS := getting-started root-finding interpolation differentiation integration matrices-linear-systems eigenvalues-eigenvectors ordinary-differential-equations least-squares fft-convolution-correlation diagnostics-common-failure-modes modern-dense-linear-algebra sparse-linear-algebra

DE_FRAGMENTS := $(addprefix $(BUILD_DIR)/de/,$(addsuffix .tex,$(DE_CHAPTERS)))
EN_FRAGMENTS := $(addprefix $(BUILD_DIR)/en/,$(addsuffix .tex,$(EN_CHAPTERS)))

PANDOC_FLAGS := --from=gfm --to=latex --top-level-division=chapter --listings --wrap=none

.PHONY: all de en clean fragments
all: de en
fragments: $(DE_FRAGMENTS) $(EN_FRAGMENTS)

$(BUILD_DIR)/de/%.tex: $(DE_MD_DIR)/%.md
	@mkdir -p $(BUILD_DIR)/de
	$(PANDOC) $(PANDOC_FLAGS) "$<" -o "$@"

$(BUILD_DIR)/en/%.tex: $(EN_MD_DIR)/%.md
	@mkdir -p $(BUILD_DIR)/en
	$(PANDOC) $(PANDOC_FLAGS) "$<" -o "$@"

de: $(DE_FRAGMENTS) de/handbook.tex sasd-handbook.sty
	@mkdir -p $(BUILD_DIR)/pdf-de
	$(LATEXMK) -lualatex -interaction=nonstopmode -halt-on-error -outdir=$(BUILD_DIR)/pdf-de -jobname=SASD-Math-Toolkit-Handbuch-V1.0-DE de/handbook.tex
	@cp $(BUILD_DIR)/pdf-de/SASD-Math-Toolkit-Handbuch-V1.0-DE.pdf ./SASD-Math-Toolkit-Handbuch-V1.0-DE.pdf

en: $(EN_FRAGMENTS) en/handbook.tex sasd-handbook.sty
	@mkdir -p $(BUILD_DIR)/pdf-en
	$(LATEXMK) -lualatex -interaction=nonstopmode -halt-on-error -outdir=$(BUILD_DIR)/pdf-en -jobname=SASD-Math-Toolkit-Handbook-V1.0-EN en/handbook.tex
	@cp $(BUILD_DIR)/pdf-en/SASD-Math-Toolkit-Handbook-V1.0-EN.pdf ./SASD-Math-Toolkit-Handbook-V1.0-EN.pdf

clean:
	rm -rf $(BUILD_DIR) SASD-Math-Toolkit-Handbuch-V1.0-DE.pdf SASD-Math-Toolkit-Handbook-V1.0-EN.pdf
