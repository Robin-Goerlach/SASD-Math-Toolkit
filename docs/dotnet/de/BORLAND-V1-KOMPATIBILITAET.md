# V1-Kompatibilitätsziel

V1 soll den funktionalen Umfang der historischen Borland Numerical Methods Toolbox in moderner Form abdecken. Es handelt sich **nicht** um eine Quellcode-Portierung. Der SASD-Code wird unabhängig neu implementiert.

Die vollständige, gepflegte Statusmatrix befindet sich in [`../en/BORLAND-V1-COMPATIBILITY.md`](../en/BORLAND-V1-COMPATIBILITY.md). Der englische Stand ist absichtlich die technische Source of Truth, damit bei vielen Algorithmen keine zwei voneinander abweichenden Tabellen entstehen.

Die Hauptbereiche sind:

- Nullstellen von Gleichungen
- Interpolation und kubische Splines
- numerische Differentiation und Integration
- Matrixverfahren und lineare Gleichungssysteme
- Eigenwerte und Eigenvektoren
- Anfangs- und Randwertprobleme gewöhnlicher Differentialgleichungen
- Least-Squares-Approximation
- FFT, Faltung und Kreuzkorrelation
- Beispiel-/Demonstrationsprogramme

Der Bereich **Nullstellensuche** ist inzwischen vollständig für das historische V1-Ziel implementiert: Bisektion, Newton-Raphson, Sekantenverfahren, Newton-Horner, Muller sowie Laguerre inklusive Polynomdeflation. Die wiederverwendbare Polynom-Basis ist unter [`POLYNOM-NULLSTELLEN.md`](POLYNOM-NULLSTELLEN.md) beschrieben.
