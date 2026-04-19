# Farkle CSharp

#### Program.cs
##### Written by: Samuel Jones
####

## Application Purpose
Farkle CSharp provides a Farkle game. Clients may connect, watch two CPUs duel, play against another client, or play against a CPU.

## Application User Guide
### Installation
1. Download GitHub Repo as ZIP
2. Unzip it
3. Install Dotnet (I use version 5.0.404)

### Network
1. Program.cs > set "NETWORK_IP" to your chosen server ip
2. Program.cs > set "NETWORK_PORT" to your chosen port

### Basic Start up
1. Run "dotnet run -- server" in terminal to launch the server
2. When server is running -> run "dotnet run -- client" in terminal to launch the client

### How to play Farkle
One can find guides online.

### Custom scoring
    1 = 100
    5 = 50
    3 * 2 = 200
    3 * 3 = 300
    3 * 4 = 400
    3 * 5 = 500
    3 * 6 = 600
    3 * 1 = 1000

    4 * 2 = 400
    4 * 3 = 600
    4 * 4 = 800
    4 * 5 = 1000
    4 * 6 = 1200
    4 * 1 = 2000

    5 * 2 = 800
    5 * 3 = 1200
    5 * 4 = 1600
    5 * 5 = 2000
    5 * 6 = 2400
    5 * 1 = 4000

    6 * 2 = 1600
    6 * 3 = 2400
    6 * 4 = 3200
    6 * 5 = 4000
    6 * 6 = 4800
    6 * 1 = 8000

    112233 = Nothing
    123456 = 1800

### PvP
1. Start up a server in a terminal window
1. Start up a client in a 2nd terminal window
2. Type "pp" then press Enter
3. Start up another client in a 3rd terminal window
4. Follow game instructions
5. When the game ends, the program will close. To play again, only the client needs to be restarted.

### Player vs Computer
1. Start up a server in a terminal window
1. Start up a client in a 2nd terminal window
2. Type "pc" then press Enter
4. Follow game instructions
5. When the game ends, the program will close. To play again, only the client needs to be restarted.

### Computer vs Computer
1. Start up a server in a terminal window
1. Start up a client in a 2nd terminal window
2. Type "cc" then press Enter
4. Look at the game events provided
5. When the game ends, the program will close. To play again, only the client needs to be restarted.

![A PvP Game](https://github.com/notsamj/Farkle-CSharp/blob/master/Runtime%20Screenshots/pvp.png?raw=true)
![A Player vs CPU Game](https://github.com/notsamj/Farkle-CSharp/blob/main/Runtime%20Screenshots/pvc.png?raw=true)
![A CPUvsCPU Game](https://github.com/notsamj/Farkle-CSharp/blob/main/Runtime%20Screenshots/cvc.png?raw=true)