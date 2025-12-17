using System;
using System.Windows;

namespace General_PCR18.PageUi
{
    public partial class FilterParametersWindow : Window
    {
        public int MedianWindow { get; private set; }
        public int SmoothPasses { get; private set; }
        public int SmoothForwardM { get; private set; }
        public int SmoothBackwardN { get; private set; }
        public int CtThreshold { get; private set; }

        public FilterParametersWindow()
        {
            InitializeComponent();
            try
            {
                txtMedianWindow.Text = Common.GlobalData.FilterParams.MedianWindow.ToString();
                txtSmoothPasses.Text = Common.GlobalData.FilterParams.SmoothPasses.ToString();
                txtSmoothForwardM.Text = Common.GlobalData.FilterParams.SmoothForwardM.ToString();
                txtSmoothBackwardN.Text = Common.GlobalData.FilterParams.SmoothBackwardN.ToString();
                txtCtThreshold.Text = Common.GlobalData.FilterParams.CtThreshold.ToString();
            }
            catch { }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int median = int.TryParse(txtMedianWindow.Text, out var v1) ? v1 : 5;
                if (median < 3) median = 3;
                if (median % 2 == 0) median += 1;

                int passes = int.TryParse(txtSmoothPasses.Text, out var v2) ? v2 : 3;
                if (passes < 0) passes = 0;

                int m = int.TryParse(txtSmoothForwardM.Text, out var v3) ? v3 : 1;
                if (m < 0) m = 0;

                int n = int.TryParse(txtSmoothBackwardN.Text, out var v4) ? v4 : 3;
                if (n < 0) n = 0;

                int ct = int.TryParse(txtCtThreshold.Text, out var v5) ? v5 : 60;
                if (ct < 0) ct = 0;

                MedianWindow = median;
                SmoothPasses = passes;
                SmoothForwardM = m;
                SmoothBackwardN = n;
                CtThreshold = ct;

                DialogResult = true;
            }
            catch { DialogResult = false; }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}


