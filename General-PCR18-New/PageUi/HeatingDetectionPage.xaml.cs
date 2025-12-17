using General_PCR18.Common;
using General_PCR18.UControl;
using General_PCR18.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for HeatingDetectionPage.xaml
    /// </summary>
    public partial class HeatingDetectionPage : BasePage
    {
        #region 变量区域
        private readonly SampleUC[] sampleList = new SampleUC[18];
        private readonly HashSet<SampleUC> selectList = new HashSet<SampleUC>();

        private readonly SynchronizationContext context;

        private readonly Dictionary<int, List<double>> dataH1Temp = new Dictionary<int, List<double>>();  // 保存所有的H1温度
        private readonly Dictionary<int, List<double>> dataH3Temp = new Dictionary<int, List<double>>();  // 保存所有的H3温度

        private readonly int maxAxisXValue = 300;  // X轴最大点
        private readonly int maxAxisYValue = 120;  // Y轴最大点
        private readonly double[] xAxisIncValue = new double[18];  // X轴数据
        private HashSet<string> selectCurvesType = new HashSet<string>() { "H1", "H3" };  // 选中的类型
        private readonly Dictionary<int, BlockingCollection<double[]>> dataQueue = new Dictionary<int, BlockingCollection<double[]>>();

        private readonly System.Threading.Thread[] chartThreads = new System.Threading.Thread[18];

        #endregion

        public HeatingDetectionPage()
        {
            InitializeComponent();

            for (int i = 0; i < 18; i++)
            {
                dataQueue.Add(i, new BlockingCollection<double[]>());
            }

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

            SampleEditActivate(false);

            context = SynchronizationContext.Current; // 获取当前 UI 线程的上下文

            // 订阅事件
            EventBus.OnHeatingDectionMessageReceived += EventBus_OnMessageReceived;

            // 初始化图表线程
            InitThread();

            // 测试
            //TestData();

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("===>HeatingDetectionPage Loaded");

            RefreshSampleUC(sampleList, true);
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
                        //LogHelper.Debug((object)("HeatingDetection收到消息: " + obj.Code));

                        context.Post(_ => { RefreshSampleUC(sampleList, true); }, null);
                    }
                    break;
                case MessageCode.TempUpdate:
                    {
                        LogHelper.Debug((object)("HeatingDetection收到消息: " + obj.Code));

                        //Key 试管序号，  Value = [H1, H2, H3]
                        Dictionary<int, double[]> dataArr = obj.Data;
                        foreach (var data in dataArr)
                        {
                            dataQueue[data.Key].Add(data.Value);

                            // H3开始加热就开始收集光信号。判断h2加热完成, h2保持固定 7s
                            if (data.Value[0] >= GlobalData.DS.HeatH1Temp[data.Key])
                            {
                                // h1 等待 + h2加热时间后 h3 开始加热                                
                                EventBus.MainMsg(new MainNotificationMessage()
                                {
                                    Code = MainMessageCode.LightStart,
                                    TubeIndex = data.Key
                                });
                            }
                            // H3结束，停止光扫描
                            if (data.Value[2] >= GlobalData.DS.HeatH3Temp[data.Key])
                            {
                                EventBus.MainMsg(new MainNotificationMessage()
                                {
                                    Code = MainMessageCode.LightStop,
                                    TubeIndex = data.Key
                                });
                            }
                        }
                    }
                    break;
                case MessageCode.RefreshUI:
                    {
                        //LogHelper.Debug((object)("HeatingDetection收到消息: " + obj.Code));

                        // 刷新按钮状态
                        { context.Post(_ => { RefreshSampleUC(sampleList, true); }, null); }
                    }
                    break;
            }
        }

        private void dpTestDate_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = true;
        }

        /// <summary>
        /// 编辑框状态切换
        /// </summary>
        /// <param name="activate"></param>
        private void SampleEditActivate(bool activate)
        {
            if (activate)
            {
                txtDockUnit.IsReadOnly = false;
                txtPatientId.IsReadOnly = false;
                txtH1Temp.IsReadOnly = false;
                txtH3Temp.IsReadOnly = false;
                txtH1Time.IsReadOnly = false;
                txtH3Time.IsReadOnly = false;
                cmbAssayType.IsEnabled = true;
                dpTestDate.IsEnabled = true;
            }
            else
            {
                txtDockUnit.IsReadOnly = true;
                txtPatientId.IsReadOnly = true;
                txtH1Temp.IsReadOnly = true;
                txtH3Temp.IsReadOnly = true;
                txtH1Time.IsReadOnly = true;
                txtH3Time.IsReadOnly = true;
                cmbAssayType.IsEnabled = false;
                dpTestDate.IsEnabled = false;
            }
        }

        /// <summary>
        /// 初始化图表曲线
        /// </summary>
        private void InitChart()
        {
            ChartArea chartArea = new ChartArea("CharArea1");

            // X轴
            chartArea.AxisX.Title = "Time";
            //chartArea.AxisX.MajorGrid.Interval = 0.016666;
            //chartArea.AxisY.LabelStyle.Interval = 40;
            chartArea.AxisX.Interval = maxAxisXValue / 7;
            chartArea.AxisX.Minimum = 0;
            chartArea.AxisX.Maximum = maxAxisXValue;
            chartArea.AxisX.MajorGrid.LineColor = Tools.HexToColor("#eaecef"); // 设置 X 轴网格线颜色

            // 手动添加自定义标签
            chartArea.AxisX.CustomLabels.Add(new CustomLabel(0, 20, Tools.SecondsToHms(0), 0, LabelMarkStyle.None));
            chartArea.AxisX.CustomLabels.Add(new CustomLabel(60, 85, Tools.SecondsToHms(85), 0, LabelMarkStyle.None));
            chartArea.AxisX.CustomLabels.Add(new CustomLabel(150, 171, Tools.SecondsToHms(171), 0, LabelMarkStyle.None));
            chartArea.AxisX.CustomLabels.Add(new CustomLabel(240, 257, Tools.SecondsToHms(257), 0, LabelMarkStyle.None));


            // Y轴
            chartArea.AxisY.Title = "Temp";
            chartArea.AxisY.MajorGrid.Interval = 10;
            chartArea.AxisY.LabelStyle.Interval = 20;
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
            chartStandard.ChartAreas.Add(chartArea);

            // 样本
            for (int i = 0; i < 18; i++)
            {
                // H1
                Series seriesH1 = new Series("H1-" + i.ToString());
                seriesH1.ChartType = SeriesChartType.Line;
                seriesH1.Color = Tools.HexToColor("#4054b2");
                seriesH1.BorderWidth = 2;
                chartStandard.Series.Add(seriesH1);

                // H3
                Series seriesH3 = new Series("H3-" + i.ToString());
                seriesH3.ChartType = SeriesChartType.Line;
                seriesH3.Color = Tools.HexToColor("#9b2fae");
                seriesH3.BorderWidth = 2;
                chartStandard.Series.Add(seriesH3);
            }

            // 默认的，不显示
            Series seriesDefault = new Series("Default");
            seriesDefault.ChartType = SeriesChartType.Line;
            seriesDefault.Color = Tools.HexToColor("#f1f1f1");
            chartStandard.Series.Add(seriesDefault);
            seriesDefault.Points.AddXY(1, 0);

        }

        /// <summary>
        /// 初始化图表线程
        /// </summary>
        private void InitThread()
        {
            for (int i = 0; i < chartThreads.Length; i++)
            {
                int localIndex = i;
                chartThreads[localIndex] = new System.Threading.Thread(() => UpdateCurves(localIndex));

                chartThreads[localIndex].Start();
            }
        }

        /// <summary>
        /// 显示隐藏曲线
        /// </summary>
        /// <param name="index"></param>
        private void ShowSeries(int index)
        {
            foreach (var item in chartStandard.Series)
            {
                string[] arr = item.Name.Split('-');
                if (item.Name == "Default" || (int.Parse(arr[1]) == index && selectCurvesType.Contains(arr[0])))
                {
                    item.Enabled = true;
                }
                else
                {
                    item.Enabled = false;
                }
            }
        }

        private readonly object lockObj = new object();

        private void UpdateSeries(string name, List<double> X, List<double> Y, double dataX, double dataY)
        {

#if DEBUG
            LogHelper.Debug("更新{0}温度：{1}", name, string.Join(",", Y));
#endif

            context.Post(_ =>
            {
                X.Add(dataX);
                Y.Add(dataY);

                if (dataY > chartStandard.ChartAreas[0].AxisY.Maximum)
                {
                    double max = Math.Max(1, (int)Math.Round(dataY * 1.2));

                    chartStandard.ChartAreas[0].AxisY.Maximum = max;
                    chartStandard.ChartAreas[0].AxisY.MajorGrid.Interval = 20;
                    chartStandard.ChartAreas[0].AxisY.LabelStyle.Interval = 40;
                }

                chartStandard.Series[name].Points.Clear();
                chartStandard.Series[name].Points.DataBindXY(X, Y);
            }, null);
        }

        /// <summary>
        /// 更新曲线
        /// </summary>
        /// <param name="index">试管序号</param>
        private void UpdateCurves(int index)
        {
            LogHelper.Debug($"Heating Thread {Thread.CurrentThread.ManagedThreadId} is processing index: {index}");

            foreach (var data in dataQueue[index].GetConsumingEnumerable())
            {
                lock (lockObj)
                {
                    // H1
                    {
                        var name = "H1-" + index.ToString();
                        List<double> x = GlobalData.DataH1X[index];
                        List<double> y = GlobalData.DataH1Y[index];
                        UpdateSeries(name, x, y, xAxisIncValue[index], data[0]);
                    }

                    // H3
                    {
                        var name = "H3-" + index.ToString();
                        List<double> x = GlobalData.DataH3X[index];
                        List<double> y = GlobalData.DataH3Y[index];
                        UpdateSeries(name, x, y, xAxisIncValue[index], data[2]);
                    }

                    xAxisIncValue[index] += 2;
                }
            }
        }

        /// <summary>
        /// 点击样本
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_ClickEventTick(SampleUC sender, bool click)
        {
            SampleEditActivate(true);

            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                selectList.Clear();

                // 管号
                if (string.IsNullOrEmpty(GlobalData.DS.HeatDockUnit[sender.Index]))
                {
                    int x = sender.Index % 6;
                    int y = sender.Index / 6;
                    string dockUnit = SampleAxisCharList[y + 6] + SampleAxisCharList[x];
                    GlobalData.DS.HeatDockUnit[sender.Index] = dockUnit;
                }

                // 回填值
                txtDockUnit.Text = GlobalData.DS.HeatDockUnit[sender.Index];
                txtPatientId.Text = GlobalData.DS.HeatPatientID[sender.Index];

                // 温度 时间
                if (GlobalData.DS.HeatH1Temp[sender.Index] > 0)
                {
                    txtH1Temp.Text = (GlobalData.DS.HeatH1Temp[sender.Index] / 10) + "c";
                    txtH3Temp.Text = (GlobalData.DS.HeatH3Temp[sender.Index] / 10) + "c";
                    txtH1Time.Text = GlobalData.DS.HeatH1Time[sender.Index] + "s";
                    txtH3Time.Text = GlobalData.DS.HeatH3Time[sender.Index] + "s";

                    int typeId = GlobalData.DS.HeatSampleType[sender.Index];
                    foreach (ComboBoxItem item in cmbAssayType.Items)
                    {
                        if (int.Parse(item.Tag.ToString()) == typeId)
                        {
                            item.IsSelected = true;
                            break;
                        }
                    }
                }
                else
                {
                    txtH1Temp.Text = "";
                    txtH3Temp.Text = "";
                    txtH1Time.Text = "";
                    txtH3Time.Text = "";
                    cmbAssayType.SelectedIndex = -1;
                }

                // 日期
                var dateStr = GlobalData.DS.HeatDateSample[sender.Index];
                if (!string.IsNullOrEmpty(dateStr))
                {
                    dpTestDate.SelectedDate = DateTime.Parse(dateStr);
                }
                else
                {
                    dpTestDate.SelectedDate = null;
                }

                selectList.Add(sender);

                context.Post(_ => ShowSeries(sender.Index), null);
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

            ChangeSelectBg(selectList, sampleList, true);
        }

        /// <summary>
        /// 点击样本下方按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_StartClickEventHandler(SampleUC sender, bool click)
        {
            StartButtonClick(sender);
        }

        /// <summary>
        /// 点击X轴
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTab_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int lastCount = selectList.Count;
            SampleEditActivate(true);

            selectList.Clear();
            string text = (sender as Label).Content.ToString();
            AddSampleAxis(text, selectList, sampleList, lastCount);

            ChangeSelectBg(selectList, sampleList, true);
        }

        /// <summary>
        /// 右上角曲线类型按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FAM_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var ctr = sender as Border;
            string tag = ctr.Tag.ToString();
            string bg = ctr.Background.ToString();
            if (bg == "#FFF1F4F9")
            {
                // 选中
                selectCurvesType.Remove(tag);
                ctr.Background = Tools.HexToBrush("#FF06919D");
                (ctr.Child as Label).Foreground = Tools.HexToBrush("#FFBFEDF0");
            }
            else
            {
                // 取消
                selectCurvesType.Add(tag);
                ctr.Background = Tools.HexToBrush("#FFF1F4F9");
                (ctr.Child as Label).Foreground = Tools.HexToBrush("#FF607C6D");
            }

            if (selectList.Count == 1)
            {
                ShowSeries(selectList.First().Index);
            }

        }

        /// <summary>
        /// 选择日期
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dpTestDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            var dp = sender as DatePicker;
            if (dp.SelectedDate.HasValue)
            {
                var val = dp.SelectedDate.Value.ToString("yyyy-MM-dd");
                if (selectList.Count > 0)
                {
                    foreach (var t in selectList)
                    {
                        GlobalData.DS.HeatDateSample[t.Index] = val;
                    }
                }
            }

            CheckInputStatus(selectList);
        }

        /// <summary>
        /// 管号
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtDockUnit_TextChanged(object sender, TextChangedEventArgs e)
        {
            var ctr = sender as TextBox;
            string val = ctr.Text.Trim();

            if (selectList.Count > 0)
            {
                foreach (var t in selectList)
                {
                    GlobalData.DS.HeatDockUnit[t.Index] = val;
                }
            }
        }

        /// <summary>
        /// H1, H3温度, 一位小数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtH1Temp_LostFocus(object sender, RoutedEventArgs e)
        {
            if (selectList.Count == 0) { return; }

            var ctr = sender as TextBox;
            string val = ctr.Text.Trim();
            if (string.IsNullOrEmpty(val)) { return; }

            val = val.Substring(0, val.Length - 1);

            bool res = Tools.IsValidDouble(val, 1, out double result);
            if (!res)
            {
                MyMessageBox.Show(Properties.Resources.msg_decimal_one_error,
                    MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            foreach (var t in selectList)
            {
                if (ctr.Tag.ToString() == "H1")
                {
                    GlobalData.DS.HeatH1Temp[t.Index] = (int)(result * 10);
                }
                else
                {
                    GlobalData.DS.HeatH3Temp[t.Index] = (int)(result * 10);
                }
            }

            CheckInputStatus(selectList);
        }

        /// <summary>
        /// H1, H3时间, 秒
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtH1Time_LostFocus(object sender, RoutedEventArgs e)
        {
            if (selectList.Count == 0) { return; }

            var ctr = sender as TextBox;
            string val = ctr.Text.Trim();
            if (string.IsNullOrEmpty(val)) { return; }

            val = val.Substring(0, val.Length - 1);

            bool res = Tools.IsNonnegativeInt(val, out int result);
            if (!res)
            {
                MyMessageBox.Show(Properties.Resources.msg_int_error,
                    MyMessageBox.CustomMessageBoxButton.OK,
                MyMessageBox.CustomMessageBoxIcon.Warning);
                return;
            }

            foreach (var t in selectList)
            {
                if (ctr.Tag.ToString() == "H1")
                {
                    GlobalData.DS.HeatH1Time[t.Index] = result;
                }
                else
                {
                    GlobalData.DS.HeatH3Time[t.Index] = result;
                }
            }

            CheckInputStatus(selectList);
        }

        /// <summary>
        /// 患者ID
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void txtPatientId_TextChanged(object sender, TextChangedEventArgs e)
        {
            var ctr = sender as TextBox;
            string val = ctr.Text.Trim();

            if (selectList.Count > 0)
            {
                foreach (var t in selectList)
                {
                    GlobalData.DS.HeatPatientID[t.Index] = val;

                    sampleList[t.Index].PatientId = val;
                }
            }

            CheckInputStatus(selectList);
        }

        /// <summary>
        /// 化验类型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cmbAssayType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem selectedItem = (sender as ComboBox).SelectedItem as ComboBoxItem;
            if (selectedItem == null)
            {
                return;

            }

            int typeId = int.Parse(selectedItem.Tag.ToString());

            if (selectList.Count > 0)
            {
                foreach (var s in selectList)
                {
                    GlobalData.DS.HeatSampleType[s.Index] = typeId;

                    // 边框颜色
                    s.BorderColor = Tools.HexToBrush(VarDef.SampleType[typeId][1]);
                    // 选中背景颜色
                    s.BackgroundColor = Tools.HexToBrush(VarDef.SampleType[typeId][2]);

                    // 设置默认值
                    GlobalData.DS.HeatH1Temp[s.Index] = (int)(double.Parse(VarDef.DefaultValues[typeId][0]) * 10);
                    GlobalData.DS.HeatH1Time[s.Index] = int.Parse(VarDef.DefaultValues[typeId][1]);
                    GlobalData.DS.HeatH3Temp[s.Index] = (int)(double.Parse(VarDef.DefaultValues[typeId][2]) * 10);
                    GlobalData.DS.HeatH3Time[s.Index] = int.Parse(VarDef.DefaultValues[typeId][1]);
                    txtH1Temp.Text = VarDef.DefaultValues[typeId][0] + "c";
                    txtH1Time.Text = VarDef.DefaultValues[typeId][1] + "s";
                    txtH3Temp.Text = VarDef.DefaultValues[typeId][2] + "c";
                    txtH3Time.Text = VarDef.DefaultValues[typeId][3] + "s";
                }
            }

            CheckInputStatus(selectList);
        }

        private void TestData()
        {
            // 样本1
            var timer1 = new System.Timers.Timer(2000)
            {
                AutoReset = true,
                Enabled = true
            };
            timer1.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                Random r = new Random((int)DateTime.Now.Ticks);
                double v = r.NextDouble() * 120;
                dataQueue[0].Add(new double[] { v, 0, r.NextDouble() * 170 });
            };

            // 样本2
            var timer2 = new System.Timers.Timer(2000)
            {
                AutoReset = true,
                Enabled = true
            };
            timer2.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                Random r = new Random((int)DateTime.Now.Ticks * 10000);
                double v = r.NextDouble() * 120;
                dataQueue[1].Add(new double[] { v, 0, r.NextDouble() * 200 });
            };
        }
    }
}
