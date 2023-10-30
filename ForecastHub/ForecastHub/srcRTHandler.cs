using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz.Util;

namespace ForecastHub
{
    internal class RTHandler : IDisposable
    {
        // Constructor
        public RTHandler() { }

        // Fetch and organize data from SQL server
        public (bool RetVal, List<string[]> Data) FetchData()
        {
            // Variables
            DateTime ts = new DateTime();
            List<string[]> data = new List<string[]>();

            try
            {
                using (SqlHandler SqlHandler = new SqlHandler())
                {
                    List<string> result = SqlHandler.ReadRData();
                    foreach (string s in result)
                    {
                        // Define string placeholder
                        string[] entry = new string[3];
                        // Extract tag name
                        entry[0] = Project.GetTagName(s.Split(new string[] { " :: " }, StringSplitOptions.None)[0]);
                        // Extract tag time stamp
                        DateTime.TryParse(s.Split(new string[] { " :: " }, StringSplitOptions.None)[1], out ts);
                        entry[1] = ts.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        // Extract tag value
                        entry[2] = s.Split(new string[] { " :: " }, StringSplitOptions.None)[2];
                        // Add entry to result
                        data.Add(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while fetching Termis database data :: {ex.Message}");
                return (false, data);
            }

            return (true, data);
        }       

        // Dispose method
        public void Dispose() { }
    }
}
