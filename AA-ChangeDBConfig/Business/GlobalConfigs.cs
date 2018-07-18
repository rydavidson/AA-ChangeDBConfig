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

        private GlobalConfigs()
        {
        }

        public static GlobalConfigs Instance
        {
            get
            {
                lock (O)
                {
                    return instance ?? (instance = new GlobalConfigs());
                }
            }
        }

    }
}
