namespace BattleshipsLan.Core.Models;

public class Board
{
    public const int Size = 10;
    public TileStatus[,] Grid { get; } = new TileStatus[Size, Size];
    public List<Ship> Ships { get; } = new();

    public Board()
    {
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                Grid[x, y] = TileStatus.Empty;
    }

    public bool PlaceShip(Ship ship, Coordinate position, ShipOrientation orientation)
    {
        // Check bounds
        int endX = position.X + (orientation == ShipOrientation.Horizontal ? ship.Size - 1 : 0);
        int endY = position.Y + (orientation == ShipOrientation.Vertical ? ship.Size - 1 : 0);

        if (endX >= Size || endY >= Size) return false;

        // Check overlap
        for (int i = 0; i < ship.Size; i++)
        {
            int x = position.X + (orientation == ShipOrientation.Horizontal ? i : 0);
            int y = position.Y + (orientation == ShipOrientation.Vertical ? i : 0);
            if (Grid[x, y] != TileStatus.Empty) return false;
        }

        // Place
        ship.Position = position;
        ship.Orientation = orientation;
        Ships.Add(ship);

        for (int i = 0; i < ship.Size; i++)
        {
            int x = position.X + (orientation == ShipOrientation.Horizontal ? i : 0);
            int y = position.Y + (orientation == ShipOrientation.Vertical ? i : 0);
            Grid[x, y] = TileStatus.Ship;
        }

        return true;
    }

    public (bool isHit, Ship? sunkShip) ReceiveShot(Coordinate coord)
    {
        if (coord.X < 0 || coord.X >= Size || coord.Y < 0 || coord.Y >= Size)
            return (false, null);

        if (Grid[coord.X, coord.Y] == TileStatus.Ship)
        {
            Grid[coord.X, coord.Y] = TileStatus.Hit;
            var ship = Ships.FirstOrDefault(s => s.GetOccupiedCoordinates().Contains(coord));
            if (ship != null)
            {
                ship.RegisterHit();
                return (true, ship.IsSunk ? ship : null);
            }
            return (true, null);
        }
        else if (Grid[coord.X, coord.Y] == TileStatus.Empty)
        {
            Grid[coord.X, coord.Y] = TileStatus.Miss;
        }

        return (false, null);
    }

    public bool IsAllSunk()
    {
        return Ships.All(s => s.IsSunk);
    }
    
    public void RandomizeShips()
    {
        Ships.Clear();
        for (int x = 0; x < Size; x++)
            for (int y = 0; y < Size; y++)
                Grid[x, y] = TileStatus.Empty;

        var types = Enum.GetValues<ShipType>();
        var rng = new Random();

        foreach (var type in types)
        {
            bool placed = false;
            while (!placed)
            {
                var ship = new Ship(type);
                int x = rng.Next(Size);
                int y = rng.Next(Size);
                var orientation = (ShipOrientation)rng.Next(2);
                
                placed = PlaceShip(ship, new Coordinate(x, y), orientation);
            }
        }
    }
}
