using System;
using System.Globalization;
using System.Windows;

namespace General_PCR18.PageUi
{
    public partial class AxisSettingsWindow : Window
    {
        public double? XMin { get; private set; }
        public double? XMax { get; private set; }
        public double? YMin { get; private set; }
        public double? YMax { get; private set; }

        private readonly double defaultXMin;
        private readonly double defaultXMax;
        private readonly double defaultYMin;
        private readonly double defaultYMax;

        public AxisSettingsWindow(double xMin, double xMax, double yMin, double yMax)
        {
            InitializeComponent();

            defaultXMin = xMin;
            defaultXMax = xMax;
            defaultYMin = yMin;
            defaultYMax = yMax;

            txtXMin.Text = xMin.ToString("0.##", CultureInfo.InvariantCulture);
            txtXMax.Text = xMax.ToString("0.##", CultureInfo.InvariantCulture);
            txtYMin.Text = yMin.ToString("0.##", CultureInfo.InvariantCulture);
            txtYMax.Text = yMax.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            txtXMin.Text = defaultXMin.ToString("0.##", CultureInfo.InvariantCulture);
            txtXMax.Text = defaultXMax.ToString("0.##", CultureInfo.InvariantCulture);
            txtYMin.Text = defaultYMin.ToString("0.##", CultureInfo.InvariantCulture);
            txtYMax.Text = defaultYMax.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                XMin = ParseDouble(txtXMin.Text);
                XMax = ParseDouble(txtXMax.Text);
                YMin = ParseDouble(txtYMin.Text);
                YMax = ParseDouble(txtYMax.Text);

                DialogResult = true;
                Close();
            }
            catch (Exception)
            {
                MessageBox.Show("请输入有效的数值", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private static double? ParseDouble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return v;
            if (double.TryParse(text, out var v2)) return v2;
            throw new FormatException();
        }
    }
}


