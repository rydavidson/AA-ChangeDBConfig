using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AA_ChangeDBConfig.Business
{
    public class Logger
    {
        protected string logFile { get; set; }
        private GlobalConfigs config = GlobalConfigs.Instance;
        StringBuilder sb = new StringBuilder();

        public Logger(string _logFile)
        {
            logFile = _logFile;
        }

        public void LogToUI(string message, string box)
        {            
            sb.AppendLine(message);
            var main = Application.Current.Windows.Cast<Window>().FirstOrDefault(windows => windows is MainWindow) as MainWindow; // get the main window so I can log to the logBox
            switch (box)
            {
                case "logBox_biz":
                    main.logBox_biz.Text += sb.ToString();
                    break;
                case "logBox_cfmx":
                    main.logBox_cfmx.Text += sb.ToString();
                    break;
                case "logBox_web":
                    main.logBox_web.Text += sb.ToString();
                    break;
                default:
                    break;
            }

            sb.Clear();
        }

        public void LogToUI(string message)
        {
            sb.AppendLine(message);
            var main = Application.Current.Windows.Cast<Window>().FirstOrDefault(windows => windows is MainWindow) as MainWindow; // get the main window so I can log to the logBox
            main.logBox_biz.Text += sb.ToString();
            main.logBox_cfmx.Text += sb.ToString();
            main.logBox_web.Text += sb.ToString();
            sb.Clear();
        }

        public void LogInfo(string s)
        {
            sb.AppendLine(s);
            ProcessWrite(" - INFO: " + sb.ToString());
            sb.Clear();
        }
        public void LogWarn(string s)
        {
            sb.AppendLine(s);
            ProcessWrite(" - WARN: " + sb.ToString());
            sb.Clear();
        }
        public void LogError(string s)
        {
            sb.AppendLine(s);
            ProcessWrite(" - ERROR: " + sb.ToString());
            sb.Clear();
        }
        public void LogDebug(string s)
        {
            if (GlobalConfigs.Instance.IsLogDebugEnabled)
            {
                sb.AppendLine(s);
                ProcessWrite(" - DEBUG: " + sb.ToString());
                sb.Clear();
            }
        }
        public void LogTrace(string s)
        {
            
            if (GlobalConfigs.Instance.IsLogTraceEnabled)
            {
                sb.AppendLine(s);
                ProcessWrite(" - TRACE: " + sb.ToString());
                sb.Clear();
            }
        }

        private void ProcessWrite(string text)
        {
            text = DateTime.Now.ToString() + text;
            File.AppendAllText(logFile, text);
        }

        // TODO Figure out why the async file writing sometimes causes log entries to not be written

        //Task ProcessWrite(string text)
        //{
        //    return WriteTextAsync(logFile, text);
        //}

        //async Task WriteTextAsync(string filePath, string text)
        //{
        //    text = DateTime.Now.ToString() + text;
        //    byte[] encodedText = Encoding.Unicode.GetBytes(text); // ran into issues with Encoding.Default so I'm using Unicode to force i18n compat
        //    using (FileStream sourceStream = new FileStream(filePath,
        //        FileMode.Append, FileAccess.Write, FileShare.None,
        //        bufferSize: 4096, useAsync: true))
        //    {
        //        await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
        //    };
        //}
    }
}
