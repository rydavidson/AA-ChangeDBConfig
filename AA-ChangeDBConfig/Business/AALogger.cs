using System.Diagnostics;
using System.Linq;
using rydavidson.Accela.Common;
using System.Windows;
using AA_ChangeDBConfig.Views;

namespace AA_ChangeDBConfig.Business
{
    public class AaLogger : Logger
    {
        private GlobalConfigs config;

        public AaLogger(string _logFile)
        {
            config = GlobalConfigs.Instance;
            LogFile = _logFile;
            IsDebug = true;
            IsVerbose = true;
        }

        public void LogToUi(string _message, string _box)
        {            
            lb.AppendLine(_message);
            MainWindow main = Application.Current.Windows.Cast<Window>().FirstOrDefault(_windows => _windows is MainWindow) as MainWindow; // get the main window so I can log to the logBox
            if (main is null)
                return;
            switch (_box)
            {
                case "av.biz":
                    main.logBox_biz.Text += lb.ToString();
                    break;
                case "av.cfmx":
                    main.logBox_cfmx.Text += lb.ToString();
                    break;
                case "av.web":
                    main.logBox_web.Text += lb.ToString();
                    break;
                case"av.indexer":
                    main.logBox_indexer.Text += lb.ToString();
                    break;
                default:
                    Warn("Couldn't locate a log box to log to. Message: " + _message);
                    break;
            }

            lb.Clear();
        }

        public void LogToUi(string _message)
        {
            lb.AppendLine(_message);
            MainWindow main = Application.Current.Windows.Cast<Window>().FirstOrDefault(_windows => _windows is MainWindow) as MainWindow; // get the main window so I can log to the logBox
            if (main is null)
                return;
            main.logBox_biz.Text += lb.ToString();
            main.logBox_cfmx.Text += lb.ToString();
            main.logBox_web.Text += lb.ToString();
            lb.Clear();
        }
    }
}
