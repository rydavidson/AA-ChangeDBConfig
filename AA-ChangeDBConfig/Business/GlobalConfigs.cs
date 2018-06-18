using AA_ChangeDBConfig.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AA_ChangeDBConfig.Business
{
    public sealed class GlobalConfigs
    {
        private static GlobalConfigs instance = null;
        private static readonly object o = new object();


        public bool IsLogDebugEnabled { get; set; }
        public bool IsLogTraceEnabled { get; set; }
        public string AAVersion { get; set; }
        public string AAInstance { get; set; }
        public string AAInstallDir { get; set; }
        public string PathToConfigFile { get; set; }
        public List<string> InstalledComponent { get; set; }
        public MSSQLConfig CurrentMSSQLConfig { get; set; }

        GlobalConfigs()
        {
        }

        public static GlobalConfigs Instance
        {
            get
            {
                lock (o)
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
