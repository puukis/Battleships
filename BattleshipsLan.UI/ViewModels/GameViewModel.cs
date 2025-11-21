using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Threading;
using BattleshipsLan.Core.Models;
using BattleshipsLan.Network;
using BattleshipsLan.Network.Protocol;

namespace BattleshipsLan.UI.ViewModels;

public class GameViewModel : ViewModelBase
{
    private readonly NetworkManager _network;
    private Board _myBoard;
    private Board _enemyBoard; // Fog of war board
    private GameState _gameState;
    private string _statusMessage = "Welcome to Battleship LAN";
    private bool _isMyTurn;
    private int _turnCount;
    private int _lastRowBombTurn = -5; // Allow immediately if we want, or wait. Let's wait 5 turns.
    private string _ipAddressInput = "127.0.0.1";
    private string _portInput = "5555";
    private string _hostIpDisplay = "Fetching...";
    private bool _isConnected;
    private bool _isHost;
    
    // Ship Placement
    private ShipOrientation _currentOrientation = ShipOrientation.Horizontal;
    
    public ObservableCollection<string> Logs { get; } = new();

    public GameViewModel()
    {
        _network = new NetworkManager();
        _network.OnMessageReceived += HandleMessage;
        _network.OnConnected += () => Dispatcher.UIThread.InvokeAsync(() => 
        {
            IsConnected = true;
            StatusMessage = "Connected! Place your ships.";
            GameState = GameState.Setup;
            _network.SendMessageAsync(new HandshakeMessage());
        });
        _network.OnDisconnected += () => Dispatcher.UIThread.InvokeAsync(() => 
        {
            IsConnected = false;
            StatusMessage = "Disconnected.";
            GameState = GameState.GameOver;
        });

        _myBoard = new Board();
        _enemyBoard = new Board(); // Tracks what we know about enemy
        GameState = GameState.Menu;
        HostIpDisplay = NetworkManager.GetLocalIPAddress();
        IpAddressInput = HostIpDisplay;
    }

    // Properties
    public Board MyBoard => _myBoard;
    public Board EnemyBoard => _enemyBoard;
    
    public GameState GameState 
    { 
        get => _gameState; 
        set 
        {
            if (RaiseAndSetIfChanged(ref _gameState, value))
            {
                OnPropertyChanged(nameof(IsMenuVisible));
                OnPropertyChanged(nameof(IsPlacementVisible));
                OnPropertyChanged(nameof(IsGameVisible));
            }
        }
    }

    public bool IsMenuVisible => GameState == GameState.Menu;
    public bool IsPlacementVisible => GameState == GameState.Setup;
    public bool IsGameVisible => GameState == GameState.Playing || GameState == GameState.GameOver;

    public string StatusMessage 
    { 
        get => _statusMessage; 
        set => RaiseAndSetIfChanged(ref _statusMessage, value); 
    }

    public bool IsMyTurn 
    { 
        get => _isMyTurn; 
        set 
        {
            RaiseAndSetIfChanged(ref _isMyTurn, value);
            OnPropertyChanged(nameof(CanUseRowBomb));
        }
    }

    public string IpAddressInput 
    { 
        get => _ipAddressInput; 
        set => RaiseAndSetIfChanged(ref _ipAddressInput, value); 
    }
    
    public string PortInput 
    { 
        get => _portInput; 
        set => RaiseAndSetIfChanged(ref _portInput, value); 
    }

    public string HostIpDisplay 
    { 
        get => _hostIpDisplay; 
        set => RaiseAndSetIfChanged(ref _hostIpDisplay, value); 
    }

    public bool IsConnected 
    { 
        get => _isConnected; 
        set => RaiseAndSetIfChanged(ref _isConnected, value); 
    }

    public bool CanUseRowBomb => IsMyTurn && GameState == GameState.Playing && (_turnCount - _lastRowBombTurn >= 5);
    public string RowBombText => CanUseRowBomb ? "Row Bomb Ready!" : $"Row Bomb (Cooldown: {5 - (_turnCount - _lastRowBombTurn)})";

    public IEnumerable<TileStatus> FlattenedMyBoard => _myBoard.Grid.Cast<TileStatus>();
    public IEnumerable<TileStatus> FlattenedEnemyBoard => _enemyBoard.Grid.Cast<TileStatus>();

    private void RefreshBoards()
    {
        OnPropertyChanged(nameof(FlattenedMyBoard));
        OnPropertyChanged(nameof(FlattenedEnemyBoard));
    }

    // Actions
    public async void HostGame()
    {
        if (int.TryParse(PortInput, out int port))
        {
            try
            {
                StatusMessage = $"Hosting on port {port}...";
                _isHost = true;
                await _network.StartHostAsync(port);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error hosting: {ex.Message}";
                _isHost = false;
            }
        }
    }

    public async void JoinGame()
    {
        if (int.TryParse(PortInput, out int port))
        {
            StatusMessage = $"Connecting to {IpAddressInput}:{port}...";
            _isHost = false;
            await _network.ConnectAsync(IpAddressInput, port);
        }
    }

    public ObservableCollection<ShipType> RemainingShips { get; } = new(Enum.GetValues<ShipType>());
    
    public string CurrentOrientationText => _currentOrientation.ToString();

    public void ToggleOrientation()
    {
        _currentOrientation = _currentOrientation == ShipOrientation.Horizontal ? ShipOrientation.Vertical : ShipOrientation.Horizontal;
        OnPropertyChanged(nameof(CurrentOrientationText));
    }

    public void PlaceShipByIndex(int index)
    {
        if (RemainingShips.Count == 0) return;
        
        var shipType = RemainingShips[0]; // Simple: place in order
        var ship = new Ship(shipType);
        
        int x = index / Board.Size;
        int y = index % Board.Size;
        
        if (_myBoard.PlaceShip(ship, new Coordinate(x, y), _currentOrientation))
        {
            RemainingShips.RemoveAt(0);
            RefreshBoards();
            StatusMessage = $"Placed {shipType}. Next: {(RemainingShips.Count > 0 ? RemainingShips[0] : "None")}";
        }
        else
        {
            StatusMessage = "Invalid placement!";
        }
    }

    public void RandomizeShips()
    {
        _myBoard.RandomizeShips();
        RemainingShips.Clear();
        RefreshBoards();
    }

    public async void ConfirmPlacement()
    {
        if (_myBoard.Ships.Count < 5) // Assuming 5 ships total
        {
            StatusMessage = "Place all ships first!";
            return;
        }
        
        StatusMessage = "Waiting for opponent...";
        await _network.SendMessageAsync(new ShipPlacementFinishedMessage());
        CheckStartGame();
    }

    private bool _opponentReady = false;
    private bool _iAmReady = false;

    private void CheckStartGame()
    {
        _iAmReady = true;
        if (_opponentReady && _iAmReady)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        GameState = GameState.Playing;
        IsMyTurn = _isHost; // Host starts first
        StatusMessage = IsMyTurn ? "Your Turn!" : "Opponent's Turn";
        _turnCount = 0;
    }

    public void FireShotByIndex(int index)
    {
        int x = index / Board.Size; // Row-major? No, Cast<TileStatus> flattens by row then column.
        // Array is [x, y]. 
        // Cast<T> on 2D array iterates: [0,0], [0,1]... [0,9], [1,0]...
        // So index = x * Size + y.
        // x = index / Size
        // y = index % Size
        int y = index % Board.Size;
        x = index / Board.Size;
        
        FireShot(x, y);
    }

    public async void FireShot(int x, int y)
    {
        if (!IsMyTurn || GameState != GameState.Playing) return;
        if (_enemyBoard.Grid[x, y] != TileStatus.Empty) return; // Already shot there

        IsMyTurn = false;
        StatusMessage = "Firing...";
        await _network.SendMessageAsync(new FireShotMessage(x, y));
    }

    public async void UseRowBomb(int rowY)
    {
        if (!CanUseRowBomb) return;

        IsMyTurn = false;
        _lastRowBombTurn = _turnCount;
        StatusMessage = "Launching Row Bomb...";
        await _network.SendMessageAsync(new AbilityRowBombMessage(rowY));
    }

    private void HandleMessage(Message msg)
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            switch (msg)
            {
                case HandshakeMessage:
                    // Handshake received
                    break;
                case ShipPlacementFinishedMessage:
                    _opponentReady = true;
                    if (_iAmReady) StartGame();
                    else StatusMessage = "Opponent is ready!";
                    break;
                case FireShotMessage fireMsg:
                    HandleIncomingShot(fireMsg);
                    break;
                case ShotResultMessage resultMsg:
                    HandleShotResult(resultMsg);
                    break;
                case AbilityRowBombMessage bombMsg:
                    HandleIncomingRowBomb(bombMsg);
                    break;
                case AbilityResultMessage abilityResult:
                    HandleAbilityResult(abilityResult);
                    break;
                case GameResultMessage gameResult:
                    GameState = GameState.GameOver;
                    StatusMessage = gameResult.YouWon ? "You Won!" : "You Lost!";
                    break;
            }
        });
    }

    private async void HandleIncomingShot(FireShotMessage msg)
    {
        var coord = new Coordinate(msg.X, msg.Y);
        var (isHit, sunkShip) = _myBoard.ReceiveShot(coord);
        
        RefreshBoards();

        var resultMsg = new ShotResultMessage
        {
            IsHit = isHit,
            IsSunk = sunkShip != null,
            SunkShipType = sunkShip?.Type,
            X = msg.X,
            Y = msg.Y
        };
        await _network.SendMessageAsync(resultMsg);

        if (_myBoard.IsAllSunk())
        {
            await _network.SendMessageAsync(new GameResultMessage { YouWon = true }); // Tell opponent they won
            GameState = GameState.GameOver;
            StatusMessage = "Defeat!";
        }
        else
        {
            IsMyTurn = true;
            _turnCount++;
            StatusMessage = "Your Turn!";
            OnPropertyChanged(nameof(RowBombText));
        }
    }

    private void HandleShotResult(ShotResultMessage msg)
    {
        _enemyBoard.Grid[msg.X, msg.Y] = msg.IsHit ? TileStatus.Hit : TileStatus.Miss;
        RefreshBoards();

        if (msg.IsSunk)
        {
            StatusMessage = $"You sunk their {msg.SunkShipType}!";
        }
        else
        {
            StatusMessage = msg.IsHit ? "Hit!" : "Miss!";
        }
        
        // Turn ends after result processed? No, turn ended when we fired. 
        // Wait for opponent to fire now.
        StatusMessage += " Opponent's Turn.";
    }

    private async void HandleIncomingRowBomb(AbilityRowBombMessage msg)
    {
        var results = new List<ShotResultMessage>();
        for (int x = 0; x < Board.Size; x++)
        {
            var coord = new Coordinate(x, msg.RowY);
            var (isHit, sunkShip) = _myBoard.ReceiveShot(coord);
            results.Add(new ShotResultMessage
            {
                IsHit = isHit,
                IsSunk = sunkShip != null,
                SunkShipType = sunkShip?.Type,
                X = x,
                Y = msg.RowY
            });
        }
        RefreshBoards();

        await _network.SendMessageAsync(new AbilityResultMessage { Results = results });

        if (_myBoard.IsAllSunk())
        {
            await _network.SendMessageAsync(new GameResultMessage { YouWon = true });
            GameState = GameState.GameOver;
            StatusMessage = "Defeat!";
        }
        else
        {
            IsMyTurn = true;
            _turnCount++;
            StatusMessage = "Your Turn!";
            OnPropertyChanged(nameof(RowBombText));
        }
    }

    private void HandleAbilityResult(AbilityResultMessage msg)
    {
        foreach (var res in msg.Results)
        {
            _enemyBoard.Grid[res.X, res.Y] = res.IsHit ? TileStatus.Hit : TileStatus.Miss;
        }
        RefreshBoards();
        StatusMessage = "Row Bomb Impact Confirmed! Opponent's Turn.";
    }
}
