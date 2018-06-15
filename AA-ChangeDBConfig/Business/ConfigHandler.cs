﻿using System;
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
        private Logger logger = new Logger("ConfigHandler.log");

        public ConfigHandler(string _pathToConfigFile)
        {
            PathToConfigFile = _pathToConfigFile;
        }

        public Dictionary<string, string> GetConfig(string component)
        {
            Dictionary<string, string> configPairs = new Dictionary<string, string>();
            Dictionary<string, string> configFiles = CommonUtils.GetAAConfigFilePaths();

            foreach(KeyValuePair<string, string> configFile in configFiles)
            {
                if(configFile.Key == component && configFile.Value == PathToConfigFile)
                {
                    
                }
            }

            return configPairs;
        }

        private string GetValueFromConfig(string key) // get a config value from a given key
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
                logger.LogError(string.Format("ArgumentNullException getting config of {0}, error: {1} {2}", key, anex.Message, anex.StackTrace));
            }
            catch (DirectoryNotFoundException dex)
            {
                logger.LogError(string.Format("Directory not found, error: {0} {1}", dex.Message, dex.StackTrace));
            }
            catch (SecurityException sex) // the name fits, not my fault
            {
                logger.LogError(string.Format("SecurityException getting config of {0}, error: {1} {2}", key, sex.Message, sex.StackTrace));
            }
            catch (UnauthorizedAccessException uaex)
            {
                logger.LogError(string.Format("Failed to get config of {0}, error: {1} {2}", key, uaex.Message, uaex.StackTrace));
            }
            catch(Exception e)
            {
                logger.LogError(string.Format("Failed to get config of {0}, error: {1} {2}", key, e.Message, e.StackTrace));
            }
            return "";
        }
    }
}
