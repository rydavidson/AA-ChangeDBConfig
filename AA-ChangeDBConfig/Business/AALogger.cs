using System.Linq;
using rydavidson.Accela.Common;
using System.Windows;
using AA_ChangeDBConfig.Views;

namespace AA_ChangeDBConfig.Business
{
    public class AaLogger : Logger
    {

        public AaLogger()
        {
            IsEnabled = false;
        }
        public AaLogger(string _logFile)
        {
            LogFile = _logFile;
            IsDebug = true;
            IsVerbose = true;

#if DEBUG
#else
isEnabled = false;
#endif
        }

        public void LogToUi(string _message)
        {
            if (!(Application.Current.Windows.Cast<Window>().FirstOrDefault(_windows => _windows is MainWindow) is MainWindow main))
                return;
            lb.AppendLine(_message);
            main.logBox_biz.Text += lb.ToString();
            main.logBox_cfmx.Text += lb.ToString();
            main.logBox_web.Text += lb.ToString();
            lb.Clear();
        }

        public void LogToUi(string _message, string _box)
        {
            if (!(Application.Current.Windows.Cast<Window>().FirstOrDefault(_windows => _windows is MainWindow) is MainWindow main))
                return;
            lb.AppendLine(_message);
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
                case "av.indexer":
                    main.logBox_indexer.Text += lb.ToString();
                    break;
                default:
                    lb.Clear(); //clear the base lb
                    Warn("Couldn't locate a log box to log to. Message: " + _message);
                    break;
            }

            lb.Clear();
        }
    }
}