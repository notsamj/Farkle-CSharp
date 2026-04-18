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
    class Program {
        static Random random = new Random();

        static int CPU_ESTIMATED_POINTS_PER_TURN = 500; // used for bots
        static int CPU_MIN_POINTS_TO_HOLD = 300; // used for bots

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

            //RunPvPLocal();
            //RunCpuVCpuLocal();
        }

        static async Task LaunchClient(){
            int serverPort = 8080;
            int bufferSize = 1024;

            IPAddress ipAddress = IPAddress.Parse("127.0.0.1"); // localhost

            // Note: Using tells the program to call .Dispose() at end of the function
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(ipAddress, serverPort);
            await using NetworkStream networkStream = client.GetStream();

            byte[] networkBuffer = new byte[bufferSize];
            int receivedInt = await networkStream.ReadAsync(networkBuffer);

            string message = Encoding.UTF8.GetString(networkBuffer, 0, receivedInt);
            Console.WriteLine($"Message received from server: \"{message}\"");
        }

        static async Task LaunchServer(){
            int serverPort = 8080;
            IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Any, serverPort);

            TcpListener tcpListener = new TcpListener(serverIpEndPoint);

            // Try starting up
            try {
                Console.WriteLine("Starting up the server!");
                // Call start
                tcpListener.Start();

                Console.WriteLine("Server has started!");
                // Note: Using tells the program to call .Dispose() at end of try{...}
                using TcpClient tcpClientHandler = await tcpListener.AcceptTcpClientAsync();


                Console.WriteLine("A client has connected!");
                await using NetworkStream networkStream = tcpClientHandler.GetStream();
                Console.WriteLine("Saaaacc");

                string message = $"Time: {DateTime.Now}";
                byte[] byteConversionOfMessage = Encoding.UTF8.GetBytes(message);

                // send message to client
                await networkStream.WriteAsync(byteConversionOfMessage);

                // Print message locally
                Console.WriteLine($"Sent to client: \" {message} \"");
            }
            // Errors
            catch (Exception exception){
                Console.WriteLine($"Server error: {exception.Message}");
            }
            // Shut down
            finally {
                Console.WriteLine("Server shutting down");
                tcpListener.Stop();
            }

            Console.WriteLine("End of launch server");
        }

        static void RunCpuVCpuLocal(){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    Console.WriteLine("Turn: Player 1.\nPoints remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = MakeLocalCPUMove(player1Score, scoreForWin, (scoreForWin-player2Score));
                    Console.WriteLine("Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    Console.WriteLine("Turn: Player 2.\nPoints remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
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

        static int[] RollDice(int numDice){
            int[] roll = new int[numDice];

            // Fill randomly
            for (int i = 0; i < numDice; i++){
                roll[i] = random.Next(1,6);
            }
            
            return roll;
        }

        static void PrintRoll(int[] roll){
            string lineToWrite = "Roll:";
            for (int i = 0; i < roll.Length; i++){
                lineToWrite += " " + roll[i].ToString();
            }
            Console.WriteLine(lineToWrite);
        }

        static void PrintBotSelection(int[] selection){
            string lineToWrite = "Bot has selected:";
            for (int i = 0; i < selection.Length; i++){
                lineToWrite += " " + selection[i].ToString();
            }
            Console.WriteLine(lineToWrite);
        }

        static bool IsViable(int[] selection){
            int[] counts = {0,0,0,0,0,0};

            // Determine counts
            for (int i = 0; i < selection.Length; i++){
                counts[selection[i]-1]++;
            }

            // Check 1s and 5s
            if (counts[0] > 0 || counts[4] > 0){
                return true;
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
                    return true;
                }
            }

            // Nothing found
            return false;
        }

        static int[] SelectionStringToInt(string userInput){
            // Note: Assumes correct format

            string[] numberStrArray = userInput.Split(' ');

            int[] selection = new int[numberStrArray.Length];

            for (int i = 0; i < numberStrArray.Length; i++){
                selection[i] = int.Parse(numberStrArray[i]);
            }

            return selection;
        }

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

        static ArrayList CopyArrayList(ArrayList arrayList){
            ArrayList newArrayList = new ArrayList();
            foreach(var element in arrayList){
                newArrayList.Add(element);
            }
            return newArrayList;
        }

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

        static int[] ArrayListOfNumToArray(ArrayList arrayList){
            int[] numArray = new int[arrayList.Count];
            int i = 0;
            foreach(int element in arrayList){
                numArray[i++] = element;
            }
            return numArray;
        }

        static void PrintArrayOfNum(int[] array){
            string outputString = "[";
            for(int i = 0; i < array.Length; i++){
                outputString += " " + array[i].ToString();
            }
            outputString += " ]";
            Console.WriteLine(outputString);
        }

        static void PrintArrayListOfNum(ArrayList arrayList){
            string outputString = "[";
            foreach(int element in arrayList){
                outputString += " " + element.ToString();
            }
            outputString += " ]";
            Console.WriteLine(outputString);
        }

        static void GenerateAvailableMoves(int[] rollCounts, int index, ArrayList availableMoves, ArrayList selectedCounts){
            // End recursion -> add
            if (index == rollCounts.Length){

                //Console.WriteLine("NEW Selected counts");
                //Console.WriteLine(index.ToString());
                //Console.WriteLine(i.ToString());
                //PrintArrayListOfNum(selectedCounts);
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

        static int MakeLocalCPUMove(int scoreAtTurnStart, int scoreForWin, int enemyScoreRemainingToWin){
            // Get initial roll
            int diceRemaining = 6;
            int[] roll = RollDice(diceRemaining);

            // TEMP
            //int[] roll = { 4, 2, 2, 5, 3, 2 };

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
                        Console.WriteLine("\"" + userInput + "\" is an invalid choice!");
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

        static int AttemptToDetermineUserChoice(string userInput){
            if (userInput == "h"){
                return 1;
            }else if (userInput == "r"){
                return 2;
            }else{
                return 0; // Invalid
            }
        }

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
