using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ForecastHub
{
    internal class CDHandler : IDisposable
    {
        // Constructor
        public CDHandler() { }

        // Fetch current data from DHMZ server
        public (bool RetVal, List<string[]> Data) FetchData()
        {
            // Variables
            DateTime ts = new DateTime();
            List<string[]> data = new List<string[]>();
            string[] entry = new string[5];
            
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = "https://vrijeme.hr/hrvatska_n.xml";
                    var httpRequest = (HttpWebRequest)WebRequest.Create(url);

                    httpRequest.Accept = "application/xml";

                    var httpResponse = (HttpWebResponse)httpRequest.GetResponse();
                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var result = streamReader.ReadToEnd();

                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(result);

                        // Find timestamp
                        XmlNode dateNode = doc.SelectSingleNode("//DatumTermin/Datum");
                        XmlNode timeNode = doc.SelectSingleNode("//DatumTermin/Termin");
                        try
                        {
                            DateTime.TryParse(dateNode.InnerText + " " + timeNode.InnerText + ":00:00", out ts);
                            entry[0] = ts.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        }
                        catch (Exception ex)
                        {
                            Logger.ToLogFile($"Error while parsing timestamp for current data :: " + ex.Message);
                            return (false, data);
                        }

                        // Find current weather values for the city of Karlovac
                        XmlNodeList cityNodes = doc.SelectNodes("//Grad/GradIme");
                        foreach (XmlNode cityNode in cityNodes)
                        {
                            if (cityNode.InnerText == "Karlovac")
                            {
                                XmlNode karlovacNode = cityNode.ParentNode;
                                XmlNode karlovacPodaci = karlovacNode.SelectSingleNode("Podatci");
                                foreach (XmlNode podatak in karlovacPodaci)
                                {
                                    if (podatak.Name == "Temp")
                                    {
                                        entry[1] = podatak.InnerText;
                                    }
                                    else if (podatak.Name == "VjetarBrzina")
                                    {
                                        entry[2] = podatak.InnerText;
                                    }
                                    else if (podatak.Name == "VjetarSmjer")
                                    {
                                        entry[3] = podatak.InnerText;
                                    }
                                    else if (podatak.Name == "Vrijeme")
                                    {
                                        entry[4] = podatak.InnerText;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while downloading current weather data :: {ex.Message}");
                return (false, data);
            }

            data.Add(entry);
            return (true, data);
        }

        // Dispose method
        public void Dispose() { }
    }
}
