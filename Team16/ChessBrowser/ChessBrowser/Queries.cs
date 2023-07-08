using Microsoft.Maui.Controls;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChessBrowser
{
    internal class Queries
    {

        /// <summary>
        /// This function runs when the upload button is pressed.
        /// Given a filename, parses the PGN file, and uploads
        /// each chess game to the user's database.
        /// </summary>
        /// <param name="PGNfilename">The path to the PGN file</param>
        internal static async Task InsertGameData(string PGNfilename, MainPage mainPage)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = mainPage.GetConnectionString();

            // TODO:
            //       Load and parse the PGN file
            //       We recommend creating separate libraries to represent chess data and load the file

            // TODO:
            //       Use this to tell the GUI's progress bar how many total work steps there are
            //       For example, one iteration of your main upload loop could be one work step
            //mainPage.SetNumWorkItems( ... );
            var games = PngReader.GetAllGames(PGNfilename);


            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO:
                    //       iterate through your data and generate appropriate insert commands

                    foreach (Game game in games)
                    {
                        var events = game.GetEvents();
                        MySqlCommand command = new MySqlCommand(events.insertQuery, conn);
                        command.Parameters.AddWithValue("@Name", events.Name);
                        command.Parameters.AddWithValue("@Site", events.Site);
                        command.Parameters.AddWithValue("@Date", events.Date);
                        command.ExecuteNonQuery();

                        command = new MySqlCommand(events.getEIDQuery, conn);
                        command.Parameters.AddWithValue("@Name", events.Name);
                        command.Parameters.AddWithValue("@Site", events.Site);
                        command.Parameters.AddWithValue("@Date", events.Date);
                        var result = command.ExecuteScalar();
                        var eID = Convert.ToUInt32(result);


                        var bPlayers = game.GetBlackPlayers();
                        command = new MySqlCommand(bPlayers.insertQuery, conn);
                        command.Parameters.AddWithValue("@Name", bPlayers.Name);
                        command.Parameters.AddWithValue("@Elo", bPlayers.Elo);
                        command.ExecuteNonQuery();

                        command = new MySqlCommand(bPlayers.selectPIDQuery, conn);
                        command.Parameters.AddWithValue("@Name", bPlayers.Name);
                        result = command.ExecuteScalar();
                        var pID = Convert.ToUInt32(result);
                        bPlayers.pID = pID;

                        var wPlayers = game.GetWhitePlayers();
                        command = new MySqlCommand(wPlayers.insertQuery, conn);
                        command.Parameters.AddWithValue("@Name", wPlayers.Name);
                        command.Parameters.AddWithValue("@Elo", wPlayers.Elo);
                        command.ExecuteNonQuery();

                        command = new MySqlCommand(wPlayers.selectPIDQuery, conn);
                        command.Parameters.AddWithValue("@Name", wPlayers.Name);
                        result = command.ExecuteScalar();
                        pID = Convert.ToUInt32(result);
                        wPlayers.pID = pID;

                        var chessGame = game.GetGames(bPlayers, wPlayers, eID);
                        command = new MySqlCommand(chessGame.insertQuery, conn);
                        command.Parameters.AddWithValue("@Round", chessGame.Round);
                        command.Parameters.AddWithValue("@Result", chessGame.Result);
                        command.Parameters.AddWithValue("@Moves", chessGame.Moves);
                        command.Parameters.AddWithValue("@BlackPlayer", chessGame.BlackPlayer);
                        command.Parameters.AddWithValue("@WhitePlayer", chessGame.WhitePlayer);
                        command.Parameters.AddWithValue("@eID", chessGame.eID);
                        command.ExecuteNonQuery();
                    }

                    // TODO:
                    //       Use this inside a loop to tell the GUI that one work step has completed:
                    await mainPage.NotifyWorkItemCompleted();

                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

        }


        /// <summary>
        /// Queries the database for games that match all the given filters.
        /// The filters are taken from the various controls in the GUI.
        /// </summary>
        /// <param name="white">The white player, or null if none</param>
        /// <param name="black">The black player, or null if none</param>
        /// <param name="opening">The first move, e.g. "1.e4", or null if none</param>
        /// <param name="winner">The winner as "W", "B", "D", or null if none</param>
        /// <param name="useDate">True if the filter includes a date range, False otherwise</param>
        /// <param name="start">The start of the date range</param>
        /// <param name="end">The end of the date range</param>
        /// <param name="showMoves">True if the returned data should include the PGN moves</param>
        /// <returns>A string separated by newlines containing the filtered games</returns>
        internal static string PerformQuery(string white, string black, string opening,
          string winner, bool useDate, DateTime start, DateTime end, bool showMoves,
          MainPage mainPage)
        {
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = mainPage.GetConnectionString();

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new MySqlConnection(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // TODO: Generate and execute an SQL command
                    string selectClause = @"
                        SELECT
                            Events.Name,
                            Events.Site,
                            Events.Date,
                            Games.Result";

                    if (showMoves)
                    {
                        selectClause += ", Games.Moves";
                    }

                    selectClause += @",
                            WhitePlayers.Name AS White,
                            WhitePlayers.Elo AS WhiteElo,
                            BlackPlayers.Name AS Black,
                            BlackPlayers.Elo AS BlackElo";

                    string fromClause = @"
                        FROM
                            Team16ChessProject.Events
                            JOIN Team16ChessProject.Games ON Team16ChessProject.Events.eID = Team16ChessProject.Games.eID
                            JOIN Team16ChessProject.Players AS WhitePlayers ON Team16ChessProject.Games.WhitePlayer = WhitePlayers.pID
                            JOIN Team16ChessProject.Players AS BlackPlayers ON Team16ChessProject.Games.BlackPlayer = BlackPlayers.pID";

                    string whereClause = "";

                    if (!string.IsNullOrEmpty(white))
                    {
                        whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                        whereClause += " WhitePlayers.Name = @White";
                    }

                    if (!string.IsNullOrEmpty(black))
                    {
                        whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                        whereClause += " BlackPlayers.Name = @Black";
                    }

                    if (!string.IsNullOrEmpty(opening))
                    {
                        whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                        whereClause += " Moves LIKE @opening";
                    }

                    if (!string.IsNullOrEmpty(winner))
                    {
                        whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                        whereClause += " Result = @winner";
                    }

                    if (useDate)
                    {
                        whereClause += string.IsNullOrEmpty(whereClause) ? "" : " AND ";
                        whereClause += " Date BETWEEN @Start AND @End";
                    }

                    string sql = selectClause + Environment.NewLine + fromClause;
                    if (!string.IsNullOrEmpty(whereClause))
                    {
                        sql += Environment.NewLine + "WHERE" + Environment.NewLine + whereClause;
                    }

                    using (MySqlCommand command = new MySqlCommand(sql, conn))
                    {
                        // Set parameter values based on the provided parameters
                        if (!string.IsNullOrEmpty(white))
                        {
                            command.Parameters.AddWithValue("@White", white);
                        }

                        if (!string.IsNullOrEmpty(black))
                        {
                            command.Parameters.AddWithValue("@Black", black);
                        }

                        if (!string.IsNullOrEmpty(opening))
                        {
                            command.Parameters.AddWithValue("@Opening", opening + "%");
                        }

                        if (!string.IsNullOrEmpty(winner))
                        {
                            command.Parameters.AddWithValue("@Winner", winner);
                        }

                        if (useDate)
                        {
                            command.Parameters.AddWithValue("@Start", start);
                            command.Parameters.AddWithValue("@End", end);
                        }

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Parse the results into the formatted string
                                parsedResult += "Event: " + reader.GetString("Name") + "\n";
                                parsedResult += "Site: " + reader.GetString("Site") + "\n";
                                parsedResult += "Date: " + reader.GetDateTime("Date").ToString() + "\n";
                                parsedResult += $"White: {reader.GetString("White")} ({reader.GetUInt32("WhiteElo")})\n";
                                parsedResult += $"Black: {reader.GetString("Black")} ({reader.GetUInt32("BlackElo")})\n";
                                parsedResult += "Result: " + reader.GetChar("Result") + "\n";
                                if (showMoves)
                                {
                                    parsedResult += "Moves: " + reader.GetString("Moves");
                                }
                                parsedResult += "\n";
                                numRows++;
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            return numRows + " results\n\n" + parsedResult;
        }

    }
}
