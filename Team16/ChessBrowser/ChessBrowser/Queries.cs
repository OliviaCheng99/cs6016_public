using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Maui.Controls;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Tracing;
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
            var games = PngReader.GetAllGames(PGNfilename);
            //for (int i = 0; i < 5; ++i)
            //{
            //    Console.WriteLine(games[i].EventName);
            //}

            // TODO:
            //       Use this to tell the GUI's progress bar how many total work steps there are
            //       For example, one iteration of your main upload loop could be one work step
            mainPage.SetNumWorkItems(games.Count);

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
                        // insert into table Events;
                        string insertEventsQuery = "INSERT IGNORE INTO Events (Name, Site, Date) " +
                            "VALUES (@Name, @Site, @Date)";
                        //Console.WriteLine(insertEventsQuery);

                        using (MySqlCommand command = new(insertEventsQuery, conn))
                        {
                            command.Parameters.AddWithValue("@Name", game.EventName);
                            command.Parameters.AddWithValue("@Site", game.Site);
                            command.Parameters.AddWithValue("@Date", game.EventDate);
                            command.ExecuteNonQuery();
                        }

                        // get table Events column eID and this is used in inserting into table Games
                        // UNIQUE KEY `Name` (`Name`,`Date`,`Site`)
                        string getEIDQuery = "SELECT eID FROM Events " +
                            "WHERE Name = @Name AND Date = @Date AND Site = @Site";
                        uint eID;
                        using (MySqlCommand command = new(getEIDQuery, conn))
                        {
                            command.Parameters.AddWithValue("@Name", game.EventName);
                            command.Parameters.AddWithValue("@Site", game.Site);
                            command.Parameters.AddWithValue("@Date", game.EventDate);
                            eID = Convert.ToUInt32(command.ExecuteScalar());
                        }

                        // insert into table Players and update the player elo to the greatest;
                        // black first
                        string insertBlackPlayerQuery = "INSERT INTO Players (Name, Elo) " +
                            "VALUES (@BlackName, @BlackElo) ON DUPLICATE KEY UPDATE Elo= GREATEST(Elo,@BlackElo)";
                        using (MySqlCommand command = new(insertBlackPlayerQuery, conn))
                        {
                            command.Parameters.AddWithValue("@BlackName", game.BlackName);
                            command.Parameters.AddWithValue("@BlackElo", game.BlackElo);
                            command.ExecuteNonQuery();
                        }
                        // get black player pID
                        string blackPIdQuery = "SELECT pID FROM Players WHERE Name = @BlackPlayerName";
                        uint blackPID;
                        using (MySqlCommand command = new(blackPIdQuery, conn))
                        {
                            command.Parameters.AddWithValue("@BlackPlayerName", game.BlackName);
                            blackPID = Convert.ToUInt32(command.ExecuteScalar());
                        }

                        // white player then
                        string insertWhitePlayerQuery = "INSERT INTO Players (Name, Elo) " +
                            "VALUES (@WhiteName, @WhiteElo) ON DUPLICATE KEY UPDATE Elo= GREATEST(Elo,@WhiteElo)";
                        using (MySqlCommand command = new(insertWhitePlayerQuery, conn))
                        {
                            command.Parameters.AddWithValue("@WhiteName", game.WhiteName);
                            command.Parameters.AddWithValue("@WhiteElo", game.WhiteElo);
                            command.ExecuteNonQuery();
                        }
                        // get white player pID
                        string whitePIdQuery = "SELECT pID FROM Players WHERE Name = @WhitePlayerName";
                        uint whitePID;
                        using (MySqlCommand command = new(whitePIdQuery, conn))
                        {
                            command.Parameters.AddWithValue("@WhitePlayerName", game.WhiteName);
                            whitePID = Convert.ToUInt32(command.ExecuteScalar());
                        }

                        // insert into table Games;
                        string insertGamesQuery = "INSERT IGNORE INTO Games " +
                           "(Round, Result, Moves, BlackPlayer, WhitePlayer,eID) " +
                           "VALUES (@Round, @Result, @Moves, @BlackPID, @WhitePID, @eID)";
                        using (MySqlCommand command = new(insertGamesQuery, conn))
                        {
                            command.Parameters.AddWithValue("@Round", game.Round);
                            command.Parameters.AddWithValue("@Result", game.Result);
                            command.Parameters.AddWithValue("@Moves", game.Moves);
                            command.Parameters.AddWithValue("@BlackPID", blackPID);
                            command.Parameters.AddWithValue("@WhitePID", whitePID);
                            command.Parameters.AddWithValue("@eID", eID);
                            command.ExecuteNonQuery();
                        }

                        // TODO:
                        //       Use this inside a loop to tell the GUI that one work step has completed:
                        await mainPage.NotifyWorkItemCompleted();
                    }

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
            //Console.WriteLine(start.ToString("yyyy-MM-dd"));
            // This will build a connection string to your user's database on atr,
            // assuimg you've typed a user and password in the GUI
            string connection = mainPage.GetConnectionString();

            // Build up this string containing the results from your query
            string parsedResult = "";

            // Use this to count the number of rows returned by your query
            // (see below return statement)
            int numRows = 0;

            using (MySqlConnection conn = new(connection))
            {
                try
                {
                    // Open a connection
                    conn.Open();

                    // Dictionary to hold the parameters
                    Dictionary<string, string> paraDict = new()
                        {
                            { "@WhitePlayerName", white },
                            { "@BlackPlayerName", black },
                            { "@OpeningMove", opening != null ? opening + "%" : null },
                            { "@Result", winner },
                            { "@StartDate", useDate ? start.ToString("yyyy-MM-dd") : null },
                            { "@EndDate", useDate ? end.ToString("yyyy-MM-dd") : null }
                        };

                    // Dictionary to hold the corresponding SQL strings
                    Dictionary<string, string> queryDict = new()
                        {
                            { "@WhitePlayerName", " AND WP.Name=@WhitePlayerName" },
                            { "@BlackPlayerName", " AND BP.Name=@BlackPlayerName" },
                            { "@OpeningMove", " AND G.Moves LIKE @OpeningMove"},
                            { "@Result", " AND G.Result=@Result" },
                            { "@StartDate", " AND E.Date>=@StartDate" },
                            { "@EndDate", " AND E.Date<=@EndDate" }
                         };

                    // basic query
                    StringBuilder query = new();
                    string queryStr = "SELECT E.Name AS EventName, E.Site, E.Date, " +
                        "WP.Name AS White, BP.Name AS Black, " +
                        "WP.Elo AS WhiteElo, BP.Elo AS BlackElo, G.Result, G.Moves " +
                        "FROM Events E INNER JOIN Games G ON E.eID = G.EID " +
                        "INNER JOIN Players WP ON G.WhitePlayer = WP.pID " +
                        "INNER JOIN Players BP ON G.BlackPlayer = BP.pID WHERE TRUE ";
                    query.Append(queryStr);

                    // TODO: Generate and execute an SQL command
                    // Iterate over the parameters to update the query
                    foreach (KeyValuePair<string, string> paraEntry in paraDict)
                    {
                        if (paraEntry.Value != null)
                        {
                            //Console.WriteLine(queryDict[paraEntry.Key]);
                            query.Append(queryDict[paraEntry.Key]);
                        }
                    }

                    using (MySqlCommand command = new(query.ToString(), conn))
                    {

                        foreach (KeyValuePair<string, string> paraEntry in paraDict)
                        {
                            if (paraEntry.Value != null)
                            {
                                command.Parameters.AddWithValue(paraEntry.Key, paraEntry.Value);
                            }
                        }

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            //Console.WriteLine("here in reader");
                            while (reader.Read())
                            {
                                numRows++;
                                parsedResult += "Event: " + reader["EventName"] + "\n";
                                parsedResult += "Site: " + reader["Site"] + "\n";
                                parsedResult += "Date: " + reader["Date"] + "\n";
                                parsedResult += "White:" + reader["White"] + " (" + reader["WhiteElo"].ToString() + ")\n";
                                parsedResult += "Black: " + reader["Black"] + " (" + reader["BlackElo"].ToString() + ")\n";
                                parsedResult += "Result: " + reader["Result"];
                                if (showMoves)
                                {
                                    parsedResult += "\n" + "Moves: " + reader["Moves"];
                                }
                                parsedResult += "\n\n";
                                //Console.WriteLine(parsedResult);
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
