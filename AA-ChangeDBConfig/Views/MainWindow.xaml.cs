using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AA_ChangeDBConfig.Business;
using Microsoft.Win32;
using rydavidson.Accela.Common;
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
        private Dictionary<string, MssqlConfig> loadedConfigs = new Dictionary<string, MssqlConfig>();
        DebugConsole debug;

        private TabItem currentTab = new TabItem();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += RunOnLoad;
            debug = new DebugConsole(this);
        }

        private void RunOnLoad(object _sender, RoutedEventArgs _e)
        {
            // Debug.WriteLine("Running from " + Environment.CurrentDirectory);

            logger = new AaLogger("UI.log");
            aautil = new AaUtil(logger);
            GlobalConfigs.Instance.IsLogDebugEnabled = true;
            GlobalConfigs.Instance.IsLogTraceEnabled = true;

            //av_webTab.Visibility = Visibility.Collapsed;
            currentTab = av_bizTab;
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

        public void RunDebugCommand(string _command, Action<string> resultAction)
        {
            StringBuilder sb = new StringBuilder();
            switch (_command)
            {
                case "show av config":
                    if (loadedConfigs.Count == 0)
                    {
                        sb.AppendLine("No loaded configs to show");
                        break;
                    }

                    foreach (KeyValuePair<string, MssqlConfig> configitem in loadedConfigs)
                    {
                        sb.AppendLine(configitem.Key + " :\n " + configitem.Value.ToString());
                    }

                    break;
                case "show sys config":

                    sb.AppendLine("Version:");
                    foreach (string version in aautil.GetAaVersions())
                    {
                        sb.AppendLine("\t" + version);

                        sb.AppendLine("Instance:");
                        foreach (string instance in aautil.GetInstancesForVersion(version))
                        {
                            sb.AppendLine("\t" + instance);

                            sb.AppendLine("Component:");
                            foreach (string comp in aautil.GetAaInstalledComponents(version, instance))
                            {
                                sb.AppendLine("\t" + comp);

                                sb.AppendLine("\t\t Config file for component:");
                                foreach (KeyValuePair<string, string> file in aautil.GetAaConfigFilePaths(version, instance))
                                {
                                    if (file.Key == comp)
                                        sb.AppendLine("\t\t" + file.Value);
                                }
                            }
                        }
                    }

                    break;
                default:
                    sb.AppendLine("Invalid command");
                    break;
            }

            resultAction(sb.ToString());
            sb.Clear();
        }

        private void RunDebugConsole(object _sender, RoutedEventArgs _e)
        {
            debug.Show();
        }

        private void LoadConfig(object _sender, RoutedEventArgs _e)
        {
            string selectedComponent = currentTab.Header.ToString();

            if (currentTab == null)
            {
                logger.Debug("Null header on currentTab");
                return;
            }
            else
            {
                logger.Debug("Loaded tab: " + currentTab.Header.ToString());
            }

            if (loadedConfigs.ContainsKey(selectedComponent))
            {
                loadedConfigs.Remove(selectedComponent);
            }


            if (GlobalConfigs.Instance.AaVersion == null || GlobalConfigs.Instance.AaInstance == null)
            {
                MessageBox.Show("You must select a version and instance to work with");
                return;
            }

            string installDir = aautil.GetAaInstallDir(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance);
            Dictionary<string, string> paths = aautil.GetAaConfigFilePaths(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance);
            //logger.LogToUI(mssql.component);
            string configDir = paths[selectedComponent];

            if (File.Exists(configDir) && !Equals(_e.OriginalSource, openConfigFileMenuItem))
            {
                loadedConfig = configDir;
                MssqlConfig mssql = new ConfigReader(loadedConfig).ReadFromConfigFile();
                mssql.AvComponent = currentTab.Header.ToString();
                // ConfigHandler config = new ConfigHandler(loadedConfig, mssql.avComponent, GlobalConfigs.Instance.AAVersion, GlobalConfigs.Instance.AAInstance, logger);

                logger.LogToUi("Loading config direct from detected file: " + loadedConfig, "logBox_" + mssql.AvComponent.Remove(0, 3));

                loadedConfigs.Add(currentTab.Header.ToString(), mssql);

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
                    MssqlConfig mssql = new ConfigReader(loadedConfig).ReadFromConfigFile();
                    mssql.AvComponent = selectedComponent;
                    loadedConfigs.Add(selectedComponent, mssql);
                    UpdateUi(mssql);
                }
            }
        }

        private void LoadConfigs(List<string> _components)
        {
        }

        private void ApplyToAll(object _sender, RoutedEventArgs _e)
        {
        }

        private void WriteConfigToFile(object _sender, RoutedEventArgs _e)
        {
            string selectedComponent = currentTab.Header.ToString();
            string path = aautil.GetAaConfigFilePaths(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance)[selectedComponent];

            if (string.IsNullOrEmpty(path))
            {
                logger.Error("Null config path");
                logger.LogToUi("Config path is empty", currentTab.Header.ToString());
                return;
            }

            if (applyToAll.IsChecked == false)
            {
                MssqlConfig mssql = loadedConfigs[selectedComponent];

                switch (selectedComponent)
                {
                    case "av.biz":
                        mssql.AvDbHost = dbServerTextBox_biz.Text;

                        // av db
                        mssql.AvDbName = avDBTextBox_biz.Text;
                        mssql.AvUser = avDBUserTextBox_biz.Text;
                        mssql.SetAvDatabasePassword(avDBPasswordTextBox_biz.Password);

                        // jetspeed db
                        mssql.AvJetspeedDbName = jetspeedDBTextBox_biz.Text;
                        mssql.AvJetspeedUser = jetspeedUserTextBox_biz.Text;
                        mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox_biz.SecurePassword);

                        break;
                    case "av.cfmx":
                        mssql.AvDbHost = dbServer_cfmx.Text;

                        // av db
                        mssql.AvDbName = avDBTextBox_cfmx.Text;
                        mssql.AvUser = avDBUserTextBox_cfmx.Text;
                        mssql.SetAvDatabasePassword(avDBPasswordTextBox_cfmx.Password);

                        break;
                    case "av.indexer":
                        mssql.AvDbHost = dbServer_indexer.Text;

                        // av db
                        mssql.AvDbName = avDBTextBox_indexer.Text;
                        mssql.AvUser = avDBUserTextBox_indexer.Text;
                        mssql.SetAvDatabasePassword(avDBPasswordTextBox_indexer.Password);

                        break;
                    case "av.web":
                        mssql.AvDbHost = dbServerTextBox_web.Text;

                        // av db
                        mssql.AvDbName = avDBTextBox_web.Text;
                        mssql.AvUser = avDBUserTextBox_web.Text;
                        mssql.SetAvDatabasePassword(avDBPasswordTextBox_web.Password);

                        break;
                    default:
                        logger.Warn("Attempted to write to a disabled or non existent component: " + mssql.ToString());
                        break;
                }

                ConfigHandler config = new ConfigHandler(path, mssql.AvComponent, GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance, logger);


                config.WriteConfigToFile(mssql);
                UpdateUi(mssql);
            }
            else
            {
                foreach (KeyValuePair<string, MssqlConfig> configPair in loadedConfigs)
                {
                    MssqlConfig mssql = configPair.Value;

                    ConfigHandler config = new ConfigHandler(loadedConfig, mssql.AvComponent, GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance, logger);

                    mssql.AvDbHost = dbServerTextBox_biz.Text;

                    // av db
                    mssql.AvDbName = avDBTextBox_biz.Text;
                    mssql.AvUser = avDBUserTextBox_biz.Text;
                    mssql.SetAvDatabasePassword(avDBPasswordTextBox_biz.Password);

                    if (mssql.AvComponent == "av.biz")
                    {
                        // jetspeed db
                        mssql.AvJetspeedDbName = jetspeedDBTextBox_biz.Text;
                        mssql.AvJetspeedUser = jetspeedUserTextBox_biz.Text;
                        mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox_biz.SecurePassword);
                    }

                    config.WriteConfigToFile(mssql);

                    UpdateUi(mssql);
                }
            }
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
                    logger.Warn("Couldn't enable a commit button. There may be an issue. Selected tab is: " + currentTab.Header.ToString());
                    break;
            }
        }

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
            if (_e.OriginalSource.GetType() == typeof(TabControl))
            {
                try
                {
                    TabControl source = (TabControl) _e.Source;
                    TabItem selected = (TabItem) source.SelectedItem;

                    currentTab = selected;
                    //mssql.AvComponent = currentTab.Header.ToString();

                    // logger.LogToUi("Current Component: " + mssql.AvComponent);
                    // logger.LogToUi("Selected tab: " + currentTab.Name);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    Debug.WriteLine(e.StackTrace);
                }
            }
        }

        private void avPassShow_Checked(object _sender, RoutedEventArgs _e)
        {
        }

        private void Window_Closing(object _sender, CancelEventArgs _e)
        {
            if (debug.IsVisible)
                debug.Close();
        }
    }
}