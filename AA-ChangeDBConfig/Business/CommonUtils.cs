using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AA_ChangeDBConfig.Business
{
    static class CommonUtils
    {
        public static Logger logger = new Logger("CommonUtils.log");
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

            }
            catch (Exception e)
            {

            }

            return instances;
        }

        private static RegistryKey GetAccelaBaseKey()
        {
            RegistryKey hklmReg;
            RegistryKey accelaBaseKey;
            try
            {
                hklmReg = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
                Debug.WriteLine(hklmReg.ToString());
                try
                {
                    accelaBaseKey = hklmReg.OpenSubKey(@"\Software\Accela Inc.");
                    Debug.WriteLine(accelaBaseKey.GetSubKeyNames().ToString());
                    return accelaBaseKey;
                }
                catch(Exception e)
                {
                    MessageBox.Show("Failed to open Accela key. Error: " + e.Message + " " + e.StackTrace);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show("Failed to open HKLM. Error: " + e.Message + " " + e.StackTrace);
            }

            return null;
        }

        public static List<string> GetAAVersions()
        {
            if(GetAccelaBaseKey() == null)
            {
                StringBuilder err = new StringBuilder();
                err.AppendLine("Couldn't open Accela Base Key");
                logger.LogError(err.ToString());
                return null;
            }
            return new List<string>(GetAccelaBaseKey().OpenSubKey(@"\AA Base Installer\").GetSubKeyNames());
        }

        public static List<string> GetAAInstancesByVersion(string version)
        {
            string[] instancekeys = GetAccelaBaseKey().OpenSubKey(@"\AA Base Installer\" + version).GetSubKeyNames();
            return new List<string>(instancekeys);
        }
    }
}
