using System.Text.Json.Serialization;
using BattleshipsLan.Core.Models;

namespace BattleshipsLan.Network.Protocol;

public enum MessageType
{
    Handshake,
    ShipPlacementFinished,
    FireShot,
    ShotResult,
    AbilityRowBomb,
    AbilityResult,
    GameResult
}

[JsonDerivedType(typeof(HandshakeMessage), typeDiscriminator: "Handshake")]
[JsonDerivedType(typeof(ShipPlacementFinishedMessage), typeDiscriminator: "ShipPlacementFinished")]
[JsonDerivedType(typeof(FireShotMessage), typeDiscriminator: "FireShot")]
[JsonDerivedType(typeof(ShotResultMessage), typeDiscriminator: "ShotResult")]
[JsonDerivedType(typeof(AbilityRowBombMessage), typeDiscriminator: "AbilityRowBomb")]
[JsonDerivedType(typeof(AbilityResultMessage), typeDiscriminator: "AbilityResult")]
[JsonDerivedType(typeof(GameResultMessage), typeDiscriminator: "GameResult")]
public abstract class Message
{
    public MessageType Type { get; set; }
}

public class HandshakeMessage : Message
{
    public HandshakeMessage() { Type = MessageType.Handshake; }
}

public class ShipPlacementFinishedMessage : Message
{
    public ShipPlacementFinishedMessage() { Type = MessageType.ShipPlacementFinished; }
}

public class FireShotMessage : Message
{
    public int X { get; set; }
    public int Y { get; set; }
    public FireShotMessage() { Type = MessageType.FireShot; }
    public FireShotMessage(int x, int y) : this() { X = x; Y = y; }
}

public class ShotResultMessage : Message
{
    public bool IsHit { get; set; }
    public bool IsSunk { get; set; }
    public ShipType? SunkShipType { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    
    public ShotResultMessage() { Type = MessageType.ShotResult; }
}

public class AbilityRowBombMessage : Message
{
    public int RowY { get; set; }
    public AbilityRowBombMessage() { Type = MessageType.AbilityRowBomb; }
    public AbilityRowBombMessage(int rowY) : this() { RowY = rowY; }
}

public class AbilityResultMessage : Message
{
    public List<ShotResultMessage> Results { get; set; } = new();
    public AbilityResultMessage() { Type = MessageType.AbilityResult; }
}

public class GameResultMessage : Message
{
    public bool YouWon { get; set; }
    public GameResultMessage() { Type = MessageType.GameResult; }
}
