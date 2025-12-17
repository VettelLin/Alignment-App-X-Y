using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using General_PCR18.Common;

namespace General_PCR18.PageUi
{
    public class BooleanNegationConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
        public object ConvertBack(object value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool b) return !b;
            return false;
        }
    }

    public partial class BasicParametersWindow : Window
    {
        public BasicParametersWindow()
        {
            InitializeComponent();
            try
            {
                EnsureDefaults();
                dgParams.ItemsSource = GlobalData.BasicParameters;
                dgParams.CellEditEnding += DgParams_CellEditEnding;
                UpdateDeleteButtonState();
            }
            catch { }
        }

        private void EnsureDefaults()
        {
            if (GlobalData.BasicParameters.Count > 0) return;

            string[] channels = new[] { "FAM", "Cy5", "VIC", "Cy5.5", "ROX" };
            for (int i = 0; i < channels.Length; i++)
            {
                GlobalData.BasicParameters.Add(new GlobalData.BasicParameterItem
                {
                    No = i + 1,
                    Channel = channels[i],
                    Target = string.Empty,
                    AutoBaseline = true,
                    BaselineStart = 3,
                    BaselineEnd = 15,
                    AutoThreshold = false,
                    NormalizedThreshold = 0.10,
                    DeltaRnThreshold = 0.10,
                    LowerThreshold = 7.00,
                    UpperThreshold = 50.00,
                });
            }
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int nextNo = GlobalData.BasicParameters.Count + 1;
                GlobalData.BasicParameters.Add(new GlobalData.BasicParameterItem
                {
                    No = nextNo,
                    Channel = string.Empty,
                    Target = string.Empty,
                    AutoBaseline = false,
                    BaselineStart = 3,
                    BaselineEnd = 15,
                    AutoThreshold = false,
                    NormalizedThreshold = 0.10,
                    DeltaRnThreshold = 0.10,
                    LowerThreshold = 7.00,
                    UpperThreshold = 50.00,
                });
                UpdateDeleteButtonState();
            }
            catch { }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgParams.SelectedItem is GlobalData.BasicParameterItem row)
                {
                    GlobalData.BasicParameters.Remove(row);
                    // re-number
                    int n = 1;
                    foreach (var item in GlobalData.BasicParameters)
                        item.No = n++;
                }
                UpdateDeleteButtonState();
            }
            catch { }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // commit edits
                if (dgParams.CommitEdit(DataGridEditingUnit.Cell, true))
                    dgParams.CommitEdit(DataGridEditingUnit.Row, true);
                DialogResult = true;
            }
            catch { DialogResult = true; }
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void UpdateDeleteButtonState()
        {
            if (btnDelete != null)
            {
                btnDelete.IsEnabled = GlobalData.BasicParameters.Any();
            }
        }

        private void DgParams_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            try
            {
                if (e.EditAction != DataGridEditAction.Commit) return;
                string header = e.Column.Header?.ToString() ?? string.Empty;
                if (header.StartsWith("Auto Baseli"))
                {
                    // 提交后读取行数据
                    if (e.Row?.Item is GlobalData.BasicParameterItem item)
                    {
                        if (item.AutoBaseline)
                        {
                            item.BaselineStart = 3;
                            item.BaselineEnd = 15;
                        }
                        // 立即刷新并把焦点切到 Baseline End 文本框，用户无需再次点击
                        dgParams.Dispatcher.InvokeAsync(() =>
                        {
                            dgParams.CommitEdit(DataGridEditingUnit.Row, true);
                            dgParams.Items.Refresh();
                            // 尝试将当前行切换成编辑模式
                            dgParams.CurrentCell = new DataGridCellInfo(e.Row.Item, dgParams.Columns[5]);
                            dgParams.BeginEdit();
                        });
                    }
                }
            }
            catch { }
        }
    }
}


