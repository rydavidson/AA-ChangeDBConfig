using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using AA_ChangeDBConfig.Business;
using log4net;
using Microsoft.Win32;
using NetSparkle;
using NetSparkle.Enums;
using rydavidson.Accela.Configuration.Common;
using rydavidson.Accela.Configuration.Handlers;
using rydavidson.Accela.Configuration.IO;
using rydavidson.Accela.Configuration.Models;
using System.Drawing;
using System.Reflection;

namespace AA_ChangeDBConfig.Views
{
    /// <inheritdoc cref="System.Windows.Window" />
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        // private AaLogger logger;
        private AaUtil aautil;
        private readonly Dictionary<string, string> instancesWithVersions = new Dictionary<string, string>();
        private string loadedConfig = "";
        private readonly Dictionary<string, MssqlConfig> loadedConfigs = new Dictionary<string, MssqlConfig>();
        private readonly DebugConsole debug;
        private AaLogger uiLogger = new AaLogger();
        private bool hasLoaded;

        //private Icon icon = new Icon("");


        private Sparkle sparkle;

        private static readonly ILog logger
            = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private TabItem currentTab = new TabItem();

        public MainWindow()
        {
            InitializeComponent();
            sparkle = new Sparkle(
                "https://rydavidson.github.io/meta/Accela/AAChangeDBConfig/appcast.xml",
                GetWindowIcon(),
                SecurityMode.Strict
            );
            Loaded += RunOnLoad;
            debug = new DebugConsole(this);
            //log4net.Config.XmlConfigurator.Configure();
            logger.Info("Application startup");
        }
        
        public Icon GetWindowIcon()
        {
            return System.Drawing.Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location);
        }

        private void RunOnLoad(object _sender, RoutedEventArgs _e)
        {
            sparkle.CheckOnFirstApplicationIdle();
            // Debug.WriteLine("Running from " + Environment.CurrentDirectory);

            // hide all the tabs at first except biz
            av_adsTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_arwTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_bizTab.IsEnabled = true;
            av_bizTab.IsSelected = true;

            av_cfmxTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_indexerTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_webTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            aautil = new AaUtil();

            GlobalConfigs.Instance.IsLogDebugEnabled = true;
            GlobalConfigs.Instance.IsLogTraceEnabled = true;

            try
            {
                int totalVersions = aautil.GetAaVersions().Count;
                foreach (string version in aautil.GetAaVersions())
                {
                    versionsComboBox_biz.Items.Add(version);
                    versionsComboBox_cfmx.Items.Add(version);
                    versionsComboBox_web.Items.Add(version);
                    versionsComboBox_indexer.Items.Add(version);

                    if (totalVersions == 1)
                    {
                        versionsComboBox_biz.SelectedItem = version;
                        versionsComboBox_cfmx.SelectedItem = version;
                        versionsComboBox_web.SelectedItem = version;
                        versionsComboBox_indexer.SelectedItem = version;
                    }

                    try
                    {
                        int totalInstances = aautil.GetInstancesForVersion(version).Count;
                        foreach (string instance in aautil.GetInstancesForVersion(version))
                        {
                            instancesWithVersions.Add(instance + version, version);
                            if (totalInstances != 1 && totalVersions != 1) continue;
                            GlobalConfigs.Instance.AaInstance = instance;
                            PopulateInstanceComboBox(this, null);
                        }
                    }
                    catch (Exception ey)
                    {
                        StringBuilder message = new StringBuilder();
                        message.AppendLine("Unable to detect installed AA instances for version: " + version);
                        uiLogger.LogToUi(message.ToString());
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
                uiLogger.LogToUi(message.ToString());
                message.AppendLine(ex.Message);
                message.Append(ex.StackTrace);
                logger.Error(message.ToString());
            }

            if (!av_bizTab.IsEnabled)
            {
                MessageBox.Show("A biz server could not be detected among the installed components. A biz server is required.", "No Biz Server Found!");
                Exit(null, null);
            }

            currentTab = av_bizTab.IsEnabled ? av_bizTab : mainTabMenu.Items[0] as TabItem;
            hasLoaded = true;
            ApplyToAll_Checked(null, null);
        }

        private void EnableOrDisableTabs(string _version, string _instance)
        {
            av_adsTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_arwTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_bizTab.IsEnabled = true;
            av_bizTab.IsSelected = true;

            av_cfmxTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_indexerTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            av_webTab.IsEnabled = false;
            av_adsTab.Visibility = Visibility.Collapsed;

            foreach (string comp in aautil.GetAaInstalledComponents(_version, _instance))
            {
                switch (comp.Trim())
                {
                    case "av.ads":
                        logger.Debug("av.ads is installed but will not be enabled");
                        break;
                    case "av.arw":
                        logger.Debug("av.arw is installed but will not be enabled");
                        break;
                    case "av.biz":
                        av_bizTab.IsEnabled = true;
                        break;
                    case "av.cfmx":
                        av_cfmxTab.IsEnabled = true;
                        break;
                    case "av.indexer":
                        av_indexerTab.IsEnabled = true;
                        break;
                    case "av.web":
                        av_webTab.IsEnabled = true;
                        break;
                    default:
                        av_bizTab.IsEnabled = true;
                        logger.Debug(comp);
                        break;
                }
            }

            ApplyToAll_Checked(null, null);
        }

        #region I/O

        private void LoadConfig(object _sender, RoutedEventArgs _e)
        {
            string selectedComponent = currentTab.Header.ToString();

            if (currentTab == null)
            {
                logger.Debug("Null header on currentTab");
                return;
            }

            logger.Debug("Loaded tab: " + currentTab.Header.ToString());

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
                ConfigReader reader = new ConfigReader(loadedConfig);
                MssqlConfig mssql = new MssqlConfig();
                try
                {
                    mssql = reader.FindValue("av.db") == "mssql" ? reader.ReadFromConfigFile() : null;
                }
                catch (Exception e)
                {
                    logger.Error("Error checking database type");
                    logger.Error(e.Message);
                    logger.Error(e.StackTrace);
                }

                if (mssql is null)
                {
                    MessageBox.Show("The database type \"" + reader.FindValue("av.db") + "\" is not supported", "Unsupported Database");
                    return;
                }

                mssql.AvComponent = currentTab.Header.ToString();
                // ConfigHandler config = new ConfigHandler(loadedConfig, mssql.avComponent, GlobalConfigs.Instance.AAVersion, GlobalConfigs.Instance.AAInstance, logger);

                uiLogger.LogToUi("Loading config direct from detected file: " + loadedConfig, "logBox_" + mssql.AvComponent.Remove(0, 3));

                loadedConfigs.Add(currentTab.Header.ToString(), mssql);

                UpdateUi(mssql);
            }
            else
            {
                OpenFileDialog getPropFile = new OpenFileDialog
                {
                    InitialDirectory = configDir,
                    Filter = "Properties files (*.properties)|*.properties"
                };

                if (getPropFile.ShowDialog() != true) return;
                try
                {
                    loadedConfig = getPropFile.FileName;
                    ConfigReader reader = new ConfigReader(loadedConfig);
                    MssqlConfig mssql = reader.FindValue("av.db") == "mssql" ? reader.ReadFromConfigFile() : null;

                    if (mssql is null)
                    {
                        MessageBox.Show("The database type \"" + reader.FindValue("av.db") + "\" is not supported", "Unsupported Database");
                        return;
                    }

                    mssql.AvComponent = reader.FindValue("av.server") != null ? "av." + reader.FindValue("av.server") : selectedComponent;
                    if (loadedConfigs.ContainsKey(mssql.AvComponent))
                        loadedConfigs.Remove(mssql.AvComponent);
                    loadedConfigs.Add(mssql.AvComponent, mssql);
                    UpdateUi(mssql);
                }
                catch (Exception ex)
                {
                    uiLogger.LogToUi("An error occured", selectedComponent);
                    logger.Error(ex.Message);
                    logger.Error(ex.StackTrace);
                }
            }
        }

        private void WriteConfigToFile(object _sender, RoutedEventArgs _e)
        {
            string selectedComponent = currentTab.Header.ToString();
            string path = aautil.GetAaConfigFilePaths(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance)[selectedComponent];

            if (string.IsNullOrEmpty(path))
            {
                logger.Error("Null config path");
                uiLogger.LogToUi("Config path is empty", currentTab.Header.ToString());
                return;
            }

            if (applyToAll.IsChecked == false)
            {
                List<string> installedComps = aautil.GetAaInstalledComponents(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance);

                if (!loadedConfigs.ContainsKey(selectedComponent))
                    loadedConfigs.Add(selectedComponent, new ConfigReader(path).ReadFromConfigFile());

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

                ConfigHandler config = new ConfigHandler(path, mssql.AvComponent, GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance, uiLogger);

                try
                {
                    config.WriteConfigToFile(mssql);
                    uiLogger.LogToUi("Updated config successfully", selectedComponent);
                }
                catch (Exception e)
                {
                    logger.Error("Failed to update config at " + config.PathToConfigFile);
                    logger.Error(e.Message);
                    logger.Error(e.StackTrace);

                    RestoreBackup(config.PathToConfigFile + ".backup", config.PathToConfigFile);

                    mssql = config.ReadConfigFromFile();
                }
                finally
                {
                    UpdateUi(mssql);
                }
            }
            else
            {
                bool loadedSelectedComponent = false;

                foreach (string comp in aautil.GetAaInstalledComponents(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance))
                {
                    MssqlConfig mssql;


                    if (loadedConfigs.ContainsKey(selectedComponent) && loadedSelectedComponent == false)
                    {
                        mssql = loadedConfigs[selectedComponent];
                        path = aautil.GetAaConfigFilePaths(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance)[selectedComponent];
                        ConfigHandler config = new ConfigHandler(path, comp, GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance, uiLogger);
                        mssql = new MssqlConfig
                        {
                            AvComponent = selectedComponent,
                            AvDbHost = dbServerTextBox_biz.Text,
                            AvDbName = avDBTextBox_biz.Text,
                            AvUser = avDBUserTextBox_biz.Text,
                            AvJetspeedDbName = jetspeedDBTextBox_biz.Text,
                            AvJetspeedUser = jetspeedUserTextBox_biz.Text
                        };
                        mssql.SetAvDatabasePassword(avDBPasswordTextBox_biz.Password);
                        mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox_biz.SecurePassword);

                        try
                        {
                            config.WriteConfigToFile(mssql);
                            uiLogger.LogToUi("Updated config successfully", selectedComponent);
                        }
                        catch (Exception e)
                        {
                            logger.Error("Failed to update config at " + config.PathToConfigFile);
                            logger.Error(e.Message);
                            logger.Error(e.StackTrace);

                            RestoreBackup(config.PathToConfigFile + ".backup", config.PathToConfigFile);

                            mssql = config.ReadConfigFromFile();
                        }
                        finally
                        {
                            UpdateUi(mssql);
                            loadedSelectedComponent = true;
                        }
                    }
                    else
                    {
                        path = aautil.GetAaConfigFilePaths(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance)[comp];
                        ConfigHandler config = new ConfigHandler(path, comp, GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance, uiLogger);
                        mssql = new MssqlConfig {AvComponent = comp, AvDbHost = dbServerTextBox_biz.Text, AvDbName = avDBTextBox_biz.Text, AvUser = avDBUserTextBox_biz.Text};
                        mssql.SetAvDatabasePassword(avDBPasswordTextBox_biz.Password);

                        if (mssql.AvComponent == "av.biz")
                        {
                            // jetspeed db
                            mssql.AvJetspeedDbName = jetspeedDBTextBox_biz.Text;
                            mssql.AvJetspeedUser = jetspeedUserTextBox_biz.Text;
                            mssql.SetJetspeedDatabasePassword(jetspeedPasswordTextBox_biz.SecurePassword);
                        }

                        try
                        {
                            config.WriteConfigToFile(mssql);
                            uiLogger.LogToUi("Updated config successfully", mssql.AvComponent);
                        }
                        catch (Exception e)
                        {
                            logger.Error("Failed to update config at " + config.PathToConfigFile);
                            logger.Error(e.Message);
                            logger.Error(e.StackTrace);

                            RestoreBackup(config.PathToConfigFile + ".backup", config.PathToConfigFile);

                            mssql = config.ReadConfigFromFile();
                        }
                        finally
                        {
                            UpdateUi(mssql);
                        }
                    }
                }
            }
        }

        private void RestoreBackup(string _pathToBackup, string _pathToRestoreTo)
        {
            // copy the backup

            string tempBackupCopy = _pathToBackup + ".tmp";
            try
            {
                File.Copy(_pathToBackup, tempBackupCopy);
            }
            catch (Exception ex)
            {
                logger.Error("Error creating temp copy of backup");
                logger.Error(ex.Message);
                logger.Error(ex.StackTrace);
            }

            // replace the config with the backup
            try
            {
                File.Replace(tempBackupCopy, _pathToRestoreTo, tempBackupCopy);
            }
            catch (Exception ex)
            {
                logger.Error("Error restoring backup");
                logger.Error(ex.Message);
                logger.Error(ex.StackTrace);
            }

            // delete the copy of the backup
            try
            {
                File.Delete(tempBackupCopy);
                File.Delete(_pathToBackup);
            }
            catch (Exception ex)
            {
                logger.Error("Error deleting temp files");
                logger.Error(ex.Message);
                logger.Error(ex.StackTrace);
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
                case "av.indexer":
                    dbServer_indexer.Text = _mssql.AvDbHost;
                    avDBTextBox_indexer.Text = _mssql.AvDbName;
                    avDBUserTextBox_indexer.Text = _mssql.AvUser;
                    avDBPasswordTextBox_indexer.Password = _mssql.GetAvDatabasePassword();
                    commitButton_indexer.IsEnabled = true;
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

        #endregion


        #region events

        private void ApplyToAll_Checked(object _sender, RoutedEventArgs _e)
        {
            if (!hasLoaded)
                return;

            try
            {
                if (applyToAll.IsChecked == true)
                {
                    av_adsTab.IsEnabled = false;
                    av_arwTab.IsEnabled = false;
                    av_cfmxTab.IsEnabled = false;
                    av_indexerTab.IsEnabled = false;
                    av_webTab.IsEnabled = false;
                }
                else
                {
                    av_cfmxTab.IsEnabled = true;
                    av_indexerTab.IsEnabled = true;
                    av_webTab.IsEnabled = true;
                }
            }
            catch (Exception e)
            {
                logger.Error(e.Message + e.StackTrace);
            }
        }

        private void SelectedInstanceChanged(object _sender, SelectionChangedEventArgs _e)
        {
            if (!(_e.OriginalSource is ComboBox clickedBox))
                return;

            ComboBox box = (ComboBox) _e.OriginalSource;


            if (box.SelectedValue is null)
                return;

            if (Equals(box, instancesComboBox_biz))
            {
                GlobalConfigs.Instance.AaInstance = box.SelectedValue.ToString();
                loadConfigButton_biz.IsEnabled = true;
            }

            if (Equals(box, instancesComboBox_cfmx))
            {
                GlobalConfigs.Instance.AaInstance = box.SelectedValue.ToString();
                loadConfigButton_cfmx.IsEnabled = true;
            }

            if (Equals(box, instancesComboBox_indexer))
            {
                GlobalConfigs.Instance.AaInstance = box.SelectedValue.ToString();
                loadConfigButton_indexer.IsEnabled = true;
            }

            if (Equals(box, instancesComboBox_web))
            {
                GlobalConfigs.Instance.AaInstance = box.SelectedValue.ToString();
                loadConfigButton_web.IsEnabled = true;
            }

            EnableOrDisableTabs(GlobalConfigs.Instance.AaVersion, GlobalConfigs.Instance.AaInstance);
        }

        private void mainTabMenu_SelectionChanged(object _sender, SelectionChangedEventArgs _e)
        {
            if (_e.OriginalSource.GetType() != typeof(TabControl)) return;
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

        private void avPassShow_Checked(object _sender, RoutedEventArgs _e)
        {
        }

        private void Window_Closing(object _sender, CancelEventArgs _e)
        {
            debug.Close();
        }

        #endregion

        #region misc

        private IEnumerable<string> LookupInstancesForVersion(string _version)
        {
            List<string> instances = new List<string>();

            foreach (KeyValuePair<string, string> instanceVersionPair in instancesWithVersions)
            {
                if (instanceVersionPair.Value == _version)
                {
                    instances.Add(instanceVersionPair.Key.Replace(_version, ""));
                }
            }

            return instances;
        }

        private void PopulateInstanceComboBox(object _sender, SelectionChangedEventArgs _e)
        {
            if (_sender == this)
            {
                if (!instancesComboBox_biz.Items.Contains(GlobalConfigs.Instance.AaInstance))
                    instancesComboBox_biz.Items.Add(GlobalConfigs.Instance.AaInstance);
                if (!instancesComboBox_cfmx.Items.Contains(GlobalConfigs.Instance.AaInstance))
                    instancesComboBox_cfmx.Items.Add(GlobalConfigs.Instance.AaInstance);
                if (!instancesComboBox_indexer.Items.Contains(GlobalConfigs.Instance.AaInstance))
                    instancesComboBox_indexer.Items.Add(GlobalConfigs.Instance.AaInstance);
                if (!instancesComboBox_web.Items.Contains(GlobalConfigs.Instance.AaInstance))
                    instancesComboBox_web.Items.Add(GlobalConfigs.Instance.AaInstance);

                instancesComboBox_biz.SelectedItem = GlobalConfigs.Instance.AaInstance;
                instancesComboBox_cfmx.SelectedItem = GlobalConfigs.Instance.AaInstance;
                instancesComboBox_indexer.SelectedItem = GlobalConfigs.Instance.AaInstance;
                instancesComboBox_web.SelectedItem = GlobalConfigs.Instance.AaInstance;
            }
            else
            {
                if (_e.OriginalSource.GetType() != typeof(ComboBox))
                    return;

                string selectedVersion = "";
                ComboBox clickedBox = _e.OriginalSource as ComboBox;

                if (clickedBox is null)
                    return;

                selectedVersion = clickedBox.SelectedValue.ToString();

                if (string.IsNullOrWhiteSpace(selectedVersion))
                    return;

                if (Equals(_e.OriginalSource, versionsComboBox_biz))
                    instancesComboBox_biz.Items.Clear();
                if (Equals(_e.OriginalSource, versionsComboBox_cfmx))
                    instancesComboBox_cfmx.Items.Clear();
                if (Equals(_e.OriginalSource, versionsComboBox_indexer))
                    instancesComboBox_indexer.Items.Clear();
                if (Equals(_e.OriginalSource, versionsComboBox_web))
                    instancesComboBox_web.Items.Clear();

                GlobalConfigs.Instance.AaVersion = selectedVersion;

                foreach (string instance in LookupInstancesForVersion(selectedVersion))
                {
                    if (!instancesComboBox_biz.Items.Contains(instance))
                        instancesComboBox_biz.Items.Add(instance);
                    if (!instancesComboBox_cfmx.Items.Contains(instance))
                        instancesComboBox_cfmx.Items.Add(instance);
                    if (!instancesComboBox_indexer.Items.Contains(instance))
                        instancesComboBox_indexer.Items.Add(instance);
                    if (!instancesComboBox_web.Items.Contains(instance))
                        instancesComboBox_web.Items.Add(instance);
                }
            }
        }

        public void RunDebugCommand(string _command, Action<string> _resultAction)
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

            _resultAction(sb.ToString());
            sb.Clear();
        }

        private void RunDebugConsole(object _sender, RoutedEventArgs _e)
        {
            debug.Show();
        }


        private void Exit(object _sender, RoutedEventArgs _e)
        {
            Environment.Exit(0);
        }

        #endregion
    }
}