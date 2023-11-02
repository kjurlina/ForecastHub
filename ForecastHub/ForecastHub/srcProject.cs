using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForecastHub
{
    internal static class Project
    {
        // Global class variables
        static string ConfigFilePath;
        static string SeparationString;

        public static string FTPAddress;
        public static string FTPUsername;
        public static string FTPPassword;
        public static string FTPFolder;

        public static readonly Dictionary<string, string> RTTagMap = new Dictionary<string, string>
        {
            { "1386", "Karlovac_ReturnTemperature_RT" },
            { "1391", "Karlovac_FlowTemperature_RT" },
            { "1408", "Karlovac_OutdoorTemperature_RT" },
            { "1422", "Karlovac_HeatFlow_RT" },
            { "1424", "Karlovac_HeatPower_RT" }            
        };

        static string[] ConfigFileContent;
        static string[] ConfigLineContent;
        static int ConfigFileNumberOfLines;

        static int i;

        // Make sure only one thread accesses file handling functions
        static readonly object FileAccessGuard = new object();

        // Class constructor
        static Project()
        {
            // Create configuration & log file names & paths
            if (Environment.OSVersion.ToString().Contains("Windows"))
            {
                ConfigFilePath = @".\config\ForecastHubConfig.txt";
                SeparationString = " :: ";
            }
            else if (Environment.OSVersion.Platform.ToString().Contains("Linux"))
            {
                ConfigFilePath = @".\config\ForecastHubConfig.txt";
                SeparationString = " :: ";
            }
            else
            {
                ConfigFilePath = "";
                SeparationString = "";
            }
        }

        // Open project
        public static bool Open()
        {
            if (CheckConfigFileExistence() && ReadConfigFile())
            {
                // This method to get project data from configuration file
                try
                {
                    i = 0;

                    while (ConfigFileContent != null && i < ConfigFileNumberOfLines)
                    {
                        ConfigLineContent = ConfigFileContent[i].Split(new[] { SeparationString }, StringSplitOptions.None);
                        if (ConfigLineContent[0] == "FTP Address")
                        {
                            FTPAddress = ConfigLineContent[1];
                        }
                        if (ConfigLineContent[0] == "FTP Username")
                        {
                            FTPUsername = ConfigLineContent[1];
                        }
                        if (ConfigLineContent[0] == "FTP Password")
                        {
                            FTPPassword = ConfigLineContent[1];
                        }
                        if (ConfigLineContent[0] == "FTP Folder")
                        {
                            FTPFolder = ConfigLineContent[1];
                        }
                        i++;
                    }

                    if (FTPAddress == null || FTPUsername == null || FTPPassword == null || FTPFolder == null)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }

                }
                catch (Exception ex)
                {
                    Logger.ToLogFile("Something went wrong while reading project data from configuration file");
                    Logger.ToLogFile(ex.Message);
                    return false;
                }
            }
            else 
            {
                Logger.ToLogFile("Something went wrong while reading configuration file");
                return false;
            }

        }

        // Check existance of configuration file
        static bool CheckConfigFileExistence()
        {
            // Check if config file exists
            return File.Exists(ConfigFilePath);
        }

        // Read project configuration file
        static bool ReadConfigFile()
        {
            // This method to read configuration file content
            ConfigFileContent = File.ReadAllLines(ConfigFilePath);

            if (ConfigFileContent.Length > 0)
            {
                // Get number of configuration file lines
                ConfigFileNumberOfLines = File.ReadAllLines(ConfigFilePath).Length;
                return true;
            }
            else
            {
                return false;
            }
        }

        // Get rutnime tag name
        public static string GetTagName(string tagId)
        {
            return RTTagMap.ContainsKey(tagId) ? RTTagMap[tagId] : "TagID not found";
        }

    }
}
