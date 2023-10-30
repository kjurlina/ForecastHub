using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ForecastHub

{
    // Forecast data handler
    internal class FDHandler : IDisposable
    {
        // Constructor
        public FDHandler() { }

        // Fetch forecast data from DHMZ server
        public (bool RetVal, List<string[]> Data) FetchData()
        {
            // Variables
            string targetFileName;
            DateTime ts = new DateTime();
            List<string[]> data = new List<string[]>();
            string[] entry = new string[5];

            // Find target file name
            if (DateTime.Now.Hour >= 0 && DateTime.Now.Hour < 17)
            {
                targetFileName = "KAR_" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") + "_00.xml";
            }
            else
            {
                targetFileName = "KAR_" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("00") + DateTime.Now.Day.ToString("00") + "_12.xml";
            }

            try
            {
                // Create FTP request for the target file
                string remoteFilePath = $"{Project.FTPAddress}/{Project.FTPFolder}/{targetFileName}";
                string xmlContent = ReadXmlContentToStringList(remoteFilePath, Project.FTPUsername, Project.FTPPassword);

                if (xmlContent != null) 
                {
                    // Store XML content into a list of strings (each line is a separate string in the list)
                    XmlDocument xmlDoc = ReadXmlContentToXmlDocument(remoteFilePath, Project.FTPUsername, Project.FTPPassword);
                    if (xmlDoc != null) 
                    {
                        // Get all "termin" nodes
                        XmlNodeList terminNodes = xmlDoc.SelectNodes("//termin");
                        foreach (XmlNode terminNode in terminNodes)
                        {
                            entry = new string[5];
                            DateTime.TryParse(terminNode.Attributes["datum"].Value + " " + terminNode.Attributes["sat"].Value.Replace("UTC","") + ":00:00", out ts);
                            entry[0] = ts.ToString("yyyy-MM-ddTHH:mm:ssZ");
                            entry[1] = terminNode.SelectSingleNode("temperatura")?.InnerText;
                            entry[2] = terminNode.SelectSingleNode("brzina_vjetra")?.InnerText;
                            entry[3] = terminNode.SelectSingleNode("smjer_vjetra")?.InnerText;
                            entry[4] = terminNode.SelectSingleNode("naoblaka")?.InnerText;

                            data.Add(entry);
                        }

                        return (true, data);
                    }
                    else
                    {
                        return (false, data);
                    }
                }
                else 
                {
                    return (false, data); 
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while fetching weather forecast data :: {ex.Message}");
                return (false, data);
            }
        }

        // Read XML content to list of string
        static string ReadXmlContentToStringList(string remoteFilePath, string username, string password)
        {
            try
            {
                // Create FTP request for downloading the file
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(remoteFilePath));
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(username, password);

                // Read the file content from the FTP server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading XML file content: {ex.Message}");
                return null;
            }
        }

        // Read XML content to XML document
        static XmlDocument ReadXmlContentToXmlDocument(string remoteFilePath, string username, string password)
        {
            try
            {
                // Create FTP request for downloading the file
                FtpWebRequest request = (FtpWebRequest)WebRequest.Create(new Uri(remoteFilePath));
                request.Method = WebRequestMethods.Ftp.DownloadFile;
                request.Credentials = new NetworkCredential(username, password);

                // Read the file content from the FTP server
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                using (Stream responseStream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string xmlContent = reader.ReadToEnd();

                    // Load the XML content into an XmlDocument
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(xmlContent);

                    return xmlDoc;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading XML file content: {ex.Message}");
                return null;
            }
        }

        // Dispose method
        public void Dispose() { }
    }

}
