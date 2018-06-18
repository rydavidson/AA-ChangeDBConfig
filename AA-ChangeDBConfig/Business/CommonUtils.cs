using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AA_ChangeDBConfig.Business
{
    static class CommonUtils
    {
        public static Logger logger = new Logger("CommonUtils.log");
        static RegistryKey accelaBaseKey;
        static RegistryKey instanceKey;

        public static List<string> GetInstancesForVersion(string versionToCheck)
        {
            List<string> instances = new List<string>();
            try
            {
                RegistryKey accelaBaseKey = GetAccelaBaseKey();
                List<string> versions = GetAAVersions();
                foreach (string version in versions)
                {
                    if (version.Trim() == versionToCheck.Trim())
                    {
                        foreach (string instance in GetAAInstancesByVersion(version))
                        {
                            instances.Add(instance);
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException uaex)
            {
                logger.LogError("UnauthorizedAccessException getting instances for version " + versionToCheck + ", error: " + uaex.Message + uaex.StackTrace);
            }
            catch (Exception e)
            {
                logger.LogError("Error getting instances for version " + versionToCheck + ", error: " + e.Message + e.StackTrace);
            }

            return instances;
        }

        private static RegistryKey GetAccelaBaseKey()
        {
            RegistryKey hklmReg;

            if (accelaBaseKey != null)
            {
                return accelaBaseKey;
            }
            try
            {
                hklmReg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
                try
                {
                    accelaBaseKey = hklmReg.OpenSubKey(@"SOFTWARE\WOW6432Node\Accela Inc.", RegistryKeyPermissionCheck.ReadSubTree);
                    return accelaBaseKey;
                }
                catch (Exception e)
                {
                    logger.LogError("Failed to open Accela key. Error: " + e.Message + " " + e.StackTrace);
                }
            }
            catch (Exception e)
            {
                logger.LogError("Failed to open HKLM. Error: " + e.Message + " " + e.StackTrace);
            }
            return accelaBaseKey;
        }

        private static RegistryKey GetInstanceKeyCached() // use this unless you really need to override the cached values
        {
            
            if(instanceKey != null)
                return instanceKey;

            return GetInstanceKey(GlobalConfigs.Instance.CachedVersion, GlobalConfigs.Instance.CachedInstance);
        }

        private static RegistryKey GetInstanceKey(string _version, string _instance)
        {
            RegistryKey reg = GetAccelaBaseKey();

            try
            {
                instanceKey = reg.OpenSubKey(string.Format(@"AA Base Installer\{0}\{1}", _version, _instance), RegistryKeyPermissionCheck.ReadSubTree);
            }
            catch (Exception ex)
            {
                logger.LogError("Error while reading install directory: " + ex.Message + ex.StackTrace);
            }

            return instanceKey;
        }

        public static List<string> GetAAVersions()
        {
            //if(GetAccelaBaseKey() == null)
            //{
            //    StringBuilder err = new StringBuilder();
            //    err.AppendLine("Couldn't open Accela Base Key");
            //    logger.LogError(err.ToString());
            //    return null;
            //}
            return new List<string>(GetAccelaBaseKey().OpenSubKey(@"AA Base Installer").GetSubKeyNames());
        }

        public static List<string> GetAAInstancesByVersion(string _version)
        {
            string[] instancekeys = GetAccelaBaseKey().OpenSubKey(@"AA Base Installer\" + _version).GetSubKeyNames();
            return new List<string>(instancekeys);
        }

        public static string GetAAInstallDir()
        {
            if(GlobalConfigs.Instance.CachedAAInstallDir != null)
            {
                return GlobalConfigs.Instance.CachedAAInstallDir;
            }
            string version = GlobalConfigs.Instance.CachedVersion;
            string instance = GlobalConfigs.Instance.CachedInstance;
            RegistryKey reg = GetAccelaBaseKey();
            string installDir = "";
            if (reg != null && version != null && instance != null)
            {
                try
                {
                    instanceKey = reg.OpenSubKey(string.Format(@"AA Base Installer\{0}\{1}", version, instance), RegistryKeyPermissionCheck.ReadSubTree);
                    installDir = instanceKey.GetValue("InstallDir").ToString();
                }
                catch (Exception ex)
                {
                    logger.LogError("Error while reading install directory: " + ex.Message + ex.StackTrace);
                }
            }
            if(installDir != "")
                logger.LogToUI("InstallDir: " + installDir);
            return installDir;
        }
        public static List<string> GetAAInstalledComponents()
        {
            RegistryKey reg = GetAccelaBaseKey();
            string components = GetInstanceKeyCached().GetValue("InstallComponents").ToString();
            logger.LogToUI("Installed Components: " + components.Split(',').ToString());
            return new List<string>(components.Split(','));
        }
        public static Dictionary<string,string> GetAAConfigFilePaths()
        {
            Dictionary<string, string> paths = new Dictionary<string, string>();
            List<string> components = GetAAInstalledComponents();
            string installDir = GetAAInstallDir();
            StringBuilder sb = new StringBuilder();

            foreach (string comp in components)
            {
                string stemp = string.Format(@"{0}\{1}\conf\av\ServerConfig.properties", installDir, comp);
                if (File.Exists(stemp))
                paths.Add(comp,stemp);
            }
            return paths;
        }
    }
}
