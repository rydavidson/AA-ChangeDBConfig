using System;
using System.Collections.Generic;
using System.IO;
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
        TabItem currentTab = new TabItem();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += RunOnLoad;
        }

        public void RunOnLoad(object sender, RoutedEventArgs e)
        {
            GlobalConfigs.Instance.IsLogDebugEnabled = true;
            GlobalConfigs.Instance.IsLogTraceEnabled = true;
            //av_webTab.Visibility = Visibility.Collapsed;
            currentTab = mainTabMenu.SelectedItem as TabItem;
            try
            {
                foreach (string version in CommonUtils.GetAAVersions())
                {
                    versionsComboBox_biz.Items.Add(version);
                    versionsComboBox_cfmx.Items.Add(version);
                    versionsComboBox_web.Items.Add(version);
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

            foreach (KeyValuePair<string, string> instanceVersionPair in instancesWithVersions)
            {
                if (instanceVersionPair.Value == version)
                {
                    instances.Add(instanceVersionPair.Key);
                }
            }
            return instances;
        }

        private void PopulateInstanceComboBox(object sender, SelectionChangedEventArgs e)
        {
            string selectedVersion = "";

            if (e.OriginalSource == versionsComboBox_biz)
                selectedVersion = versionsComboBox_biz.SelectedValue.ToString();
            if (e.OriginalSource == versionsComboBox_cfmx)
                selectedVersion = versionsComboBox_cfmx.SelectedValue.ToString();
            if (e.OriginalSource == versionsComboBox_web)
                selectedVersion = versionsComboBox_web.SelectedValue.ToString();

            GlobalConfigs.Instance.AAVersion = selectedVersion;

            if (selectedVersion != "")
            {
                foreach (string instance in LookupInstancesForVersion(selectedVersion))
                {
                    if (!instancesComboBox_biz.Items.Contains(instance))
                        instancesComboBox_biz.Items.Add(instance);
                    if (!instancesComboBox_cfmx.Items.Contains(instance))
                        instancesComboBox_cfmx.Items.Add(instance);
                    if (!instancesComboBox_web.Items.Contains(instance))
                        instancesComboBox_web.Items.Add(instance);
                }
            }
        }

        private void LoadConfig(object sender, RoutedEventArgs e)
        {

            if (GlobalConfigs.Instance.AAVersion == null || GlobalConfigs.Instance.AAInstance == null)
            {
                MessageBox.Show("You must select a version and instance to work with");
                return;
            }
            string initialDir = CommonUtils.GetAAInstallDir();
            Dictionary<string, string> paths = CommonUtils.GetAAConfigFilePaths(GlobalConfigs.Instance.AAVersion, GlobalConfigs.Instance.AAInstance);
            mssql.component = currentTab.Header.ToString();
            //logger.LogToUI(mssql.component);
            initialDir = paths[mssql.component];

            if (File.Exists(initialDir) && e.OriginalSource != openConfigFileMenuItem)
            {

                loadedConfig = initialDir;
                ConfigHandler config = new ConfigHandler(loadedConfig, mssql.component);

                logger.LogToUI("Loading config direct from detected file: " + loadedConfig, "logBox_" + mssql.component.Remove(0, 3));
                if (config.GetValueFromConfig("av.db") == "mssql")
                {
                    mssql.serverHostname = config.GetValueFromConfig("av.db.host");

                    // av db
                    mssql.avDatabaseName = config.GetValueFromConfig("av.db.sid");
                    mssql.avDatabaseUser = config.GetValueFromConfig("av.db.username");
                    mssql.SetAVDatabasePassword(config.GetValueFromConfig("av.db.password"));

                    if (mssql.component == "av.biz")
                    {
                        // jetspeed db
                        mssql.jetspeedDatabaseName = config.GetValueFromConfig("av.jetspeed.db.sid");
                        mssql.jetspeedDatabaseUser = config.GetValueFromConfig("av.jetspeed.db.username");
                        mssql.SetJetspeedDatabasePassword(config.GetValueFromConfig("av.jetspeed.db.password"));
                    }
                    UpdateUI(mssql);
                }
                else
                {
                    MessageBox.Show("Unsupported connection type detected: " + config.GetValueFromConfig("av.db"));
                    logger.LogToUI("Unsupported database connection type", mssql.component);
                }
            }
            else
            {
                OpenFileDialog getPropFile = new OpenFileDialog();
                getPropFile.InitialDirectory = initialDir;
                getPropFile.Filter = "Properties files (*.properties)|*.properties";

                if (getPropFile.ShowDialog() == true)
                {
                    loadedConfig = getPropFile.FileName;
                    ConfigHandler config = new ConfigHandler(loadedConfig, mssql.component);

                    if (config.GetValueFromConfig("av.db") == "mssql")
                    {
                        mssql.serverHostname = config.GetValueFromConfig("av.db.host");

                        // av db
                        mssql.avDatabaseName = config.GetValueFromConfig("av.db.sid");
                        mssql.avDatabaseUser = config.GetValueFromConfig("av.db.username");
                        mssql.SetAVDatabasePassword(config.GetValueFromConfig("av.db.password"));

                        if (mssql.component == "av.biz")
                        {
                            // jetspeed db
                            mssql.jetspeedDatabaseName = config.GetValueFromConfig("av.jetspeed.db.sid");
                            mssql.jetspeedDatabaseUser = config.GetValueFromConfig("av.jetspeed.db.username");
                            mssql.SetJetspeedDatabasePassword(config.GetValueFromConfig("av.jetspeed.db.password"));
                        }
                        UpdateUI(mssql);
                    }
                    else
                    {
                        MessageBox.Show("Unsupported connection type detected: " + config.GetValueFromConfig("av.db"));
                        logger.LogToUI("Unsupported database connection type", mssql.component);
                    }

                }
            }

        }
        private void WriteConfigToFile(object sender, RoutedEventArgs e)
        {
            if (loadedConfig == "" || loadedConfig == null)
            {
                if (GlobalConfigs.Instance.PathToConfigFile == "" || GlobalConfigs.Instance.PathToConfigFile == null)
                    return;
                loadedConfig = GlobalConfigs.Instance.PathToConfigFile;
            }
            ConfigHandler config = new ConfigHandler(loadedConfig, mssql.component);

            mssql.serverHostname = dbServerTextBox_biz.Text;

            // av db
            mssql.avDatabaseName = avDBTextBox_biz.Text;
            mssql.avDatabaseUser = avDBUserTextBox_biz.Text;
            mssql.SetAVDatabasePassword(avDBPasswordTextBox_biz.Password);

            // jetspeed db
            mssql.jetspeedDatabaseName = jetspeedDBTextBox_biz.Text;
            mssql.jetspeedDatabaseUser = jetspeedUserTextBox_biz.Text;
            mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox_biz.SecurePassword);

            config.WriteMSSQLConfigToFile(mssql);
            UpdateUI(mssql);

        }

        public void UpdateUI(MSSQLConfig _mssql)
        {
            switch (_mssql.component)
            {
                case "av.biz":
                    dbServerTextBox_biz.Text = _mssql.serverHostname;
                    avDBTextBox_biz.Text = _mssql.avDatabaseName;
                    avDBUserTextBox_biz.Text = _mssql.avDatabaseUser;
                    avDBPasswordTextBox_biz.Password = _mssql.GetAVDatabasePassword();
                    jetspeedDBTextBox_biz.Text = _mssql.jetspeedDatabaseName;
                    jetspeedUserTextBox_biz.Text = _mssql.jetspeedDatabaseUser;
                    jetspeedPasswordTextBox_biz.Password = _mssql.GetJetspeedDatabasePassword();
                    commitButton_biz.IsEnabled = true;
                    break;
                case "av.cfmx":
                    dbServer_cfmx.Text = _mssql.serverHostname;
                    avDBTextBox_cfmx.Text = _mssql.avDatabaseName;
                    avDBUserTextBox_cfmx.Text = _mssql.avDatabaseUser;
                    avDBPasswordTextBox_cfmx.Password = _mssql.GetAVDatabasePassword();
                    commitButton_cfmx.IsEnabled = true;
                    break;
                case "av.web":
                    dbServerTextBox_web.Text = _mssql.serverHostname;
                    avDBTextBox_web.Text = _mssql.avDatabaseName;
                    avDBUserTextBox_web.Text = _mssql.avDatabaseUser;
                    avDBPasswordTextBox_web.Password = _mssql.GetAVDatabasePassword();
                    commitButton_web.IsEnabled = true;
                    break;
                default:
                    logger.LogWarn("Couldn't enable a commit button. There may be an issue. Selected component is: " + mssql.component);
                    break;

            }
        }

        private void SelectedInstanceChanged(object sender, SelectionChangedEventArgs e)
        {

            if (e.OriginalSource == instancesComboBox_biz)
            {
                GlobalConfigs.Instance.AAInstance = instancesComboBox_biz.SelectedValue.ToString();
                loadConfigButton_biz.IsEnabled = true;
            }
            if (e.OriginalSource == instancesComboBox_cfmx)
            {
                GlobalConfigs.Instance.AAInstance = instancesComboBox_cfmx.SelectedValue.ToString();
                loadConfigButton_cfmx.IsEnabled = true;
            }
            if (e.OriginalSource == instancesComboBox_web)
            {
                GlobalConfigs.Instance.AAInstance = instancesComboBox_web.SelectedValue.ToString();
                loadConfigButton_web.IsEnabled = true;
            }

        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void mainTabMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentTab = mainTabMenu.SelectedItem as TabItem;
            //logger.LogDebug("Tab changed. Current tab is: " + currentTab.Header.ToString());
        }
    }
}
