# Singulärwertzerlegung

## Zweck

`SingularValueDecomposition` ist der moderne Dense-Fallback für Probleme, bei denen eine einfache Vollrang-Sicht über QR oder LU nicht ausreicht. Unterstützt werden beliebige reelle dichte Matrixformen mit der Zerlegung

`A = U * S * V^T`.

Die dünnen linken und rechten Singulärvektoren werden zusammen mit absteigend sortierten Singulärwerten bereitgestellt.

Die abhängigkeitfreie Managed-Referenzimplementierung verwendet eine **einseitige Jacobi-SVD**. Sie bildet bewusst nicht `A^T*A`, weil die Normalmatrix die Konditionszahl quadriert und schwache Singulärrichtungen zerstören kann, bevor überhaupt eine Rangdiagnose möglich ist.

## Öffentliche Diagnostik

Die Faktorisierung liefert unter anderem:

- absteigend sortierte Singulärwerte;
- `EstimatedRank`;
- `RankThreshold`;
- `ConditionNumber` und `ReciprocalConditionNumber` auf Basis der 2-Norm;
- die Zahl der benötigten Jacobi-Sweeps;
- dünne linke und rechte Singulärvektormatrizen.

Der Rang ist bewusst toleranzabhängig. Ein Singulärwert wird nur beibehalten, wenn er größer ist als

`largestSingularValue * relativeRankTolerance`.

Dieselbe Schwelle wird für Least Squares und Pseudoinverse verwendet, damit Diagnose und Verhalten konsistent bleiben.

## Matrixformen

Der einseitige Jacobi-Kern arbeitet mit quadratischen bzw. hohen Matrizen. Breite Matrizen werden über

`A^T = Ut * S * Vt^T`

behandelt. Daraus folgt

`A = Vt * S * Ut^T`.

Die Singulärwerte bleiben identisch; die Rollen der dünnen linken und rechten Singulärvektoren werden vertauscht. Normalgleichungen werden nicht gebildet.

## Least Squares und Minimalnorm

`SolveLeastSquares(b)` berechnet eine abgeschnittene SVD-Lösung. Für überbestimmte Systeme wird die Residualnorm minimiert. Für unterbestimmte oder rangdefiziente Systeme wird die Minimum-Euklidische-Norm-Lösung entsprechend der eingestellten Rangschwelle zurückgegeben.

Damit wählen wir nicht willkürlich unabhängige Spalten aus und perturbieren ein singuläres System auch nicht stillschweigend.

## Pseudoinverse

`PseudoInverse()` bildet die Moore-Penrose-Pseudoinverse aus den beibehaltenen Singulärtripeln:

`A+ = V * S+ * U^T`.

Abgeschnittene Singulärrichtungen tragen null bei. Für wiederholte Lösungen ist `SolveLeastSquares` meist sinnvoller, weil keine komplette Pseudoinverse materialisiert werden muss.

## Numerische Hinweise

- Das Jacobi-Konvergenzkriterium verwendet normierte Kreuzkorrelationen der Arbeitsspalten und ist damit skalierungsbewusster als ein rohes Skalarprodukt.
- Spaltennormen werden über skaliertes Sum-of-Squares-Akkumulieren berechnet, um unnötigen Über-/Unterlauf zu vermeiden.
- Öffentliche Singulärvektoren werden defensiv ausgegeben.
- Exakte Null-Singulärrichtungen erhalten eine deterministische orthonormale Ergänzung, damit auch bei exakter Rangdefizienz eine strukturell brauchbare dünne `U`-Matrix vorliegt.
- Die Managed-Implementierung priorisiert Auditierbarkeit und robuste Diagnose vor maximalem Durchsatz. Große Workloads können später über einen optionalen LAPACK-artigen Backend beschleunigt werden.

## Verhältnis zu QR und Cholesky

| Struktur | Sinnvolle Faktorisierung |
| --- | --- |
| Allgemeines quadratisches System | pivotierte LU |
| Symmetrisch positiv definites System | Cholesky |
| Quadratisches/hohes Vollrang-Least-Squares | Householder-QR |
| Rangdefizientes Least Squares | SVD |
| Unterbestimmte Minimalnormlösung | SVD |
| Rang-/2-Norm-Konditionsanalyse | SVD |

Die Modernisierung baut damit bewusst mehrere komplementäre Zerlegungen statt eines angeblich universellen Solvers auf.
