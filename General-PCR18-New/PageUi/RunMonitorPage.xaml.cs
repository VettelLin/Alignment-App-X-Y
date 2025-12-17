using General_PCR18.Algorithm;
using General_PCR18.Common;
using General_PCR18.UControl;
using General_PCR18.Util;
using Newtonsoft.Json.Linq;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Markup;
using System.Xml.Linq;

namespace General_PCR18.PageUi
{
    /// <summary>
    /// Interaction logic for RunMonitorPage.xaml
    /// </summary>
    public partial class RunMonitorPage : BasePage
    {
        #region 变量区域
        private readonly SampleUC[] sampleList = new SampleUC[18];
        private readonly HashSet<SampleUC> selectList = new HashSet<SampleUC>();
        private readonly SampleUC[] sampleTimeList = new SampleUC[18];  // 显示倒计时
        private readonly bool[] countdownStarted = new bool[18];  // 是否已开始70分钟倒计时（每孔）

        private readonly SynchronizationContext context;

        private readonly int maxCycle = 60;  // 最大轮次，超过则停止当前孔位收集和下发采集指令
        private readonly int maxAxisXValue = 61;  // X轴默认最大点
        private readonly int maxAxisYValue = 5000;  // Y轴默认最大点 5000
        private readonly double[,] xAxisIncValue = new double[18, 6];  // X轴数据
        private HashSet<string> selectCurvesType = new HashSet<string>() { "FAM", "Cy5", "VIC", "Cy55", "ROX" };  // 选中的类型 FAM, Cy5, VIC, Cy55, ROX
        private readonly Dictionary<int, BlockingCollection<double[]>> dataQueue = new Dictionary<int, BlockingCollection<double[]>>();

        private System.Windows.Forms.ToolTip tooltip; // 显示点信息

        private readonly System.Threading.Thread[] chartThreads = new System.Threading.Thread[18];

        private readonly object lockObj = new object();

        #endregion

        public RunMonitorPage()
        {
            InitializeComponent();

            for (int i = 0; i < 18; i++)
            {
                dataQueue[i] = new BlockingCollection<double[]>();
            }

            // 初始化每孔倒计时开始标记
            for (int i = 0; i < countdownStarted.Length; i++)
            {
                countdownStarted[i] = false;
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

            // 倒计时样本
            SampleData sampleTimeData = new SampleData()
            {
                Width = 80,
                Height = 100,
                SeparateHeight = 5,
                Margin = 5,
                ButtonDisplay = "Hidden",
                SampleTypeDisplay = "Hidden",
            };
            InitSample(sampleGrid2, sampleTimeList, sampleTimeData);

            InitChart();  // 初始化图表

            context = SynchronizationContext.Current; // 获取当前 UI 线程的上下文

            // 订阅事件
            EventBus.OnRunMonitorMessageReceived += EventBus_OnMessageReceived;
            // 图表线程
            InitThread();

            // 测试
            //TestData();

            this.Loaded += Page_Loaded;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("===>RunMonitorPage Loaded");

            RefreshSampleUC(sampleList);
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
                        //LogHelper.Debug((object)("RunMonitorPage收到消息: " + obj.Code));

                        context.Post(_ => { RefreshSampleUC(sampleList, true); }, null);

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MessageCode.LightUpdate:
                    {
                        // 收到光数据  Value = [FAM, Cy5, VIC, Cy5.5, ROX, MOT]
                        Dictionary<int, double[]> dataArr = obj.Data;

                        LogHelper.Debug("RunMonitorPage收到消息: {0}", obj.Code);

                        foreach (var data in dataArr)
                        {
                            int tubeIndex = data.Key;

                            LogHelper.Debug("RunMonitorPage 试管 {0} 状态: {1}", tubeIndex, GlobalData.GetStatus(tubeIndex));

                            if (GlobalData.GetStatus(tubeIndex) == TUBE_STATUS.Lighting)
                            {
                                dataQueue[tubeIndex].Add(data.Value);
                            }
                        }

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MessageCode.LightClear:
                    {
                        LogHelper.Debug("RunMonitorPage收到消息: {0}, {1}", obj.Code, obj.TubeIndex);

                        // 清空数据
                        int tubeIndex = obj.TubeIndex;
                        context.Post(_ => { ResetTube(tubeIndex); }, null);

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MessageCode.StartDetectionTime:
                    {
                        LogHelper.Debug("RunMonitorPage收到消息: {0}, {1}", obj.Code, obj.TubeIndex);

                        // 开始倒计时
                        int tubeIndex = obj.TubeIndex;
                        context.Post(_ => { DetectionTime(tubeIndex); }, null);

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MessageCode.StopDetectionTime:
                    {
                        LogHelper.Debug("RunMonitorPage收到消息: {0}, {1}", obj.Code, obj.TubeIndex);

                        // 停止倒计时
                        int tubeIndex = obj.TubeIndex;
                        context.Post(_ => { StopDetectionTime(tubeIndex); }, null);

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MessageCode.StageCountdown:
                    {
                        LogHelper.Debug("RunMonitorPage收到消息: {0}, {1}", obj.Code, obj.TubeIndex);

                        // 停止倒计时
                        int tubeIndex = obj.TubeIndex;
                        context.Post(_ => { StageCountdownTime(tubeIndex, obj.CountdownSeconds, obj.CountdownTitle); }, null);

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
                case MessageCode.EnvTempUpdate:
                    {
                        // 环境温度更新                        
                        context.Post(_ =>
                        {
                            envTempSP.Visibility = Visibility.Visible;
                            envTemp.Content = obj.Data[0][0];
                        }, null);
                    }
                    break;
                case MessageCode.HideEnvTempTag:
                    {
                        // 隐藏环境温度标签                        
                        context.Post(_ =>
                        {
                            envTempSP.Visibility = Visibility.Hidden;
                        }, null);
                    }
                    break;
                case MessageCode.ShowEnvTempTag:
                    {
                        // 显示环境温度标签
                        context.Post(_ =>
                        {
                            envTempSP.Visibility = Visibility.Visible;
                        }, null);
                    }
                    break;
                case MessageCode.HotCoverTempUpdate:
                    {
                        // 热盖温度更新                        
                        context.Post(_ =>
                        {
                            hotCoverTemp.Content = obj.Data[0][0];
                        }, null);
                    }
                    break;
                case MessageCode.LysisTempUpdate:
                    {
                        // 裂解温度 A/B/C 行更新（Row: 0->A, 1->B, 2->C）
                        context.Post(_ =>
                        {
                            foreach (var kv in obj.Data)
                            {
                                int row = kv.Key;
                                double value = kv.Value?[0] ?? double.NaN;
                                if (double.IsNaN(value))
                                {
                                    continue;
                                }
                                switch (row)
                                {
                                    case 0:
                                        lysisTempA.Content = value;
                                        break;
                                    case 1:
                                        lysisTempB.Content = value;
                                        break;
                                    case 2:
                                        lysisTempC.Content = value;
                                        break;
                                }
                            }
                        }, null);
                    }
                    break;
                case MessageCode.RefreshUI:
                    {
                        //LogHelper.Debug((object)("RunMonitorPage收到消息: " + obj.Code));

                        // 刷新按钮状态
                        context.Post(_ => { RefreshSampleUC(sampleList, true); }, null);

                        // 回调
                        obj.Callback?.Invoke();
                    }
                    break;
            }
        }

        /// <summary>
        /// Amplification 扫描倒计时
        /// </summary>
        /// <param name="tubeIndex"></param>
        private void DetectionTime(int tubeIndex)
        {
            int duration = 70; // minute 固定 70 分钟

            // 显示
            SampleUC sample = sampleTimeList[tubeIndex];
            if (!countdownStarted[tubeIndex])
            {
                sample.SetTotalSecond(duration * 60);
                sample.StartTimer("");
                countdownStarted[tubeIndex] = true;
            }
        }

        /// <summary>
        /// 加热时显示倒计时，1、Lysis 阶段（裂解）7分钟。2、Valving 阶段（阀控）：3 mins
        /// </summary>
        /// <param name="tubeIndex">试管下标</param>
        /// <param name="seconds">秒</param>
        private void StageCountdownTime(int tubeIndex, int seconds, string text)
        {
            // 改为在插管时即开始 70 分钟总倒计时（只在未开始时触发一次）
            SampleUC sample = sampleTimeList[tubeIndex];
            if (!countdownStarted[tubeIndex])
            {
                sample.SetTotalSecond(70 * 60);
                sample.StartTimer("");
                countdownStarted[tubeIndex] = true;
            }
        }

        /// <summary>
        /// 停止倒计时
        /// </summary>
        /// <param name="tubeIndex"></param>
        private void StopDetectionTime(int tubeIndex)
        {
            SampleUC sample = sampleTimeList[tubeIndex];
            sample.StopTimer();
            countdownStarted[tubeIndex] = false;
        }

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="tubeIndex"></param>
        private void ResetTube(int tubeIndex)
        {
            // clear tube data
            ResetTubeData(tubeIndex);

            // 清空x数据
            for (int i = 0; i < 6; i++)
            {
                xAxisIncValue[tubeIndex, i] = 0;
            }
            // 重置倒计时标记
            countdownStarted[tubeIndex] = false;

            // 图表数据
            chartDeltaRn.Series["FAM-" + tubeIndex].Points.Clear();
            chartDeltaRn.Series["Cy5-" + tubeIndex].Points.Clear();
            chartDeltaRn.Series["VIC-" + tubeIndex].Points.Clear();
            chartDeltaRn.Series["Cy55-" + tubeIndex].Points.Clear();
            chartDeltaRn.Series["ROX-" + tubeIndex].Points.Clear();

            if (selectList.Count == 1 && selectList.First().Index == tubeIndex)
            {
                chartDeltaRn.ChartAreas[0].AxisX.Maximum = maxAxisXValue;
                chartDeltaRn.ChartAreas[0].AxisY.Maximum = maxAxisYValue;
                chartDeltaRn.ChartAreas[0].AxisX.Interval = 3;
            }
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
            chartArea.AxisY.Title = "Fn";
            //chartArea.AxisY.Interval = 500;
            chartArea.AxisY.Minimum = 0;
            chartArea.AxisY.Maximum = 5000;
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


            // 样本曲线
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
            HitTestResult result = chart.HitTest(e.X, e.Y);

            // 检查是否悬停在数据点上
            if (result.ChartElementType == ChartElementType.DataPoint)
            {
                DataPoint dataPoint = result.Series.Points[result.PointIndex];

                // 显示数据点的信息
                int yVal = (int)dataPoint.YValues[0];
                int xVal = (int)dataPoint.XValue;
                string[] arr = result.Series.Name.Split('-');
                string info = $"{arr[0]}: {yVal}, {xVal}";
                tooltip.Show(info, chart, e.X + 20, e.Y + 20);
            }
            else
            {
                tooltip.Hide(chart);
            }
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
            foreach (var item in chartDeltaRn.Series)
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

            // 检查 x 轴 y 轴
            try
            {
                if (selectList.Count == 1 && selectList.First().Index == index)
                {
                    double x = GlobalData.DataFAMX[index].LastOrDefault();
                    //double y = GetSelectedMaxData(index);

                    double yMin = GetSelectedRnYMin(index);
                    //double yMax = GetSelectedMaxData(tubeIndex);
                    double yMax = GetSelectedRnYMax(index);

                    UpdateAxisXY(x, yMax, yMin);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 获取选中的曲线类型中最大的值
        /// </summary>
        /// <param name="tubeIndex"></param>
        /// <returns></returns>
        private double GetSelectedMaxData(int tubeIndex)
        {
            List<double> values = new List<double>();
            if (selectCurvesType.Contains("FAM") && GlobalData.DataFAMY[tubeIndex].Count > 0)
                values.Add(GlobalData.DataFAMY[tubeIndex].Max());
            if (selectCurvesType.Contains("Cy5") && GlobalData.DataCy5Y[tubeIndex].Count > 0)
                values.Add(GlobalData.DataCy5Y[tubeIndex].Max());
            if (selectCurvesType.Contains("VIC") && GlobalData.DataVICY[tubeIndex].Count > 0)
                values.Add(GlobalData.DataVICY[tubeIndex].Max());
            if (selectCurvesType.Contains("Cy55") && GlobalData.DataCy55Y[tubeIndex].Count > 0)
                values.Add(GlobalData.DataCy55Y[tubeIndex].Max());
            if (selectCurvesType.Contains("ROX") && GlobalData.DataROXY[tubeIndex].Count > 0)
                values.Add(GlobalData.DataROXY[tubeIndex].Max());

            double y = values.Count > 0 ? values.Max() : 1;

            return y;
        }

        private double GetSelectedRnYMin(int tubeIndex)
        {
            List<double> values = new List<double>();
            if (selectCurvesType.Contains("FAM"))
                values.Add(GlobalData.RnYMin[tubeIndex, 0]);
            if (selectCurvesType.Contains("Cy5"))
                values.Add(GlobalData.RnYMin[tubeIndex, 1]);
            if (selectCurvesType.Contains("VIC"))
                values.Add(GlobalData.RnYMin[tubeIndex, 2]);
            if (selectCurvesType.Contains("Cy55"))
                values.Add(GlobalData.RnYMin[tubeIndex, 3]);
            if (selectCurvesType.Contains("ROX"))
                values.Add(GlobalData.RnYMin[tubeIndex, 4]);

            double y = values.Count > 0 ? values.Min() : 0;

            return y;
        }

        private double GetSelectedRnYMax(int tubeIndex)
        {
            List<double> values = new List<double>();
            if (selectCurvesType.Contains("FAM"))
                values.Add(GlobalData.RnYMax[tubeIndex, 0]);
            if (selectCurvesType.Contains("Cy5"))
                values.Add(GlobalData.RnYMax[tubeIndex, 1]);
            if (selectCurvesType.Contains("VIC"))
                values.Add(GlobalData.RnYMax[tubeIndex, 2]);
            if (selectCurvesType.Contains("Cy55"))
                values.Add(GlobalData.RnYMax[tubeIndex, 3]);
            if (selectCurvesType.Contains("ROX"))
                values.Add(GlobalData.RnYMax[tubeIndex, 4]);

            double y = values.Count > 0 ? values.Max() : 0;

            return y;
        }

        /// <summary>
        /// 动态设置x轴 y轴
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="dataY"></param>
        private void UpdateAxisXY(double dataX, double dataY, double yMin = 0)
        {
            ChartArea chartArea = chartDeltaRn.ChartAreas[0];

            // X 轴轮次
            if (dataX > chartArea.AxisX.Maximum)
            {
                chartArea.AxisX.Maximum = Math.Ceiling(dataX * 1.2);
                chartArea.AxisX.Interval = Math.Max(1, Math.Floor(chartArea.AxisX.Maximum / 10));
            }
            else
            {
                if (dataX < maxAxisXValue)
                {
                    chartArea.AxisX.Maximum = maxAxisXValue;
                    chartArea.AxisX.Interval = 3;
                }
            }

            // Y 轴 max
            // 始终根据当前数据最大值设置为高出 10%，但不低于默认下限 maxAxisYValue
            {
                int desiredMax = Math.Max(maxAxisYValue, Math.Max(1, (int)Math.Round(dataY * 1.10)));
                chartArea.AxisY.Maximum = desiredMax;
            }

            // Y 轴 min
            if (yMin >= 0)
            {
                chartArea.AxisY.Minimum = 0;
            }
            else
            {
                chartArea.AxisY.Minimum = ((int)(yMin * 1.6 / 10)) * 10;
            }
        }

        /// <summary>
        /// 更新曲线数据
        /// </summary>
        /// <param name="tubeIndex"></param>
        /// <param name="name"></param>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        /// <param name="dataX"></param>
        /// <param name="dataY"></param>
        private void UpdateSeries(int tubeIndex, string name, List<double> X, List<double> Y)
        {
            var logX = X.ToArray();
            var logY = Y.ToArray();
            int dataX = logX.Length;

#if DEBUG
            LogHelper.Debug($"UpdateSeries, 光曲线: {name}, ListX={logX.Length}, ListY={string.Join(" ", logY)}");

            TubeData tube = GlobalData.TubeDatas[tubeIndex];
            LogHelper.Debug("试管 {0} 光数据: FAM={1}, Cy5={2}, VIC={3}, Cy55={4}, ROX={5}",
                tubeIndex, tube.GetOriginalData(0).Count, tube.GetOriginalData(1).Count,
                tube.GetOriginalData(2).Count, tube.GetOriginalData(3).Count, tube.GetOriginalData(4).Count);
#endif

            // 计算 DeltaRn 数据
            List<double> rn = Y;
            var (dtFAM, dtCy5, dtVIC, dtCy55, dtROX) = PcrAlgorigthm.DeltaRn(tubeIndex);

            // 记录 DetalRn min 和 max
            int lightType = 0;
            string lightName = name.Split('-')[0];
            if (lightName == "FAM")
            {
                lightType = 0;
                rn = dtFAM.ToList();
            }
            else if (lightName == "Cy5")
            {
                lightType = 1;
                rn = dtCy5.ToList();
            }
            else if (lightName == "VIC")
            {
                lightType = 2;
                rn = dtVIC.ToList();
            }
            else if (lightName == "Cy55")
            {
                lightType = 3;
                rn = dtCy55.ToList();
            }
            else if (lightName == "ROX")
            {
                lightType = 4;
                rn = dtROX.ToList();
            }
            else if (lightName == "MOT")
            {
                lightType = 5;
            }

            if (rn.Count > 0)
            {
                GlobalData.RnYMin[tubeIndex, lightType] = rn.Min();
                GlobalData.RnYMax[tubeIndex, lightType] = rn.Max();
            }
            else
            {
                GlobalData.RnYMin[tubeIndex, lightType] = 0;
                GlobalData.RnYMax[tubeIndex, lightType] = 0;
            }

            context.Post(_ =>
                {
                    try
                    {
                        // 如果当前显示的试管是正在更新的，则检查 x轴 y轴的变化
                        if (selectList.Count == 1 && selectList.First().Index == tubeIndex)
                        {
                            double yMin = GetSelectedRnYMin(tubeIndex);
                            //double yMax = GetSelectedMaxData(tubeIndex);
                            double yMax = GetSelectedRnYMax(tubeIndex);

                            UpdateAxisXY(dataX, yMax, yMin);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Error(ex);
                    }

                    if (lightName != "MOT")
                    {
                        try
                        {
                            chartDeltaRn.Series[name].Points.Clear();

                            int c = Math.Min(X.Count, rn.Count);
                            if (c > 0)
                            {
                                List<double> a = X.GetRange(0, c);
                                List<double> b = rn.GetRange(0, c);

                                chartDeltaRn.Series[name].Points.DataBindXY(a, b);
                            }
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Error(ex);
                        }
                    }

                }, null);
        }

        /// <summary>
        /// 队列中取数据更新曲线
        /// </summary>
        /// <param name="index">试管序号</param>
        private void UpdateCurves(int index)
        {
            LogHelper.Debug($"RunMonitor Thread {Thread.CurrentThread.ManagedThreadId} is processing index: {index}");

            foreach (var data in dataQueue[index].GetConsumingEnumerable())
            {
                lock (lockObj)
                {
#if DEBUG
                    LogHelper.Debug("UpdateCurves, tubeIndex={0}, data={1}", index, string.Join(" ", data));
#endif
                    // FAM
                    if (!double.IsNaN(data[0]))
                    {
                        double cycle = xAxisIncValue[index, 0];
                        if (cycle < maxCycle)
                        {
                            var name = "FAM-" + index.ToString();
                            List<double> x = GlobalData.DataFAMX[index];
                            List<double> y = GlobalData.DataFAMY[index];

                            x.Add(cycle + 1);
                            y.Add(data[0]);

                            GlobalData.TubeDatas[index].AddOriginalData(0, (uint)data[0]);

                            UpdateSeries(index, name, x, y);
                            xAxisIncValue[index, 0] += 1;
                        }
                    }

                    // Cy5
                    if (!double.IsNaN(data[1]))
                    {
                        double cycle = xAxisIncValue[index, 1];
                        if (cycle < maxCycle)
                        {
                            var name = "Cy5-" + index.ToString();
                            List<double> x = GlobalData.DataCy5X[index];
                            List<double> y = GlobalData.DataCy5Y[index];

                            x.Add(cycle + 1);
                            y.Add(data[1]);

                            GlobalData.TubeDatas[index].AddOriginalData(1, (uint)data[1]);

                            UpdateSeries(index, name, x, y);
                            xAxisIncValue[index, 1] += 1;
                        }
                    }

                    // VIC
                    if (!double.IsNaN(data[2]))
                    {
                        double cycle = xAxisIncValue[index, 2];
                        if (cycle < maxCycle)
                        {
                            var name = "VIC-" + index.ToString();
                            List<double> x = GlobalData.DataVICX[index];
                            List<double> y = GlobalData.DataVICY[index];

                            x.Add(cycle + 1);
                            y.Add(data[2]);

                            GlobalData.TubeDatas[index].AddOriginalData(2, (uint)data[2]);

                            UpdateSeries(index, name, x, y);
                            xAxisIncValue[index, 2] += 1;
                        }
                    }

                    // Cy55
                    if (!double.IsNaN(data[3]))
                    {
                        double cycle = xAxisIncValue[index, 3];
                        if (cycle < maxCycle)
                        {
                            var name = "Cy55-" + index.ToString();
                            List<double> x = GlobalData.DataCy55X[index];
                            List<double> y = GlobalData.DataCy55Y[index];

                            x.Add(cycle + 1);
                            y.Add(data[3]);

                            GlobalData.TubeDatas[index].AddOriginalData(3, (uint)data[3]);

                            UpdateSeries(index, name, x, y);
                            xAxisIncValue[index, 3] += 1;
                        }
                    }

                    // ROX
                    if (!double.IsNaN(data[4]))
                    {
                        double cycle = xAxisIncValue[index, 4];
                        if (cycle < maxCycle)
                        {
                            var name = "ROX-" + index.ToString();
                            List<double> x = GlobalData.DataROXX[index];
                            List<double> y = GlobalData.DataROXY[index];

                            x.Add(cycle + 1);
                            y.Add(data[4]);

                            GlobalData.TubeDatas[index].AddOriginalData(4, (uint)data[4]);

                            UpdateSeries(index, name, x, y);
                            xAxisIncValue[index, 4] += 1;
                        }
                    }

                    // MOT
                    if (!double.IsNaN(data[5]))
                    {
                        double cycle = xAxisIncValue[index, 5];
                        if (cycle < maxCycle)
                        {
                            var name = "MOT-" + index.ToString();
                            List<double> x = GlobalData.DataMOTX[index];
                            List<double> y = GlobalData.DataMOTY[index];

                            x.Add(cycle + 1);
                            y.Add(data[5]);

                            GlobalData.TubeDatas[index].AddOriginalData(5, (uint)data[5]);

                            UpdateSeries(index, name, x, y);
                            xAxisIncValue[index, 5] += 1;
                        }
                    }

                    // 超过最大轮次，设置为完成
                    if (xAxisIncValue[index, 0] >= maxCycle
                        && xAxisIncValue[index, 1] >= maxCycle
                        && xAxisIncValue[index, 2] >= maxCycle
                        && xAxisIncValue[index, 3] >= maxCycle
                        && xAxisIncValue[index, 4] >= maxCycle
                        && xAxisIncValue[index, 5] >= maxCycle)
                    {
                        CheckData(index);

                        GlobalData.SetStatus(index, TUBE_STATUS.LightingCompleted);
                        ComputeLighting(index);

                        EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.AutoExportLight, TubeIndex = index });
                        EventBus.RunMonitor(new NotificationMessage() { Code = MessageCode.StopDetectionTime, TubeIndex = index });

                        // 刷新UI
                        SendRefreshUIEvent();

                        LogHelper.Debug($"试管 {index} 光曲线完成");
                    }
                }
            }
        }

        /// <summary>
        /// TODO: 计算数据
        /// </summary>
        /// <param name="index"></param>
        private void ComputeLighting(int index)
        {
            var tubeData = GlobalData.TubeDatas[index];

        }

        private void CheckData(int tubeIndex)
        {
            FinxedCycle(GlobalData.DataFAMX[tubeIndex], GlobalData.DataFAMY[tubeIndex]);
            FinxedCycle(GlobalData.DataCy5X[tubeIndex], GlobalData.DataCy5Y[tubeIndex]);
            FinxedCycle(GlobalData.DataVICX[tubeIndex], GlobalData.DataVICY[tubeIndex]);
            FinxedCycle(GlobalData.DataCy55X[tubeIndex], GlobalData.DataCy55Y[tubeIndex]);
            FinxedCycle(GlobalData.DataROXX[tubeIndex], GlobalData.DataROXY[tubeIndex]);
            FinxedCycle(GlobalData.DataMOTX[tubeIndex], GlobalData.DataMOTY[tubeIndex]);
        }

        private void FinxedCycle(List<double> cycle, List<double> data)
        {
            if (cycle.Count < maxCycle)
            {
                double last = cycle[cycle.Count - 1];
                for (int i = 0; i < maxCycle - cycle.Count; i++)
                {
                    cycle.Add(last + i + 1);
                }
            }
            else if (cycle.Count > maxCycle)
            {
                for (int i = cycle.Count - 1; i >= maxCycle; i--)
                {
                    cycle.RemoveAt(i);
                }
            }

            if (data.Count < maxCycle)
            {
                double last = data[data.Count - 1];
                for (int i = 0; i < maxCycle - data.Count; i++)
                {
                    data.Add(last);
                }
            }
            else if (data.Count > maxCycle)
            {
                for (int i = data.Count - 1; i >= maxCycle; i--)
                {
                    data.RemoveAt(i);
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

            context.Post(_ => ShowSeries(sender.Index), null);
        }

        /// <summary>
        /// 点击样本下方按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="click"></param>
        private void Sample_StartClickEventHandler(SampleUC sender, bool click)
        {
            lock (lockObj)
            {
                StartButtonClick(sender);
            }
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
        }

        /// <summary>
        /// 点击X轴 2
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void XTab2_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

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

        private void TestData()
        {
            // 五种光源的数据准备：FAM、Cy5、VIC、Cy5.5、ROX
            List<double> fam = new List<double>() {
                4933,4944,4935,4957,4938,4921,4951,4943,4930,4925,4944,4969,4988,5009,5096,5162,5279,5487,5754,6137,6733,7490,8597,9952,11388,12928,14326,15594,16604,17327,17913,18306,18617,18883,18967,19170,19246,19446,19541,19670,19709,19830,19953,19966,19970,20078,20199,20269,20290,20362,20490,20340,20401,20507,20402,20488,20555,20684,20674,20662
            };

            List<double> cy5 = new List<double>() {
                4609,4557,4621,4599,4570,4581,4589,4580,4595,4624,4614,4618,4635,4646,4678,4717,4766,4880,5019,5257,5569,5926,6481,7100,7750,8332,8863,9280,9699,10005,10213,10269,10431,10507,10560,10668,10763,10768,10859,10904,10969,11028,11060,11042,11087,11100,11151,11148,11193,11222,11303,11244,11258,11306,11328,11345,11319,11381,11388,11383
            };

            List<double> vic = new List<double>() {
                5362,5357,5405,5372,5423,5423,5398,5431,5451,5465,5493,5539,5646,5768,5931,6242,6656,7259,8213,9606,11463,13747,16330,18873,20932,22222,22976,23383,23698,23881,24092,24330,24378,24499,24562,24608,24740,24859,24943,25050,25003,24996,25176,25117,25166,25128,25135,25310,25267,25334,25431,25407,25377,25490,25386,25450,25450,25521,25580,25446
            };

            List<double> cy55 = new List<double>() {
                7719,7789,7959,8042,8015,7991,8056,8083,8078,8099,8205,8217,8276,8458,8725,9125,9677,10538,11866,13676,16196,19713,23870,28330,32950,36877,39848,41791,43313,43586,44226,44420,44602,44855,44970,44953,45161,45071,45291,45362,45497,45486,45651,45542,45581,45766,45744,45892,45735,45877,46028,45905,45863,45899,46069,45947,46088,46172,46190,46170
            };

            List<double> rox = new List<double>() {
                4644,4613,4725,4685,4745,4760,4730,4768,4774,4804,4851,4881,4968,5153,5366,5680,6159,6885,7986,9433,11465,13893,16652,19200,20886,21962,22584,22691,22792,22952,23043,23218,23186,23321,23187,23352,23353,23340,23393,23377,23492,23560,23559,23558,23500,23461,23521,23434,23423,23458,23657,23493,23503,23473,23526,23488,23547,23361,23684,23499
            };

            List<double> mot = new List<double>() {
                28204,28323,28354,28375,28316,28431,28396,28360,28357,28372,28449,28364,28384,28366,28342,28488,28391,28396,28412,28353,28452,28316,28396,28350,28455,28332,28347,28415,28351,28389,28324,28426,28308,28356,28347,28357,28329,28466,28365,28289,28413,28388,28460,28346,28340,28378,28348,28428,28352,28392,28368,28367,28365,28327,28377,28281,28367,28326,28414,28338
            };


            int i = 0;
            var timer = new System.Timers.Timer(200)
            {
                AutoReset = true,
                Enabled = true
            };

            timer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                if (GlobalData.GetStatus(0) == TUBE_STATUS.Lighting)
                {
                    if (i < fam.Count)
                    {
                        double f = fam[i];
                        double c5 = cy5[i];
                        double v = vic[i];
                        double c55 = cy55[i];
                        double r = rox[i];
                        double m = mot[i];
                        i++;

                        // 加入 5 通道荧光值 + 占位值
                        dataQueue[0].Add(new double[] { f, c5, v, c55, r, double.NaN });

                        Thread.Sleep(500);
                        dataQueue[0].Add(new double[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, m });
                    }

                    // 5轮停止
                    if (i >= 5)
                    {
                        timer.Stop();
                    }
                }
            };  // 👈 这一行你漏写了

            // 样本2（五通道数据）
            List<double> fam2 = new List<double>() {
            4134, 11553, 11594, 11664, 11803, 11923, 12060, 12248, 12356, 12508,
            12864, 13184, 13563, 13983, 14442, 14981, 15595, 16221, 16881, 17589,
            18239, 19106, 19853, 20621, 21323, 22086, 22749, 23464, 23966, 24291,
            24892, 25372, 25837, 26053, 26367, 26540, 26747, 27058, 27138, 27344,
            27552, 27596, 27831, 27869, 28080, 28105, 28243, 28238, 28355, 28640,
            28545, 28641, 28659, 28554, 28640, 28730, 28729, 28684, 28455, 28460
        };


            List<double> cy5_2 = new List<double>() {
                3958, 5106, 5112, 5144, 5175, 5208, 5217, 5280, 5351, 5385,
                5460, 5570, 5648, 5757, 5871, 6062, 6178, 6309, 6480, 6646,
                6857, 7007, 7187, 7355, 7469, 7627, 7763, 7841, 7999, 8124,
                8190, 8283, 8349, 8413, 8475, 8507, 8556, 8634, 8657, 8661,
                8710, 8791, 8758, 8784, 8817, 8832, 8838, 8939, 8898, 8959,
                8957, 8979, 8987, 8965, 8991, 9023, 9039, 9008, 8998, 9030
            };


            List<double> vic2 = new List<double>() {
                4194, 6448, 6542, 6646, 6819, 6934, 7153, 7377, 7676, 8002,
                8375, 8809, 9290, 9855, 10544, 11278, 12038, 12879, 13683, 14533,
                15430, 16291, 17214, 17864, 18672, 19325, 19869, 20360, 20785, 21247,
                21528, 22022, 22052, 22353, 22451, 22541, 22673, 22747, 22905, 22921,
                23073, 23061, 23158, 23163, 23179, 23220, 23125, 23318, 23420, 23314,
                23337, 23367, 23254, 23203, 23240, 23368, 23259, 23146, 23200, 23154
            };


            List<double> cy55_2 = new List<double>() {
                5186, 8653, 8862, 9096, 9326, 9645, 9986, 10409, 10965, 11471,
                12190, 13024, 13978, 14979, 16219, 17402, 18795, 20258, 21817, 23155,
                24689, 26066, 27515, 28750, 29844, 30786, 31540, 32259, 32783, 33151,
                33547, 33685, 33906, 33917, 34033, 34225, 34175, 34176, 34255, 34134,
                34375, 34307, 34216, 34418, 34314, 34239, 34307, 34118, 34182, 34199,
                34096, 34010, 33927, 33893, 34051, 33924, 33938, 33598, 33509, 33506
            };


            List<double> rox2 = new List<double>() {
                3478, 5413, 5592, 5815, 6073, 6412, 6765, 7282, 7819, 8335,
                9006, 9852, 10670, 11621, 12553, 13671, 14609, 15608, 16429, 17125,
                17808, 18182, 18556, 18832, 18834, 19001, 19169, 19107, 19137, 19207,
                19204, 19293, 19274, 19240, 19244, 19291, 19330, 19306, 19287, 19219,
                19274, 19274, 19245, 19287, 19296, 19183, 19263, 19240, 19190, 19187,
                19313, 19221, 19213, 19117, 19252, 19282, 19257, 19138, 19075, 19092
            };


            int i2 = 0;
            var timer2 = new System.Timers.Timer(200)
            {
                AutoReset = true,
                Enabled = true
            };

            timer2.Elapsed += (object sender, ElapsedEventArgs e) =>
            {
                if (GlobalData.GetStatus(1) == TUBE_STATUS.Lighting)
                {
                    if (i2 < fam2.Count)
                    {
                        dataQueue[1].Add(new double[] { fam2[i2], cy5_2[i2], vic2[i2], cy55_2[i2], rox2[i2], 0 });
                        i2++;
                    }
                }
            };

        }

    }
}