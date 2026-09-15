# Eigenwerte und Eigenvektoren

Dieses Dokument beschreibt den auf Veröffentlichungsqualität gebrachten C#-Referenzstand für den historischen V1-Bereich der Eigenwerte und Eigenvektoren. Die Algorithmen sind eigenständig implementiert und bleiben bewusst gut nachvollziehbare Referenzverfahren statt mit LAPACK, Sparse-Krylov-Bibliotheken oder anderen hochoptimierten Eigensolvern konkurrieren zu wollen.

## Ergebnismodell und Diagnostik

Ein reelles Eigenpaar erfüllt

`A * v = lambda * v`.

`Eigenpair` ist jetzt ein unveränderliches Value-Objekt. Es besitzt eine eigene Kopie des Eigenvektors, lehnt nicht-endliche öffentliche Werte ab, stellt mit `Components` eine schreibgeschützte Sicht bereit und behält `Eigenvector` aus Kompatibilitätsgründen als defensiv kopiertes Array bei.

Alle iterativen Eigenwertverfahren liefern `IterativeResult<T>`. Anwendungen sollten `Converged` bzw. `Status`, `Iterations` und `Residual` auswerten und nicht allein aus einem vorhandenen Zahlenwert auf Konvergenz schließen.

`EigenSolvers.EigenpairResidualNorm(matrix, pair)` berechnet unabhängig davon den Gleichungsdefekt

`||A*v - lambda*v||2`.

Ein kleines Residuum zeigt, dass das Eigenpaar die Eigenwertgleichung im absoluten Sinn gut erfüllt. Es ist kein Konditionsschätzer und beweist insbesondere bei empfindlichen oder dicht beieinanderliegenden Eigenwerten nicht automatisch eine ebenso genaue Lage des Eigenwerts.

## Potenzmethode

`EigenSolvers.PowerMethod` approximiert ein Eigenpaar zum betragsmäßig dominanten Eigenwert. Ohne expliziten Startvektor wird deterministisch der Einsvektor verwendet.

Die Normalisierung benutzt jetzt eine skalierte Normberechnung. Dadurch führt ein gültiger endlicher Vektor wie `[1e308, 1e308]` nicht allein deshalb zum Überlauf, weil eine naive Implementierung seine Komponenten quadrieren würde.

Konvergenz verlangt sowohl eine ausreichend kleine Änderung des Rayleigh-Quotienten als auch ein ausreichend kleines Eigenpaar-Residuum. Der Startvektor muss einen Anteil in der gewünschten dominanten Eigenrichtung besitzen. Weiß eine Anwendung, dass der Standardvektor dazu orthogonal sein könnte, sollte sie einen passenden Startvektor angeben.

## Inverse Potenzmethode

`EigenSolvers.InversePowerMethod` ist die unverschobene inverse Iteration. Sie zielt auf den betragsmäßig nahe bei Null liegenden Eigenwert, sofern die Matrix nicht singulär ist und der Startvektor die entsprechende Eigenrichtung enthält.

Die Matrix wird einmal LU-faktorisiert und diese Faktorisierung für alle Iterationen wiederverwendet. Mit dem optionalen `pivotTolerance` kann eine Anwendung explizit festlegen, ab welcher Pivotgröße sie die Matrix als numerisch singulär behandelt.

Eine singuläre bzw. numerisch singuläre Matrix ist damit ein Fehler der Problemvoraussetzung und führt bereits beim Aufbau der Faktorisierung zu `ArithmeticException`, nicht zu einer scheinbar gewöhnlichen Nichtkonvergenz.

Eine verschobene inverse Iteration wird in V1 bewusst nicht versteckt eingebaut. Ein späteres Verfahren soll den Shift ausdrücklich in seiner API zeigen, weil dieser das numerische Problem und das Singularitätsverhalten verändert.

## Wielandt-Deflation

`WielandtDeflation.Create` implementiert die klassische Rang-1-Transformation

`B = A - v*x^T`

für ein bereits bekanntes rechtes Eigenpaar. Für die Konstruktion wird die betragsmäßig größte Eigenvektorkomponente gewählt, damit die notwendige Division nicht unnötig durch eine kleine Komponente erfolgt.

Eine Deflation ist nur sinnvoll, wenn das übergebene Paar tatsächlich ein hinreichend gutes Eigenpaar von `A` ist. Deshalb prüft die Factory jetzt `||A*v-lambda*v||2` gegen eine konfigurierbare relative `residualTolerance`. Das akzeptierte `SourceResidual` bleibt als Diagnoseinformation verfügbar. Unpassende oder zu ungenaue behauptete Eigenpaare werden abgelehnt, statt still eine irreführende Deflationsmatrix zu erzeugen.

`EigenSolvers.WielandtSecondEigenpair` kombiniert die dominante Potenziteration, validierte Deflation, eine zweite Potenziteration und die Rückabbildung auf die Ausgangsmatrix. Dies ist vor allem ein historisches Referenzverfahren. Bei mehrfachen oder nahezu mehrfachen dominanten Eigenwerten kann die Rückabbildung schlecht konditioniert sein; ein solcher Fall wird als `NumericalBreakdown` gemeldet.

## Zyklisches Jacobi-Verfahren

`EigenSolvers.CyclicJacobi` berechnet das vollständige Eigensystem einer **reellen symmetrischen** Matrix. Jeder Sweep führt Ebenenrotationen über alle Nebendiagonalpaare aus und sammelt dieselben orthogonalen Rotationen in der Eigenvektormatrix.

Für die Rotationsparameter werden die beteiligten Matrixwerte jetzt vor der Tangensberechnung skaliert. Dadurch werden vermeidbare Überläufe bei großen, aber endlichen Matrixwerten reduziert. Auch Frobenius- und euklidische Normen verwenden skalierte Quadratsummen, statt sehr große Werte unmittelbar zu quadrieren.

Das Ergebnis `SymmetricEigendecomposition` besitzt folgende Konventionen:

- Eigenwerte sind absteigend nach ihrem **numerischen Wert**, nicht nach Betrag sortiert;
- Eigenvektoren stehen spaltenweise und sind bis auf Gleitkommafehler orthonormal;
- `GetEigenvalue(index)` vermeidet eine unnötige Kopie des gesamten Eigenwertarrays;
- `GetEigenvector(index)` liefert eine unabhängige Vektorkopie;
- `GetEigenpair(index)` liefert ein unveränderliches Eigenpaar;
- `Eigenvalues` und `Eigenvectors` bleiben defensive Convenience-Kopien.

Das `Residual` des `IterativeResult` ist beim zyklischen Jacobi die Frobenius-Norm der Nebendiagonale der transformierten Arbeitsmatrix. Es ist damit ein Konvergenzmaß der Diagonalisierung und **nicht** das größte finale `||A*v-lambda*v||2`. Für eine unabhängige Prüfung einzelner Eigenpaare steht `EigenpairResidualNorm` zur Verfügung.

Die Symmetrie wird mit einer eigenen relativen `symmetryTolerance` geprüft. Eine Matrix außerhalb dieser Toleranz wird abgelehnt, weil das reell-symmetrische Jacobi-Verfahren kein allgemeiner nicht-symmetrischer Eigensolver ist.

## Mehrfache und nahe Eigenwerte

Das Vorzeichen eines Eigenvektors ist nicht eindeutig: `v` und `-v` beschreiben dieselbe eindimensionale Eigenrichtung. Bei mehrfachen Eigenwerten kann jede orthonormale Basis des zugehörigen Eigenraums korrekt sein. Tests und Anwendungen sollten deshalb Residuen, Orthogonalität und Unterraumeigenschaften bevorzugen und nicht Komponenten mit genau einem willkürlich gewählten Referenzvektor vergleichen.

Kleine spektrale Abstände können außerdem die Potenziteration verlangsamen und Eigenvektoren empfindlich gegenüber Störungen machen. Ein kleines Residuum bleibt ein wichtiges Qualitätsmerkmal der Eigenwertgleichung, beseitigt aber nicht die Kondition des mathematischen Problems.

## Aktueller Umfang

V1 enthält bewusst die historischen Referenzverfahren: Potenzmethode, unverschobene inverse Potenzmethode, Wielandt-Deflation bzw. zweites Eigenpaar und zyklisches Jacobi für reelle symmetrische Matrizen. Noch nicht enthalten sind ein allgemeiner QR-/Schur-Solver für nicht-symmetrische Matrizen, allgemeine komplexe Eigenpaare, verschobene inverse Iteration, Sparse-Lanczos-/Arnoldi-Verfahren oder Konditionsschätzer.

Solche Funktionen gehören in spätere SASD-Erweiterungen und sollten über klare APIs ergänzt werden, ohne die verständliche dependency-freie Referenzimplementierung unnötig zu verkomplizieren.
