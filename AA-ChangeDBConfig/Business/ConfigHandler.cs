using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security;
using System.Windows;

namespace AA_ChangeDBConfig.Business
{
    class ConfigHandler
    {
        // ConfigHandler exposes methods to deal with common functions needed for config files, such as retrieving and updating values

        private string PathToConfigFile { get; set; }
        private bool isDebug = false;

        public ConfigHandler(string _pathToConfigFile)
        {
            PathToConfigFile = _pathToConfigFile;
        }

        public Dictionary<string, string> GetConfig()
        {
            Dictionary<string, string> configPairs = new Dictionary<string, string>();



            return configPairs;

        }

        private string GetKeyValueFromConfig(string key)
        {
            try
            {
                string content = File.ReadAllText(PathToConfigFile, Encoding.Default);
                int indexOfKey = content.IndexOf(key);
                int indexEndOfValue = content.IndexOf(Environment.NewLine, indexOfKey);
                int indexOfEquals = content.IndexOf("=", indexOfKey);
                string value = content.Substring(indexOfEquals, (indexEndOfValue - indexOfEquals) - 1);
                return value;
            }
            catch (ArgumentNullException anex)
            {
                if(isDebug)
                MessageBox.Show(anex.Message + " | " + anex.StackTrace);
            }
            catch(DirectoryNotFoundException dex)
            {
                if (isDebug)
                    MessageBox.Show(dex.Message + " | " + dex.StackTrace);
            }
            catch (SecurityException sex) // the name fits, not my fault
            {
                if (isDebug)
                    MessageBox.Show(sex.Message + " | " + sex.StackTrace);
            }
            catch(UnauthorizedAccessException uaex)
            {
                if (isDebug)
                    MessageBox.Show(uaex.Message + " | " + uaex.StackTrace);
            }
            return "";
        }
    }
}
