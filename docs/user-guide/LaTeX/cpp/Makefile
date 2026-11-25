PANDOC ?= pandoc
LATEXMK ?= latexmk

BUILD_DIR := build
DE_MD_DIR := ../../cpp/de
EN_MD_DIR := ../../cpp/en

DE_CHAPTERS := README erste-schritte parallele-ausfuehrung deskriptive-statistik
EN_CHAPTERS := README getting-started parallel-execution descriptive-statistics

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

de: $(DE_FRAGMENTS) de/handbook.tex sasd-cpp-handbook.sty
	@mkdir -p $(BUILD_DIR)/pdf-de
	$(LATEXMK) -lualatex -interaction=nonstopmode -halt-on-error \
		-outdir=$(BUILD_DIR)/pdf-de \
		-jobname=SASD-Math-Toolkit-Cpp-Handbuch-V0.2-DE de/handbook.tex
	@cp $(BUILD_DIR)/pdf-de/SASD-Math-Toolkit-Cpp-Handbuch-V0.2-DE.pdf ./SASD-Math-Toolkit-Cpp-Handbuch-V0.2-DE.pdf

en: $(EN_FRAGMENTS) en/handbook.tex sasd-cpp-handbook.sty
	@mkdir -p $(BUILD_DIR)/pdf-en
	$(LATEXMK) -lualatex -interaction=nonstopmode -halt-on-error \
		-outdir=$(BUILD_DIR)/pdf-en \
		-jobname=SASD-Math-Toolkit-Cpp-Handbook-V0.2-EN en/handbook.tex
	@cp $(BUILD_DIR)/pdf-en/SASD-Math-Toolkit-Cpp-Handbook-V0.2-EN.pdf ./SASD-Math-Toolkit-Cpp-Handbook-V0.2-EN.pdf

clean:
	rm -rf $(BUILD_DIR) \
		SASD-Math-Toolkit-Cpp-Handbuch-V0.2-DE.pdf \
		SASD-Math-Toolkit-Cpp-Handbook-V0.2-EN.pdf
