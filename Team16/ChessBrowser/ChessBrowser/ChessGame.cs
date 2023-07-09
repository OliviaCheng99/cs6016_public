namespace ChessBrowser
{
    public class Game
    {
        public string EventName { get; set; }
        public string Site { get; set; }
        public string Round { get; set; }
        public string WhiteName { get; set; }
        public string BlackName { get; set; }
        public uint WhiteElo { get; set; }
        public uint BlackElo { get; set; }
        public char Result { get; set; }
        public DateTime EventDate { get; set; }
        public string Moves { get; set; }

        public Game() { }
    }
}