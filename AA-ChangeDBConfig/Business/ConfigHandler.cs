using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security;
using System.Windows;
using AA_ChangeDBConfig.Models;

namespace AA_ChangeDBConfig.Business
{
    class ConfigHandler
    {
        // ConfigHandler exposes methods to deal with common functions needed for config files, such as retrieving and updating values

        private string PathToConfigFile { get; set; }
        private Logger logger = new Logger("ConfigHandler.log");
        private string component;

        public ConfigHandler() { }

        public ConfigHandler(string _pathToConfigFile, string _component)
        {
            //PathToConfigFile = _pathToConfigFile.Replace("\"","");
            PathToConfigFile = _pathToConfigFile;
            GlobalConfigs.Instance.PathToConfigFile = _pathToConfigFile;
            component = _component;
        }

        private Dictionary<string, string> GetConfig()
        {
            Dictionary<string, string> configPairs = new Dictionary<string, string>();
            Dictionary<string, string> configFiles = CommonUtils.GetAAConfigFilePaths(GlobalConfigs.Instance.AAVersion, GlobalConfigs.Instance.AAInstance);

            foreach (KeyValuePair<string, string> configFile in configFiles)
            {
                if (configFile.Key == component)
                {
                    PathToConfigFile = configFile.Value;

                }
            }

            return configPairs;
        }

        public void WriteMSSQLConfigToFile(MSSQLConfig mssql)
        {

            logger.LogToUI("Backing up " + PathToConfigFile);

            if (File.Exists(PathToConfigFile + ".backup"))
                File.Delete(PathToConfigFile + ".backup");

            File.Copy(PathToConfigFile, PathToConfigFile + ".backup");

            WriteValueToConfig("av.db.host", mssql.serverHostname);
            WriteValueToConfig("av.jetspeed.db.host", mssql.serverHostname);

            WriteValueToConfig("av.db.sid", mssql.avDatabaseName);
            WriteValueToConfig("av.db.username", mssql.avDatabaseUser);
            WriteValueToConfig("av.db.password", mssql.GetAVDatabasePassword());

            WriteValueToConfig("av.jetspeed.db.sid", mssql.jetspeedDatabaseName);
            WriteValueToConfig("av.jetspeed.db.username", mssql.jetspeedDatabaseUser);
            WriteValueToConfig("av.jetspeed.db.password", mssql.GetJetspeedDatabasePassword());

            logger.LogToUI("Updated config successfully");

        }

        public string GetValueFromConfig(string key) // get a config value from a given key
        {
            try
            {
                string content = File.ReadAllText(PathToConfigFile, Encoding.Default);
                int indexOfKey = content.IndexOf(key);
                int indexEndOfValue = content.IndexOf(Environment.NewLine, indexOfKey);
                int indexOfEquals = content.IndexOf("=", indexOfKey);
                string value = content.Substring(indexOfEquals + 1, (indexEndOfValue - indexOfEquals) - 1);

                switch (key)
                {
                    case "av.db.host":
                        value += ":" + new ConfigHandler(PathToConfigFile, component).GetValueFromConfig("av.db.port");
                        break;
                    case "av.jetspeed.db.host":
                        value += ":" + new ConfigHandler(PathToConfigFile, component).GetValueFromConfig("av.jetspeed.db.port");
                        break;
                    default:
                        break;
                }

                return value;
            }
            catch (ArgumentNullException anex)
            {
                logger.LogError(string.Format("ArgumentNullException getting config of {0}, error: {1}{2}", key, anex.Message, anex.StackTrace));
            }
            catch (DirectoryNotFoundException dex)
            {
                logger.LogError(string.Format("Directory not found, error: {0}{1}", dex.Message, dex.StackTrace));
            }
            catch (SecurityException sex)
            {
                logger.LogError(string.Format("SecurityException getting config of {0}, error: {1}{2}", key, sex.Message, sex.StackTrace));
            }
            catch (UnauthorizedAccessException uaex)
            {
                logger.LogError(string.Format("Failed to get config of {0}, error: {1}{2}", key, uaex.Message, uaex.StackTrace));
            }
            catch (Exception e)
            {
                logger.LogError(string.Format("Failed to get config of {0}, error: {1}{2}", key, e.Message, e.StackTrace));
            }
            return "";
        }

        public void WriteValueToConfig(string key, string newValue)
        {
            if ((key == "av.db.host" || key == "av.jetspeed.db.host") && newValue.Contains(":"))
            {
                logger.LogTrace("Entering port strip logic");
                string port = newValue.Substring(newValue.IndexOf(":") + 1, newValue.Length - newValue.IndexOf(":") - 1);
                logger.LogTrace("New port: " + port);
                switch (key)
                {
                    case "av.db.host":
                        new ConfigHandler(PathToConfigFile, component).WriteValueToConfig("av.db.port", port);
                        break;
                    case "av.jetspeed.db.host":
                        new ConfigHandler(PathToConfigFile, component).WriteValueToConfig("av.jetspeed.db.port", port);
                        break;
                    default:
                        break;
                }
                newValue = newValue.Remove(newValue.IndexOf(":"));
            }

            string content = File.ReadAllText(PathToConfigFile, Encoding.Default); // read in the config file
            int indexOfKey = content.IndexOf(key); // get the start of the config item
            if (indexOfKey < 0)
                return; // exit if the config item isn't found

                int indexEndOfValue = content.IndexOf(Environment.NewLine, indexOfKey); // get the end of the config item
                int indexOfEquals = content.IndexOf("=", indexOfKey); // get the index of the equals sign after the config item
                string oldValue = content.Substring(indexOfEquals + 1, (indexEndOfValue - indexOfEquals)).Trim(); // get the old value
                if (!oldValue.Contains("password"))
                    logger.LogTrace("Old value: " + oldValue);

                string oldConfigLine = content.Substring(indexOfKey, (indexEndOfValue - indexOfKey)); // get the entire line
                string newConfigLine = oldConfigLine.Replace(oldValue, newValue); // replace the old value in the line with the new value
                if (!newConfigLine.Contains("password"))
                    logger.LogToUI("New config line: " + newConfigLine);

                string newFile = content.Replace(oldConfigLine, newConfigLine);

                //logger.LogToUI("Writing new config");

                File.WriteAllText(PathToConfigFile, newFile, Encoding.Default);
                if (!key.Contains("password"))
                    logger.LogTrace("Updated " + key + " to " + newConfigLine);

        }
    }
}
