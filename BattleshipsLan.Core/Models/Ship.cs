namespace BattleshipsLan.Core.Models;

public class Ship
{
    public ShipType Type { get; }
    public int Size => (int)Type;
    public Coordinate Position { get; set; }
    public ShipOrientation Orientation { get; set; }
    public int Hits { get; private set; }

    public bool IsSunk => Hits >= Size;

    public Ship(ShipType type)
    {
        Type = type;
        Hits = 0;
    }

    public void RegisterHit()
    {
        Hits++;
    }

    public List<Coordinate> GetOccupiedCoordinates()
    {
        var coords = new List<Coordinate>();
        for (int i = 0; i < Size; i++)
        {
            int x = Position.X + (Orientation == ShipOrientation.Horizontal ? i : 0);
            int y = Position.Y + (Orientation == ShipOrientation.Vertical ? i : 0);
            coords.Add(new Coordinate(x, y));
        }
        return coords;
    }
}
