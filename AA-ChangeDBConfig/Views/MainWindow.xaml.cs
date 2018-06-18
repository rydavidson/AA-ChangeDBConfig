using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using AA_ChangeDBConfig.Business;
using AA_ChangeDBConfig.Models;
using Microsoft.Win32;

namespace AA_ChangeDBConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Logger logger = new Logger("UI.log");
        Dictionary<string, string> instancesWithVersions = new Dictionary<string, string>();
        string loadedConfig = "";
        MSSQLConfig mssql = new MSSQLConfig();


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

        private void LoadConfigFromFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog getPropFile = new OpenFileDialog();
            getPropFile.InitialDirectory = CommonUtils.GetAAInstallDir();
            getPropFile.Filter = "Properties files (*.properties)|*.properties";

            if(getPropFile.ShowDialog() == true)
            {
                loadedConfig = getPropFile.FileName;
                ConfigHandler config = new ConfigHandler(loadedConfig);

                if(config.GetValueFromConfig("av.db") == "mssql")
                {
                    mssql.serverHostname = config.GetValueFromConfig("av.db.host");

                    // av db
                    mssql.avDatabaseName = config.GetValueFromConfig("av.db.sid");
                    mssql.avDatabaseUser = config.GetValueFromConfig("av.db.username");
                    mssql.SetAVDatabasePassword(config.GetValueFromConfig("av.db.password"));

                    // jetspeed db
                    mssql.jetspeedDatabaseName = config.GetValueFromConfig("av.jetspeed.db.sid");
                    mssql.jetspeedDatabaseUser = config.GetValueFromConfig("av.jetspeed.db.username");
                    mssql.SetJetspeedDatabasePassword(config.GetValueFromConfig("av.jetspeed.db.password"));

                    UpdateUI(mssql);
                }
                else
                {
                    MessageBox.Show("Unsupported connection type detected: " + config.GetValueFromConfig("av.db"));
                    logger.LogToUI("Unsupported database connection type");
                }

            }
        }
        private void WriteConfigToFile(object sender, RoutedEventArgs e)
        {
            ConfigHandler config = new ConfigHandler(loadedConfig);

            mssql.serverHostname = dbServerTextBox.Text;

            // av db
            mssql.avDatabaseName = avDBTextBox.Text;
            mssql.avDatabaseUser = avDBUserTextBox.Text;
            mssql.SetAVDatabasePassword(avDBPasswordTextBox.Password);

            // jetspeed db
            mssql.jetspeedDatabaseName = jetspeedDBTextBox.Text;
            mssql.jetspeedDatabaseUser = jetspeedUserTextBox.Text;
            mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox.SecurePassword);

            config.WriteMSSQLConfigToFile(mssql);
            UpdateUI(mssql);

        }

        public void UpdateUI(MSSQLConfig _mssql)
        {
            dbServerTextBox.Text = _mssql.serverHostname;

            // av db
            avDBTextBox.Text = _mssql.avDatabaseName;
            avDBUserTextBox.Text = _mssql.avDatabaseUser;
            avDBPasswordTextBox.Password = _mssql.GetAVDatabasePassword();

            // jetspeed db
            jetspeedDBTextBox.Text = _mssql.jetspeedDatabaseName;
            jetspeedUserTextBox.Text = _mssql.jetspeedDatabaseUser;
            jetspeedPasswordTextBox.Password = _mssql.GetJetspeedDatabasePassword();
        }
    }
}
