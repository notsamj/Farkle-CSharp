#nullable disable

using System;
using System.Text.RegularExpressions;
using System.Collections; // Using for array-list

using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

/*
    Note: using custom rules + the one's listed on Wikipedia modified

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
*/
namespace Farkle_CSharp {

    /*
        Class Name: Program
        Description: Main class
    */
    class Program {
        static int CPU_ESTIMATED_POINTS_PER_TURN = 500; // used for bots
        static int CPU_MIN_POINTS_TO_HOLD = 300; // used for bots

        static int NETWORK_BUFFER_SIZE = 1024;
        static int NETWORK_PORT = 8080;
        static string NETWORK_IP = "127.0.0.1";

        static Random random = new Random();
        static ArrayList extraBuffer = new ArrayList();

        /*
            Method Name: Main
            Method Parameters: 
                args:
                    An array of string arguments 
            Method Description: Main method
            Method Return: Promise (implicit)
        */
        static async Task Main(string[] args) {
            Console.WriteLine("Welcome to Farkle!");

            // Expect 1 arg
            if (args.Length != 1){
                throw new Exception("Unexpected argment length: " + args.Length.ToString());
            }

            string mode = args[0];
            Console.WriteLine("Selected mode: " + mode);

            // Check which mode
            if (mode == "server"){
                await LaunchServer();
            }else if (mode == "client"){
                await LaunchClient();
            }else{
                Console.WriteLine("Mode: " + mode + " not found!");
            }
        }

        /*
            Method Name: LaunchClient
            Method Parameters: None
            Method Description: Runs a game as a client
            Method Return: Promise (implicit)
        */
        static async Task LaunchClient(){
            int serverPort = NETWORK_PORT;

            IPAddress ipAddress = IPAddress.Parse(NETWORK_IP); // localhost

            // Note: Using tells the program to call .Dispose() at end of the function
            using TcpClient client = new TcpClient();

            // Do server connection here
            try {
                await client.ConnectAsync(ipAddress, serverPort);
                await using NetworkStream networkStream = client.GetStream();

                Console.WriteLine("Client connected to server!");

                bool connectionActive = true;

                // Loop input and ouput while connected
                while (connectionActive){
                    string messageFromServer = await ReceiveMessageFromNetworkStream(networkStream);

                    // If connection failed
                    if (messageFromServer == null){
                        Console.WriteLine("Connection to server ended unexpectedly!");
                        connectionActive = false;
                        continue;
                    }

                    // Expect format: (io)|o|c,message. Where message has no commas
                    string[] responseSplit = messageFromServer.Split(',');
                    if (responseSplit.Length != 2 || (responseSplit[0] != "io" && responseSplit[0] != "o" && responseSplit[0] != "c")){
                        Console.WriteLine("Invalid format message received from server: \"" + messageFromServer + "\"");
                        connectionActive = false;
                        continue;
                    }

        
                    string messageType = responseSplit[0];
                    
                    // Server is asking for a response
                    if (messageType == "io"){
                        Console.Write(responseSplit[1] + "\n> ");
                        string userInput = Console.ReadLine().Trim();
                        await SendMessageOverNetworkStream(networkStream, userInput);
                    }
                    // Received a closing message
                    else if (messageType == "c"){
                        Console.WriteLine(responseSplit[1]);
                        connectionActive = false;
                    }
                    else{ // o
                        Console.WriteLine(responseSplit[1]);
                    }
                } 
            }
            // For errors
            catch (Exception exception){
                Console.WriteLine($"Client error: {exception.Message}");
            }

            // Goodbye
            Console.WriteLine("Disconnected. Program closing!");
        }

        /*
            Method Name: SendMessageOverNetworkStream
            Method Parameters: 
                networkStream:
                    A network stream between two endpoints
                message:
                    A message to send
            Method Description: Sends a message over a network stream
            Method Return: Promise (implicit)
        */
        static async Task SendMessageOverNetworkStream(NetworkStream networkStream, string message){
            byte[] networkBuffer = Encoding.UTF8.GetBytes(message + "\n"); // \n ends the message
            await networkStream.WriteAsync(networkBuffer, 0, networkBuffer.Length);
        }

        /*
            Method Name: SendMessageOverNetworkStream
            Method Parameters: 
                networkStream:
                    A network stream between two endpoints
            Method Description: Receives a message either from an "extraBuffer" or from the network stream
            Method Return: Promise containing a string
        */
        static async Task<string> ReceiveMessageFromNetworkStream(NetworkStream networkStream){
            // If I have stuff in extra buffer
            if (extraBuffer.Count > 0){
                string earlierMessage = (string) extraBuffer[0];
                extraBuffer.RemoveAt(0);
                return earlierMessage;
            }

            int bufferSize = NETWORK_BUFFER_SIZE;

            byte[] networkBuffer = new byte[bufferSize];
            int messageLength = await networkStream.ReadAsync(networkBuffer);

            // Failed to get a message
            if (messageLength == 0){
                return null;
            }

            // Return message received
            string messageFromNS = Encoding.UTF8.GetString(networkBuffer, 0, messageLength);
            string[] msgSplit = messageFromNS.Split('\n');
            
            // Add all messages to message array
            for (int i = 0; i < msgSplit.Length; i++){
                string msg = msgSplit[i].Trim();
                // Skip blank!
                if (msg.Length == 0){ continue; }

                extraBuffer.Add(msg);
            }


            string firstMessage = (string) extraBuffer[0];
            // Remove it from buffer
            extraBuffer.RemoveAt(0);

            return firstMessage;
        }

        /*
            Method Name: LaunchServer
            Method Parameters: None
            Method Description: Launches the server
            Method Return: Promise (implicit)
        */
        static async Task LaunchServer(){
            int serverPort = NETWORK_PORT;
            IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Any, serverPort);

            TcpListener tcpListener = new TcpListener(serverIpEndPoint);

            // Try starting up
            try {
                Console.WriteLine("Starting up the server!");
                // Call start
                tcpListener.Start();

                Console.WriteLine("Server has started!");

                // Run forever -> require CTRL + C to shut down
                while (true){
                    // Note: Using tells the program to call .Dispose() at end of try{...}
                    using TcpClient tcpClientHandlerMain = await tcpListener.AcceptTcpClientAsync();

                    await using NetworkStream mainClientNS = tcpClientHandlerMain.GetStream();

                    Console.WriteLine("Session started with new Client!");

                    // Run a session
                    await RunSession(tcpListener, mainClientNS);

                    Console.WriteLine("Session has ended with Client!");
                }
            }
            // Errors
            catch (Exception exception){
                Console.WriteLine($"Server error: {exception.Message}");
            }
            // Shut down
            finally {
                Console.WriteLine("Server shutting down!");
                tcpListener.Stop();
            }

            Console.WriteLine("Program closing!");
        }

        /*
            Method Name: RunSession
            Method Parameters: 
                tcpListener:
                    A tcp listener for accepting new clients
                primaryClientNS:
                    A network stream between the server and its primary client
            Method Description: Runs a game session
            Method Return: Promise (implicit)
        */
        static async Task RunSession(TcpListener tcpListener, NetworkStream primaryClientNS){
            string gameType = "none";
            string clientChoice = "";

            // While game type is not set
            while (gameType == "none"){
                // Send client menu
                await SendMessageOverNetworkStream(primaryClientNS, "io," + "Please select a game type: pp - player vs player | pc - player vs cpu | cc - cpu vs cpu");

                // Receive client choice
                clientChoice = await ReceiveMessageFromNetworkStream(primaryClientNS);

                // If client choice is acceptable -> set game type;
                if (clientChoice == "pp" || clientChoice == "pc" || clientChoice == "cc"){
                    gameType = clientChoice;
                }
                // Not acceptable
                else{
                    await SendMessageOverNetworkStream(primaryClientNS, "o," + "Invalid choice: \"" + clientChoice + "\"");
                }
            }

            // We have the type

            // If pvp
            if (clientChoice == "pp"){
                await RunPvPOnline(tcpListener, primaryClientNS);
            }
            // If player vs cpu
            else if (clientChoice == "pc"){
                await RunPlayervCPUOnline(primaryClientNS);
            }
            // CPU vs cpu
            else{
                await RunCPUvCPUOnline(primaryClientNS);
            }

        }

        /*
            Method Name: RunCPUvCPUOnline
            Method Parameters: 
                primaryClientNS:
                    A network stream between the server and its primary client
            Method Description: Runs a game between two Computer Player Users
            Method Return: Promise (implicit)
        */
        static async Task RunCPUvCPUOnline(NetworkStream primaryClientNS){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            // Create a list for sending messages later
            ArrayList clientNSList = new ArrayList();
            clientNSList.Add(primaryClientNS);

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    await ServerSendClient(primaryClientNS, "Turn: Player 1. Points remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = await MakeOnlineCPUMove(clientNSList, player1Score, scoreForWin, (scoreForWin-player2Score));
                    await ServerSendClient(primaryClientNS, "Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    await ServerSendClient(primaryClientNS, "Turn: Player 2. Points remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = await MakeOnlineCPUMove(clientNSList, player2Score, scoreForWin, (scoreForWin-player1Score));
                    await ServerSendClient(primaryClientNS, "Player 2 ends their turn with " + player2Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }
                
                // Swap turn
                player1Turn = !player1Turn;
            }

            // Determine winner
            if (player1Score >= scoreForWin){
                await ServerSendClose(primaryClientNS, "Player 1 has won with " + player1Score.ToString() + " points.");
                await ServerSendClose(primaryClientNS, "Player 2 has lost with "+ player2Score.ToString() + " points.");
            }else{
                await ServerSendClose(primaryClientNS, "Player 2 has won with " + player2Score.ToString() + " points.");
                await ServerSendClose(primaryClientNS, "Player 1 has lost with "+ player1Score.ToString() + " points.");
            }
        }

        /*
            Method Name: RunCpuVCpuLocal
            Method Parameters: None
            Method Description: Runs a local game between two Computer Player Users
            Method Return: void
        */
        static void RunCpuVCpuLocal(){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    Console.WriteLine("Turn: Player 1. Points remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = MakeLocalCPUMove(player1Score, scoreForWin, (scoreForWin-player2Score));
                    Console.WriteLine("Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    Console.WriteLine("Turn: Player 2. Points remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = MakeLocalCPUMove(player2Score, scoreForWin, (scoreForWin-player1Score));
                    Console.WriteLine("Player 2 ends their turn with " + player2Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }
                
                // Swap turn
                player1Turn = !player1Turn;
            }

            // Determine winner
            if (player1Score >= scoreForWin){
                Console.WriteLine("Player 1 has won with " + player1Score.ToString() + " points.");
                Console.WriteLine("Player 2 has lost with "+ player2Score.ToString() + " points.");
            }else{
                Console.WriteLine("Player 2 has won with " + player2Score.ToString() + " points.");
                Console.WriteLine("Player 1 has lost with "+ player1Score.ToString() + " points.");
            }
        }

        /*
            Method Name: ServerSendAll
            Method Parameters: 
                client1NS:
                    A network stream between the server and client1
                client2NS:
                    A network stream between the server and client2
                message:
                    A message to send
            Method Description: Prints a message and sends to two clients
            Method Return: Promise (implicit)
        */
        static async Task ServerSendAll(NetworkStream client1NS, NetworkStream client2NS, string message){
            Console.WriteLine(message);
            await SendMessageOverNetworkStream(client1NS, "o,"+message);
            await SendMessageOverNetworkStream(client2NS, "o,"+message);
        }


        /*
            Method Name: ServerSendClient
            Method Parameters: 
                clientNS:
                    A network stream between the server and a client
                message:
                    A message to send
            Method Description: Prints a message and sends to a client
            Method Return: Promise (implicit)
        */
        static async Task ServerSendClient(NetworkStream clientNS, string message){
            Console.WriteLine(message);
            await SendMessageOverNetworkStream(clientNS, "o,"+message);
        }

        /*
            Method Name: ServerSendClientList
            Method Parameters: 
                clientList:
                    List of network streams between the server and clients
                message:
                    A message to send
            Method Description: Prints a message and sends to all listed clients
            Method Return: Promise (implicit)
        */
        static async Task ServerSendClientList(ArrayList clientList, string message){
            Console.WriteLine(message);

            // Loop through all clients and send
            foreach (NetworkStream clientNS in clientList){
                await SendMessageOverNetworkStream(clientNS, "o,"+message);
            }
        }

        /*
            Method Name: ServerSendClientNoPrint
            Method Parameters: 
                clientNS:
                    A network stream between the server and a client
                message:
                    A message to send
            Method Description: Sends a message to a client
            Method Return: Promise (implicit)
        */
        static async Task ServerSendClientNoPrint(NetworkStream clientNS, string message){
            await SendMessageOverNetworkStream(clientNS, "o,"+message);
        }

        /*
            Method Name: ServerSendClose
            Method Parameters: 
                clientNS:
                    A network stream between the server and a client
                message:
                    A message to send
            Method Description: Prints and sends a closing message to a client
            Method Return: Promise (implicit)
        */
        static async Task ServerSendClose(NetworkStream clientNS, string message){
            Console.WriteLine(message);
            await SendMessageOverNetworkStream(clientNS, "c,"+message);
        }

        /*
            Method Name: ServerSendClose
            Method Parameters: 
                client1NS:
                    A network stream between the server and client1
                client2NS:
                    A network stream between the server and client2
                message:
                    A message to send
            Method Description: Prints and sends a closing message to both clients
            Method Return: Promise (implicit)
        */
        static async Task ServerSendClose(NetworkStream client1NS, NetworkStream client2NS, string message){
            await ServerSendClose(client1NS, message);
            // Only print once to server
            await SendMessageOverNetworkStream(client2NS, "c,"+message);
        }

        /*
            Method Name: ServerSendIO
            Method Parameters: 
                clientNS:
                    A network stream between the server and a client
                message:
                    A message to send
            Method Description: Requests for and returns input from a client
            Method Return: Promise containing a string
        */
        static async Task<string> ServerSendIO(NetworkStream clientNS, string message){
            // Send request
            await SendMessageOverNetworkStream(clientNS, "io,"+message);
            // Await IO
            return await ReceiveMessageFromNetworkStream(clientNS);
        }

        /*
            Method Name: MakeOnlineMove
            Method Parameters: 
                movingClient:
                    A network stream between the server and the client making a move
                otherClient:
                    A network stream between the server and the client not making a move
                int:
                    Score of the moving client at round start
            Method Description: Facilitates a move for an online client
            Method Return: Promise containing an int
        */
        static async Task<int> MakeOnlineMove(NetworkStream movingClient, NetworkStream otherClient, int scoreAtTurnStart){
            string selectionFormat = @"^[1-6]( [1-6]){0,5}$";

            // Get initial roll
            int diceRemaining = 6;
            int[] roll = RollDice(diceRemaining);
            await ServerSendAll(movingClient, otherClient, GetRollString(roll));

            int finalScore = scoreAtTurnStart; // Placeholder
            int expectedNewPoints = 0;
            bool rollIsLive = true;
            while (rollIsLive){
                // If roll has no moves
                if (!IsViable(roll)){
                     await ServerSendAll(movingClient, otherClient, "No viable moves found for roll.");
                    expectedNewPoints = 0;
                    rollIsLive = false;
                    break;
                }

                bool selectionIsMade = false;
                int[] selection = null; // Placeholder

                // Get the selection from the user
                while (!selectionIsMade){
                    string userInput = await ServerSendIO(movingClient, "Make your selection: ");

                    // Error -> return -1
                    if (userInput == null){
                        return -1;
                    }

                    bool validSelectionStr = Regex.IsMatch(userInput, selectionFormat);
                    // If not correct
                    if (!validSelectionStr){
                        await ServerSendClient(movingClient, "Invalid selection. Selection example: 1 1 5 4 4 4");
                        continue;
                    }

                    // Move input into selection
                    selection = SelectionStringToInt(userInput);

                    // Check if selection matches roll
                    if (!SelectionMatchesRoll(selection, roll)){
                        await ServerSendClient(movingClient, "Selection does not match dice provided. Please try again.");
                        continue;
                    }

                    // Check if the selection is a valid move
                    if (!SelectionIsAValidMove(selection)){
                        await ServerSendClient(movingClient, "Selection is not a valid move. Please try again.");
                        continue;
                    }

                    selectionIsMade = true;

                    await ServerSendClient(otherClient, "Other client has selected: " + userInput);
                }


                expectedNewPoints += ScoreSelection(selection);

                // Update demaining dice
                diceRemaining = roll.Length - selection.Length;
                // New roll if zero
                if (diceRemaining == 0){
                    diceRemaining = 6;
                }

                // Read character, check for h / r and determine dice remaining and stuff
                int userChoice = 0; // 0 -> none, 1 -> hold, 2 -> reroll
                while (userChoice == 0){
                    Console.WriteLine("Moving player must pick an option: 'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + " 'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");
                    await ServerSendClientNoPrint(otherClient, "Other client must pick an option: 'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + " 'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");
                    string userInput = await ServerSendIO(movingClient, "Please pick an option: 'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + " 'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");

                    // Error -> return -1
                    if (userInput == null){
                        return -1;
                    }

                    userChoice = AttemptToDetermineUserChoice(userInput);

                    // If invalid choise
                    if (userChoice == 0){
                        await ServerSendClient(movingClient, "\"" + userInput + "\" is an invalid choice! 'h' for hold, 'r' for reroll!");
                    }

                }

                // User chose hold
                if (userChoice == 1){
                    Console.WriteLine("Moving player has chosen to hold!");
                    await ServerSendClientNoPrint(otherClient, "Other client has chosen to hold!");
                    rollIsLive = false;
                }
                // Reroll
                else{
                    Console.WriteLine("Moving player has chosen to reroll!");
                    await ServerSendClientNoPrint(otherClient, "Other client has chosen to reroll!");
                }

                // If re-rolling
                if (rollIsLive){
                    // New roll
                    roll = RollDice(diceRemaining);
                    await ServerSendAll(movingClient, otherClient, GetRollString(roll));
                }
            }

            // Return the final score
            finalScore = scoreAtTurnStart + expectedNewPoints;
            return finalScore;
        }

        /*
            Method Name: MakeOnlineMove
            Method Parameters: 
                movingClient:
                    A network stream between the server and the client making a move
                int:
                    Score of the moving client at round start
            Method Description: Facilitates a move for an online client
            Method Return: Promise containing an int
        */
        static async Task<int> MakeOnlineMove(NetworkStream movingClient, int scoreAtTurnStart){
            string selectionFormat = @"^[1-6]( [1-6]){0,5}$";

            // Get initial roll
            int diceRemaining = 6;
            int[] roll = RollDice(diceRemaining);
            await ServerSendClient(movingClient, GetRollString(roll));

            int finalScore = scoreAtTurnStart; // Placeholder
            int expectedNewPoints = 0;
            bool rollIsLive = true;
            while (rollIsLive){
                // If roll has no moves
                if (!IsViable(roll)){
                     await ServerSendClient(movingClient, "No viable moves found for roll.");
                    expectedNewPoints = 0;
                    rollIsLive = false;
                    break;
                }

                bool selectionIsMade = false;
                int[] selection = null; // Placeholder

                // Get the selection from the user
                while (!selectionIsMade){
                    string userInput = await ServerSendIO(movingClient, "Make your selection: ");

                    // Error -> return -1
                    if (userInput == null){
                        return -1;
                    }

                    bool validSelectionStr = Regex.IsMatch(userInput, selectionFormat);
                    // If not correct
                    if (!validSelectionStr){
                        await ServerSendClient(movingClient, "Invalid selection. Selection example: 1 1 5 4 4 4");
                        continue;
                    }

                    // Move input into selection
                    selection = SelectionStringToInt(userInput);

                    // Check if selection matches roll
                    if (!SelectionMatchesRoll(selection, roll)){
                        await ServerSendClient(movingClient, "Selection does not match dice provided. Please try again.");
                        continue;
                    }

                    // Check if the selection is a valid move
                    if (!SelectionIsAValidMove(selection)){
                        await ServerSendClient(movingClient, "Selection is not a valid move. Please try again.");
                        continue;
                    }

                    selectionIsMade = true;
                }


                expectedNewPoints += ScoreSelection(selection);

                // Update demaining dice
                diceRemaining = roll.Length - selection.Length;
                // New roll if zero
                if (diceRemaining == 0){
                    diceRemaining = 6;
                }

                // Read character, check for h / r and determine dice remaining and stuff
                int userChoice = 0; // 0 -> none, 1 -> hold, 2 -> reroll
                while (userChoice == 0){
                    string userInput = await ServerSendIO(movingClient, "Please pick an option: 'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + " 'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");

                    // Error -> return -1
                    if (userInput == null){
                        return -1;
                    }

                    userChoice = AttemptToDetermineUserChoice(userInput);

                    // If invalid choise
                    if (userChoice == 0){
                        await ServerSendClient(movingClient, "\"" + userInput + "\" is an invalid choice! 'h' for hold, 'r' for reroll!");
                    }
                }

                // User chose hold
                if (userChoice == 1){
                    rollIsLive = false;
                }

                // If re-rolling
                if (rollIsLive){
                    // New roll
                    roll = RollDice(diceRemaining);
                    await ServerSendClient(movingClient, GetRollString(roll));
                }
            }

            // Return the final score
            finalScore = scoreAtTurnStart + expectedNewPoints;
            return finalScore;
        }

        /*
            Method Name: RunPlayervCPUOnline
            Method Parameters: 
                clientNS:
                    A network stream between the server and its client
            Method Description: Runs a game between an online player and a CPU
            Method Return: Promise (implicit)
        */
        static async Task RunPlayervCPUOnline(NetworkStream clientNS){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true; // Player 1 is human

            // Make client list
            ArrayList clientNSList = new ArrayList();
            clientNSList.Add(clientNS);

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    await ServerSendClient(clientNS, "Turn: Player 1. Points remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = await MakeOnlineMove(clientNS, player1Score);

                    // Score is -1, error occured
                    if (player1Score == -1){
                        Console.WriteLine("Player 1 has disconnected. Game over.");
                        break;
                    }

                    await ServerSendClient(clientNS, "Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    await ServerSendClient(clientNS, "Turn: Player 2. Points remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = await MakeOnlineCPUMove(clientNSList, player2Score, scoreForWin, (scoreForWin-player1Score));

                    // Score is -1, error occured
                    if (player1Score == -1){
                        await ServerSendClose(clientNS, "Player 2 has disconnected. Game over.");
                        break;
                    }

                    await ServerSendClient(clientNS, "Player 2 ends their turn with " + player2Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }
                
                // Swap turn
                player1Turn = !player1Turn;
            }

            // Determine winner
            if (player1Score >= scoreForWin){
                await ServerSendClose(clientNS, "Player 1 has won with " + player1Score.ToString() + " points.");
                await ServerSendClose(clientNS, "Player 2 has lost with "+ player2Score.ToString() + " points.");
            }else{
                await ServerSendClose(clientNS, "Player 2 has won with " + player2Score.ToString() + " points.");
                await ServerSendClose(clientNS, "Player 1 has lost with "+ player1Score.ToString() + " points.");
            }
        }

        /*
            Method Name: RunPvPOnline
            Method Parameters: 
                tcpListener:
                    A TCP listener to accept another client
                client1NS:
                    A network stream between the server and its first client
            Method Description: Runs a game between two online players
            Method Return: Promise (implicit)
        */
        static async Task RunPvPOnline(TcpListener tcpListener, NetworkStream client1NS){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            await ServerSendClient(client1NS, "Waiting for client 2 to connect...");

            // Note: Using tells the program to call .Dispose() at end of try{...}
            using TcpClient tcpClientHandler2 = await tcpListener.AcceptTcpClientAsync();

            Console.WriteLine("Client 2 has connected!");
            await using NetworkStream client2NS = tcpClientHandler2.GetStream();

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    await ServerSendAll(client1NS, client2NS, "Turn: Player 1. Points remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = await MakeOnlineMove(client1NS, client2NS, player1Score);

                    // Score is -1, error occured
                    if (player1Score == -1){
                        await ServerSendClose(client2NS, "Player 1 has disconnected. Game over.");
                        break;
                    }

                    await ServerSendAll(client1NS, client2NS, "Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    await ServerSendAll(client1NS, client2NS, "Turn: Player 2. Points remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = await MakeOnlineMove(client2NS, client1NS, player2Score);

                    // Score is -1, error occured
                    if (player1Score == -1){
                        await ServerSendClose(client1NS, "Player 2 has disconnected. Game over.");
                        break;
                    }

                    await ServerSendAll(client1NS, client2NS, "Player 2 ends their turn with " + player2Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }
                
                // Swap turn
                player1Turn = !player1Turn;
            }

            // Determine winner
            if (player1Score >= scoreForWin){
                await ServerSendClose(client1NS, client2NS, "Player 1 has won with " + player1Score.ToString() + " points.");
                await ServerSendClose(client1NS, client2NS, "Player 2 has lost with "+ player2Score.ToString() + " points.");
            }else{
                await ServerSendClose(client1NS, client2NS, "Player 2 has won with " + player2Score.ToString() + " points.");
                await ServerSendClose(client1NS, client2NS, "Player 1 has lost with "+ player1Score.ToString() + " points.");
            }
        }


        /*
            Method Name: RunPvPLocal
            Method Parameters: None
            Method Description: Runs a game between two local players
            Method Return: void
        */
        static void RunPvPLocal(){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    Console.WriteLine("Turn: Player 1.\nPoints remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = MakeLocalMove(player1Score);
                    Console.WriteLine("Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    Console.WriteLine("Turn: Player 2.\nPoints remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = MakeLocalMove(player2Score);
                    Console.WriteLine("Player 2 ends their turn with " + player2Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }
                
                // Swap turn
                player1Turn = !player1Turn;
            }

            // Determine winner
            if (player1Score >= scoreForWin){
                Console.WriteLine("Player 1 has won with " + player1Score.ToString() + " points.");
                Console.WriteLine("Player 2 has lost with "+ player2Score.ToString() + " points.");
            }else{
                Console.WriteLine("Player 2 has won with " + player2Score.ToString() + " points.");
                Console.WriteLine("Player 1 has lost with "+ player1Score.ToString() + " points.");
            }
        }

        /*
            Method Name: RollDice
            Method Parameters: 
                numDice:
                    The number of dice to roll
            Method Description: Rolls the specified # of dice and provides an array rep
            Method Return: int[]
        */
        static int[] RollDice(int numDice){
            int[] roll = new int[numDice];

            // Fill randomly
            for (int i = 0; i < numDice; i++){
                roll[i] = random.Next(1,6);
            }
            
            return roll;
        }

        /*
            Method Name: PrintRoll
            Method Parameters: 
                roll:
                    Array of numbers representing a roll
            Method Description: Prints out a roll
            Method Return: void
        */
        static void PrintRoll(int[] roll){
            Console.WriteLine(GetRollString(roll));
        }

        /*
            Method Name: GetRollString
            Method Parameters: 
                roll:
                    Array of numbers representing a roll
            Method Description: Converts a roll to string
            Method Return: Roll rep string
        */
        static string GetRollString(int[] roll){
            string lineToWrite = "Roll:";
            for (int i = 0; i < roll.Length; i++){
                lineToWrite += " " + roll[i].ToString();
            }
            return lineToWrite;
        }

        /*
            Method Name: GetSelectionString
            Method Parameters: 
                selection:
                    Array of numbers representing a selection
            Method Description: Converts a selection to string
            Method Return: Selection rep string
        */
        static string GetSelectionString(int[] selection){
            string lineToWrite = "";
            for (int i = 0; i < selection.Length; i++){
                lineToWrite += " " + selection[i].ToString();
            }
            return lineToWrite;
        }

        /*
            Method Name: PrintBotSelection
            Method Parameters: 
                selection:
                    Array of numbers representing a selection
            Method Description: Prints out a selection by a bot
            Method Return: void
        */
        static void PrintBotSelection(int[] selection){
            string lineToWrite = "Bot has selected:";
            for (int i = 0; i < selection.Length; i++){
                lineToWrite += " " + selection[i].ToString();
            }
            Console.WriteLine(lineToWrite);
        }

        /*
            Method Name: IsViable
            Method Parameters: 
                roll:
                    Array of numbers representing a roll
            Method Description: Checks if a given roll is viable
            Method Return: boolean, true -> it's viable, false -> not viable
        */
        static bool IsViable(int[] roll){
            int[] counts = {0,0,0,0,0,0};

            // Determine counts
            for (int i = 0; i < roll.Length; i++){
                counts[roll[i]-1]++;
            }

            // Check 1s and 5s
            if (counts[0] > 0 || counts[4] > 0){
                return true;
            }

            // Check for straight
            if (roll.Length == 6){
                bool straightActive = true;
                for (int i = 0; i < counts.Length; i++){
                    // If count is > 1 then not a straight
                    if (counts[i] > 1){
                        straightActive = false;
                        break;
                    }
                }

                // If a straight
                if (straightActive){
                    return true;
                }
            }

            // Check for over 3
            for (int i = 0; i < counts.Length; i++){
                if (counts[i] >= 3){
                    return true;
                }
            }

            // Nothing found
            return false;
        }

        /*
            Method Name: SelectionStringToInt
            Method Parameters: 
                selectionString:
                    A string representing a selection
            Method Description: Converts a selection string to a integer array
            Method Return: array of integers representing the selection
        */
        static int[] SelectionStringToInt(string selectionString){
            // Note: Assumes correct format

            string[] numberStrArray = selectionString.Split(' ');

            int[] selection = new int[numberStrArray.Length];

            for (int i = 0; i < numberStrArray.Length; i++){
                selection[i] = int.Parse(numberStrArray[i]);
            }

            return selection;
        }

        /*
            Method Name: SelectionMatchesRoll
            Method Parameters: 
                selection:
                    A selection array of integers
                roll:
                    A roll array of integers
            Method Description: Checks if the selected items match the roll
            Method Return: boolean, true -> matches, false -> doesn't match
        */
        static bool SelectionMatchesRoll(int[] selection, int[] roll){
            int[] rollCounts = {0,0,0,0,0,0};

            // Determine rollCounts
            for (int i = 0; i < roll.Length; i++){
                rollCounts[roll[i]-1]++;
            }

            int[] selectionCounts = {0,0,0,0,0,0};

            // Determine selectionCounts
            for (int i = 0; i < selection.Length; i++){
                selectionCounts[selection[i]-1]++;
            }

            // Check that they match
            for (int i = 0; i < selectionCounts.Length; i++){
                // If user selected invalid dice
                if (selectionCounts[i] > rollCounts[i]){
                    return false;
                }
            }

            // Must be valid
            return true;
        }

        /*
            Method Name: SelectionIsAValidMove
            Method Parameters: 
                selection:
                    Array of numbers representing a selection
            Method Description: Checks if a given selection is valid move
            Method Return: boolean, true -> it's a valid move, false -> not a valid move
        */
        static bool SelectionIsAValidMove(int[] selection){
            // Empty
            if (selection.Length == 0){
                return false;
            }

            int[] counts = {0,0,0,0,0,0};

            // Determine counts
            for (int i = 0; i < selection.Length; i++){
                counts[selection[i]-1]++;
            }

            // Check for straight
            if (selection.Length == 6){
                bool straightActive = true;
                for (int i = 0; i < counts.Length; i++){
                    // If count is > 1 then not a straight
                    if (counts[i] > 1){
                        straightActive = false;
                        break;
                    }
                }

                // If a straight
                if (straightActive){
                    return true;
                }
            }

            // Check for over 3
            for (int i = 0; i < counts.Length; i++){
                if (counts[i] >= 3){
                    counts[i] = 0;
                }
            }

            // Check that nothing remains from 2 to 4
            for (int i = 1; i < counts.Length - 2; i++){
                if (counts[i] > 0){
                    return false;
                }
            }
            // Check that no sixes are left
            if (counts[5] > 0){
                return false;
            }


            // No problems found, it's valid
            return true;
        }

        static ArrayList GenerateAvailableMoves(int[] roll){
            // Note: Assumes roll is viable

            /*  
                I need an algorithm to generate all possible combinations
                E.g. 
                ArraySize=3
                0
                1
                2
                    0,1
                    0,2
                    1,2

                ArraySize=4
                0
                1
                2
                3
                    0,1
                    0,2
                    0,3
                    1,2
                    1,3
                    2,3

                        0,1,2
                        0,1,3
                        0,2,3
                        1,2,3
            */

            ArrayList availableMoves = new ArrayList();

            // Create counts
            int[] counts = {0,0,0,0,0,0};

            // Determine counts
            for (int i = 0; i < roll.Length; i++){
                counts[roll[i]-1]++;
            }            

            // Generate options
            GenerateAvailableMoves(counts, 0, availableMoves, new ArrayList{0,0,0,0,0,0});

            // Filter out movies with no value
            ArrayList viableMovesArrayList = new ArrayList();
            foreach (ArrayList moveCounts in availableMoves){
                ArrayList move = CountsArrayListToSelectionArrayList(moveCounts);
                int[] selection = ArrayListOfNumToArray(move);
                // Check if selection matches roll
                if (!SelectionMatchesRoll(selection, roll)){
                    throw new Exception("Failure in CPU move generation");
                }

                // Check if the selection is a valid move
                if (!SelectionIsAValidMove(selection)){
                    continue;
                }
                // It's viable
                viableMovesArrayList.Add(move);
            }

            /*
            Console.WriteLine("Viable move count: " + viableMovesArrayList.Count.ToString());
            foreach (ArrayList move in viableMovesArrayList){
                PrintArrayListOfNum(move);
            }*/

            return viableMovesArrayList;
        }

        /*
            Method Name: GetCPUSelection
            Method Parameters: 
                roll:
                    Array of integers representing a roll
                scoreAtTurnStart:
                    The player score at start of turn
                scoreForWin:
                    The score required for a player to win
                enemyScoreRemainingToWin:
                    The number of points required by the enemy to reach the scoreForWin
            Method Description: Instructs the CPU to make a selection
            Method Return: Array of integers (selection)
        */
        static int[] GetCPUSelection(int[] roll, int scoreAtTurnStart, int scoreForWin, int expectedNewPoints, int enemyScoreRemainingToWin){
            int myCurrentScoreBalance = scoreAtTurnStart + expectedNewPoints;
            int myPointsRemainingForWin = scoreForWin - myCurrentScoreBalance;

            ArrayList availableMoves = GenerateAvailableMoves(roll);

            // If count is zero -> error because the roll was already checked to be viable
            if (availableMoves.Count == 0){
                throw new Exception("Unexpected lack in moves.");
            }

            int highestPointsPossible = 0;
            ArrayList highestPointsPossibleMove = null;
            // Find the highest points possible
            foreach (ArrayList move in availableMoves){
                int[] selection = ArrayListOfNumToArray(move);
                int selectionValue = ScoreSelection(selection);

                // If value is highest than seen before then update
                if (selectionValue > highestPointsPossible){
                    highestPointsPossible = selectionValue;
                    highestPointsPossibleMove = move;
                }
            }

            // If we can win right now -> do so
            if (highestPointsPossible > myPointsRemainingForWin){
                return ArrayListOfNumToArray(highestPointsPossibleMove);
            }

            // SO we can't win, is the enemy about to win -> If so, prioritize re-rolls
            if (enemyScoreRemainingToWin - CPU_ESTIMATED_POINTS_PER_TURN <= 0){
                // Take the highest scoring move that clears the dice if possible
                int highestPointsPossibleCLR = 0;
                ArrayList highestPointsPossibleMoveCLR = null;
                foreach (ArrayList move in availableMoves){
                    int[] selection = ArrayListOfNumToArray(move);
                    int selectionValue = ScoreSelection(selection);

                    // If value is highest than seen before then update
                    if (selectionValue > highestPointsPossibleCLR && selection.Length == roll.Length){
                        highestPointsPossibleCLR = selectionValue;
                        highestPointsPossibleMoveCLR = move;
                    }
                }

                // If found such a move -> use it
                if (highestPointsPossibleMoveCLR != null){
                    return ArrayListOfNumToArray(highestPointsPossibleMoveCLR);
                }

                // Take the highest scoring move that uses minimal dice 
                int highestPointsPossibleMIND = 0;
                int numDiceUsedMIND = roll.Length;
                ArrayList highestPointsPossibleMoveMIND = null;
                foreach (ArrayList move in availableMoves){
                    int[] selection = ArrayListOfNumToArray(move);
                    int selectionValue = ScoreSelection(selection);
                    int numDiceUsed = selection.Length;

                    // If uses less dice, or the same but more points then prioritize
                    if (numDiceUsed < numDiceUsedMIND || (numDiceUsed == numDiceUsedMIND && highestPointsPossibleMIND < selectionValue)){
                        highestPointsPossibleMIND = selectionValue;
                        numDiceUsedMIND = numDiceUsed; 
                        highestPointsPossibleMoveMIND = move;
                    }
                }

                // If found such a move -> use it
                if (highestPointsPossibleMoveMIND != null){
                    return ArrayListOfNumToArray(highestPointsPossibleMoveMIND);
                }
            }

            // Go with highest points move
            return ArrayListOfNumToArray(highestPointsPossibleMove);
        }

        /*
            Method Name: CopyArrayList
            Method Parameters: 
                arrayList:
                    An array list
            Method Description: Copies all elements from an array list to a new one
            Method Return: An arraylist
        */
        static ArrayList CopyArrayList(ArrayList arrayList){
            ArrayList newArrayList = new ArrayList();
            foreach(var element in arrayList){
                newArrayList.Add(element);
            }
            return newArrayList;
        }

        /*
            Method Name: CountsArrayListToSelectionArrayList
            Method Parameters: 
                arrayList:
                    An arraylist representing counts for die faces
            Method Description: Converts a die counts array list to a selection array list
            Method Return: An arraylist
        */
        static ArrayList CountsArrayListToSelectionArrayList(ArrayList arrayList){
            ArrayList outputList = new ArrayList();
            int i = 0;
            foreach(int count in arrayList){
                for (int j = 0; j < count; j++){
                    outputList.Add(i+1);
                }
                i++;
            }
            return outputList;
        }

        /*
            Method Name: ArrayListOfNumToArray
            Method Parameters: 
                arrayList:
                    An arraylist full of integers
            Method Description: Converts an array list whose elements are all integers to an integer array
            Method Return: An array of integers
        */
        static int[] ArrayListOfNumToArray(ArrayList arrayList){
            int[] numArray = new int[arrayList.Count];
            int i = 0;
            foreach(int element in arrayList){
                numArray[i++] = element;
            }
            return numArray;
        }

        /*
            Method Name: PrintArrayOfNum
            Method Parameters: 
                array:
                    An array of integers
            Method Description: Prints an array of integers
            Method Return: void
        */
        static void PrintArrayOfNum(int[] array){
            string outputString = "[";
            for(int i = 0; i < array.Length; i++){
                outputString += " " + array[i].ToString();
            }
            outputString += " ]";
            Console.WriteLine(outputString);
        }

        /*
            Method Name: PrintArrayListOfNum
            Method Parameters: 
                arrayList:
                    An array list of integers
            Method Description: Prints the array list contents
            Method Return: void
        */
        static void PrintArrayListOfNum(ArrayList arrayList){
            string outputString = "[";
            foreach(int element in arrayList){
                outputString += " " + element.ToString();
            }
            outputString += " ]";
            Console.WriteLine(outputString);
        }

        /*
            Method Name: GenerateAvailableMoves
            Method Parameters: 
                rollCounts:
                    An array of counts for die faces
                index
                    The current index in counts
                availableMoves
                    An array list storing available moves
                selectedCounts
                    An array list with state data on the currently generating move
            Method Description: Produces a list of moves
            Method Return: void
        */
        static void GenerateAvailableMoves(int[] rollCounts, int index, ArrayList availableMoves, ArrayList selectedCounts){
            // End recursion -> add
            if (index == rollCounts.Length){
                availableMoves.Add(selectedCounts);
                return;
            }
            int numOfThisFace = rollCounts[index];
            
            // Run with 0,1,2,3,4,5,6 (6 is max)
            for (int i = 0; i < numOfThisFace+1; i++){
                ArrayList copyOfSelectedCounts = CopyArrayList(selectedCounts);
                copyOfSelectedCounts[index] = i;

                // Recurse
                GenerateAvailableMoves(rollCounts, index+1, availableMoves, copyOfSelectedCounts);
            }
        }

        /*
            Method Name: MakeOnlineCPUMove
            Method Parameters: 
                clientList:
                    An array list full of client network streams
                scoreAtTurnStart: 
                    The score of the moving player at turn start
                scoreForWin:
                    The score required for a player to win
                enemyScoreRemainingToWin:
                    The required number of points for the enemy to reach the winning state
            Method Description: Makes a move for an online CPU
            Method Return: Promise containing an int. The int is the score at end of the turn 
        */
        static async Task<int> MakeOnlineCPUMove(ArrayList clientList, int scoreAtTurnStart, int scoreForWin, int enemyScoreRemainingToWin){
            // Get initial roll
            int diceRemaining = 6;
            int[] roll = RollDice(diceRemaining);

            await ServerSendClientList(clientList, GetRollString(roll));

            int finalScore = scoreAtTurnStart; // Placeholder
            int expectedNewPoints = 0;
            bool rollIsLive = true;
            while (rollIsLive){
                // If roll has no moves
                if (!IsViable(roll)){
                    await ServerSendClientList(clientList, "No viable moves found for roll.");
                    expectedNewPoints = 0;
                    rollIsLive = false;
                    break;
                }

                int[] selection = GetCPUSelection(roll, scoreAtTurnStart, scoreForWin, expectedNewPoints, enemyScoreRemainingToWin); 
                    
                // Print for user viewing
                await ServerSendClientList(clientList, "CPU has selected:" + GetSelectionString(selection));

                expectedNewPoints += ScoreSelection(selection);

                // Update demaining dice
                diceRemaining = roll.Length - selection.Length;
                // New roll if zero
                if (diceRemaining == 0){
                    diceRemaining = 6;
                }

                await ServerSendClientList(clientList, "CPU must pick an option: 'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + " 'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");

                // Read character, check for h / r and determine dice remaining and stuff
                int userChoice = GetCPUContinuationChoice(scoreAtTurnStart, scoreForWin, expectedNewPoints, diceRemaining, enemyScoreRemainingToWin); // 0 -> none, 1 -> hold, 2 -> reroll

                // User chose hold
                if (userChoice == 1){
                    rollIsLive = false;
                    await ServerSendClientList(clientList, "CPU has chosen to hold");
                }else{
                    await ServerSendClientList(clientList, "CPU has chosen to reroll");
                }

                // If re-rolling
                if (rollIsLive){
                    // New roll
                    roll = RollDice(diceRemaining);
                    await ServerSendClientList(clientList, GetRollString(roll));
                }
            }

            // Return the final score
            finalScore = scoreAtTurnStart + expectedNewPoints;
            return finalScore;
        }

        /*
            Method Name: MakeLocalCPUMove
            Method Parameters: 
                scoreAtTurnStart:
                    The score of the CPU at the start of the turn
                scoreForWin:
                    The required score to win the game
                enemyScoreRemainingToWin:
                    The required number of points for the enemy to reach the winning state
            Method Description: The CPU makes their move in the game
            Method Return: int - The number of points at end of turn
        */
        static int MakeLocalCPUMove(int scoreAtTurnStart, int scoreForWin, int enemyScoreRemainingToWin){
            // Get initial roll
            int diceRemaining = 6;
            int[] roll = RollDice(diceRemaining);

            PrintRoll(roll);

            int finalScore = scoreAtTurnStart; // Placeholder
            int expectedNewPoints = 0;
            bool rollIsLive = true;
            while (rollIsLive){
                // If roll has no moves
                if (!IsViable(roll)){
                    Console.WriteLine("No viable moves found for roll.");
                    expectedNewPoints = 0;
                    rollIsLive = false;
                    break;
                }

                int[] selection = GetCPUSelection(roll, scoreAtTurnStart, scoreForWin, expectedNewPoints, enemyScoreRemainingToWin); 
                    
                // Print for user viewing
                PrintBotSelection(selection);

                expectedNewPoints += ScoreSelection(selection);

                // Update demaining dice
                diceRemaining = roll.Length - selection.Length;
                // New roll if zero
                if (diceRemaining == 0){
                    diceRemaining = 6;
                }

                Console.WriteLine("CPU must pick an option:\n'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + "\n'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");

                // Read character, check for h / r and determine dice remaining and stuff
                int userChoice = GetCPUContinuationChoice(scoreAtTurnStart, scoreForWin, expectedNewPoints, diceRemaining, enemyScoreRemainingToWin); // 0 -> none, 1 -> hold, 2 -> reroll

                // User chose hold
                if (userChoice == 1){
                    rollIsLive = false;
                    Console.WriteLine("CPU has chosen to hold");
                }else{
                    Console.WriteLine("CPU has chosen to reroll");
                }

                // If re-rolling
                if (rollIsLive){
                    // New roll
                    roll = RollDice(diceRemaining);
                    PrintRoll(roll);
                }
            }

            // Return the final score
            finalScore = scoreAtTurnStart + expectedNewPoints;
            return finalScore;
        }

        /*
            Method Name: GetCPUContinuationChoice
            Method Parameters:
                scoreAtTurnStart:
                    The score of the CPU at the start of the turn
                scoreForWin:
                    The score required for a player to win
                expectedNewPoints:
                    The number of the points that would be added if the currently moving player were to win
                diceRemaining:  
                    The number of dice remaining to roll if the player chooses to roll
                enemyScoreRemainingToWin:
                    The number of points the enemy needs to earn in order to reach the winning state
            Method Description: The CPU decides if they wish to hold or re-roll
            Method Return: int - 1 -> hold & 0 -> re-roll
        */
        static int GetCPUContinuationChoice(int scoreAtTurnStart, int scoreForWin, int expectedNewPoints, int diceRemaining, int enemyScoreRemainingToWin){
            // If I will win if I hold -> HOLD
            if (scoreAtTurnStart + expectedNewPoints > scoreForWin){
                return 1;
            }

            // If enemy is expected to win next turn and I am not yet going to win - > ROLL
            if (enemyScoreRemainingToWin < CPU_ESTIMATED_POINTS_PER_TURN){
                return 0;
            }

            // Reroll if my expected points are much less than I'd like
            if (expectedNewPoints < CPU_MIN_POINTS_TO_HOLD){
                return 0;
            }
            // Otherwise, I'm happy -> hold
            else{
                return 1;
            }
        }

        /*
            Method Name: MakeLocalMove
            Method Parameters:
                scoreAtTurnStart:
                    Score of the human player at turn start
            Method Description: The human player makes their move
            Method Return: int - Points at end of turn (score)
        */
        static int MakeLocalMove(int scoreAtTurnStart){
            string selectionFormat = @"^[1-6]( [1-6]){0,5}$";

            // Get initial roll
            int diceRemaining = 6;
            int[] roll = RollDice(diceRemaining);
            PrintRoll(roll);

            int finalScore = scoreAtTurnStart; // Placeholder
            int expectedNewPoints = 0;
            bool rollIsLive = true;
            while (rollIsLive){
                // If roll has no moves
                if (!IsViable(roll)){
                    Console.WriteLine("No viable moves found for roll.");
                    expectedNewPoints = 0;
                    rollIsLive = false;
                    break;
                }

                bool selectionIsMade = false;
                int[] selection = null; // Placeholder

                // Get the selection from the user
                while (!selectionIsMade){
                    Console.Write("Make your selection: ");
                    string userInput = Console.ReadLine().Trim();

                    bool validSelectionStr = Regex.IsMatch(userInput, selectionFormat);
                    // If not correct
                    if (!validSelectionStr){
                        Console.WriteLine("Invalid selection. Selection example: 1 1 5 4 4 4");
                        continue;
                    }

                    // Move input into selection
                    selection = SelectionStringToInt(userInput);

                    // Check if selection matches roll
                    if (!SelectionMatchesRoll(selection, roll)){
                        Console.WriteLine("Selection does not match dice provided. Please try again.");
                        continue;
                    }

                    // Check if the selection is a valid move
                    if (!SelectionIsAValidMove(selection)){
                        Console.WriteLine("Selection is not a valid move. Please try again.");
                        continue;
                    }

                    selectionIsMade = true;
                }

                expectedNewPoints += ScoreSelection(selection);

                // Update demaining dice
                diceRemaining = roll.Length - selection.Length;
                // New roll if zero
                if (diceRemaining == 0){
                    diceRemaining = 6;
                }

                // Read character, check for h / r and determine dice remaining and stuff
                int userChoice = 0; // 0 -> none, 1 -> hold, 2 -> reroll
                while (userChoice == 0){
                    Console.WriteLine("Please pick an option:\n'h': " + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " -> " + (scoreAtTurnStart + expectedNewPoints).ToString() + "\n'r': reroll" + " " + diceRemaining.ToString() + " dice " + "(" + scoreAtTurnStart.ToString() + " + " + expectedNewPoints.ToString() + " + ? -> ??) OR (" + scoreAtTurnStart.ToString() + " + 0 -> " + scoreAtTurnStart.ToString() + ")");
                    string userInput = Console.ReadLine();
                    userChoice = AttemptToDetermineUserChoice(userInput);

                    // If invalid choise
                    if (userChoice == 0){
                        Console.WriteLine("\"" + userInput + "\" is an invalid choice! 'h' for hold, 'r' for reroll!");
                    }
                }

                // User chose hold
                if (userChoice == 1){
                    rollIsLive = false;
                }

                // If re-rolling
                if (rollIsLive){
                    // New roll
                    roll = RollDice(diceRemaining);
                    PrintRoll(roll);
                }
            }

            // Return the final score
            finalScore = scoreAtTurnStart + expectedNewPoints;
            return finalScore;
        }

        /*
            Method Name: AttemptToDetermineUserChoice
            Method Parameters:
                userInput:
                    User input string
            Method Description: Checks if the user wants to hold or re-roll
            Method Return: int - 1->hold,2->reroll,0->invalid choice
        */
        static int AttemptToDetermineUserChoice(string userInput){
            if (userInput == "h"){
                return 1;
            }else if (userInput == "r"){
                return 2;
            }else{
                return 0; // Invalid
            }
        }

        /*
            Method Name: ScoreSelection
            Method Parameters:
                selection:
                    Array of integers - a selection in the game
            Method Description: Calculates a score for a given selection
            Method Return: int
        */
        static int ScoreSelection(int[] selection){
            // Note: Assumes selection is valid

            int score = 0;

            int[] counts = {0,0,0,0,0,0};

            // Determine counts
            for (int i = 0; i < selection.Length; i++){
                counts[selection[i]-1]++;
            }

            // Check for straight
            if (selection.Length == 6){
                bool straightActive = true;
                for (int i = 0; i < counts.Length; i++){
                    // If count is > 1 then not a straight
                    if (counts[i] > 1){
                        straightActive = false;
                        break;
                    }
                }

                // If a straight
                if (straightActive){
                    return 1800;
                }
            }

            // Check for over 3
            for (int i = 0; i < counts.Length; i++){
                // If at least 3 in a row
                if (counts[i] >= 3){
                    // Formula
                    int baseScore = 100 * (i+1);
                    // 3 * 1 = 1000
                    if (i == 0){
                        baseScore *= 10;
                    }
                    // E.g. counts[i] = 3 so 2^0=1 so if i = 0 then roll=1 so 100*=1000, 1000 * 1 = 1000. If counts[i] were to equal 4, then 2^1 * 1000 = 2000
                    score += baseScore * (int)Math.Pow(2, counts[i]-3);

                    // Scoring so remove
                    counts[i] = 0;
                }
            }

            // Check 1s 
            score += 100 * counts[0];

            // Check 5s
            score += 50 * counts[4];


            // Nothing found
            if (score == 0){
                throw new ArgumentException("Valid move expected.");
            }
            

            return score;
        }

    } // Close class
} // Close namespace
