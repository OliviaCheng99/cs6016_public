using System;
using System.Text;
namespace Pgn
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
        public Game() {
        }
    }
}