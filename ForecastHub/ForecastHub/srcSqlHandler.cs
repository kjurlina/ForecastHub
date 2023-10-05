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

namespace ForecastHub
{
    internal class SqlHandler : IDisposable
    {
        // Variables
        private readonly string connectionString = "Server=HRATOMX8162\\WINCCFLEX2014;Database=Forecast;Integrated Security=True";
        private readonly string tableName = "[test].[ForecastRaw]";

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
        public void WriteFData (List<string []> data)
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
        
        // Method to check single database entry
        private int CheckDatabaseEntry(string tagName, string tagTimeStamp)
        {
            try
            {
                // Define the SQL query
                string countQuery = $"SELECT COUNT(*) FROM {tableName} WHERE TagName = @TagName AND TagTime = @TagTime";

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
                string insertQuery = $"INSERT INTO {tableName} (TagName, TagTime, TagValue) VALUES (@TagName, @TagTime, @TagValue)";

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
                            try
                            {
                                value = (float)DecodeWindDirection(tagValue);
                            }
                            catch (ArgumentException ex)
                            {
                                Logger.ToLogFile(ex.Message);
                                Logger.ToLogFile("There was an error decoding wind direction. Using -1.0 as value");
                                value = (float)-1.0;
                            }
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
                string updateQuery = $"UPDATE {tableName} SET TagValue = @TagValue WHERE TagName = @TagName AND TagTime = @TagTime";

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
            if (windRoseToAzimuthMap.ContainsKey(windRoseDirection))
            {
                return windRoseToAzimuthMap[windRoseDirection];
            }
            else
            {
                throw new ArgumentException("Invalid wind rose direction");
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
