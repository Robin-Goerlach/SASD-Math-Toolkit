# Polynome und Nullstellensuche

## Verfahren auswählen

| Problem | Erste Wahl | Wichtigster Kompromiss |
| --- | --- | --- |
| Reelle Wurzel in einem bekannten Vorzeichenwechselintervall | Bisektion | Robust, aber linear konvergent |
| Zuverlässige Ableitung und guter Start | Newton-Raphson | Lokal schnell; Ableitung kann unbrauchbar werden |
| Keine Ableitung, zwei brauchbare Starts | Sekante | Ableitungsfrei, erhält aber keine Klammer |
| Allgemeine komplexe Funktion | Muller | Kann die reelle Achse verlassen; benötigt drei Starts |
| Eine Polynomwurzel | Newton-Horner oder Laguerre | Newton ist lokal; Laguerre nutzt zwei Ableitungen |
| Alle Polynomwurzeln | Laguerre-Gesamtsuche | Deflation benötigt abschließendes Polishing |

Kein Verfahren garantiert bei beliebigem Start, dass genau die fachlich
gewünschte Wurzel erreicht wird. Status und Residuum gehören deshalb immer zur
gespeicherten Näherung.

## Skalare reelle Nullstellen

```cpp
#include <sasd/numerics/root_finding/root_solvers.hpp>

#include <cmath>
#include <iostream>

int main() {
    const auto funktion = [](double x) { return std::cos(x) - x; };

    const auto ergebnis =
        sasd::numerics::root_finding::RootSolvers::bisection(funktion, 0.0, 1.0);

    if (ergebnis.converged()) {
        std::cout << ergebnis.root() << " Residuum=" << ergebnis.residual() << '\n';
    }
}
```

Newton-Raphson erhält zusätzlich einen Ableitungs-Callback. Das
Sekantenverfahren erhält zwei Startnäherungen. Alle drei verwenden
`RootFindingOptions`:

```cpp
sasd::numerics::root_finding::RootFindingOptions optionen;
optionen.tolerance = 1.0e-12;
optionen.maximum_iterations = 100;
```

`not_bracketed`, `numerical_breakdown` und
`maximum_iterations_reached` sind Ergebnisstatus und keine Exceptions.
Ungültige Intervalle, nicht endliche Eingaben und nicht endliche Callbacks
verletzen dagegen den API-Vertrag und werden mit Exceptions abgewiesen.

## Polynomdarstellung

Koeffizienten stehen in absteigender Potenzreihenfolge:

```cpp
#include <sasd/numerics/polynomials/polynomial.hpp>

// x^3 - 2x - 5
const sasd::numerics::polynomials::Polynomial polynom{1.0, 0.0, -2.0, -5.0};

const auto wert = polynom.evaluate(2.0);
const auto auswertung = polynom.evaluate_with_derivatives(2.0);
const auto ableitung = polynom.derivative();
```

Das Objekt besitzt seine Koeffizienten und veröffentlicht nur einen
schreibgeschützten Span. `deflate` führt die synthetische Division durch
`(x-root)` aus und liefert Quotient sowie Rest. Ein großer Rest zeigt, dass der
übergebene Wert keine hinreichend genaue Wurzel war.

## Komplexe und polynomiale Wurzeln

```cpp
#include <sasd/numerics/root_finding/polynomial_root_solvers.hpp>

#include <complex>

using sasd::numerics::root_finding::PolynomialRootSolvers;

const auto eine_wurzel =
    PolynomialRootSolvers::newton_horner(polynom, std::complex<double>{2.0, 0.0});

const sasd::numerics::polynomials::Polynomial quartik{1.0, 0.0, 0.0, 0.0, -1.0};
const auto alle_wurzeln = PolynomialRootSolvers::find_all_roots_laguerre(quartik);
```

Die Gesamtsuche nutzt deterministische Ersatzstartwerte, synthetische
Deflation und einen Polishing-Durchlauf am ursprünglichen Polynom.
`maximum_residual()` ist dadurch eine nützliche abschließende Qualitätsangabe.

Das Muller-Verfahren erhält eine Funktion von `std::complex<double>` nach
`std::complex<double>`. Die komplexe Quadratwurzel des lokalen quadratischen
Modells erlaubt reellen Startpunkten, für `z^2+1` zu `+i` oder `-i` zu gelangen.

## Parallele Ausführung

Der nächste Newton-, Sekanten-, Muller- oder Laguerre-Punkt hängt vom vorherigen
Punkt ab. Threads innerhalb einer einzelnen Rekurrenz hätten keine unabhängige
Arbeit. CPP-M2 hält einzelne Suchen deshalb seriell. Eine Anwendung darf
unabhängige Gleichungen oder Parametersätze außen parallel verteilen; eine
spätere Batch-API kann dies mit `ExecutionOptions` formalisieren.
