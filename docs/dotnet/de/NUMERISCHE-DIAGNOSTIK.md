# Numerische Diagnostik und Beendigungsverträge

Dieses Dokument definiert die fachübergreifenden Diagnosekonventionen der C#/.NET-Referenzimplementierung. Es ergänzt die verfahrensspezifischen Dokumente und soll bei weiteren Sprachimplementierungen schrittweise Teil des sprachneutralen Verhaltensvertrags werden.

## 1. Drei Ebenen der Fehlermeldung

Die Bibliothek trennt bewusst drei Kategorien.

### 1.1 Verletzung des Aufrufvertrags

Ungültige Dimensionen, Null-Argumente, nicht-endliche erforderliche Eingaben, nicht-positive Toleranzen und andere Vertragsverletzungen des Aufrufers verwenden normale .NET-Exceptions, üblicherweise `ArgumentException` oder `ArgumentOutOfRangeException`.

Die C#-Kernimplementierung wandelt fehlerhafte öffentliche Eingaben deshalb normalerweise nicht in `IterationStatus.InvalidInput` um. Dieser Enum-Wert bleibt als gemeinsames Vokabular für resultatorientierte Adapter oder spätere Sprachimplementierungen erhalten, ist aber kein Grund, die Validierung an der C#-API-Grenze abzuschwächen.

### 1.2 Erwartbare numerische Beendigung

Ist der Aufruf gültig, aber ein iterativer bzw. adaptiver Prozess kann sein Kriterium nicht erfüllen, soll nach Möglichkeit ein Ergebnisstatus zurückgegeben werden. Beispiele sind:

- `IterationStatus.MaximumIterationsReached`;
- `IterationStatus.NumericalBreakdown`;
- `IterationStatus.NotBracketed`;
- `AdaptiveIntegrationStatus.MaximumDepthReached`;
- `AdaptiveIntegrationStatus.NumericalResolutionReached`;
- `AdaptiveOdeStatus.MaximumStepAttemptsReached`;
- `AdaptiveOdeStatus.MinimumStepSizeReached`.

Damit bleibt normaler numerischer Kontrollfluss ohne Exceptions auswertbar.

### 1.3 Direkter arithmetischer Fehler

Eine direkte Operation, die kein sinnvolles endliches Ergebnis erzeugen kann und keinen passenden iterativen Ergebnisumschlag besitzt, darf `ArithmeticException` auslösen. Beispiele sind ein numerisch singulärer direkter Solver oder eine übergelaufene direkte Rechnung.

## 2. Semantik von `IterativeResult<T>`

`IterativeResult<T>` trennt fünf Konzepte:

- `Value`: berechneter Wert bzw. methodenspezifische beste Näherung;
- `Iterations`: Anzahl abgeschlossener Iterationen;
- `Status`: Beendigungsgrund;
- `Residual`: optionaler problemspezifischer Defekt;
- `Message`: optionale lesbare Diagnoseinformation.

`Converged` bedeutet exakt `Status == IterationStatus.Converged`.

`HasFiniteResidual` bedeutet exakt `double.IsFinite(Residual)` und impliziert bewusst keine Konvergenz. Ein Ergebnis kann am Iterationslimit ein sinnvolles endliches Residuum besitzen; ein numerischer Breakdown kann auftreten, bevor überhaupt ein sinnvolles Residuum bestimmbar ist.

Der Standardwert `Residual = double.NaN` bedeutet, dass über dieses generische Feld kein endliches Residuum vorliegt. Er bedeutet nicht, dass nicht-endliche Werte als normale Eingaben akzeptiert werden sollen.

## 3. Ein Residuum ist problemspezifisch

Der Begriff „Residuum“ benötigt immer eine mathematische Definition. V1 verwendet unter anderem:

- Nullstellensuche: `|f(x)|`;
- lineare Gleichungssysteme: `||A*x-b||∞`;
- Eigenpaare: `||A*v-lambda*v||2`;
- iterative Verfahren: ein von der jeweiligen API dokumentiertes Residuum.

Residuen verschiedener Fachbereiche besitzen unterschiedliche Dimensionen, Skalen und Interpretationen. Sie sind nicht allein deshalb vergleichbar, weil die Eigenschaft `Residual` heißt.

Ein Residuum ist außerdem nicht automatisch der Vorwärtsfehler der gesuchten Größe. Erst die Konditionierung beschreibt, wie sich ein Gleichungsdefekt in einen Lösungsfehler überträgt.

## 4. Fehlerschätzungen sind keine exakten Fehler

Adaptive Integration und RKF-artige ODE-Verfahren gewinnen Fehlerindikatoren aus Differenzen numerischer Näherungen. Diese Schätzungen steuern die Verfeinerung, vergleichen aber nicht mit einer exakten Lösung.

Die Dokumentation verwendet deshalb bewusst Begriffe wie `EstimatedError`, lokale Fehlerschätzung oder Verfeinerungsindikator und behauptet keinen bekannten exakten globalen Fehler.

## 5. Verantwortung für Toleranzen

Es gibt keine veränderbare globale Konvergenztoleranz. Jedes Verfahren besitzt und dokumentiert sein eigenes Kriterium.

`NumericConstants.DefaultTolerance` ist ein brauchbarer Standardwert, aber keine universelle Genauigkeitsgarantie. `NumericConstants.NearlyZero` ist ein kleiner absoluter Schutzwert für numerische Breakdowns, nicht Maschinen-Epsilon und keine allgemeine Näherungsgleichheit.

Wenn ein Schwellwert eine wesentliche numerische Policy-Entscheidung darstellt, soll eine öffentliche API ihn ausdrücklich anbieten. Pivot-Toleranzen bei LU und inverser Iteration sind Beispiele.

Spätere M3-Helfer für skalierte Vergleiche dürfen wiederverwendbare Abstraktionen ergänzen, sollen aber die einfachen V1-Verträge nicht nachträglich verschleiern.

## 6. Statuswerte verschiedener Fachbereiche müssen nicht derselbe Enum sein

Das Toolkit zwingt nicht jedes numerische Verfahren in einen einzigen übergroßen Status-Enum. `IterationStatus` beschreibt gemeinsames iteratives Verhalten; adaptive Quadratur und adaptive ODE-Integration behalten eigene Statuswerte, weil Tiefenlimit und Mindestschritt fachlich unterschiedliche Bedeutung und Gegenmaßnahmen besitzen.

Architektonisch gefordert ist konsistente **Semantik**, nicht ein einziger Enum-Typ.

## 7. Policy für endliche Werte

Öffentliche numerische Eingaben sollen endlich sein, sofern nichts anderes ausdrücklich dokumentiert ist. Callback-Ergebnisse werden in stabilen V1-Verfahren geprüft, wenn eine Fortpflanzung von `NaN` oder Unendlich die Diagnose zerstören würde.

Ein durch Arithmetik erzeugter nicht-endlicher Wert gilt als numerischer Fehler und nicht als normal konvergiertes Ergebnis.

## 8. Unabhängige Diagnostik

Wo sinnvoll, stellen wichtige Ergebnisbereiche einen problemspezifischen Defekt bereit, der unabhängig von der internen Iterationsänderung erneut berechnet werden kann. Beispiele sind:

- `RootResult.Residual`;
- `LinearSystemSolvers.ResidualInfinityNorm`;
- `EigenSolvers.EigenpairResidualNorm`.

Das ist besser, als nur eine interne „Änderung zwischen zwei Iterationen“ zu liefern, denn kleine Änderungen können auch bei noch nicht ausreichend erfüllter Gleichung auftreten.

Einige iterative Verfahren verlangen bewusst sowohl ein Änderungskriterium als auch ein Problemresiduum. `GaussSeidel` ist ein Beispiel: Konvergenz erfordert eine hinreichend kleine Komponentenänderung und ein hinreichend kleines Residuum des linearen Systems.

## 9. Konditionierung gehört in die Interpretation

V1-Statuswerte berichten, was das Verfahren beobachtet. Sie versuchen nicht, eine universelle Konditionszahl zu erraten oder jedes sensitive Problem automatisch als Fehler zu markieren.

Die Dokumentation unterscheidet deshalb:

- algorithmische Konvergenz;
- Rückwärtsdefekt/Residuum;
- Vorwärtsfehler;
- Problemkonditionierung;
- Modell- bzw. Diskretisierungsfehler.

Spätere moderne Linear-Algebra- oder Statistikmodule können explizite Konditionsschätzungen ergänzen, wenn dies mathematisch sinnvoll ist.

## 10. Determinismus und Reproduzierbarkeit

Referenzalgorithmen sollen bei gleichen endlichen Eingaben und Optionen deterministisch sein, solange Zufall nicht ausdrücklich Teil einer späteren API ist. Diagnosen sollen genügend Informationen liefern, damit Tests und Anwendungen den Beendigungsgrund nachvollziehen können.

Für reproduzierbare Forschungs- oder Validierungsabläufe sollten Anwendungen neben dem Endwert Methode, Optionen, Status, Iterations-/Verfeinerungszahlen, Residuum/Fehlerindikator und Toolkit-Version bzw. Commit speichern.

## 11. Richtung für weitere Sprachen

Wenn C++, Java, JavaScript/TypeScript und Fortran hinzukommen, soll die sprachneutrale `spec/`-Ebene die semantischen Trennungen dieses Dokuments bewahren und zugleich idiomatische Fehler- und Exceptionmechanismen der jeweiligen Sprache zulassen.

Cross-Language-Konformität vergleicht mathematisches Verhalten, Beendigungskategorien und Diagnostik – nicht identische Quellstruktur oder identische Exception-Klassennamen.
