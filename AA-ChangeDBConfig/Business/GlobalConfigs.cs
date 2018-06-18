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
        public string CachedVersion { get; set; }
        public string CachedInstance { get; set; }
        public Dictionary<string, string> ConfigKeys { get; set; }

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
