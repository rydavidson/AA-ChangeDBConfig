using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AA_ChangeDBConfig.Business
{
    public class Logger
    {
        protected string logFile { get; set; }
        public Logger(string _logFile)
        {
            logFile = _logFile;
        }

        public void LogInfo(string s)
        {
            ProcessWrite("- INFO: " + s);
        }
        public void LogWarn(string s)
        {
            ProcessWrite("- WARN: " + s);
        }
        public void LogError(string s)
        {
            ProcessWrite("- ERROR: " + s);
        }
        public void LogDebug(string s)
        {
            ProcessWrite("- DEBUG: " + s);
        }
        public void LogTrace(string s)
        {
            ProcessWrite("- TRACE: " + s);
        }

        Task ProcessWrite(string text)
        {
            return WriteTextAsync(logFile, text);
        }

        async Task WriteTextAsync(string filePath, string text)
        {
            text = DateTime.Now.ToString() + text;
            byte[] encodedText = Encoding.Unicode.GetBytes(text);            
            using (FileStream sourceStream = new FileStream(filePath,
                FileMode.Append, FileAccess.Write, FileShare.None,
                bufferSize: 4096, useAsync: true))
            {
                await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
            };
        }
    }
}
