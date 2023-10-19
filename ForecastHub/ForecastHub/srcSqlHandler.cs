using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Globalization;
using System.Threading;
using System.Runtime.CompilerServices;

namespace ForecastHub
{
    internal class SqlHandler : IDisposable
    {
        // Variables
        private readonly string connectionString = "Server=GTKC-TERMIS\\TERMIS;Database=termisdb;Integrated Security=True";
        private readonly string forecastTableName = "[test].[ForecastRaw]";
        private readonly string runtimeTableName = "[dbo].[tblTagVals]";

        // Constructor
        public SqlHandler() { }

        // Method to write current weather data to SQL server
        public void WriteCData(List <string[]> data)
        {
            int entriesCreated = 0;
            int entriesUpdated = 0;

            // Handle current temperature
            int count = CheckDatabaseEntry("Karlovac_Temperature_RT", data[0][0]);
            if (count == 0)
            {
                CreateDatabaseEntry("Karlovac_Temperature_RT", data[0][0], data[0][1]);
                entriesCreated++;
            }
            else if (count == 1)
            {
                UpdateDatabaseEntry("Karlovac_Temperature_RT", data[0][0], data[0][1]);
                entriesUpdated++;
            }

            // Handle current wind speed
            count = CheckDatabaseEntry("Karlovac_WindSpeed_RT", data[0][0]);
            if (count == 0)
            {
                CreateDatabaseEntry("Karlovac_WindSpeed_RT", data[0][0], data[0][2]);
                entriesCreated++;
            }
            else if (count == 1)
            {
                UpdateDatabaseEntry("Karlovac_WindSpeed_RT", data[0][0], data[0][2]);
                entriesUpdated++;
            }

            // Handle current wind direction
            count = CheckDatabaseEntry("Karlovac_WindDirection_RT", data[0][0]);
            if (count == 0)
            {
                CreateDatabaseEntry("Karlovac_WindDirection_RT", data[0][0], data[0][3]);
                entriesCreated++;
            }
            else if (count == 1)
            {
                UpdateDatabaseEntry("Karlovac_WindDirection_RT", data[0][0], data[0][3]);
                entriesUpdated++;
            }

            // Handle current cloudiness
            count = CheckDatabaseEntry("Karlovac_Cloudiness_RT", data[0][0]);
            if (count == 0)
            {
                CreateDatabaseEntry("Karlovac_Cloudiness_RT", data[0][0], data[0][4]);
                entriesCreated++;
            }
            else if (count == 1)
            {
                UpdateDatabaseEntry("Karlovac_Cloudiness_RT", data[0][0], data[0][4]);
                entriesUpdated++;
            }

            Logger.ToLogFile($"Writting current weather data to database :: Entries created = {entriesCreated}, Entries updated = {entriesUpdated}");
        }

        // Method to write weather forecast data to SQL server
        public void WriteFData(List<string[]> data)
        {
            int entriesCreated = 0;
            int entriesUpdated = 0;

            // Handle temperature forecast entries
            foreach (string[] line in data)
            {
                // Handle current temperature
                int count = CheckDatabaseEntry("Karlovac_Temperature_F_1H3D", line[0]);
                if (count == 0)
                {
                    CreateDatabaseEntry("Karlovac_Temperature_F_1H3D", line[0], line[1]);
                    entriesCreated++;
                }
                else if (count == 1)
                {
                    UpdateDatabaseEntry("Karlovac_Temperature_F_1H3D", line[0], line[1]);
                    entriesUpdated++;
                }

                // Handle current wind speed
                count = CheckDatabaseEntry("Karlovac_WindSpeed_F_1H3D", line[0]);
                if (count == 0)
                {
                    CreateDatabaseEntry("Karlovac_WindSpeed_F_1H3D", line[0], line[2]);
                    entriesCreated++;
                }
                else if (count == 1)
                {
                    UpdateDatabaseEntry("Karlovac_WindSpeed_F_1H3D", line[0], line[2]);
                    entriesUpdated++;
                }

                // Handle current wind direction
                count = CheckDatabaseEntry("Karlovac_WindDirection_F_1H3D", line[0]);
                if (count == 0)
                {
                    CreateDatabaseEntry("Karlovac_WindDirection_F_1H3D", line[0], line[3]);
                    entriesCreated++;
                }
                else if (count == 1)
                {
                    UpdateDatabaseEntry("Karlovac_WindDirection_F_1H3D", line[0], line[3]);
                    entriesUpdated++;
                }

                // Handle current cloudiness
                count = CheckDatabaseEntry("Karlovac_Cloudiness_F_1H3D", line[0]);
                if (count == 0)
                {
                    CreateDatabaseEntry("Karlovac_Cloudiness_F_1H3D", line[0], line[4]);
                    entriesCreated++;
                }
                else if (count == 1)
                {
                    UpdateDatabaseEntry("Karlovac_Cloudiness_F_1H3D", line[0], line[4]);
                    entriesUpdated++;
                }
            }

            Logger.ToLogFile($"Writting weather forecast data to database :: Entries created = {entriesCreated}, Entries updated = {entriesUpdated}");
        }

        // Method to write runtime data to SQL server
        public void WriteRData(List<string[]> data)
        {
            int entriesCreated = 0;
            int entriesUpdated = 0;

            foreach (string[] line in data)
            {

            }
        }

        // Method to read runtime data
        public List<string> ReadRData()
        {
            List<string> result = new List<string>();

            try 
            {
                // Define SQL query paramaters
                DateTime now = DateTime.Now;
                // DateTime targetTime = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
                DateTime targetTime = DateTime.Parse("2022-12-20 08:15:00");
                int toleranceMinutes = 15;
                int[] tagIDs = { 1371, 1372, 1373, 1374 };

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    foreach (int tagID in tagIDs)
                    {
                        // Define SQL query with parameters
                        string sqlQuery = $"SELECT TOP 1 * FROM {runtimeTableName} " +
                                          "WHERE tblTagDefs_fk = @TagID " +
                                          "AND TimeTag >= @StartTime " +
                                          "AND TimeTag <= @EndTime " +
                                          "ORDER BY ABS(DATEDIFF(MINUTE, @TargetTime, TimeTag))";

                        using (SqlCommand command = new SqlCommand(sqlQuery, connection))
                        {
                            command.Parameters.AddWithValue("@TagID", tagID);
                            command.Parameters.AddWithValue("@StartTime", targetTime.AddMinutes(-toleranceMinutes));
                            command.Parameters.AddWithValue("@EndTime", targetTime.AddMinutes(toleranceMinutes));
                            command.Parameters.AddWithValue("@TargetTime", targetTime);

                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    int selectedTagID = reader.GetInt32(reader.GetOrdinal("tblTagDefs_fk"));
                                    DateTime timestamp = reader.GetDateTime(reader.GetOrdinal("TimeTag"));
                                    double value = reader.GetDouble(reader.GetOrdinal("Value"));
                                    string resultString = $"TagID = {selectedTagID} :: TagTimeStamp = {timestamp} :: TagValue = {value}";
                                    result.Add(resultString);
                                }
                            }
                        }
                    }

                    foreach (string s in result)
                    {
                        Console.WriteLine(s);
                    }
                    Console.WriteLine();

                }

                return result;
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while trying to read runtime data :: {ex}");
                return null;
;            }
        }
        
        // Method to check single database entry
        private int CheckDatabaseEntry(string tagName, string tagTimeStamp)
        {
            try
            {
                // Define the SQL query
                string countQuery = $"SELECT COUNT(*) FROM {forecastTableName} WHERE TagName = @TagName AND TagTime = @TagTime";

                // Open connetction and check database entry existance
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand countCommand = new SqlCommand(countQuery, connection))
                    {
                        countCommand.Parameters.AddWithValue("@TagName", tagName);
                        countCommand.Parameters.AddWithValue("@TagTime", tagTimeStamp);
                        return (int)countCommand.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while checking database entry existence {tagName} with time stamp {tagTimeStamp} :: {ex}");
                return -1;
            }
        }

        // Method to create single database entry
        private bool CreateDatabaseEntry(string tagName, string tagTimeStamp, string tagValue)
        {
            try
            {
                // Define the SQL query
                string insertQuery = $"INSERT INTO {forecastTableName} (TagName, TagTime, TagValue) VALUES (@TagName, @TagTime, @TagValue)";

                // Open connetction and create database entry
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand insertCommand = new SqlCommand(insertQuery, connection))
                    {
                        float value = (float)0.0;
                        // Temperature and wind speed are casted directly
                        if (tagName.Contains("Temperature") || tagName.Contains("WindSpeed"))
                        {
                            float.TryParse(tagValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }
                        // Wind direction is decoded in a separate method based on wind rose compass, but only for current weather data
                        else if (tagName.Contains("WindDirection_RT"))
                        {
                            value = (float)DecodeWindDirection(tagValue);
                        }
                        // Wind speed for forecast data it is already in azimuth degrees. Thanks DHMZ :-)
                        else if (tagName.Contains("WindDirection_F_1H3D"))
                        {
                            float.TryParse(tagValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }
                        // Cloudiness is decoded in a separate method based on whatsoever, but only for current data
                        else if (tagName.Contains("Cloudiness_RT"))
                        {
                            value = (float)DecodeCloudiness(tagValue);
                        }
                        // Cloudiness for forecast data it is already represented as number. Thanks DHMZ :-)
                        else if (tagName.Contains("Cloudiness_F_1H3D"))
                        {
                            float.TryParse(tagValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }

                        insertCommand.Parameters.AddWithValue("@TagName", tagName);
                        insertCommand.Parameters.AddWithValue("@TagTime", tagTimeStamp);
                        insertCommand.Parameters.AddWithValue("@TagValue", value);
                        insertCommand.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while creating database entry for tag {tagName} with time stamp {tagTimeStamp} :: {ex}");
                return false;
            }
        }

        // Method to update single tag existence
        private bool UpdateDatabaseEntry(string tagName, string tagTimeStamp, string tagValue)
        {
            try
            {
                // Define the SQL query
                string updateQuery = $"UPDATE {forecastTableName} SET TagValue = @TagValue WHERE TagName = @TagName AND TagTime = @TagTime";

                // Open connetction and create database entry
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        float value = (float)0.0;
                        // Temperature and wind speed are casted directly
                        if (tagName.Contains("Temperature") || tagName.Contains("WindSpeed"))
                        {
                            float.TryParse(tagValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }
                        // Wind speed for forecast data it is already in azimuth degrees. Thanks DHMZ :-)
                        else if (tagName.Contains("WindDirection_F_1H3D"))
                        {
                            float.TryParse(tagValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }
                        // Cloudiness is decoded in a separate method based on whatsoever, but only for current data
                        else if (tagName.Contains("Cloudiness_RT"))
                        {
                            value = (float)DecodeCloudiness(tagValue);
                        }
                        // Cloudiness for forecast data it is already represented as number. Thanks DHMZ :-)
                        else if (tagName.Contains("Cloudiness_F_1H3D"))
                        {
                            float.TryParse(tagValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
                        }

                        updateCommand.Parameters.AddWithValue("@TagName", tagName);
                        updateCommand.Parameters.AddWithValue("@TagTime", tagTimeStamp);
                        updateCommand.Parameters.AddWithValue("@TagValue", value);
                        updateCommand.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while updating database entry for tag {tagName} with time stamp {tagTimeStamp} :: {ex}");
                return false;
            }
        }

        // Method to decode wind direction to float
        private double DecodeWindDirection(string windRoseDirection)
        {
            try
            {
                // Create a dictionary to map wind rose directions to azimuth degrees
                Dictionary<string, double> windRoseToAzimuthMap = new Dictionary<string, double>
                {
                    {"N", 0.0},
                    {"NNE", 22.5},
                    {"NE", 45.0},
                    {"ENE", 67.5},
                    {"E", 90.0},
                    {"ESE", 112.5},
                    {"SE", 135.0},
                    {"SSE", 157.5},
                    {"S", 180.0},
                    {"SSW", 202.5},
                    {"SW", 225.0},
                    {"WSW", 247.5},
                    {"W", 270.0},
                    {"WNW", 292.5},
                    {"NW", 315.0},
                    {"NNW", 337.5}
                };

                // Convert the wind rose direction to uppercase for case insensitivity
                windRoseDirection = windRoseDirection.ToUpper();

                // Check if the wind rose direction exists in the dictionary
                double azimuth;
                if (windRoseToAzimuthMap.TryGetValue(windRoseDirection, out azimuth))
                {
                    return azimuth;
                }
                else
                {
                    // Handle the case where the wind rose direction is not found
                    Logger.ToLogFile($"Invalid wind rose direction");
                    return -1.0;
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"There was an error decoding wind direction :: {ex.Message}");
                return -1.0;
            }
        }

        // Method to decode cloudiness
        private double DecodeCloudiness(string description)
        {
            // TBD
            return 0.0;
        }

        // Dispose method
        public void Dispose() { }
    }
}
