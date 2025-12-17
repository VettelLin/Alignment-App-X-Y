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

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for TestWin.xaml
    /// </summary>
    public partial class TestWin : Window
    {
        private MainWindow mainWindow;

        public TestWin(MainWindow mainWindow)
        {
            InitializeComponent();

            this.mainWindow = mainWindow;
        }

        private void CmdButton_Click(object sender, RoutedEventArgs e)
        {
            string cmd = cmdInput.Text;
            if (string.IsNullOrEmpty(cmd))
            {
                return;
            }

            mainWindow.ReceiveTestCmd(cmd.Replace(" ", ""));
        }

        private void CmdButton_Click2(object sender, RoutedEventArgs e)
        {
            string cmd = cmdInput2.Text;
            if (string.IsNullOrEmpty(cmd))
            {
                return;
            }

            mainWindow.SendTestCmd(cmd.Replace(" ", ""));
        }
    }
}
