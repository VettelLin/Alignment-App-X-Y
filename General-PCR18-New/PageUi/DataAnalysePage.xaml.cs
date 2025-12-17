using General_PCR18.Algorithm;
using General_PCR18.Common;
using General_PCR18.UControl;
using General_PCR18.Util;
using MathNet.Numerics.Distributions;
using NPOI.HSSF.Record.CF;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SixLabors.ImageSharp.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.Globalization;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for DataAnalysePage.xaml
    /// </summary>
    public partial class DataAnalysePage : BasePage
    {
        #region 变量区域
        private readonly SampleUC[] sampleList = new SampleUC[18];
        private readonly HashSet<SampleUC> selectList = new HashSet<SampleUC>();
        private readonly HashSet<int> activeCurveIndices = new HashSet<int>();

        private readonly SynchronizationContext context;

        private readonly int maxAxisXValue = 61;  // X轴最大点
        private readonly int maxAxisYValue = 5000;  // Y轴最大点 5000
        private readonly double[] xAxisIncValue = new double[18];  // X轴数据
        private HashSet<string> selectCurvesType = new HashSet<string>() { "FAM" };  // "FAM", "Cy5", "VIC", "Cy55", "ROX"
        private System.Windows.Forms.ToolTip tooltip; // 显示点信息
        private bool isDraggingSelect = false;
        private Point? dragStartPoint = null;
        private Rectangle selectionRect = null;
        private HashSet<SampleUC> dragStartSelectionSnapshot = null;
        private bool isBatchRendering = false;
        private readonly double[] suggestedYMaxType1 = new double[18];
        private readonly double[] suggestedYMaxType3 = new double[18];
        private System.Windows.Forms.ContextMenuStrip chartContextMenu; // 右键菜单
        private System.Windows.Forms.ToolStripMenuItem menuSaveImage;    // 保存图片项

        // 轴范围手动控制
        private bool manualAxis = false;
        private double? manualXMin = null;
        private double? manualXMax = null;
        private double? manualYMin = null;
        private double? manualYMax = null;

        #endregion

        public DataAnalysePage()
        {
            InitializeComponent();

            // 初始化样本
            SampleData sampleData = new SampleData()
            {
                Width = 80,
                Height = 100,
                SeparateHeight = 5,
                Margin = 5,
                Sample_ClickEventTick = Sample_ClickEventTick,
                Sample_StartClickEventHandler = Sample_StartClickEventHandler,
            };
            InitSample(sampleGrid, sampleList, sampleData);

            InitChart();

            context = SynchronizationContext.Current; // 获取当前 UI 线程的上下文

            // 订阅事件
            EventBus.OnDataAnalyseMessageReceived += EventBus_OnMessageReceived;

            cmbCurveType.SelectedIndex = 0;
            cmbFAM.SelectedIndex = 0;

            // 保底：确保下拉项中 Tag="MOT" 的显示文本为 "All"（避免 Debug 下旧缓存）
            foreach (var obj in cmbFAM.Items)
            {
                if (obj is ComboBoxItem cbi && cbi.Tag != null && cbi.Tag.ToString() == "MOT")
                {
                    cbi.Content = "All";
                    break;
                }
            }

            // 默认选择 All（优先匹配 Tag=All/MOT 或显示文本为 All）
            foreach (var obj in cmbFAM.Items)
            {
                if (obj is ComboBoxItem cbi)
                {
                    string tag = cbi.Tag?.ToString();
                    string content = cbi.Content?.ToString();
                    if (string.Equals(tag, "MOT", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(tag, "ALL", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(content, "All", StringComparison.OrdinalIgnoreCase))
                    {
                        cmbFAM.SelectedItem = cbi;
                        break;
                    }
                }
            }

            this.Loaded += Page_Loaded;
            // 绑定样本网格的拖拽选择事件
            sampleGrid.MouseLeftButtonDown += SampleGrid_MouseLeftButtonDown;
            sampleGrid.MouseMove += SampleGrid_MouseMove;
            sampleGrid.MouseLeftButtonUp += SampleGrid_MouseLeftButtonUp;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("===>DataAnalysePage Loaded");

            RefreshSampleUC(sampleList);
        }

        /// <summary>
        /// 初始化图表曲线
        /// </summary>
        private void InitChart()
        {
            ChartArea chartArea = new ChartArea("CharArea1");

            // X轴
            chartArea.AxisX.Title = "Cycle#";
            chartArea.AxisX.Interval = 3;
            chartArea.AxisX.Minimum = 1;
            chartArea.AxisX.Maximum = maxAxisXValue;
            chartArea.AxisX.MajorGrid.LineColor = Tools.HexToColor("#eaecef"); // 设置 X 轴网格线颜色

            // Y轴
            chartArea.AxisY.Title = "Rn";
            //chartArea.AxisY.Interval = 500;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = maxAxisYValue;
            chartArea.AxisY.MajorGrid.LineColor = Tools.HexToColor("#eaecef"); // 设置 Y 轴网格线颜色

            // 启用 X 轴和 Y 轴的网格线
            chartArea.AxisX.MajorGrid.Enabled = true;
            chartArea.AxisY.MajorGrid.Enabled = true;

            chartArea.AxisX.Enabled = AxisEnabled.True;  // 始终显示
            chartArea.AxisY.Enabled = AxisEnabled.True;

            // 设置图表的背景颜色
            chartArea.BackColor = Tools.HexToColor("#fafafa");

            // 加入图表
            chartDeltaRn.ChartAreas.Add(chartArea);

            // 样本
            for (int i = 0; i < 18; i++)
            {
                // FAM
                Series seriesFAM = new Series("FAM-" + i.ToString());
                seriesFAM.ChartType = SeriesChartType.Line;
                seriesFAM.Color = Tools.HexToColor("#0000ff");
                seriesFAM.BorderWidth = 2;
                chartDeltaRn.Series.Add(seriesFAM);

                // Cy5
                Series seriesCy5 = new Series("Cy5-" + i.ToString());
                seriesCy5.ChartType = SeriesChartType.Line;
                seriesCy5.Color = Tools.HexToColor("#ff0000");
                seriesCy5.BorderWidth = 2;
                chartDeltaRn.Series.Add(seriesCy5);

                // VIC
                Series seriesVIC = new Series("VIC-" + i.ToString());
                seriesVIC.ChartType = SeriesChartType.Line;
                seriesVIC.Color = Tools.HexToColor("#00ff00");
                seriesVIC.BorderWidth = 2;
                chartDeltaRn.Series.Add(seriesVIC);

                // Cy5.5
                Series seriesCy55 = new Series("Cy55-" + i.ToString());
                seriesCy55.ChartType = SeriesChartType.Line;
                seriesCy55.Color = Tools.HexToColor("#8B0000");
                seriesCy55.BorderWidth = 2;
                chartDeltaRn.Series.Add(seriesCy55);

                // ROX
                Series seriesROX = new Series("ROX-" + i.ToString());
                seriesROX.ChartType = SeriesChartType.Line;
                seriesROX.Color = Tools.HexToColor("#d6a01d");
                seriesROX.BorderWidth = 2;
                chartDeltaRn.Series.Add(seriesROX);
            }

            // 默认的，不显示
            Series seriesDefault = new Series("Default");
            seriesDefault.ChartType = SeriesChartType.Line;
            seriesDefault.Color = Tools.HexToColor("#f1f1f1");
            chartDeltaRn.Series.Add(seriesDefault);
            seriesDefault.Points.AddXY(1, 0);

            chartDeltaRn.MouseMove += ChartDeltaRn_MouseMove;

            tooltip = new System.Windows.Forms.ToolTip()
            {
                Active = true,
                AutoPopDelay = 5000,
                InitialDelay = 500,
                ReshowDelay = 500,
                ShowAlways = true,
            };

            // 右键菜单（保存图片）
            chartContextMenu = new System.Windows.Forms.ContextMenuStrip();
            menuSaveImage = new System.Windows.Forms.ToolStripMenuItem();
            menuSaveImage.Click += MenuSaveImage_Click;
            chartContextMenu.Items.Add(menuSaveImage);
            UpdateContextMenuLocalization();

            chartDeltaRn.MouseDown += ChartDeltaRn_MouseDown;
        }

        private void ThresholdVisbility(bool visible)
        {
            lblThreshold.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
            txtThreshold.Visibility = visible ? Visibility.Visible : Visibility.Hidden;
        }

        private void UpdateContextMenuLocalization()
        {
            try
            {
                string text = Properties.Resources.ResourceManager.GetString("save_image", Properties.Resources.Culture);
                if (string.IsNullOrWhiteSpace(text))
                {
                    // 简单兜底
                    text = (Properties.Resources.Culture != null && Properties.Resources.Culture.Name.StartsWith("en")) ? "Save Image" : "保存图片";
                }
                menuSaveImage.Text = text;
            }
            catch { menuSaveImage.Text = "保存图片"; }
        }

        private void ChartDeltaRn_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            try
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    var chart = sender as System.Windows.Forms.DataVisualization.Charting.Chart;
                    if (chart == null) return;
                    var hit = chart.HitTest(e.X, e.Y);
                    // 仅在坐标轴/绘图区相关元素内弹出
                    bool inAxisArea = hit.ChartElementType == ChartElementType.PlottingArea
                                      || hit.ChartElementType == ChartElementType.Axis
                                      || hit.ChartElementType == ChartElementType.Gridlines
                                      || hit.ChartElementType == ChartElementType.StripLines
                                      || hit.ChartElementType == ChartElementType.AxisLabels
                                      || hit.ChartElementType == ChartElementType.DataPoint;
                    if (inAxisArea)
                    {
                        UpdateContextMenuLocalization();
                        chartContextMenu.Show(chart, new System.Drawing.Point(e.X, e.Y));
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private void MenuSaveImage_Click(object sender, EventArgs e)
        {
            try
            {
                using (var sfd = new System.Windows.Forms.SaveFileDialog())
                {
                    // 标题与过滤
                    string title = Properties.Resources.ResourceManager.GetString("save_image", Properties.Resources.Culture);
                    if (string.IsNullOrWhiteSpace(title)) title = "Save Image";
                    sfd.Title = title;
                    sfd.Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp";
                    sfd.AddExtension = true;
                    sfd.OverwritePrompt = true;
                    sfd.FileName = "chart.png";

                    if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        var format = System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Png;
                        string ext = System.IO.Path.GetExtension(sfd.FileName)?.ToLowerInvariant();
                        if (ext == ".jpg" || ext == ".jpeg") format = System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Jpeg;
                        else if (ext == ".bmp") format = System.Windows.Forms.DataVisualization.Charting.ChartImageFormat.Bmp;
                        chartDeltaRn.SaveImage(sfd.FileName, format);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 显示tooltip信息
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChartDeltaRn_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (!(sender is Chart chart)) return;

            // 将鼠标位置转换为图表坐标系中的点
            System.Windows.Forms.DataVisualization.Charting.HitTestResult result = chart.HitTest(e.X, e.Y);

            // 检查是否悬停在数据点上
            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                DataPoint dataPoint = result.Series.Points[result.PointIndex];

                // 显示数据点的信息
                double yVal = Math.Truncate(dataPoint.YValues[0] * 10) / 10;
                int xVal = (int)dataPoint.XValue;
                string[] arr = result.Series.Name.Split('-');
                int tubeIdx = 0;
                try { tubeIdx = int.Parse(arr[1]); } catch { }
                string dockUnit = Tools.GetDockUnit(tubeIdx);
                string info = $"{dockUnit}  {arr[0]}: {yVal}, {xVal}";
                tooltip.Show(info, chart, e.X + 20, e.Y + 20);
            }
            else
            {
                tooltip.Hide(chart);
            }
        }

        /// <summary>
        /// 处理全局事件
        /// </summary>
        /// <param name="obj"></param>
        private void EventBus_OnMessageReceived(NotificationMessage obj)
        {
            switch (obj.Code)
            {
                case MessageCode.PcrKeyStatus:
                    {
                        //LogHelper.Debug((object)("DataAnalyse收到消息: " + obj.Code));

                        context.Post(_ => { RefreshSampleUC(sampleList, true); }, null);
                    }
                    break;
                case MessageCode.RefreshUI:
                    {
                        //LogHelper.Debug((object)("DataAnalyse收到消息: " + obj.Code));

                        // 刷新按钮状态
                        { context.Post(_ => { RefreshSampleUC(sampleList, true); }, null); }
                    }
                    break;
            }
        }

        /// <summary>
        /// 点击样本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_ClickEventTick(SampleUC sender, bool click)
        {
            int type = 1;
            ComboBoxItem selectedItem = cmbCurveType.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                type = int.Parse(selectedItem.Tag.ToString());
            }

            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                selectList.Clear();

                selectList.Add(sender);
            }
            else
            {
                // 多选
                var sam = selectList.Where(s => s.Index == sender.Index).FirstOrDefault();
                if (sam != null)
                {
                    selectList.Remove(sam);
                }
                else
                {
                    selectList.Add(sender);
                }
            }

            ChangeSelectBg(selectList, sampleList);
            LoadCurvesDataForSelected(type);
        }

        /// <summary>
        /// 点击样本下方按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_StartClickEventHandler(SampleUC sender, bool click)
        {

        }

        /// <summary>
        /// 点击X轴
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int lastCount = selectList.Count;

            selectList.Clear();
            string text = (sender as Label).Content.ToString();
            AddSampleAxis(text, selectList, sampleList, lastCount);

            ChangeSelectBg(selectList, sampleList);
            // 选中轴后，同步加载曲线
            int type = GetCurveType();
            LoadCurvesDataForSelected(type);
        }

        /// <summary>
        /// 曲线切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbCurveType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                return;
            }

            int tubeIndex = GetActiveTubeIndex();

            foreach (var s in chartDeltaRn.Series)
            {
                if (s.Name != "Default")
                {
                    s.Points.Clear();
                }
            }

            string tag = selectedItem.Tag.ToString();
            if (tag == "1")
            {
                // Y轴
                chartDeltaRn.ChartAreas[0].AxisY.Title = "Rn";
                //chartArea.AxisY.Interval = 500;
                AxisDefault(1);
                LoadCurvesDataForSelected(1);
                ThresholdVisbility(true);
            }
            else if (tag == "2")
            {
                // Y轴
                chartDeltaRn.ChartAreas[0].AxisY.Title = "Rn";
                //chartArea.AxisY.Interval = 0.1;
                AxisDefault(2);

                LoadCurvesDataForSelected(2);
                ThresholdVisbility(true);
            }
            else if (tag == "3")
            {
                // Y轴
                chartDeltaRn.ChartAreas[0].AxisY.Title = "Fn";
                //chartArea.AxisY.Interval = 500;
                AxisDefault(3);

                LoadCurvesDataForSelected(3);
                ThresholdVisbility(false);
            }
        }

        private void AxisDefault(int type)
        {
            foreach (var item in chartDeltaRn.Series)
            {
                if (item.Name != "Default")
                {
                    item.Points.Clear();
                }
            }

            ChartArea chartArea = chartDeltaRn.ChartAreas[0];
            chartArea.AxisX.Interval = 3;
            chartArea.AxisX.Minimum = 1;
            chartArea.AxisX.Maximum = maxAxisXValue;

            if (manualAxis)
            {
                if (manualXMin.HasValue) chartArea.AxisX.Minimum = manualXMin.Value;
                if (manualXMax.HasValue) chartArea.AxisX.Maximum = manualXMax.Value;
                if (manualYMin.HasValue) chartArea.AxisY.Minimum = manualYMin.Value;
                if (manualYMax.HasValue) chartArea.AxisY.Maximum = manualYMax.Value;
                return;
            }

            if (type == 1)
            {
                chartArea.AxisY.Minimum = -1;
                chartArea.AxisY.Maximum = maxAxisYValue; // 初值
            }
            else if (type == 2)
            {
                chartArea.AxisY.Minimum = 0.0;
                chartArea.AxisY.Maximum = 1.0;
            }
            else
            {
                chartArea.AxisY.Minimum = -1;
                chartArea.AxisY.Maximum = maxAxisYValue; // 初值
            }
        }

        /// <summary>
        /// 获取选中的曲线类型中最大的值
        /// </summary>
        /// <param name="tubeIndex"></param>
        /// <returns></returns>      
        private (double, double) GetSelectedNormalizedMaxData(List<double> normalizedFAM,
            List<double> normalizedCy5, List<double> normalizedVIC, List<double> normalizedCy55,
            List<double> normalizedROX)
        {
            List<double> valuesMax = new List<double>();
            List<double> valuesMin = new List<double>();
            if (selectCurvesType.Contains("FAM") && normalizedFAM.Count > 0)
            {
                valuesMax.Add(normalizedFAM.Max());
                valuesMin.Add(normalizedFAM.Min());
            }

            if (selectCurvesType.Contains("Cy5") && normalizedCy5.Count > 0)
            {
                valuesMax.Add(normalizedCy5.Max());
                valuesMin.Add(normalizedCy5.Min());
            }

            if (selectCurvesType.Contains("VIC") && normalizedVIC.Count > 0)
            {
                valuesMax.Add(normalizedVIC.Max());
                valuesMin.Add(normalizedVIC.Min());
            }

            if (selectCurvesType.Contains("Cy55") && normalizedCy55.Count > 0)
            {
                valuesMax.Add(normalizedCy55.Max());
                valuesMin.Add(normalizedCy55.Min());
            }

            if (selectCurvesType.Contains("ROX") && normalizedROX.Count > 0)
            {
                valuesMax.Add(normalizedROX.Max());
                valuesMin.Add(normalizedROX.Min());
            }

            double yMax = valuesMax.Count > 0 ? valuesMax.Max() : 1;

            double yMin = valuesMin.Count > 0 ? valuesMin.Min() : 0;

            return (yMax, yMin);
        }

        /// <summary>
        /// 动态设置x轴 y轴
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="dataY"></param>
		private void UpdateAxisXY(double dataX, double dataY, double yMin = 0)
        {
            try
            {
                ChartArea chartArea = chartDeltaRn.ChartAreas[0];

                // 手动模式：不再自动调整轴范围
                if (manualAxis)
                {
                    if (manualXMin.HasValue) chartArea.AxisX.Minimum = manualXMin.Value;
                    if (manualXMax.HasValue) chartArea.AxisX.Maximum = manualXMax.Value;
                    if (manualYMin.HasValue) chartArea.AxisY.Minimum = manualYMin.Value;
                    if (manualYMax.HasValue) chartArea.AxisY.Maximum = manualYMax.Value;
                    return;
                }

                // X 轴轮次
                if (dataX > chartDeltaRn.ChartAreas[0].AxisX.Maximum)
                {
                    double max = Math.Ceiling(dataX * 1.2);
                    double it = Math.Max(1, Math.Floor(max / 10));

                    Console.WriteLine("===> x 轴 max={0}, Interval={1}", max, it);

                    chartArea.AxisX.Maximum = max;
                    chartArea.AxisX.Interval = it;
                }
                else
                {
                    if (dataX < maxAxisXValue)
                    {
                        chartArea.AxisX.Maximum = maxAxisXValue;
                        chartArea.AxisX.Interval = 3;
                    }
                }

                // Y 轴 max（批量渲染时只放大不缩小，避免多选时被后续曲线压缩）
                double headroom = Math.Max(200, Math.Abs(dataY) * 0.10);
                double desiredMax = Math.Max(1, (int)Math.Round(dataY + headroom));
                if (desiredMax > chartDeltaRn.ChartAreas[0].AxisY.Maximum)
                {
                    chartDeltaRn.ChartAreas[0].AxisY.Maximum = desiredMax;
                }
                else if (!isBatchRendering)
                {
                    if (desiredMax < chartDeltaRn.ChartAreas[0].AxisY.Maximum)
                    {
                        chartDeltaRn.ChartAreas[0].AxisY.Maximum = desiredMax;
                    }
                }

                // Y 轴 min
                if (yMin >= 0)
                {
                    chartArea.AxisY.Minimum = -1;
                }
                else
                {
                    chartArea.AxisY.Minimum = Tools.CalculateYMin(yMin);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 更新曲线数据
        /// </summary>
        /// <param name="name"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="dataY"></param>
        private void UpdateSeries(string name, List<double> X, List<double> Y, double dataY, double yMin = 0)
        {
            try
            {
                ChartArea chartArea = chartDeltaRn.ChartAreas[0];

                // 根据管位行别裁剪显示轮次：
                // Row A（0行）：左移2轮，仅显示58轮
                // Row B（1行）：左移1轮，仅显示59轮
                // Row C（2行）：不变
                List<double> xToBind = X;
                List<double> yToBind = Y;
                try
                {
                    string[] arr = name.Split('-'); // e.g. "FAM-7"
                    int tubeIdx = int.Parse(arr[1]);
                    int row = tubeIdx / 6; // 0:A,1:B,2:C
                    int skip = 0;
                    int maxKeep = int.MaxValue;
                    if (row == 0)
                    {
                        skip = 2;
                        maxKeep = 58;
                    }
                    else if (row == 1)
                    {
                        skip = 1;
                        maxKeep = 59;
                    }

                    if (skip > 0 || maxKeep != int.MaxValue)
                    {
                        int available = Math.Max(0, Math.Min(Y.Count - skip, maxKeep));
                        if (available > 0)
                        {
                            // 取从 skip 开始的 available 个点，并将 X 重新标为 1..available，实现“左移”
                            yToBind = Y.Skip(skip).Take(available).ToList();
                            xToBind = Enumerable.Range(1, available).Select(i => (double)i).ToList();
                        }
                        else
                        {
                            yToBind = new List<double>();
                            xToBind = new List<double>();
                        }
                    }
                }
                catch { }

                double dataX = xToBind.Count;

                UpdateAxisXY(dataX, dataY, yMin);

                chartDeltaRn.Series[name].Points.Clear();
                chartDeltaRn.Series[name].Points.DataBindXY(xToBind, yToBind);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 显示隐藏曲线
        /// </summary>
        /// <param name="index"></param>
		private void ShowSeries()
        {
            foreach (var item in chartDeltaRn.Series)
            {
                string[] arr = item.Name.Split('-');
                if (item.Name == "Default")
                {
                    item.Enabled = true;
                }
                else
                {
                    int tubeIdx = int.Parse(arr[1]);
                    item.Enabled = activeCurveIndices.Contains(tubeIdx) && selectCurvesType.Contains(arr[0]);
                }
            }
        }

        private void LoadCurvesDataForSelected(int type)
        {
            IEnumerable<int> candidateIndices;
            if (selectList.Count == 0)
            {
                candidateIndices = new int[] { GetActiveTubeIndex() };
            }
            else
            {
                candidateIndices = selectList.OrderBy(s => s.Index).Select(s => s.Index).ToArray();
            }

            activeCurveIndices.Clear();
            ctRows.Clear();
            foreach (var idx in candidateIndices)
            {
                var status = GlobalData.GetStatus(idx);
                if ((status == TUBE_STATUS.Lighting || status == TUBE_STATUS.LightingPaused || status == TUBE_STATUS.LightingCompleted)
                    && GlobalData.DataFAMX[idx].Count > 0)
                {
                    activeCurveIndices.Add(idx);
                }
            }

            if (activeCurveIndices.Count == 0)
            {
                // 没有可渲染的数据，按类型重置坐标轴并退出
                AxisDefault(type);
                ShowSeries();
                try { chartDeltaRn.Invalidate(); chartDeltaRn.Update(); } catch { }
                return;
            }

            ShowSeries();
            AxisDefault(type);
            isBatchRendering = true;
            ApplyGlobalAxisForSelection(type);
            foreach (var idx in activeCurveIndices.OrderBy(i => i))
            {
                LoadCurvesData(idx, type);
            }
            isBatchRendering = false;
            // 先根据可见点修正一次坐标（若此时没有点，回退到全局估计）
            bool adjusted = AdjustYAxisByVisibleSeries(type);
            if (!adjusted) { ApplyGlobalAxisForSelection(type); }
            try { chartDeltaRn.Invalidate(); chartDeltaRn.Update(); } catch { }
        }

        private bool AdjustYAxisByVisibleSeries(int type)
        {
            try
            {
                if (manualAxis) return true; // 手动模式下不自动调整
                if (type == 2) return true; // 归一化固定 0..1
                double yMax = double.MinValue;
                double yMin = double.MaxValue;
                bool hasPoint = false;
                foreach (var s in chartDeltaRn.Series)
                {
                    if (s.Name == "Default" || !s.Enabled || s.Points.Count == 0) continue; // 仅统计可见系列
                    foreach (var p in s.Points)
                    {
                        double v = p.YValues != null && p.YValues.Length > 0 ? p.YValues[0] : 0;
                        if (!hasPoint) { yMax = v; yMin = v; hasPoint = true; }
                        else { if (v > yMax) yMax = v; if (v < yMin) yMin = v; }
                    }
                }
                if (hasPoint)
                {
                    var area = chartDeltaRn.ChartAreas[0];
                    // 动态留白：最大值上方保留 max(200, 10%) 的空间
                    double headroom = Math.Max(200, Math.Abs(yMax) * 0.10);
                    area.AxisY.Maximum = Math.Max(1, (int)Math.Round(yMax + headroom));
                    if (yMin >= 0)
                    {
                        area.AxisY.Minimum = -1;
                    }
                    else
                    {
                        area.AxisY.Minimum = Tools.CalculateYMin(yMin);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
            return false;
        }

        private void ApplyGlobalAxisForSelection(int type)
        {
            try
            {
                if (manualAxis) return; // 手动模式，不应用全局估计
                if (type == 2)
                {
                    // 归一化固定 0..1
                    return;
                }
                List<double> yMaxs = new List<double>();
                List<double> yMins = new List<double>();
                int xMax = maxAxisXValue;
                int[] indices = activeCurveIndices.Count > 0 ? activeCurveIndices.ToArray() : new int[] { GetActiveTubeIndex() };

                // 优先使用导入后预计算的建议 Y 轴最大值，避免首次全选卡在 5000
                if (type == 1 || type == 3)
                {
                    List<double> cached = new List<double>();
                    foreach (var idx in indices)
                    {
                        double v = (type == 1) ? suggestedYMaxType1[idx] : suggestedYMaxType3[idx];
                        if (v > 0) cached.Add(v);
                        xMax = Math.Max(xMax, GlobalData.DataFAMX[idx].Count);
                    }
                    if (cached.Count > 0)
                    {
                        ChartArea area = chartDeltaRn.ChartAreas[0];
                        area.AxisX.Maximum = Math.Max(maxAxisXValue, xMax);
                        area.AxisY.Maximum = cached.Max();
                        area.AxisY.Minimum = -1;
                        return;
                    }
                }
                foreach (var idx in indices)
                {
                    if (GlobalData.GetStatus(idx) != TUBE_STATUS.LightingCompleted || GlobalData.DataFAMX[idx].Count == 0) continue;
                    xMax = Math.Max(xMax, GlobalData.DataFAMX[idx].Count);
                    if (type == 1)
                    {
                        var (dtFAM, dtCy5, dtVIC, dtCy55, dtROX) = PcrAlgorigthm.DeltaRn(idx);
                        AggregateChannelExtents(dtFAM, dtCy5, dtVIC, dtCy55, dtROX, yMaxs, yMins);
                    }
                    else // type == 3
                    {
                        // 复用原始流程中的 baselineCorrected* 作为范围估计
                        double[] rawFAM = GlobalData.DataFAMY[idx].ToArray();
                        double[] rawCy5 = GlobalData.DataCy5Y[idx].ToArray();
                        double[] rawHEX = GlobalData.DataVICY[idx].ToArray();
                        double[] rawCy5_5 = GlobalData.DataCy55Y[idx].ToArray();
                        double[] rawROX = GlobalData.DataROXY[idx].ToArray();
                        double[] mot = GlobalData.DataMOTY[idx].ToArray();
                        var (cF, cCy5, cHEX, cCy55, cROX, cMOT) = PcrAlgorigthm.CrosstlkCorrection(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, mot);
                        var (fF, fCy5, fHEX, fCy55, fROX, fMot) = PcrAlgorigthm.MedianFiltering(cF, cCy5, cHEX, cCy55, cROX, cMOT);
                        var (bF, bCy5, bHEX, bCy55, bROX, ct) = PcrAlgorigthm.BaselineAdjust(fF, fCy5, fHEX, fCy55, fROX, fMot);
                        // 这里必须传入 bCy55 而不是 bCy5
                        AggregateChannelExtents(bF.ToList(), bCy5.ToList(), bHEX.ToList(), bCy55.ToList(), bROX.ToList(), yMaxs, yMins);
                    }
                }
                ChartArea chartArea = chartDeltaRn.ChartAreas[0];
                chartArea.AxisX.Maximum = Math.Max(maxAxisXValue, xMax);
                if (yMaxs.Count > 0)
                {
                    double ymax = yMaxs.Max();
                    double ymin = yMins.Count > 0 ? yMins.Min() : 0;
                    // 动态留白：最大值上方保留 max(200, 10%) 的空间
                    double headroom = Math.Max(200, Math.Abs(ymax) * 0.10);
                    chartArea.AxisY.Maximum = Math.Max(1, (int)Math.Round(ymax + headroom));
                    if (ymin >= 0)
                    {
                        chartArea.AxisY.Minimum = -1;
                    }
                    else
                    {
                        chartArea.AxisY.Minimum = Tools.CalculateYMin(ymin);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private void AggregateChannelExtents(IList<double> fam, IList<double> cy5, IList<double> vic, IList<double> cy55, IList<double> rox, List<double> yMaxs, List<double> yMins)
        {
            bool includeFAM = selectCurvesType.Contains("FAM");
            bool includeCy5 = selectCurvesType.Contains("Cy5");
            bool includeVIC = selectCurvesType.Contains("VIC");
            bool includeCy55 = selectCurvesType.Contains("Cy55");
            bool includeROX = selectCurvesType.Contains("ROX");
            if (includeFAM && fam != null && fam.Count > 0) { yMaxs.Add(fam.Max()); yMins.Add(fam.Min()); }
            if (includeCy5 && cy5 != null && cy5.Count > 0) { yMaxs.Add(cy5.Max()); yMins.Add(cy5.Min()); }
            if (includeVIC && vic != null && vic.Count > 0) { yMaxs.Add(vic.Max()); yMins.Add(vic.Min()); }
            if (includeCy55 && cy55 != null && cy55.Count > 0) { yMaxs.Add(cy55.Max()); yMins.Add(cy55.Min()); }
            if (includeROX && rox != null && rox.Count > 0) { yMaxs.Add(rox.Max()); yMins.Add(rox.Min()); }
        }

        private void LoadCurvesData(int tubeIndex, int type)
        {
            // 只显示选中的
            ShowSeries();

            if (GlobalData.DataFAMX[tubeIndex].Count == 0)
            {
                LogHelper.Debug("试管 {0} 没有数据", tubeIndex);
                AxisDefault(type);
                return;
            }

            double[] currentCt = new double[5];

            if (type == 1)
            {
                int threshold = GetThreshold();
                // DeltaRn曲线
                var (dtFAM, dtCy5, dtVIC, dtCy55, dtROX) = PcrAlgorigthm.DeltaRn(tubeIndex, threshold);
                List<double> deltaFAM = dtFAM.ToList();
                List<double> deltaCy5 = dtCy5.ToList();
                List<double> deltaVIC = dtVIC.ToList();
                List<double> deltaCy55 = dtCy55.ToList();
                List<double> deltaROX = dtROX.ToList();

                var (y, yMin) = GetSelectedNormalizedMaxData(deltaFAM, deltaCy5,
                    deltaVIC, deltaCy55, deltaROX);

                UpdateSeries("FAM-" + tubeIndex, GlobalData.DataFAMX[tubeIndex], deltaFAM, y, yMin);
                UpdateSeries("Cy5-" + tubeIndex, GlobalData.DataCy5X[tubeIndex], deltaCy5, y, yMin);
                UpdateSeries("VIC-" + tubeIndex, GlobalData.DataVICX[tubeIndex], deltaVIC, y, yMin);
                UpdateSeries("Cy55-" + tubeIndex, GlobalData.DataCy55X[tubeIndex], deltaCy55, y, yMin);
                UpdateSeries("ROX-" + tubeIndex, GlobalData.DataROXX[tubeIndex], deltaROX, y, yMin);

                // 从 TubeData 读取CT（DeltaRn流程中已在 BaselineAdjust/MotCalibration 阶段写入）
                for (int i = 0; i < 5; i++)
                {
                    currentCt[i] = GlobalData.TubeDatas[tubeIndex].GetCT(i);
                }
                // Debug 兜底：若 Ct 尚未写入（0 或 NaN），即时计算一次并重取
                bool ctInvalid = true;
                for (int i = 0; i < 5; i++)
                {
                    if (!double.IsNaN(currentCt[i]) && currentCt[i] > 0)
                    {
                        ctInvalid = false; break;
                    }
                }
                if (ctInvalid)
                {
                    try
                    {
                        var _ = PcrAlgorigthm.DeltaRn(tubeIndex, threshold);
                        for (int i = 0; i < 5; i++)
                        {
                            currentCt[i] = GlobalData.TubeDatas[tubeIndex].GetCT(i);
                        }
                    }
                    catch { }
                }
            }
            else if (type == 2)
            {
                int threshold = GetThreshold();

                // 归一化曲
                var (normalizedFAM,
                     normalizedCy5,
                     normalizedVIC,
                     normalizedCy55,
                     normalizedROX
                    ) = PcrAlgorigthm.Normalized(tubeIndex, threshold);

                var (y, yMin) = GetSelectedNormalizedMaxData(normalizedFAM, normalizedCy5,
                    normalizedVIC, normalizedCy55, normalizedROX);

                UpdateSeries("FAM-" + tubeIndex, GlobalData.DataFAMX[tubeIndex], normalizedFAM, y, yMin);
                UpdateSeries("Cy5-" + tubeIndex, GlobalData.DataCy5X[tubeIndex], normalizedCy5, y, yMin);
                UpdateSeries("VIC-" + tubeIndex, GlobalData.DataVICX[tubeIndex], normalizedVIC, y, yMin);
                UpdateSeries("Cy55-" + tubeIndex, GlobalData.DataCy55X[tubeIndex], normalizedCy55, y, yMin);
                UpdateSeries("ROX-" + tubeIndex, GlobalData.DataROXX[tubeIndex], normalizedROX, y, yMin);

                for (int i = 0; i < 5; i++)
                {
                    currentCt[i] = GlobalData.TubeDatas[tubeIndex].GetCT(i);
                }
                // Debug 兜底：若 Ct 尚未写入（0 或 NaN），即时计算一次并重取
                bool ctInvalid2 = true;
                for (int i = 0; i < 5; i++)
                {
                    if (!double.IsNaN(currentCt[i]) && currentCt[i] > 0)
                    {
                        ctInvalid2 = false; break;
                    }
                }
                if (ctInvalid2)
                {
                    try
                    {
                        var _ = PcrAlgorigthm.DeltaRn(tubeIndex);
                        for (int i = 0; i < 5; i++)
                        {
                            currentCt[i] = GlobalData.TubeDatas[tubeIndex].GetCT(i);
                        }
                    }
                    catch { }
                }
            }
            else
            {
                // 原始扩增曲线
                double[] rawFAM = GlobalData.DataFAMY[tubeIndex].ToArray();
                double[] rawCy5 = GlobalData.DataCy5Y[tubeIndex].ToArray();
                double[] rawHEX = GlobalData.DataVICY[tubeIndex].ToArray();
                double[] rawCy5_5 = GlobalData.DataCy55Y[tubeIndex].ToArray();
                double[] rawROX = GlobalData.DataROXY[tubeIndex].ToArray();
                double[] mot = GlobalData.DataMOTY[tubeIndex].ToArray();

                // === Step 1: Crosstalk Correction ===         
                var (correctedFAM,
                    correctedCy5,
                    correctedHEX,
                    correctedCy5_5,
                    correctedROX,
                    correctedMOT
                    ) = PcrAlgorigthm.CrosstlkCorrection(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, mot);

                // === Step 2: Median Filtering ===
                var (filteredFAM, filteredCy5, filteredHEX,
                    filteredCy5_5, filteredROX, filtermot
                    ) = PcrAlgorigthm.MedianFiltering(correctedFAM, correctedCy5, correctedHEX, correctedCy5_5, correctedROX, correctedMOT);

                // === Step 3: Baseline Correction ===
                var (
                    baselineCorrectedFAM,
                    baselineCorrectedCy5,
                    baselineCorrectedHEX,
                    baselineCorrectedCy5_5,
                    baselineCorrectedROX,
                    ct
                    ) = PcrAlgorigthm.BaselineAdjust(filteredFAM, filteredCy5, filteredHEX,
                    filteredCy5_5, filteredROX, filtermot);

                // === Step 4: MOT Calibration ===
                var (motCalibratedFAM, motCalibratedCy5, motCalibratedHEX, motCalibratedCy5_5, motCalibratedROX, ctValue)
                    = PcrAlgorigthm.MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX,
                    baselineCorrectedCy5_5, baselineCorrectedROX, mot, ct);

                // === Step 5: Smoothed ===
                var (smoothedFAM, smoothedCy5, smoothedHEX, smoothedCy5_5, smoothedROX)
                    = PcrAlgorigthm.SmoothData(motCalibratedFAM, motCalibratedCy5, motCalibratedHEX, motCalibratedCy5_5, motCalibratedROX);


                var (y, yMin) = GetSelectedNormalizedMaxData(
                    baselineCorrectedFAM.ToList(),
                    baselineCorrectedCy5.ToList(),
                    baselineCorrectedHEX.ToList(),
                    baselineCorrectedCy5_5.ToList(),
                    baselineCorrectedROX.ToList());

                UpdateSeries("FAM-" + tubeIndex, GlobalData.DataFAMX[tubeIndex], smoothedFAM.ToList(), y, yMin);
                UpdateSeries("Cy5-" + tubeIndex, GlobalData.DataCy5X[tubeIndex], smoothedCy5.ToList(), y, yMin);
                UpdateSeries("VIC-" + tubeIndex, GlobalData.DataVICX[tubeIndex], smoothedHEX.ToList(), y, yMin);
                UpdateSeries("Cy55-" + tubeIndex, GlobalData.DataCy55X[tubeIndex], smoothedCy5_5.ToList(), y, yMin);
                UpdateSeries("ROX-" + tubeIndex, GlobalData.DataROXX[tubeIndex], smoothedROX.ToList(), y, yMin);

                // 使用 BaselineAdjust 返回的 ct 数组
                for (int i = 0; i < 5; i++)
                {
                    currentCt[i] = ct[i];
                }
            }

            // 更新标准曲线区域下方的Ct显示
            try
            {
                // 再次应用一次可见性，确保在所有通道数据绑定后根据通道选择生效
                ShowSeries();
                try { chartDeltaRn.Invalidate(); chartDeltaRn.Update(); } catch { }
                // 已按需求隐藏下方五条Ct文本提示

                // 追加到下方表格：将所选孔位的五种通道Ct按行罗列
                AppendCtRowsToGrid(tubeIndex, currentCt);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

        }

        private int GetThreshold()
        {
            try
            {
                string text = txtThreshold?.Text ?? "";
                // 允许小数、空格；取整并限制范围
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double dv))
                {
                    int v = (int)Math.Round(dv);
                    if (v < 0) v = 0;
                    if (v > 5000000) v = 5000000; // 上限保护
                    return v;
                }
            }
            catch { }
            // 兜底默认阈值
            return 100;
        }

        private class CtRow
        {
            public int Seq { get; set; }
            public string Well { get; set; }
            public string SampleType { get; set; }
            public string SampleId { get; set; }
            public string Channel { get; set; }
            public string Ct { get; set; }
        }

        private readonly List<CtRow> ctRows = new List<CtRow>();

        private void AppendCtRowsToGrid(int tubeIndex, double[] currentCt)
        {
            try
            {
                string well = Tools.GetDockUnit(tubeIndex);
                int typeId = GlobalData.DS.HeatSampleType[tubeIndex];
                string sampleType = VarDef.SampleType.ContainsKey(typeId) ? VarDef.SampleType[typeId][0] : "UN";
                string sampleId = GlobalData.DS.HeatSampleID[tubeIndex] ?? string.Empty;
                string[] channels = new string[] { "FAM", "Cy5", "VIC", "Cy55", "ROX" };
                for (int i = 0; i < 5; i++)
                {
                    ctRows.Add(new CtRow
                    {
                        Seq = ctRows.Count + 1,
                        Well = well,
                        SampleType = sampleType,
                        SampleId = sampleId,
                        Channel = channels[i],
                        Ct = FormatCt(currentCt[i])
                    });
                }
                dataGrid.ItemsSource = null;
                dataGrid.ItemsSource = ctRows;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 下拉选择曲线类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void cmbFAM_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem typeCBI = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (typeCBI != null)
            {
                string tag = typeCBI.Tag?.ToString() ?? string.Empty;
                selectCurvesType.Clear();
                // 兼容 Debug 生成工件异常：若 Tag 被编译为 "All"，同样当作全通道
                if (string.Equals(tag, "MOT", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(tag, "ALL", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(typeCBI.Content?.ToString(), "All", StringComparison.OrdinalIgnoreCase))
                {
                    selectCurvesType.UnionWith(new string[] { "FAM", "Cy5", "VIC", "Cy55", "ROX" });
                }
                else
                {
                    selectCurvesType.Add(tag);
                }
            }

            // 渠道切换后按当前选择的孔位整体刷新（含全选场景），以触发全局坐标计算
            int type = GetCurveType();
            LoadCurvesDataForSelected(type);
        }

        private int GetActiveTubeIndex()
        {
            // 优先在已选样本中找到第一个有数据的试管
            if (selectList.Count >= 1)
            {
                foreach (var s in selectList.OrderBy(s => s.Index))
                {
                    int i = s.Index;
                    var status = GlobalData.GetStatus(i);
                    if ((status == TUBE_STATUS.Lighting || status == TUBE_STATUS.LightingPaused || status == TUBE_STATUS.LightingCompleted)
                        && GlobalData.DataFAMX[i].Count > 0)
                    {
                        return i;
                    }
                }
                // 若已选中但都没数据，则退回到选中第一个
                return selectList.First().Index;
            }
            // 否则，从全局中找第一个有数据的试管
            for (int i = 0; i < 18; i++)
            {
                var status = GlobalData.GetStatus(i);
                if ((status == TUBE_STATUS.Lighting || status == TUBE_STATUS.LightingPaused || status == TUBE_STATUS.LightingCompleted)
                    && GlobalData.DataFAMX[i].Count > 0)
                {
                    return i;
                }
            }
            return 0;
        }

        private int GetCurveType()
        {
            int type = 1;
            ComboBoxItem selectedItem = cmbCurveType.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                type = int.Parse(selectedItem.Tag.ToString());
            }

            return type;
        }

        // 外部强制刷新当前选择的曲线（用于参数变更后立即生效）
        public void ForceRefreshCurves()
        {
            try
            {
                int type = GetCurveType();
                LogHelper.Info("ForceRefreshCurves type={0} selectedChannels={1}", type, string.Join(",", selectCurvesType));
                LoadCurvesDataForSelected(type);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 导入数据文件进行分析
        /// </summary>
        public void OpenDataFileDialog()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "Excel|*.xlsx";

            ConfigCache configCache = CacheFileUtil.Read();
            if (configCache != null && !string.IsNullOrEmpty(configCache.DataPath) && Directory.Exists(configCache.DataPath))
            {
                openFileDialog.InitialDirectory = configCache.DataPath;
            }

            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == true)
            {
                IWorkbook workbook;

                string filename = openFileDialog.FileName;
                List<DataTable> dataTables = new List<DataTable>();
                List<string> tubeNames = new List<string>();

                try
                {
                    // 允许在 Excel 打开文件时也能读取（共享读写）
                    using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        // 读取文件
                        // Cycle
                        // FAM(CT=), FAM(Filter), FAM(DeltaRn), FAM(Normalize)
                        // Cy5(CT=), Cy5(Filter), Cy5(DeltaRn), Cy5(Normalize)
                        // VIC(CT=), VIC(Filter), VIC(DeltaRn), VIC(Normalize)
                        // Cy5.5(CT=), Cy5.5(Filter), Cy5.5(DeltaRn), Cy5.5(Normalize)
                        // ROX(CT=), ROX(Filter), ROX(DeltaRn), ROX(Normalize)

                        workbook = new XSSFWorkbook(fs);
                        for (int i = 0; i < workbook.NumberOfSheets; i++)
                        {
                            string rawName = workbook.GetSheetName(i);
                            string sName = NormalizeDockSheetName(rawName);

                            ISheet sheet = workbook.GetSheetAt(i);
                            DataTable dt = ExcelHelper.SheetToDatatable(sheet);

                            if (dt == null || dt.Columns.Count < 21)
                            {
                                throw new Exception("The contents of the file are incorrect");
                            }

                            // 删除调试输出，避免某些环境下触发格式化异常

                            int tubeIndex = Tools.GetDockIndex(sName);
                            if (tubeIndex < 0 || tubeIndex > 17)
                            {
                                throw new Exception("The contents of the file are incorrect");
                            }

                            dataTables.Add(dt);
                            tubeNames.Add(sName);
                        }
                    }

                    if (dataTables.Count == 0)
                    {
                        throw new Exception("The contents of the file are incorrect");
                    }

                    for (int i = 0; i < dataTables.Count; i++)
                    {
                        PutImportData(dataTables[i], tubeNames[i]);
                    }

                    // 导入完成后预计算各孔位的建议 Y 轴最大值（type1/3: 最大值 + 5000）
                    PrecomputeSuggestedAxisYMax();

                    // 显示第一试管数据
                    int firstTubeIndex = Tools.GetDockIndex(tubeNames[0]);
                    sampleList[firstTubeIndex].TriggerCustomEvent();

                    // 触发一次曲线加载以刷新Ct显示
                    int type = GetCurveType();
                    LoadCurvesData(firstTubeIndex, type);
                }
                catch (Exception ex)
                {
                    LogHelper.Error(ex);

                    string msg = Properties.Resources.file_cannot_access;
                    if (ex is IOException)
                    {
                        msg += "\n\nPossible reason: The file is opened by another program.";
                    }
                    else if (ex.Message?.IndexOf("incorrect", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        msg += "\n\nPossible reason: Sheet name must start with A/B/C+1-6 (e.g., B5), and the first row must be header with ≥21 columns.";
                    }

                    MyMessageBox.Show(msg,
                        MyMessageBox.CustomMessageBoxButton.OK,
                        MyMessageBox.CustomMessageBoxIcon.Warning);
                }
            }
        }

        /// <summary>
        /// 兼容形如 "B5-2025-..." 的工作表名，提取前缀孔位（A1..C6）
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private static string NormalizeDockSheetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            // 捕获以 A/B/C 开头、紧随 1-6 的前缀
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(name.Trim(), @"^([ABCabc])\s*([1-6])\b");
            if (m.Success)
            {
                char row = char.ToUpper(m.Groups[1].Value[0]);
                string col = m.Groups[2].Value;
                return $"{row}{col}";
            }

            return name;
        }

        private void PutImportData(DataTable dt, string tubeName)
        {
            try
            {
                int tubeIndex = Tools.GetDockIndex(tubeName);

                // clear tube data
                ResetTubeData(tubeIndex);

                // 保存用户信息
                string[] infoData = new string[dt.Rows.Count];

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    GlobalData.DataFAMX[tubeIndex].Add(i + 1);
                    GlobalData.DataCy5X[tubeIndex].Add(i + 1);
                    GlobalData.DataVICX[tubeIndex].Add(i + 1);
                    GlobalData.DataCy55X[tubeIndex].Add(i + 1);
                    GlobalData.DataROXX[tubeIndex].Add(i + 1);
                    GlobalData.DataMOTX[tubeIndex].Add(i + 1);

                    double fam = SafeToDouble(dt.Rows[i], 1);
                    double cy5 = SafeToDouble(dt.Rows[i], 5);
                    double vic = SafeToDouble(dt.Rows[i], 9);
                    double cy55 = SafeToDouble(dt.Rows[i], 13);
                    double rox = SafeToDouble(dt.Rows[i], 17);
                    double mot = 0;
                    try
                    {
                        mot = SafeToDouble(dt.Rows[i], 21);
                    }
                    catch { }

                    try
                    {
                        infoData[i] = dt.Rows[i][22].ToString();
                    }
                    catch { }

                    GlobalData.DataFAMY[tubeIndex].Add(fam);
                    GlobalData.DataCy5Y[tubeIndex].Add(cy5);
                    GlobalData.DataVICY[tubeIndex].Add(vic);
                    GlobalData.DataCy55Y[tubeIndex].Add(cy55);
                    GlobalData.DataROXY[tubeIndex].Add(rox);
                    GlobalData.DataMOTY[tubeIndex].Add(mot);

                    GlobalData.TubeDatas[tubeIndex].AddOriginalData(0, (uint)Math.Max(0, fam));
                    GlobalData.TubeDatas[tubeIndex].AddOriginalData(1, (uint)Math.Max(0, cy5));
                    GlobalData.TubeDatas[tubeIndex].AddOriginalData(2, (uint)Math.Max(0, vic));
                    GlobalData.TubeDatas[tubeIndex].AddOriginalData(3, (uint)Math.Max(0, cy55));
                    GlobalData.TubeDatas[tubeIndex].AddOriginalData(4, (uint)Math.Max(0, rox));
                    GlobalData.TubeDatas[tubeIndex].AddOriginalData(5, (uint)Math.Max(0, mot));
                }

                GlobalData.SetStatus(tubeIndex, TUBE_STATUS.LightingCompleted);

                // info
                try
                {
                    if (!string.IsNullOrEmpty(infoData[0]))
                    {
                        GlobalData.DS.HeatPatientID[tubeIndex] = infoData[0];
                    }
                    if (!string.IsNullOrEmpty(infoData[1]))
                    {
                        GlobalData.DS.HeatSampleType[tubeIndex] = int.Parse(infoData[1]);
                    }
                }
                catch { }

                // Excel 导入完成后，计算一次并写入TubeData中的CT（调用DeltaRn流程会写入）
                try
                {
                    var _ = PcrAlgorigthm.DeltaRn(tubeIndex);
                }
                catch { }

                context.Post(_ => { RefreshSampleUC(sampleList, true); }, null);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 安全读取 DataRow 指定列为 double，失败返回 0
        /// </summary>
        private static double SafeToDouble(DataRow row, int columnIndex)
        {
            try
            {
                if (row == null) return 0;
                if (columnIndex < 0 || columnIndex >= row.Table.Columns.Count) return 0;
                object val = row[columnIndex];
                if (val == null) return 0;
                if (val is double d) return d;
                if (val is int i) return i;
                if (double.TryParse(val.ToString(), out double r)) return r;
            }
            catch { }
            return 0;
        }

        private string FormatCt(double ct)
        {
            if (double.IsNaN(ct) || ct <= 0) return "-";
            return ct.ToString("0.00");
        }

        private void SampleGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                isDraggingSelect = true;
                dragStartPoint = e.GetPosition(sampleGrid);
                dragStartSelectionSnapshot = (Keyboard.Modifiers == ModifierKeys.Control)
                    ? new HashSet<SampleUC>(selectList)
                    : new HashSet<SampleUC>();
                sampleGrid.CaptureMouse();
                if (selectionRect == null)
                {
                    selectionRect = new Rectangle()
                    {
                        Stroke = Tools.HexToBrush("#3b82f6"),
                        StrokeThickness = 1,
                        Fill = new SolidColorBrush(Color.FromArgb(40, 59, 130, 246))
                    };
                    selectionCanvas.Children.Add(selectionRect);
                }
                Canvas.SetLeft(selectionRect, dragStartPoint.Value.X);
                Canvas.SetTop(selectionRect, dragStartPoint.Value.Y);
                selectionRect.Width = 0;
                selectionRect.Height = 0;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private void PrecomputeSuggestedAxisYMax()
        {
            try
            {
                for (int i = 0; i < 18; i++)
                {
                    if (GlobalData.GetStatus(i) != TUBE_STATUS.LightingCompleted || GlobalData.DataFAMX[i].Count == 0)
                    {
                        suggestedYMaxType1[i] = 0;
                        suggestedYMaxType3[i] = 0;
                        continue;
                    }

                    // Type 1: DeltaRn 五通道取最大
                    var (dtFAM, dtCy5, dtVIC, dtCy55, dtROX) = PcrAlgorigthm.DeltaRn(i);
                    double y1 = 0;
                    if (dtFAM != null && dtFAM.Length > 0) y1 = Math.Max(y1, dtFAM.Max());
                    if (dtCy5 != null && dtCy5.Length > 0) y1 = Math.Max(y1, dtCy5.Max());
                    if (dtVIC != null && dtVIC.Length > 0) y1 = Math.Max(y1, dtVIC.Max());
                    if (dtCy55 != null && dtCy55.Length > 0) y1 = Math.Max(y1, dtCy55.Max());
                    if (dtROX != null && dtROX.Length > 0) y1 = Math.Max(y1, dtROX.Max());
                    if (y1 > 0)
                    {
                        double headroom1 = Math.Max(200, Math.Abs(y1) * 0.10);
                        suggestedYMaxType1[i] = Math.Max(1, (int)Math.Round(y1 + headroom1));
                    }
                    else
                    {
                        suggestedYMaxType1[i] = 0;
                    }

                    // Type 3: 原始扩增，使用基线校正后曲线的最大值
                    double[] rawFAM = GlobalData.DataFAMY[i].ToArray();
                    double[] rawCy5 = GlobalData.DataCy5Y[i].ToArray();
                    double[] rawHEX = GlobalData.DataVICY[i].ToArray();
                    double[] rawCy5_5 = GlobalData.DataCy55Y[i].ToArray();
                    double[] rawROX = GlobalData.DataROXY[i].ToArray();
                    double[] mot = GlobalData.DataMOTY[i].ToArray();

                    var (cF, cCy5, cHEX, cCy55, cROX, cMOT) = PcrAlgorigthm.CrosstlkCorrection(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, mot);
                    var (fF, fCy5, fHEX, fCy55, fROX, fMot) = PcrAlgorigthm.MedianFiltering(cF, cCy5, cHEX, cCy55, cROX, cMOT);
                    var (bF, bCy5, bHEX, bCy55, bROX, ct) = PcrAlgorigthm.BaselineAdjust(fF, fCy5, fHEX, fCy55, fROX, fMot);

                    double y3 = 0;
                    if (bF != null && bF.Length > 0) y3 = Math.Max(y3, bF.Max());
                    if (bCy5 != null && bCy5.Length > 0) y3 = Math.Max(y3, bCy5.Max());
                    if (bHEX != null && bHEX.Length > 0) y3 = Math.Max(y3, bHEX.Max());
                    if (bCy55 != null && bCy55.Length > 0) y3 = Math.Max(y3, bCy55.Max());
                    if (bROX != null && bROX.Length > 0) y3 = Math.Max(y3, bROX.Max());

                    if (y3 > 0)
                    {
                        double headroom3 = Math.Max(200, Math.Abs(y3) * 0.10);
                        suggestedYMaxType3[i] = Math.Max(1, (int)Math.Round(y3 + headroom3));
                    }
                    else
                    {
                        suggestedYMaxType3[i] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private void SampleGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isDraggingSelect || dragStartPoint == null) return;
            try
            {
                Point current = e.GetPosition(sampleGrid);
                double x = Math.Min(current.X, dragStartPoint.Value.X);
                double y = Math.Min(current.Y, dragStartPoint.Value.Y);
                double w = Math.Abs(current.X - dragStartPoint.Value.X);
                double h = Math.Abs(current.Y - dragStartPoint.Value.Y);
                Canvas.SetLeft(selectionRect, x);
                Canvas.SetTop(selectionRect, y);
                selectionRect.Width = w;
                selectionRect.Height = h;

                Rect sel = new Rect(x, y, w, h);
                HashSet<SampleUC> inRect = new HashSet<SampleUC>();
                foreach (var uc in sampleList)
                {
                    if (uc == null) continue;
                    Point tl = uc.TransformToAncestor(sampleGrid).Transform(new Point(0, 0));
                    Rect r = new Rect(tl, new Size(uc.ActualWidth, uc.ActualHeight));
                    if (sel.IntersectsWith(r))
                    {
                        inRect.Add(uc);
                    }
                }

                selectList.Clear();
                foreach (var uc in dragStartSelectionSnapshot)
                {
                    selectList.Add(uc);
                }
                foreach (var uc in inRect)
                {
                    selectList.Add(uc);
                }
                ChangeSelectBg(selectList, sampleList);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private void SampleGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (!isDraggingSelect) return;
                isDraggingSelect = false;
                dragStartPoint = null;
                dragStartSelectionSnapshot = null;
                sampleGrid.ReleaseMouseCapture();
                if (selectionRect != null)
                {
                    selectionRect.Width = 0;
                    selectionRect.Height = 0;
                }
                int type = GetCurveType();
                LoadCurvesDataForSelected(type);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 限制输入正整数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtThreshold_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int textBoxInt;
            try
            {
                textBoxInt = int.Parse($"{e.Text}");
            }
            catch (FormatException)
            {
                e.Handled = true;
            }
        }

        private void txtThreshold_LostFocus(object sender, RoutedEventArgs e)
        {
            int LostFocus;
            try
            {
                //转化为int类型，如果里面为空则会抓取错误，显示默认信息100；也可防止以0为开头的数字
                LostFocus = int.Parse(txtThreshold.Text);
                //将LostFocus转化为string类型，
                txtThreshold.Text = LostFocus.ToString();
            }
            catch (FormatException)
            {
                txtThreshold.Text = "100";
            }

            // 失去焦点后立即按当前类型刷新曲线
            try
            {
                int type = GetCurveType();
                LoadCurvesDataForSelected(type);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        private void btnThresholdSetting_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 打开轴设置窗口，初始为当前轴范围
                var area = chartDeltaRn.ChartAreas[0];
                double xMin = area.AxisX.Minimum;
                double xMax = area.AxisX.Maximum;
                double yMin = area.AxisY.Minimum;
                double yMax = area.AxisY.Maximum;

                AxisSettingsWindow win = new AxisSettingsWindow(xMin, xMax, yMin, yMax);
                win.Owner = Window.GetWindow(this);
                bool? ok = win.ShowDialog();
                if (ok == true)
                {
                    // 应用并进入手动模式
                    manualAxis = true;
                    manualXMin = win.XMin ?? xMin;
                    manualXMax = win.XMax ?? xMax;
                    manualYMin = win.YMin ?? yMin;
                    manualYMax = win.YMax ?? yMax;

                    // 刷新曲线但不改变手动范围
                    int type = GetCurveType();
                    LoadCurvesDataForSelected(type);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }
    }
}
