# Polynome und komplexe Nullstellen

Dieser Entwicklungsschritt ergänzt die C#-Implementierung um eine wiederverwendbare Polynom-Basis sowie Newton-Horner, Muller und Laguerre mit Deflation.

## Architektur

`Sasd.Numerics.Polynomials.Polynomial` ist unveränderlich. Die Koeffizienten werden in **absteigender Potenzreihenfolge** gespeichert. `[1, -6, 11, -6]` beschreibt also `x^3 - 6x^2 + 11x - 6`.

Die Polynomklasse stellt Horner-Auswertung, eine gemeinsame Auswertung von Funktionswert und ersten beiden Ableitungen, die formale erste Ableitung sowie synthetische Division/Deflation bereit. Damit hängt die grundlegende Polynom-Arithmetik nicht von einem bestimmten Nullstellenverfahren ab und kann später auch von anderen SASD-Modulen und Sprachimplementierungen wiederverwendet werden.

## Nullstellenverfahren

- `PolynomialRootSolvers.NewtonHorner` verwendet die gemeinsame Horner-Auswertung von Polynom und Ableitung.
- `ComplexRootSolvers.Muller` arbeitet allgemein mit komplexwertigen Funktionen und kann die reelle Achse verlassen.
- `PolynomialRootSolvers.Laguerre` sucht eine komplexe Polynomnullstelle.
- `PolynomialRootSolvers.FindAllRootsLaguerre` wiederholt Laguerre, deflationiert das Polynom und verbessert die gefundenen Nullstellen abschließend nochmals am Originalpolynom.

Komplexe Werte werden mit `System.Numerics.Complex` dargestellt.

## Numerische Leitplanken

Der Code ist bewusst verständlich und defensiv statt vorzeitig optimiert. Bei Muller und Laguerre wird jeweils der betragsmäßig größere Nenner gewählt, um Auslöschung zu reduzieren. Die Suche aller Nullstellen nutzt deterministische Ersatzstartpunkte auf einem Cauchy-Radius, falls der Start bei Null numerisch scheitert. Da Deflation Rundungsfehler weitertragen kann, werden die Ergebnisse am Ende gegen das ursprüngliche Polynom nachgeschärft und der maximale Restfehler zurückgegeben.

Weitergehende Themen wie beliebige Genauigkeit, Intervallarithmetik, Companion-Matrix-Verfahren oder SIMD-Optimierung gehören bewusst nicht in diesen Schritt.
