# Diagnostik und parallele Ausführung

## Status, Wert und Residuum

`IterativeResult<T>` bewahrt fünf voneinander getrennte Informationen:

- berechneter Wert oder beste verfügbare Näherung;
- Zahl abgeschlossener Iterationen;
- Terminierungsstatus;
- problemspezifisches Residuum, sofern verfügbar;
- optionale Diagnosemeldung.

Ein erreichtes Iterationslimit kann weiterhin eine brauchbare Näherung und ein
endliches Residuum liefern. Bei einem numerischen Breakdown kann dagegen jedes
sinnvolle Residuum fehlen. Der Aufrufer darf beide Situationen nicht still als
Erfolg behandeln.

## CPU-Verhalten auswählen

Algorithmen mit paralleler Arbeitsmöglichkeit akzeptieren
`sasd::numerics::execution::ExecutionOptions`:

```cpp
#include <sasd/numerics/execution/execution_options.hpp>

using sasd::numerics::execution::ExecutionOptions;
using sasd::numerics::execution::ExecutionPolicy;

ExecutionOptions automatisch;  // Standard: sinnvoll verfügbare Kerne nutzen

ExecutionOptions seriell;
seriell.policy = ExecutionPolicy::sequential;

ExecutionOptions begrenzt_parallel;
begrenzt_parallel.policy = ExecutionPolicy::parallel;
begrenzt_parallel.maximum_threads = 4;
```

Die Optionen versprechen nicht, dass jeder Algorithmus Threads erzeugt. Eine
skalare Rekurrenz oder kleine Matrix bleibt seriell. Ein großer unabhängiger
Batch kann im Automatikmodus mehrere Worker nutzen.

## Reproduzierbarkeit

Die Aufteilung zusammenhängender Arbeitsbereiche ist deterministisch. Parallele
Fließkommareduktionen können jedoch die Reihenfolge der Operationen und damit
letzte Bits verändern. Die Statistik aus CPP-M1 legt die Bereichsaufteilung fest
und kombiniert Bereiche bei gleicher Konfiguration in Indexreihenfolge. Die
Bibliothek prüft mathematische Toleranzen, Residuen oder Rekonstruktionsfehler. `sequential` ist
sinnvoll, wenn reihenfolgeabhängiges Verhalten untersucht wird oder die
Anwendung bereits auf äußerer Ebene parallelisiert.
