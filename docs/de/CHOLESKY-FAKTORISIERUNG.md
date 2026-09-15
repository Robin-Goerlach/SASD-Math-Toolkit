# Cholesky-Faktorisierung

`CholeskyFactorization` ist nach Householder-QR der zweite moderne Baustein der dichten linearen Algebra. Das Verfahren spezialisiert reelle symmetrisch positiv definite Matrizen über `A = L * L^T`.

Die Faktorisierung kann für Vektoren und mehrere rechte Seiten wiederverwendet werden. Sie bietet eine defensive Kopie von `L`, gewöhnliche und logarithmische Determinanten sowie explizite relative Toleranzen für Symmetrie und numerische positive Definitheit.

`LogDeterminant()` ist besonders für Statistik- und Likelihood-Rechnungen nützlich, weil nicht zuerst eine gewöhnliche Determinante gebildet werden muss, die bereits über- oder unterlaufen könnte.

Für SPD-Systeme ist Cholesky der bevorzugte Ausgangspunkt, für allgemeine quadratische Systeme pivotierte LU, für quadratisches/hohes Least Squares Householder-QR und für rangdefiziente oder unterbestimmte Probleme die geplante SVD.
