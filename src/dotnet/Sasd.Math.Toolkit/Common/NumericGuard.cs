namespace Sasd.Numerics.Common;

internal static class NumericGuard
{
    public static void Positive(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be finite and greater than zero.");
        }
    }

    public static void Positive(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be greater than zero.");
        }
    }

    public static void Finite(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, "Value must be finite.");
        }
    }

    public static void SameLength<TLeft, TRight>(
        IReadOnlyCollection<TLeft> left,
        IReadOnlyCollection<TRight> right,
        string leftName,
        string rightName)
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);

        if (left.Count != right.Count)
        {
            throw new ArgumentException($"{leftName} and {rightName} must have the same number of elements.");
        }
    }
}
