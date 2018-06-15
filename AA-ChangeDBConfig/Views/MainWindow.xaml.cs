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
using System.Windows.Navigation;
using System.Windows.Shapes;
using AA_ChangeDBConfig.Business;

namespace AA_ChangeDBConfig
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += RunOnLoad;
        }

        private void RunOnLoad(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (string version in CommonUtils.GetAAVersions())
                {
                    versionsComboBox.Items.Add(version);
                }
            }
            catch (Exception ex)
            {
                StringBuilder message = new StringBuilder();
                message.AppendLine("Unable to detect installed AA versions. If an error was encountered it will be shown below.");
                message.AppendLine();
                message.AppendLine(ex.Message);
                message.AppendLine(ex.StackTrace);
                MessageBox.Show(message.ToString());

            }

        }
    }
}
