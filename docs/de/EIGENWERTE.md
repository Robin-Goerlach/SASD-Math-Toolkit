# Eigenwerte und Eigenvektoren

Der C#-Referenzstand deckt jetzt den vollständigen historischen V1-Bereich für Eigenwerte und Eigenvektoren ab. Gleichzeitig bleiben die APIs als Grundlage für spätere SASD-Anwendungen nutzbar.

## Enthaltene Verfahren

`EigenSolvers.PowerMethod` bestimmt das Eigenpaar zum betragsmäßig dominanten Eigenwert.

`EigenSolvers.InversePowerMethod` verwendet eine einmal berechnete LU-Faktorisierung für alle Iterationen und sucht in der derzeit unverschobenen Variante den Eigenwert in der Nähe von Null.

`WielandtDeflation.Create` erzeugt eine klassische Rang-1-Deflation. Aus einem bekannten Eigenpaar `A v = lambda v` wird eine Matrix `B = A - v x^T` aufgebaut, in der der entfernte Eigenwert durch Null ersetzt wird. Als Pivot wird die betragsmäßig größte Komponente des Eigenvektors gewählt, damit die notwendige Division nicht unnötig schlecht konditioniert wird.

`EigenSolvers.WielandtSecondEigenpair` kombiniert Potenzmethode, Wielandt-Deflation und Rücktransformation und liefert damit das Eigenpaar mit dem zweitgrößten Eigenwertbetrag, sofern das Problem numerisch geeignet ist. Bei mehrfachen oder nahezu mehrfachen dominanten Eigenwerten kann die Rücktransformation schlecht konditioniert sein; dieser Fall wird ausdrücklich als numerischer Abbruch gemeldet.

`EigenSolvers.CyclicJacobi` berechnet das vollständige Eigensystem einer reellen symmetrischen Matrix. Die Jacobi-Rotationen werden zyklisch über alle Nebendiagonalelemente ausgeführt. Die mitgeführten orthogonalen Rotationen bilden am Ende die Eigenvektormatrix.

Die Klasse `SymmetricEigendecomposition` liefert die Eigenwerte absteigend sortiert. Die Eigenvektoren stehen spaltenweise in der zugehörigen Matrix. Öffentliche Arrays und Matrizen werden als Kopien zurückgegeben, damit ein abgeschlossenes Ergebnis nicht versehentlich verändert wird.

## Numerische Konventionen

Die Jacobi-Konvergenz wird über die Frobenius-Norm der Nebendiagonale relativ zur Frobenius-Norm der Eingabematrix bewertet. Für die Symmetrieprüfung existiert eine eigene relative Toleranz.

Die Implementierungen sind absichtlich gut nachvollziehbare Referenzalgorithmen. SIMD, Blockverfahren oder LAPACK/BLAS-Anbindungen sind spätere Optimierungs- bzw. Backend-Themen und sollen die verständliche Referenzimplementierung nicht verdrängen.
