using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AA_ChangeDBConfig.Business;

namespace AA_ChangeDBConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Logger logger = new Logger("UI.log");
        Dictionary<string, string> instancesWithVersions = new Dictionary<string, string>();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += RunOnLoad;
        }

        public void RunOnLoad(object sender, RoutedEventArgs e)
        {
            GlobalConfigs.Instance.IsLogDebugEnabled = true;
            GlobalConfigs.Instance.IsLogTraceEnabled = true;


            try
            {
                foreach (string version in CommonUtils.GetAAVersions())
                {
                    versionsComboBox.Items.Add(version);
                    try
                    {
                        foreach (string instance in CommonUtils.GetAAInstancesByVersion(version))
                        {
                            instancesWithVersions.Add(instance, version);
                        }
                    }
                    catch (Exception ey)
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Unable to detect installed AA instances for version: " + version);
                        logger.LogToUI(message.ToString());
                        message.AppendLine(ey.Message);
                        message.Append(ey.StackTrace);
                        logger.LogError(message.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine("Unable to detect installed AA versions.");
                logger.LogToUI(message.ToString());
                message.AppendLine(ex.Message);
                message.Append(ex.StackTrace);
                logger.LogError(message.ToString());
            }


        }

        private List<string> LookupInstancesForVersion(string version)
        {
            List<string> instances = new List<string>();

            foreach(KeyValuePair<string, string> instanceVersionPair in instancesWithVersions)
            {
                if(instanceVersionPair.Value == version)
                {
                    instances.Add(instanceVersionPair.Key);
                }
            }
            return instances;
        }

        private void PopulateInstanceComboBox(object sender, SelectionChangedEventArgs e)
        {
            string selectedVersion = versionsComboBox.SelectedValue.ToString();
            GlobalConfigs.Instance.CachedVersion = selectedVersion;
            if (selectedVersion != "")
            {
                foreach (string instance in LookupInstancesForVersion(selectedVersion))
                {
                    instancesComboBox.Items.Add(instance);
                }
            }
        }

        private void SelectedInstanceChanged(object sender, SelectionChangedEventArgs e)
        {
            GlobalConfigs.Instance.CachedInstance = instancesComboBox.SelectedValue.ToString();
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
