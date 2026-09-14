using Sasd.Numerics.Integration;
using Sasd.Numerics.RootFinding;

var root = RootSolvers.NewtonRaphson(
    x => System.Math.Cos(x) - x,
    x => -System.Math.Sin(x) - 1.0,
    initialGuess: 0.0);

Console.WriteLine($"cos(x) = x -> root {root.Root:G17}, iterations {root.Iterations}");

var integral = NumericalIntegration.AdaptiveSimpson(System.Math.Sin, 0.0, System.Math.PI);
Console.WriteLine($"Integral of sin(x) from 0 to pi -> {integral:G17}");
