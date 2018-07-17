using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AA_ChangeDBConfig.Views
{
    /// <summary>
    /// Interaction logic for DebugConsole.xaml
    /// </summary>
    public partial class DebugConsole : Window
    {
        MainWindow main;
        public DebugConsole(MainWindow _main)
        {
            InitializeComponent();
            main = _main;
        }

        private void RunCommand(object _sender, KeyEventArgs _e)
        {
            if (_e.Key == Key.Return)
            {
                main.RunDebugCommand(commandBox.Text.Trim(), LogtoBox);
            }
        }

        private void LogtoBox(string _message)
        {
            debugConsoleBox.Text += _message + Environment.NewLine;
        }
    }
}
