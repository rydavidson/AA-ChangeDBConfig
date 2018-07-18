using System;
using System.Windows;
using System.Windows.Input;

namespace AA_ChangeDBConfig.Views
{
    /// <inheritdoc cref="System.Windows.Window" />
    /// <summary>
    /// Interaction logic for DebugConsole.xaml
    /// </summary>
    public partial class DebugConsole : Window
    {
        private readonly MainWindow main;
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
