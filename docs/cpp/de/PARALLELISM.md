# Parallele Ausführung

## Ziel

Die C++-Ausgabe soll vorhandene CPU-Kerne nutzen, wenn eine relevante Aufgabe
dadurch schneller wird, und gleichzeitig eine verständliche portable
Referenzimplementierung behalten. Eine hohe Threadzahl allein ist kein
Performance-Nachweis; jeder parallelisierte Algorithmus muss später gegen
seinen seriellen Pfad gemessen werden.

## Öffentliche Richtlinie

`sasd::numerics::execution::ExecutionOptions` enthält:

- `policy`: `automatic`, `sequential` oder `parallel`;
- `maximum_threads`: Obergrenze; Null verwendet die erkannte Hardwaregrenze;
- `minimum_work_per_thread`: Granularitätsschwelle für den Automatikmodus.

Die Optionen werden als lokale Werte an unterstützende Algorithmen übergeben.
Es gibt weder eine veränderbare globale Poolgröße noch vergleichbaren
prozessweiten Zustand.

## Geeignete Arbeiten

Gute Kandidaten sind unabhängige Matrixzeilen/-spalten, Batch-Auswertungen,
unabhängige Transformationen und große elementweise Operationen. Eine einzelne
skalare Newton-Rekurrenz wird durch das Erzeugen von Threads nicht schneller.

Reduktionen benötigen besondere Sorgfalt. Eine andere Summationsreihenfolge
kann die Rundung verändern. Der Algorithmus muss dies dokumentieren, geeignete
stabile Formulierungen verwenden und eine mathematische Qualitätsgröße testen,
statt zufällige Bitgleichheit zu verlangen.

## Oversubscription

Eine Anwendung parallelisiert möglicherweise bereits Simulationen, Anfragen
oder Datensätze. Sie kann innerhalb der Bibliothek `sequential` wählen oder
`maximum_threads` begrenzen, damit verschachtelte Worker nicht um dieselben
Kerne konkurrieren.

## Verhalten des internen Workers

Der interne Worker zerlegt Indizes in stabile zusammenhängende Bereiche, begrenzt
die Workerzahl nach Hardware und Arbeitsmenge, wartet auf alle Worker und wirft
die erste Worker-Exception im aufrufenden Thread erneut. Er veröffentlicht
bewusst keine allgemeine Thread-Pool-API; eine spätere persistente Pool- oder
Backend-Lösung bleibt damit nach echten Messungen möglich.

Mittelwert und Varianz aus CPP-M1 sind die ersten öffentlichen Algorithmen mit
dieser Richtlinie. Jeder Worker berechnet ein stabiles zusammenhängendes
Teilergebnis; die Teilergebnisse werden anschließend in Bereichsreihenfolge
zusammengeführt. Bei gleicher Ausführungskonfiguration beeinflusst die
Threadplanung deshalb die Reduktionsreihenfolge nicht.
