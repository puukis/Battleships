namespace BattleshipsLan.Core.Models;

public enum TileStatus
{
    Empty,
    Ship,
    Hit,
    Miss
}

public enum GameState
{
    Menu,
    Setup,
    Playing,
    GameOver
}

public enum ShipOrientation
{
    Horizontal,
    Vertical
}

public enum ShipType
{
    Carrier = 5,
    Battleship = 4,
    Cruiser = 3,
    Submarine = 3,
    Destroyer = 2
}
