# Grafische V1-Demoanwendung

## Zweck

Die historische Numerical Methods Toolbox enthielt grafische Demonstrationsprogramme. Das SASD Math Toolkit ersetzt diese Rolle durch eine neu geschriebene .NET-Sample-Anwendung. DOS-Grafikcode oder historische Beispielprogramme werden weder nachgebaut noch weiterverteilt.

Das Sample erzeugt ein eigenständiges HTML-Dokument mit eingebetteten SVG-Diagrammen. Dadurch bleibt die Demo plattformunabhängig und gut nachvollziehbar, ohne die numerische Bibliothek von einem UI-Framework abhängig zu machen.

## Architekturgrenze

Die wiederverwendbare Assembly `Sasd.Math.Toolkit` bleibt unabhängig von Browsern, Dateien und Diagrammrendering. Die Demo liegt ausschließlich unter:

`./samples/dotnet/Sasd.Math.Toolkit.Sample/`

Das Sample referenziert die Numerikbibliothek, niemals umgekehrt. Diese Abhängigkeitsrichtung ist wichtig, damit spätere GUI-, Notebook- oder Web-Frontends denselben mathematischen Kern verwenden können, ohne Demo-spezifischen Code mitzuziehen.

Der kleine `SvgLineChart`-Renderer ist bewusst nur Bestandteil des Samples. Er ist keine geplante allgemeine Charting-API. Eine spätere SASD-UI-/Grafikkomponente kann ihn ersetzen, ohne numerische APIs ändern zu müssen.

## Gezeigte numerische Pfade

Der erzeugte Bericht demonstriert derzeit vier repräsentative Abläufe:

1. Newton-Raphson-Nullstellensuche für `cos(x) - x`.
2. Adaptive Simpson-Integration von `sin(x)` auf `[0, pi]`.
3. Kompakte Real-FFT eines deterministischen Zweiton-Signals.
4. FFT-gestützte reelle Faltung und Kreuzkorrelation mit der dokumentierten vollständigen Lag-Konvention.

Neben numerischen Kennwerten enthält der Bericht SVG-Diagramme. Die Zahlenformatierung verwendet immer `InvariantCulture`, sodass der Output auf Entwicklerrechnern und CI-Agenten reproduzierbar bleibt.

## Demo starten

Vom Repository-Hauptverzeichnis aus:

```bash
dotnet run --project samples/dotnet/Sasd.Math.Toolkit.Sample/Sasd.Math.Toolkit.Sample.csproj
```

Standardmäßig entsteht `sasd-math-toolkit-demo.html` im aktuellen Verzeichnis. Als einziges Argument kann ein eigener Ausgabepfad angegeben werden.

Zum Betrachten ist keine Netzwerkverbindung erforderlich. CSS und SVG sind eingebettet; JavaScript und externe Chart-Bibliotheken werden nicht verwendet.

## Tests

`DemoApplicationTests` prüft deterministische und kulturunabhängige Berichtserzeugung, die Selbstständigkeit des Dokuments sowie das Fehlen nicht-endlicher numerischer Ausgaben. Die CI kompiliert zusätzlich die ausführbare Sample-Anwendung gemeinsam mit der Bibliothek.

Damit besitzt der historische Grafik-Demo-Punkt einen ausdrücklichen modernen Ersatz, während Visualisierungsbelange außerhalb des wiederverwendbaren Numerikkerns bleiben.
