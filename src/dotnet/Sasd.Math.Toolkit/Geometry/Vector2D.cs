namespace Sasd.Numerics.Geometry;

/// <summary>
/// Lightweight immutable 2D vector for future graphics/game-toolkit integration.
/// </summary>
public readonly record struct Vector2D(double X, double Y)
{
    public double Length => System.Math.Sqrt((X * X) + (Y * Y));
    public Vector2D Normalized => Length == 0.0 ? this : this / Length;
    public static double Dot(Vector2D left, Vector2D right) => (left.X * right.X) + (left.Y * right.Y);
    public static Vector2D operator +(Vector2D left, Vector2D right) => new(left.X + right.X, left.Y + right.Y);
    public static Vector2D operator -(Vector2D left, Vector2D right) => new(left.X - right.X, left.Y - right.Y);
    public static Vector2D operator *(Vector2D vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);
    public static Vector2D operator /(Vector2D vector, double scalar) => new(vector.X / scalar, vector.Y / scalar);
}
