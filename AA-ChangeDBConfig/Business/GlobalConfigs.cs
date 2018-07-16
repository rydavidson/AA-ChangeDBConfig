using rydavidson.Accela.Configuration.Models;
using System.Collections.Generic;

namespace AA_ChangeDBConfig.Business
{
    public sealed class GlobalConfigs
    {
        private static GlobalConfigs instance = null;
        private static readonly object O = new object();


        public bool IsLogDebugEnabled { get; set; }
        public bool IsLogTraceEnabled { get; set; }
        public string AaVersion { get; set; }
        public string AaInstance { get; set; }
        public string AaInstallDir { get; set; }
        public string PathToConfigFile { get; set; }
        public List<string> InstalledComponent { get; set; }
        public MssqlConfig CurrentMssqlConfig { get; set; }

        GlobalConfigs()
        {
        }

        public static GlobalConfigs Instance
        {
            get
            {
                lock (O)
                {
                    if (instance == null)
                    {
                        instance = new GlobalConfigs();
                    }
                    return instance;
                }
            }
        }

    }
}
