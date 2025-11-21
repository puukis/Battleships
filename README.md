# Battleship LAN Walkthrough

## Build Instructions

1.  **Prerequisites**: Ensure you have the .NET 9.0 SDK installed.
2.  **Build**: Open a terminal in the project root and run:
    ```bash
    dotnet build
    ```
3.  **Publish (Optional)**: To create a standalone app bundle for macOS:
    ```bash
    dotnet publish BattleshipsLan.UI -c Release -r osx-arm64 --self-contained
    ```
    (Use `osx-x64` for Intel Macs).

## How to Run

You need two instances of the game running on the same network (LAN).

### Host
1.  Run the app:
    ```bash
    dotnet run --project BattleshipsLan.UI
    ```
2.  In the Main Menu, note your IP address displayed.
3.  Enter a port (default 5000) and click **Host**.
4.  Wait for the opponent to connect.

### Client (Join)
1.  Run the app on a second machine (or second terminal window):
    ```bash
    dotnet run --project BattleshipsLan.UI
    ```
2.  In the Main Menu, enter the **Host's IP address**.
3.  Enter the same port (default 5000).
4.  Click **Join**.

## Gameplay

1.  **Ship Placement**:
    - Click **Randomize Fleet** to place ships automatically.
    - Or select a ship from the list, toggle orientation, and click on the board to place it.
    - Once all ships are placed, click **Confirm Placement**.
2.  **Battle**:
    - The game starts once both players confirm placement.
    - Take turns firing at the enemy board by clicking on cells.
    - **Row Bomb**: Once every 5 turns, the "Row Bomb" button becomes active. Select a row index (0-9) and click "Fire Row Bomb" to hit an entire row!
3.  **Win Condition**:
    - Sink all enemy ships to win.

## Troubleshooting
- **Firewall**: macOS might ask to allow network connections. Click "Allow".
- **Connection Failed**: Ensure both devices are on the same Wi-Fi/LAN and the IP address is correct.
