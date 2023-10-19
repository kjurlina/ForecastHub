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
            List<string[]> data = new List<string[]>();
            string[] entry = new string[3];

            try
            {
                using (SqlHandler SqlHandler = new SqlHandler())
                {
                    List<string> result = SqlHandler.ReadRData();
                    foreach (string s in result)
                    {
                        // Extract tag name
                        entry[0] = DecodeTagName(s.Split(new string[] { " :: " }, StringSplitOptions.None)[0]);
                        // Extract tag time stamp
                        entry[1] = s.Split(new string[] { " :: " }, StringSplitOptions.None)[1];
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

        // Decode tag name from tag ID
        private string DecodeTagName(string TagID)
        {
            try
            {
                if (string.IsNullOrEmpty(TagID))
                {
                    return string.Empty;
                }
                else if (TagID.Contains("1371"))
                {
                    return "TO_000100_TT101";
                }
                else if (TagID.Contains("1372"))
                {
                    return "TO_000100_TT102";
                }
                else if (TagID.Contains("1373"))
                {
                    return "XX_000000_FT000";
                }
                else if (TagID.Contains("1374"))
                {
                    return "XX_000000_QT102";
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                Logger.ToLogFile($"Error while decoding tag name {TagID} :: {ex.Message}");
                return string.Empty;
            }
        }        

        // Dispose method
        public void Dispose() { }
    }
}
