using System;
using System.Text.RegularExpressions;

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

        static void Main(string[] args) {
            Console.WriteLine("Welcome to Farkle!");

            // runPvPLocal();
            runCpuVCpuLocal();
        }

        static void runCpuVCpuLocal(){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    Console.WriteLine("Turn: Player 1.\nPoints remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = makeLocalMove(player1Score);
                    Console.WriteLine("Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    Console.WriteLine("Turn: Player 2.\nPoints remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = makeLocalMove(player2Score);
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

        static void runPvPLocal(){
            const int scoreForWin = 2000;

            int player1Score = 0;
            int player2Score = 0;

            bool player1Turn = true;

            // Loop while no player 
            while (player1Score < scoreForWin && player2Score < scoreForWin){
                if (player1Turn){
                    Console.WriteLine("Turn: Player 1.\nPoints remaining for Player 1 to win: " + (scoreForWin-player1Score).ToString());
                    player1Score = makeLocalMove(player1Score);
                    Console.WriteLine("Player 1 ends their turn with " + player1Score.ToString() + "/" + scoreForWin.ToString() + " points to win.");
                }else{
                    Console.WriteLine("Turn: Player 2.\nPoints remaining for Player 2 to win: " + (scoreForWin-player2Score).ToString());
                    player2Score = makeLocalMove(player2Score);
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

        static int[] rollDice(int numDice){
            int[] roll = new int[numDice];

            // Fill randomly
            for (int i = 0; i < numDice; i++){
                roll[i] = random.Next(1,6);
            }
            
            return roll;
        }

        static void printRoll(int[] roll){
            string lineToWrite = "Roll:";
            for (int i = 0; i < roll.Length; i++){
                lineToWrite += " " + roll[i].ToString();
            }
            Console.WriteLine(lineToWrite);
        }

        static bool isViable(int[] roll){
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

        static int[] selectionStringToInt(string userInput){
            // Note: Assumes correct format

            string[] numberStrArray = userInput.Split(' ');

            int[] selection = new int[numberStrArray.Length];

            for (int i = 0; i < numberStrArray.Length; i++){
                selection[i] = int.Parse(numberStrArray[i]);
            }

            return selection;
        }

        static bool selectionMatchesRoll(int[] selection, int[] roll){
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

        static bool selectionIsAValidMove(int[] selection){
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

        static int makeLocalMove(int scoreAtTurnStart){
            string selectionFormat = @"^[1-6]( [1-6]){0,5}$";

            // Get initial roll
            int diceRemaining = 6;
            int[] roll = rollDice(diceRemaining);
            printRoll(roll);

            int finalScore = scoreAtTurnStart; // Placeholder
            int expectedNewPoints = 0;
            bool rollIsLive = true;
            while (rollIsLive){
                // If roll has no moves
                if (!isViable(roll)){
                    Console.WriteLine("No viable moves found for roll.");
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
                    selection = selectionStringToInt(userInput);

                    // Check if selection matches roll
                    if (!selectionMatchesRoll(selection, roll)){
                        Console.WriteLine("Selection does not match dice provided. Please try again.");
                        continue;
                    }

                    // Check if the selection is a valid move
                    if (!selectionIsAValidMove(selection)){
                        Console.WriteLine("Selection is not a valid move. Please try again.");
                        continue;
                    }

                    selectionIsMade = true;
                }

                expectedNewPoints += scoreSelection(selection);

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
                    userChoice = attemptToDetermineUserChoice(userInput);

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
                    roll = rollDice(diceRemaining);
                    printRoll(roll);
                }
            }

            // Return the final score
            finalScore = scoreAtTurnStart + expectedNewPoints;
            return finalScore;
        }

        static int attemptToDetermineUserChoice(string userInput){
            if (userInput == "h"){
                return 1;
            }else if (userInput == "r"){
                return 2;
            }else{
                return 0; // Invalid
            }
        }

        static int scoreSelection(int[] selection){
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
