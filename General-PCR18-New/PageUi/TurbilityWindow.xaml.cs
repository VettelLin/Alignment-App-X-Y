using System;
using General_PCR18.Common;
using System.Windows;

namespace General_PCR18.PageUi
{
    public partial class TurbilityWindow : Window
    {
        public bool[] UseAdjust { get; private set; } = new bool[5];
        public int[] PreCycles { get; private set; } = new int[5];
        public double[] AdjustScale { get; private set; } = new double[5];

        public TurbilityWindow()
        {
            InitializeComponent();
            try
            {
                // Map global flags to UI (1..5 => FAM, HEX, ROX, Cy5, Cy5.5)
                var flags = GlobalData.TurbidityEnabled;
                chkUse1.IsChecked = flags[0]; // FAM
                chkUse2.IsChecked = flags[1]; // HEX(VIC)
                chkUse3.IsChecked = flags[2]; // ROX
                chkUse4.IsChecked = flags[3]; // Cy5
                chkUse5.IsChecked = flags[4]; // Cy5.5
                // 回填比例
                try
                {
                    txtRatio1.Text = GlobalData.TurbidityAdjustScale[0].ToString("0.0");
                    txtRatio2.Text = GlobalData.TurbidityAdjustScale[1].ToString("0.0");
                    txtRatio3.Text = GlobalData.TurbidityAdjustScale[2].ToString("0.0");
                    txtRatio4.Text = GlobalData.TurbidityAdjustScale[3].ToString("0.0");
                    txtRatio5.Text = GlobalData.TurbidityAdjustScale[4].ToString("0.0");
                }
                catch { }
            }
            catch { }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UseAdjust[0] = chkUse1.IsChecked == true;
                UseAdjust[1] = chkUse2.IsChecked == true;
                UseAdjust[2] = chkUse3.IsChecked == true;
                UseAdjust[3] = chkUse4.IsChecked == true;
                UseAdjust[4] = chkUse5.IsChecked == true;

                Util.LogHelper.Debug("TurbilityWindow OK: Use=[{0},{1},{2},{3},{4}] Cycles=[{5},{6},{7},{8},{9}] Ratio=[{10},{11},{12},{13},{14}]",
                    UseAdjust[0], UseAdjust[1], UseAdjust[2], UseAdjust[3], UseAdjust[4],
                    txtCycles1.Text, txtCycles2.Text, txtCycles3.Text, txtCycles4.Text, txtCycles5.Text,
                    txtRatio1.Text, txtRatio2.Text, txtRatio3.Text, txtRatio4.Text, txtRatio5.Text);

                PreCycles[0] = int.TryParse(txtCycles1.Text, out var c1) ? c1 : 10;
                PreCycles[1] = int.TryParse(txtCycles2.Text, out var c2) ? c2 : 10;
                PreCycles[2] = int.TryParse(txtCycles3.Text, out var c3) ? c3 : 10;
                PreCycles[3] = int.TryParse(txtCycles4.Text, out var c4) ? c4 : 10;
                PreCycles[4] = int.TryParse(txtCycles5.Text, out var c5) ? c5 : 10;

                AdjustScale[0] = double.TryParse(txtRatio1.Text, out var r1) ? r1 : 0.1;
                AdjustScale[1] = double.TryParse(txtRatio2.Text, out var r2) ? r2 : 0.2;
                AdjustScale[2] = double.TryParse(txtRatio3.Text, out var r3) ? r3 : 0.2;
                AdjustScale[3] = double.TryParse(txtRatio4.Text, out var r4) ? r4 : 0.2;
                AdjustScale[4] = double.TryParse(txtRatio5.Text, out var r5) ? r5 : 0.2;
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

