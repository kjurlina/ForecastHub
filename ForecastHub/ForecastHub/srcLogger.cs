using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ForecastHub
{
    internal static class Logger
    {
        // Global class variables
        static string LogFilePath;
        static string SeparationString;
        static bool Verbose = true;

        // Make sure only one thread accesses file handling functions
        static readonly object FileAccessGuard = new object();

        // Class constructor
        static Logger()
        {
            // Create configuration & log file names & paths
            if (Environment.OSVersion.ToString().Contains("Windows"))
            {
                LogFilePath = @".\logs\ForecastHubLog.txt";
                SeparationString = " :: ";
            }
            else if (Environment.OSVersion.Platform.ToString().Contains("Linux"))
            {
                LogFilePath = @".\logs\ForecastHubLog.txt";
                SeparationString = " :: ";
            }
            else
            {
                LogFilePath = "";
                SeparationString = "";
            }
        }

        // Check log file existance
        public static bool CheckLogFileExistence()
        {
            // Check if config file exists
            return File.Exists(LogFilePath);
        }

        // Check log file size
        public static long ChecklLogFileSize()
        {
            // Check log file size
            long LogFileSize = new FileInfo(LogFilePath).Length;
            return LogFileSize;
        }

        // Create log file
        public static void CreateLogFile()
        {
            Directory.CreateDirectory("logs");
            var fileStream = File.Create(LogFilePath);
            fileStream.Close();
            ToLogFile("New log file created", false);
        }

        // Archive log file
        public static void ArchiveLogFile()
        {
            // First log that existing log file will be archived
            ToLogFile("Application will archive this log file because it's too big", false);

            // Save file and extend name with current timestamp
            string ArchiveLogFilePath;
            string LogFileTS = DateTime.Now.Year.ToString() + "_" +
                               DateTime.Now.Month.ToString() + "_" +
                               DateTime.Now.Day.ToString() + "_" +
                               DateTime.Now.Hour.ToString() + "_" +
                               DateTime.Now.Minute.ToString() + "_" +
                               DateTime.Now.Second.ToString();

            ArchiveLogFilePath = @".\logs\ForecastHubLog_" + LogFileTS + ".txt";

            // Create current log file archive copy
            File.Copy(LogFilePath, ArchiveLogFilePath);
            File.Delete(LogFilePath);
        }

        // Log message to console
        public static void ToConsole(string message)
        {
            // Output message to console
            string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
            Console.WriteLine(MessageTS + SeparationString + message);
        }

        // Log message to file with file check
        public static void ToLogFile(string message)
        {
            // Output message to log file - with log file check
            // Prior to writing lock the file (multithreading)
            try
            {
                lock (FileAccessGuard)
                {
                    // Prior to writing to log file, check it's existance
                    // Also check if file size is exceeded - if yes, archive and create new one
                    if (!CheckLogFileExistence())
                    {
                        // Create log file
                        CreateLogFile();
                    }
                    else if (ChecklLogFileSize() > 1048576)
                    {
                        // If log file is too big archive it and create new one
                        ArchiveLogFile();
                        CreateLogFile();
                    }

                    string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
                    using (StreamWriter sw = File.AppendText(LogFilePath))
                    {
                        sw.WriteLine(MessageTS + SeparationString + message);
                        if (Verbose)
                        {
                            Console.WriteLine(MessageTS + SeparationString + message);
                        }
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Something went wrong writing message to log file. Please call ATO support");
                Console.WriteLine(ex.ToString());
            }
        }

        // Log message to file without file check
        public static void ToLogFile(string message, bool check)
        {
            // Output message to log file - without file check
            // Prior to writing lock the file (multithreading)
            try
            {
                lock (FileAccessGuard)
                {
                    string MessageTS = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff");
                    using (StreamWriter sw = File.AppendText(LogFilePath))
                    {
                        sw.WriteLine(MessageTS + SeparationString + message);
                        if (Verbose)
                        {
                            Console.WriteLine(MessageTS + SeparationString + message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong writing message to log file. Please call ATO support");
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
