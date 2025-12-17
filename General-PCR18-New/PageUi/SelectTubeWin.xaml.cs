using General_PCR18.Common;
using General_PCR18.Util;
using NPOI.OpenXmlFormats.Shared;
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
    /// Interaction logic for SelectTubeWin.xaml
    /// </summary>
    public partial class SelectTubeWin : Window
    {
        private List<CheckBox> checkBoxes = new List<CheckBox>();
        private List<int> tubeList = new List<int>();

        public SelectTubeWin()
        {
            InitializeComponent();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.Loaded += Win_Loaded;
        }

        private void Win_Loaded(object sender, RoutedEventArgs e)
        {
            tubeList.Clear();

            for (int i = 0; i < 18; i++)
            {
                if (GlobalData.GetStatus(i) == TUBE_STATUS.LightingCompleted)
                {
                    string c = Tools.GetDockUnit(i);

                    CheckBox ckbox = new CheckBox
                    {
                        Content = c,
                        Width = 60,
                        Height = 30,
                        Tag = i
                    };
                    panelTube.Children.Add(ckbox);
                    checkBoxes.Add(ckbox);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            CheckBox ckbox = sender as CheckBox;
            foreach (CheckBox checkBox in checkBoxes)
            {
                checkBox.IsChecked = ckbox.IsChecked;
            }
        }

        public List<int> TubeSelected()
        {
            foreach (CheckBox checkBox in checkBoxes)
            {
                if ((bool)checkBox.IsChecked)
                {
                    tubeList.Add((int)checkBox.Tag);
                }
            }
            return tubeList;
        }
    }
}
