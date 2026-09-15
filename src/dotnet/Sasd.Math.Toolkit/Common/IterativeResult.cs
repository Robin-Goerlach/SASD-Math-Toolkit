namespace Sasd.Numerics.Common;

/// <summary>
/// Standard result envelope for iterative algorithms.
/// </summary>
/// <typeparam name="T">Type of the calculated value or best available approximation.</typeparam>
/// <param name="Value">
/// Numerical value produced by the algorithm. Its interpretation for non-converged statuses is
/// method-specific; callers must not assume that a populated value implies convergence.
/// </param>
/// <param name="Iterations">Number of completed iterations reported by the algorithm.</param>
/// <param name="Status">Reason why iteration terminated.</param>
/// <param name="Residual">
/// Problem-specific residual or defect measure when the algorithm can report one. A value of
/// <see cref="double.NaN"/> means that no finite residual is available through this generic field.
/// </param>
/// <param name="Message">Optional diagnostic message intended to explain a non-routine termination.</param>
/// <remarks>
/// <para>
/// The result envelope deliberately keeps <see cref="Status"/>, <see cref="Value"/> and
/// <see cref="Residual"/> separate. They answer different questions: how the algorithm stopped,
/// what approximation it produced and how well that approximation satisfies a problem-facing
/// equation or defect measure.
/// </para>
/// <para>
/// Residual magnitudes are not comparable across unrelated numerical problems. For example,
/// <c>|f(x)|</c>, <c>||A*x-b||</c> and <c>||A*v-lambda*v||</c> have different scales and units even
/// though each is legitimately called a residual.
/// </para>
/// </remarks>
public sealed record IterativeResult<T>(
    T Value,
    int Iterations,
    IterationStatus Status,
    double Residual = double.NaN,
    string? Message = null)
{
    /// <summary>
    /// Gets whether the algorithm reported that its convergence criterion was satisfied.
    /// </summary>
    public bool Converged => Status == IterationStatus.Converged;

    /// <summary>
    /// Gets whether <see cref="Residual"/> contains a finite residual suitable for comparison or logging.
    /// </summary>
    /// <remarks>
    /// A non-converged result can still have a finite and useful residual. Conversely, a numerical
    /// breakdown can legitimately leave the generic residual unavailable. This property therefore
    /// describes residual availability only; it is not another convergence flag.
    /// </remarks>
    public bool HasFiniteResidual => double.IsFinite(Residual);
}
