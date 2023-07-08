using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Pgn
{
    public class PngReader
    {
        public static void Main()
        {
            string currentDir = Directory.GetCurrentDirectory();
            //Console.WriteLine(currentDir);
            string pgnDir = Path.GetFullPath(Path.Combine(currentDir, "../../../../..")) ;
            //Console.WriteLine(pgnDir);
            string[] pgnFiles = Directory.GetFiles(pgnDir, "*.pgn");
            //Console.WriteLine(pgnFiles[0]);
            var games = GetAllGames(pgnFiles[0]); // only print out the kb1.pgn

            var type = games[0].GetType();
            PropertyInfo[] properties = type.GetProperties();
            int index = 0;
            while (index < 5)
            {
                foreach (PropertyInfo property in properties)
                {
                    Console.WriteLine("Property name: " + property.Name);
                    Console.WriteLine("Property value: " + property.GetValue(games[index], null));
                }
                index++;
            }
        }

        private static readonly Regex tagPattern = new Regex(@"\[(\w+)\s+""(.+)""\]");

        // methods
        public static List<Game> GetAllGames(string path)
        {
            // Read all lines from the file into an array of strings
            string[] lines = File.ReadAllLines(path);

            // Initialize
            Game currentGame = new();
            List<Game> games = new();
            Dictionary<string, Action<Game, string>> tagHandlers = new Dictionary<string, Action<Game, string>>
            {
                { "Event", (game, value) => game.EventName = value },
                { "Site", (game, value) => game.Site = value },
                { "Round", (game, value) => game.Round = value },
                { "White", (game, value) => game.White = value },
                { "Black", (game, value) => game.Black = value },
                { "Result", (game, value) => game.Result = HandleResult(value) },
                { "WhiteElo", (game, value) => game.WhiteElo = uint.Parse(value) },
                { "BlackElo", (game, value) => game.BlackElo = uint.Parse(value) },
                { "EventDate", (game, value) => game.EventDate = HandleDate(value) },
            };


            // To keep track if we are reading moves for a game
            bool isReadingMoves = false;

            foreach (string line in lines)
            {
                // If the line is not blank and starts with a '[', it's a tag pair line
                if (!string.IsNullOrWhiteSpace(line) && line.StartsWith("["))
                {
                    Match match = tagPattern.Match(line);
                    if (match.Success)
                    {
                        string tag = match.Groups[1].Value;
                        string value = match.Groups[2].Value;
                        if (tagHandlers.ContainsKey(tag))
                        {
                            tagHandlers[tag](currentGame, value);
                        }
                    }
                }
                // If the line is blank, it means either the next lines will be the chess moves
                // or it's the end of the current game and the start of the new game
                else if (string.IsNullOrWhiteSpace(line))
                {
                    if (isReadingMoves)  // It's the end of the current game
                    {
                        games.Add(currentGame);
                        currentGame = new Game();
                        isReadingMoves = false;
                    }
                    else  // It's the start of the moves of the current game
                    {
                        isReadingMoves = true;
                    }
                }
                // If we're reading moves and the line is not blank, it's a move
                else if (isReadingMoves && !string.IsNullOrWhiteSpace(line))
                {
                    currentGame.Moves += line + "\n";
                }
            }

            // make sure to add the last game if it hasn't been added yet
            if (currentGame.EventName != null && !games.Contains(currentGame))
            {
                games.Add(currentGame);
            }

            return games;
        }
        //For the Result field, the extracted data should be converted to a single character: 'W', 'B', or 'D' for white wins, black wins, or draw, respectively.
        ///A PGN Result of "1-0" means white won, "0-1" means black won, and "1/2-1/2" is a draw.
        static char HandleResult(string res)
        {
            char newRes = new();
            switch (res)
            {
                case "1-0":
                    newRes = 'W';
                    break;
                case "0-1":
                    newRes = 'B';
                    break;
                case "1/2-1/2":
                    newRes = 'D';
                    break;
            }
            return newRes;

        }
        static DateTime HandleDate(string date) {
            DateTime res = new();
            string format = "yyyy.MM.dd";

            return date.Contains('?')?
                DateTime.ParseExact("0000-00-00", format, CultureInfo.InvariantCulture):
                DateTime.ParseExact(date, format, CultureInfo.InvariantCulture);
        }

    }
}

