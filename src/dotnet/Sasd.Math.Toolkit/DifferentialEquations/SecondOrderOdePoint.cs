namespace Sasd.Numerics.DifferentialEquations;

/// <summary>
/// One solution point of a scalar second-order ordinary differential equation.
/// </summary>
/// <param name="X">Independent-variable value.</param>
/// <param name="Y">Computed value of <c>y</c>.</param>
/// <param name="FirstDerivative">Computed value of <c>y'</c>.</param>
public sealed record SecondOrderOdePoint(double X, double Y, double FirstDerivative);
