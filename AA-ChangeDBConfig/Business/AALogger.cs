using System.Linq;
using rydavidson.Accela.Common;
using System.Windows;
using AA_ChangeDBConfig.Views;

namespace AA_ChangeDBConfig.Business
{
    public class AaLogger : Logger
    {
        private GlobalConfigs config = GlobalConfigs.Instance;

        public AaLogger(string _logFile)
        {
            LogFile = _logFile;
            IsDebug = GlobalConfigs.Instance.IsLogDebugEnabled;
            IsVerbose = GlobalConfigs.Instance.IsLogTraceEnabled;
        }

        public void LogToUi(string _message, string _box)
        {            
            lb.AppendLine(_message);
            MainWindow main = Application.Current.Windows.Cast<Window>().FirstOrDefault(_windows => _windows is MainWindow) as MainWindow; // get the main window so I can log to the logBox
            switch (_box)
            {
                case "logBox_biz":
                    main.logBox_biz.Text += lb.ToString();
                    break;
                case "logBox_cfmx":
                    main.logBox_cfmx.Text += lb.ToString();
                    break;
                case "logBox_web":
                    main.logBox_web.Text += lb.ToString();
                    break;
                default:
                    break;
            }

            lb.Clear();
        }

        public void LogToUi(string _message)
        {
            lb.AppendLine(_message);
            MainWindow main = Application.Current.Windows.Cast<Window>().FirstOrDefault(_windows => _windows is MainWindow) as MainWindow; // get the main window so I can log to the logBox
            main.logBox_biz.Text += lb.ToString();
            main.logBox_cfmx.Text += lb.ToString();
            main.logBox_web.Text += lb.ToString();
            lb.Clear();
        }
    }
}
