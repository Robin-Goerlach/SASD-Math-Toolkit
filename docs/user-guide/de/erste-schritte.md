# Erste Schritte mit C#/.NET

## Aktuelle Entwicklungsform

Das SASD Math Toolkit zielt derzeit auf .NET 10 und wird als Klassenbibliothek in diesem Repository entwickelt. Solange noch kein Paket-Feed bereitgestellt wird, kann ein anderes Projekt direkt `src/dotnet/Sasd.Math.Toolkit/Sasd.Math.Toolkit.csproj` referenzieren oder mit der Repository-Solution arbeiten.

Das komplette Repository wird so gebaut und getestet:

```bash
dotnet restore Sasd.Math.Toolkit.slnx
dotnet build Sasd.Math.Toolkit.slnx --configuration Release
dotnet test Sasd.Math.Toolkit.slnx --configuration Release
```

## Namespaces

Die Assembly heißt `Sasd.Math.Toolkit`; die numerischen APIs beginnen mit `Sasd.Numerics`. Dadurch entsteht keine unnötige Verwechslung mit `System.Math`, und die Fachbereiche bleiben sichtbar:

```csharp
using Sasd.Numerics.Differentiation;
using Sasd.Numerics.Integration;
using Sasd.Numerics.LinearAlgebra;
```

## Eine erste Berechnung

Die erste Ableitung einer aufrufbaren Funktion lässt sich direkt näherungsweise bestimmen:

```csharp
var derivative = NumericalDifferentiation.FirstDerivative(Math.Sin, 0.3);
Console.WriteLine(derivative);
```

Das Ergebnis sollte nahe bei `Math.Cos(0.3)` liegen. Numerische Verfahren liefern Näherungen; Gleitkommaergebnisse sollten daher normalerweise mit Toleranzen und nicht auf exakte Gleichheit verglichen werden.

## Grafische Numerik-Demo starten

Das Repository enthält ein plattformunabhängiges Sample, das echte Toolkit-APIs ausführt und einen eigenständigen HTML-/SVG-Bericht erzeugt:

```bash
dotnet run --project samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj
```

Die erzeugte Datei `sasd-math-toolkit-demo.html` kann anschließend in einem Browser geöffnet werden. Der Bericht enthält derzeit Nullstellensuche, Integration, kompakte Real-FFT, Faltung und Korrelation. Netzwerkzugriff und JavaScript werden nicht benötigt, sodass der Bericht zugleich als deterministisches Demo-/Smoke-Artefakt geeignet ist.

Dieses Sample ersetzt die Rolle der historischen Grafikdemos durch neu geschriebenen C#/.NET-Code. Das Rendering bleibt außerhalb der Numerikbibliothek, damit Anwendungsprojekte nicht an eine bestimmte UI- oder Chart-Technologie gebunden werden.

## Fehlerbehandlung

Verletzungen von Programmverträgen wie unpassende Dimensionen, ungültige Indizes oder nicht-endliche Eingaben werden im Allgemeinen über normale .NET-Ausnahmen gemeldet. Iterative mathematisch gültige Verfahren, deren Konvergenz scheitern kann, liefern dagegen typischerweise Ergebnisobjekte mit Status, Iterationszahl und Residuum.

Diese Trennung ist beabsichtigt: Eine fehlerhafte Eingabe ist etwas anderes als eine gültige Iteration, die innerhalb der gesetzten Grenzen nicht konvergiert.
