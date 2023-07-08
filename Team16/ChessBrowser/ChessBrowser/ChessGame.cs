namespace ChessBrowser
{
    public class Game
    {
        public string EventName { get; set; }
        public string Site { get; set; }
        public string Round { get; set; }
        public string White { get; set; }
        public string Black { get; set; }
        public uint WhiteElo { get; set; }
        public uint BlackElo { get; set; }
        public char Result { get; set; }
        public DateTime EventDate { get; set; }
        public string Moves { get; set; }

        //public Game(string eventName, string site, string eventDate,
        //    string round, string whitePlayer, string blackPlayer,
        //    string whiteElo, string blackElo, char result, string moves)
        //{
        //    EventName = eventName;
        //    Site = site;
        //    EventDate = eventDate;
        //    Round = round;
        //    White = whitePlayer;
        //    Black = blackPlayer;
        //    WhiteElo = whiteElo;
        //    BlackElo = blackElo;
        //    Result = result;
        //    Moves = moves;
        //}
        public Game()
        {
        }

        public Events GetEvents()
        {
            return new Events(this);
        }

        public Players GetBlackPlayers()
        {
            return new Players(this.Black, this.BlackElo);
        }

        public Players GetWhitePlayers()
        {
            return new Players(this.White, this.WhiteElo);
        }

        public Games GetGames(Players blackPlayer, Players whitePlayer, uint eID)
        {
            return new Games(this, blackPlayer, whitePlayer, eID);
        }
    }

    public class Events
    {
        public string insertQuery = "INSERT IGNORE INTO `Team16ChessProject`.`Events` " +
                     "(`Name`, `Site`, `Date`) " +
                     "VALUES (@Name, @Site, @Date)";

        public string getEIDQuery = "SELECT eID FROM `Team16ChessProject`.`Events` " +
                     "WHERE `Name` = @Name AND `Site` = @Site AND `Date` = @Date";

        public String Name { get; set; }
        public String Site { get; set; }
        public DateTime Date { get; set; }
        public uint eID { get; set; }

        public Events() { }

        public Events(Game game)
        {
            Name = game.EventName;
            Site = game.Site;
            Date = game.EventDate;
        }
    }

    public class Games
    {
        public string insertQuery = "INSERT IGNORE INTO `Team16ChessProject`.`Games` " +
                     "(`Round`, `Result`, `Moves`, `BlackPlayer`, `WhitePlayer`, `eID`) " +
                     "VALUES (@Round, @Result, @Moves, @BlackPlayer, @WhitePlayer, @eID)";
        public string Round { get; set; }
        public char Result { get; set; }
        public string Moves { get; set; }
        public uint BlackPlayer { get; set; }
        public uint WhitePlayer { get; set; }
        public uint eID { get; set; }

        public Games() { }

        public Games(Game game, Players blackPlayer, Players whitePlayer, uint eID)
        {
            Round = game.Round;
            Result = game.Result;
            Moves = game.Moves;
            BlackPlayer = blackPlayer.pID;
            WhitePlayer = whitePlayer.pID;
            this.eID = eID;
        }

    }

    public class Players
    {
        public string insertQuery = "INSERT INTO `Team16ChessProject`.`Players` " +
                     "(`Name`, `Elo`) " +
                     "VALUES (@Name, @Elo) " +
                     "ON DUPLICATE KEY UPDATE `Elo` = IF(VALUES(`Elo`) > `Elo`, VALUES(`Elo`), `Elo`)";

        public string selectPIDQuery = "SELECT pID FROM `Team16ChessProject`.`Players` " +
                     "WHERE `Name` = @Name";

        public String Name { get; set; }
        public uint Elo { get; set; }
        public uint pID { get; set; }

        public Players() { }

        public Players(String name, uint elo)
        {
            Name = name;
            Elo = elo;
        }
    }
}