using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AA_ChangeDBConfig.Business;
using Microsoft.Win32;
using rydavidson.Accela.Configuration.Common;
using rydavidson.Accela.Configuration.Handlers;
using rydavidson.Accela.Configuration.IO;
using rydavidson.Accela.Configuration.Models;

namespace AA_ChangeDBConfig.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private AaLogger logger;
        private AaUtil aautil;
        private readonly Dictionary<string, string> instancesWithVersions = new Dictionary<string, string>();
        private string loadedConfig = "";
        private MssqlConfig mssql = new MssqlConfig();
        private TabItem currentTab = new TabItem();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += RunOnLoad;
        }

        private void RunOnLoad(object _sender, RoutedEventArgs _e)
        {
            logger = new AaLogger("UI.log");
            aautil = new AaUtil(logger);
            GlobalConfigs.Instance.IsLogDebugEnabled = true;
            GlobalConfigs.Instance.IsLogTraceEnabled = true;
            //av_webTab.Visibility = Visibility.Collapsed;
            currentTab = mainTabMenu.SelectedContent as TabItem;
            try
            {
                foreach (string version in aautil.GetAaVersions())
                {
                    versionsComboBox_biz.Items.Add(version);
                    versionsComboBox_cfmx.Items.Add(version);
                    versionsComboBox_web.Items.Add(version);
                    try
                    {
                        foreach (string instance in aautil.GetInstancesForVersion(version))
                        {
                            instancesWithVersions.Add(instance, version);
                        }
                    }
                    catch (Exception ey)
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Unable to detect installed AA instances for version: " + version);
                        logger.LogToUi(message.ToString());
                        message.AppendLine(ey.Message);
                        message.Append(ey.StackTrace);
                        logger.Error(message.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine("Unable to detect installed AA versions.");
                logger.LogToUi(message.ToString());
                message.AppendLine(ex.Message);
                message.Append(ex.StackTrace);
                logger.Error(message.ToString());
            }
        }

        private IEnumerable<string> LookupInstancesForVersion(string _version)
        {
            List<string> instances = new List<string>();

            foreach (KeyValuePair<string, string> instanceVersionPair in instancesWithVersions)
            {
                if (instanceVersionPair.Value == _version)
                {
                    instances.Add(instanceVersionPair.Key);
                }
            }
            return instances;
        }

        private void PopulateInstanceComboBox(object _sender, SelectionChangedEventArgs _e)
        {
            string selectedVersion = "";

            if (_e.OriginalSource == versionsComboBox_biz)
                selectedVersion = versionsComboBox_biz.SelectedValue.ToString();
            if (_e.OriginalSource == versionsComboBox_cfmx)
                selectedVersion = versionsComboBox_cfmx.SelectedValue.ToString();
            if (_e.OriginalSource == versionsComboBox_web)
                selectedVersion = versionsComboBox_web.SelectedValue.ToString();

            GlobalConfigs.Instance.AaVersion = selectedVersion;

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

        private void LoadConfig(object _sender, RoutedEventArgs _e)
        {
            currentTab = mainTabMenu.SelectedContent as TabItem; // Reload the currently selected tab //TODO Get the selected tab to reload when the tab is changed
            if (currentTab == null)
            {
                logger.Debug("Null header on currentTab");
                return;
            }
            else
            {
                logger.Debug("Loaded tab: " + currentTab.Header.ToString());
            }

            if (GlobalConfigs.Instance.AaVersion == null || GlobalConfigs.Instance.AaInstance == null)
            {
                MessageBox.Show("You must select a version and instance to work with");
                return;
            }
            string installDir = aautil.GetAaInstallDir(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance);
            Dictionary<string, string> paths = aautil.GetAaConfigFilePaths(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance);
            //logger.LogToUI(mssql.component);
            string configDir = paths[currentTab.Header.ToString()];

            if (File.Exists(configDir) && !Equals(_e.OriginalSource,openConfigFileMenuItem))
            {

                loadedConfig = configDir;
                mssql = new ConfigReader(loadedConfig).ReadFromConfigFile();
                mssql.AvComponent = currentTab.Header.ToString();
                // ConfigHandler config = new ConfigHandler(loadedConfig, mssql.avComponent, GlobalConfigs.Instance.AAVersion, GlobalConfigs.Instance.AAInstance, logger);

                logger.LogToUi("Loading config direct from detected file: " + loadedConfig, "logBox_" + mssql.AvComponent.Remove(0, 3));
                UpdateUi(mssql);
            }
            else
            {
                OpenFileDialog getPropFile = new OpenFileDialog();
                getPropFile.InitialDirectory = configDir;
                getPropFile.Filter = "Properties files (*.properties)|*.properties";

                if (getPropFile.ShowDialog() == true)
                {
                    loadedConfig = getPropFile.FileName;
                    mssql = new ConfigReader(loadedConfig).ReadFromConfigFile();
                    mssql.AvComponent = currentTab.Header.ToString();
                    UpdateUi(mssql);
                }

            }
        }

        private void WriteConfigToFile(object _sender, RoutedEventArgs _e)
        {
            if (string.IsNullOrEmpty(loadedConfig))
            {
                if (string.IsNullOrEmpty(GlobalConfigs.Instance.PathToConfigFile))
                {
                    logger.Error("Null path");
                    return;
                }
                    
                logger.Warn("Loaded config path is empty, falling back to cached path");
                loadedConfig = GlobalConfigs.Instance.PathToConfigFile;
            }
            ConfigHandler config = new ConfigHandler(loadedConfig, mssql.AvComponent, GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance, logger);

            mssql.AvDbHost = dbServerTextBox_biz.Text;

            // av db
            mssql.AvDbName = avDBTextBox_biz.Text;
            mssql.AvUser = avDBUserTextBox_biz.Text;
            mssql.SetAvDatabasePassword(avDBPasswordTextBox_biz.Password);

            // jetspeed db
            mssql.AvJetspeedDbName = jetspeedDBTextBox_biz.Text;
            mssql.AvJetspeedUser = jetspeedUserTextBox_biz.Text;
            mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox_biz.SecurePassword);

            config.WriteConfigToFile(mssql);
            UpdateUi(mssql);
        }

        private void UpdateUi(MssqlConfig _mssql)
        {
            switch (_mssql.AvComponent)
            {
                case "av.biz":
                    dbServerTextBox_biz.Text = _mssql.AvDbHost;
                    avDBTextBox_biz.Text = _mssql.AvDbName;
                    avDBUserTextBox_biz.Text = _mssql.AvUser;
                    avDBPasswordTextBox_biz.Password = _mssql.GetAvDatabasePassword();
                    jetspeedDBTextBox_biz.Text = _mssql.AvJetspeedDbName;
                    jetspeedUserTextBox_biz.Text = _mssql.AvJetspeedUser;
                    jetspeedPasswordTextBox_biz.Password = _mssql.GetJetspeedDatabasePassword();
                    commitButton_biz.IsEnabled = true;
                    break;
                case "av.cfmx":
                    dbServer_cfmx.Text = _mssql.AvDbHost;
                    avDBTextBox_cfmx.Text = _mssql.AvDbName;
                    avDBUserTextBox_cfmx.Text = _mssql.AvUser;
                    avDBPasswordTextBox_cfmx.Password = _mssql.GetAvDatabasePassword();
                    commitButton_cfmx.IsEnabled = true;
                    break;
                case "av.web":
                    dbServerTextBox_web.Text = _mssql.AvDbHost;
                    avDBTextBox_web.Text = _mssql.AvDbName;
                    avDBUserTextBox_web.Text = _mssql.AvUser;
                    avDBPasswordTextBox_web.Password = _mssql.GetAvDatabasePassword();
                    commitButton_web.IsEnabled = true;
                    break;
                default:
                    logger.Warn("Couldn't enable a commit button. There may be an issue. Selected component is: " + mssql.AvComponent);
                    break;

            }
        }

        [SuppressMessage("ReSharper", "InvertIf")]
        private void SelectedInstanceChanged(object _sender, SelectionChangedEventArgs _e)
        {

            if (Equals(_e.OriginalSource, instancesComboBox_biz))
            {
                GlobalConfigs.Instance.AaInstance = instancesComboBox_biz.SelectedValue.ToString();
                loadConfigButton_biz.IsEnabled = true;
            }
            if (Equals(_e.OriginalSource, instancesComboBox_cfmx))
            {
                GlobalConfigs.Instance.AaInstance = instancesComboBox_cfmx.SelectedValue.ToString();
                loadConfigButton_cfmx.IsEnabled = true;
            }
            if (Equals(_e.OriginalSource, instancesComboBox_web))
            {
                GlobalConfigs.Instance.AaInstance = instancesComboBox_web.SelectedValue.ToString();
                loadConfigButton_web.IsEnabled = true;
            }

        }

        private void Exit(object _sender, RoutedEventArgs _e)
        {
            Environment.Exit(0);
        }

        private void mainTabMenu_SelectionChanged(object _sender, SelectionChangedEventArgs _e)
        {
            currentTab = mainTabMenu.SelectedItem as TabItem;
            //logger.LogDebug("Tab changed. Current tab is: " + currentTab.Header.ToString());
        }

        private void avPassShow_Checked(object _sender, RoutedEventArgs _e)
        {
            
        }
    }
}
