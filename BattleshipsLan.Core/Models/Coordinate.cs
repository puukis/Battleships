namespace BattleshipsLan.Core.Models;

public struct Coordinate
{
    public int X { get; }
    public int Y { get; }

    public Coordinate(int x, int y)
    {
        X = x;
        Y = y;
    }

    public override string ToString() => $"{(char)('A' + X)}{Y + 1}";

    public override bool Equals(object? obj) => obj is Coordinate other && X == other.X && Y == other.Y;
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public static bool operator ==(Coordinate left, Coordinate right) => left.Equals(right);
    public static bool operator !=(Coordinate left, Coordinate right) => !(left == right);
}
