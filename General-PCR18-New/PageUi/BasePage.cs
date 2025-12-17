using General_PCR18.Common;
using General_PCR18.UControl;
using General_PCR18.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace General_PCR18.PageUi
{
    public class BasePage : Page
    {
        /// <summary>
        /// 样本轴字符
        /// </summary>
        public readonly IReadOnlyList<string> SampleAxisCharList = new List<string>()
        {
            "1", "2", "3", "4", "5", "6", "A", "B", "C"
        };

        public struct SampleData
        {
            public int Width;
            public int Height;
            public int SeparateHeight;
            public int Margin;
            public string ButtonDisplay;
            public string SampleTypeDisplay;
            public SampleUC.ClickEventHandler Sample_ClickEventTick;
            public SampleUC.StartClickEventHandler Sample_StartClickEventHandler;
        }

        /// <summary>
        /// 创建样本控件
        /// </summary>
        /// <param name="index">索引</param>
        /// <param name="data">通用属性</param>
        /// <returns></returns>
        public SampleUC NewSample(int index, SampleData data)
        {
            SampleUC sample = new SampleUC()
            {
                Index = index,
                Width = data.Width,
                Height = data.Height,
                SeparateHeight = new GridLength(data.SeparateHeight),
            };
            if ("Hidden" == data.SampleTypeDisplay)
            {
                sample.SampleTypeDisplay = "Hidden";
            }
            sample.Margin = new Thickness(data.Margin);
            sample.ClickEventTick += data.Sample_ClickEventTick;
            sample.StartClickEventTick += data.Sample_StartClickEventHandler;

            if ("Hidden" == data.ButtonDisplay)
            {
                sample.ButtonDisplay = "Hidden";
            }

            return sample;
        }

        /// <summary>
        /// 绘制样本UI
        /// </summary>
        public void InitSample(Grid sampleGrid, SampleUC[] sampleList, SampleData sampleData)
        {
            for (int i = 0; i < 3; i++)
            {
                sampleGrid.RowDefinitions.Add(new RowDefinition());
            }

            for (int i = 0; i < 6; i++)
            {
                sampleGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < 18; i++)
            {
                int row = i / 6; // 计算行索引
                int col = i % 6; // 计算列索引

                SampleUC sample = NewSample(i, sampleData);
                sampleList[i] = sample;
                sampleGrid.Children.Add(sample);

                sample.SetValue(Grid.RowProperty, row);
                sample.SetValue(Grid.ColumnProperty, col);
            }
        }

        /// <summary>
        /// 把样本加入已选
        /// </summary>
        /// <param name="list"></param>
        /// <param name="selectList"></param>
        public void AddSampleToSelected(IEnumerable<SampleUC> list, HashSet<SampleUC> selectList)
        {
            if (list == null) return;
            foreach (var uc in list)
            {
                selectList.Add(uc);
            }
        }

        /// <summary>
        /// 点击样本左轴或顶轴时，加入已选
        /// </summary>
        /// <param name="tabText"></param>
        /// <param name="selectList"></param>
        /// <param name="sampleList"></param>
        /// <param name="lastCount">上次选中个数</param>
        public void AddSampleAxis(string tabText, HashSet<SampleUC> selectList, SampleUC[] sampleList, int lastCount)
        {
            switch (tabText)
            {
                case "All":
                    if (lastCount == sampleList.Length)
                    {
                        selectList.Clear();
                    }
                    else
                    {
                        AddSampleToSelected(sampleList.ToList(), selectList);
                    }
                    break;
                case "1":
                case "2":
                case "3":
                case "4":
                case "5":
                case "6":
                    {
                        int n = int.Parse(tabText);
                        List<int> arr = new List<int>() { n - 1, n + 5, n + 11 };
                        AddSampleToSelected(sampleList.Where(s => arr.Contains(s.Index)), selectList);
                    }
                    break;
                case "A":
                case "B":
                case "C":
                    {
                        // 65, 66, 67
                        // 0, 6, 12
                        int v = tabText[0] - 65;  // 0, 1, 2
                        List<int> arr = new List<int>();
                        for (int i = 0; i < 6; i++)
                        {
                            arr.Add(v * 6 + i);
                        }
                        AddSampleToSelected(sampleList.Where(s => arr.Contains(s.Index)), selectList);
                    }
                    break;
            }
        }

        /// <summary>
        /// 切换X,Y轴选中状态
        /// </summary>
        /// <param name="selectTabs">选中的轴</param>
        private void ChangeTxtBkBg(HashSet<string> selectTabs)
        {
            foreach (string t in SampleAxisCharList)
            {
                var textBlock = (Label)this.FindName("txtBk" + t);
                if (selectTabs.Contains(t))
                {
                    textBlock.Background = Tools.HexToBrush("#bbcae3");
                }
                else
                {
                    textBlock.Background = Tools.HexToBrush("#f1f4f9");
                }
            }
        }

        /// <summary>
        /// 改变选中样本的颜色
        /// </summary>
        /// <param name="selects"></param>
        /// <param name="sampleList"></param>
        /// <param name="isHeat">是否加热界面</param>
        public void ChangeSelectBg(HashSet<SampleUC> selects, SampleUC[] sampleList, bool isHeat = false)
        {
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                HashSet<string> selectTabs = new HashSet<string>();
                // 选中颜色
                foreach (var s in selects)
                {
                    var typeId = GlobalData.DS.HeatSampleType[s.Index];
                    s.BackgroundColor = Tools.HexToBrush(VarDef.SampleType[typeId][2]);

                    // 同时改变对应轴tab的颜色               
                    int x = s.Index % 6;
                    int y = s.Index / 6;
                    selectTabs.Add(SampleAxisCharList[x]);
                    selectTabs.Add(SampleAxisCharList[y + 6]);
                }
                ChangeTxtBkBg(selectTabs);

                // 未选中颜色
                foreach (var sample in sampleList)
                {
                    if (!selects.Contains(sample))
                    {
                        sample.BackgroundColor = Tools.HexToBrush("#ffffff");
                    }
                }
            }));
        }

        /// <summary>
        /// 切换页面时，刷新样本UI状态
        /// </summary>
        public void RefreshSampleUC(SampleUC[] sampleList, bool isHeat = false)
        {
            foreach (var s in sampleList)
            {
                int typeId = GlobalData.DS.HeatSampleType[s.Index];
                s.PatientId = GlobalData.DS.HeatPatientID[s.Index];
                s.SampleTypeText = VarDef.SampleType[typeId][0];

                s.BorderColor = Tools.HexToBrush(VarDef.SampleType[typeId][1]);

                // 刷新按钮状态
                RefreshButun(s);
            }
        }

        /// <summary>
        /// 检查样本信息输入是否完成
        /// </summary>
        public void CheckInputStatus(HashSet<SampleUC> selects)
        {
            foreach (var s in selects)
            {
                bool tubeIsInto = GlobalData.DS.PCRKeyStatus[s.Index];  // 试管是否插入

                int h1Temp = GlobalData.DS.HeatH1Temp[s.Index];
                int h3Temp = GlobalData.DS.HeatH3Temp[s.Index];
                int h1Time = GlobalData.DS.HeatH1Time[s.Index];
                int h3Time = GlobalData.DS.HeatH3Time[s.Index];
                string patientId = GlobalData.DS.HeatPatientID[s.Index];
                string dateSample = GlobalData.DS.HeatDateSample[s.Index];

                if (tubeIsInto)
                {
                    // 参数都设置
                    if (h1Temp > 0 && h3Temp > 0 && h1Time > 0 && h3Time > 0
                        && !string.IsNullOrEmpty(patientId) && !string.IsNullOrEmpty(dateSample)
                        && GlobalData.GetStatus(s.Index) == TUBE_STATUS.NoParameters)
                    {
                        // 参数已经设置
                        LogHelper.Debug("设置试管 {0} 参数完成", s.Index);

                        GlobalData.SetStatus(s.Index, TUBE_STATUS.ParametersSet);  // 已设置参数
                    }
                }

                RefreshButun(s);
            }
        }

        /// <summary>
        /// 刷新看有页面试管状态
        /// </summary>
        public void SendRefreshUIEvent()
        {
            EventBus.DataAnalyse(new NotificationMessage { Code = MessageCode.RefreshUI });
            EventBus.HeatingDection(new NotificationMessage { Code = MessageCode.RefreshUI });
            EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.RefreshUI });
            EventBus.SampleRegistration(new NotificationMessage { Code = MessageCode.RefreshUI });
        }

        /// <summary>
        /// 刷新按钮状态
        /// </summary>
        /// <param name="s"></param>
        public void RefreshButun(SampleUC s)
        {
            TUBE_STATUS buttonStatus = GlobalData.GetStatus(s.Index);

            // 设置按钮
            switch (buttonStatus)
            {
                case TUBE_STATUS.NoSample:
                    {
                        s.ButtonText = "No Sample";
                        s.ButtonEnabled = false;
                    }
                    break;
                case TUBE_STATUS.NoParameters:
                    {
                        s.ButtonText = "Start";
                        s.ButtonEnabled = false;
                    }
                    break;
                case TUBE_STATUS.ParametersSet:
                    {
                        s.ButtonText = "Start";
                        s.ButtonEnabled = true;
                    }
                    break;
                case TUBE_STATUS.Heating:
                    {
                        s.ButtonText = "Stop";
                        s.ButtonEnabled = true;
                    }
                    break;
                case TUBE_STATUS.HeatingPaused:
                    {
                        s.ButtonText = "Start";
                        s.ButtonEnabled = true;
                    }
                    break;
                case TUBE_STATUS.HeatingCompleted:
                    {
                        s.ButtonText = "Start";
                        s.ButtonEnabled = true;
                    }
                    break;
                case TUBE_STATUS.Lighting:
                    {
                        s.ButtonText = "Stop";
                        s.ButtonEnabled = true;
                    }
                    break;
                case TUBE_STATUS.LightingPaused:
                    {
                        s.ButtonText = "Start";
                        s.ButtonEnabled = true;
                    }
                    break;
                case TUBE_STATUS.LightingCompleted:
                    {
                        s.ButtonText = "Start";
                        s.ButtonEnabled = true;
                    }
                    break;
            }
        }

        /// <summary>
        /// 点击按钮处理
        /// </summary>
        /// <param name="s"></param>
        public void StartButtonClick(SampleUC s)
        {
            int tubeIndex = s.Index;
            TUBE_STATUS buttonStatus = GlobalData.GetStatus(tubeIndex);

            LogHelper.Debug("点击按钮: tubeIndex={0}, status={1}", tubeIndex, buttonStatus);

            // 保存患者信息
            EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.SavePatienInfo, TubeIndex = tubeIndex });

            switch (buttonStatus)
            {
                case TUBE_STATUS.ParametersSet:
                    {
                        // 在MainWindow中处理加热命令
                        EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.HeatStart, TubeIndex = tubeIndex });
                    }
                    break;
                case TUBE_STATUS.Heating:
                    {
                        // 点击STOP
                        string msg = string.Format(Properties.Resources.msg_stop_experiment, Tools.GetDockUnit(tubeIndex));
                        MyMessageBox.CustomMessageBoxResult result =
                        MyMessageBox.Show(msg,
                        MyMessageBox.CustomMessageBoxButton.YesNo,
                        MyMessageBox.CustomMessageBoxIcon.Question);
                        if (result == MyMessageBox.CustomMessageBoxResult.Yes)
                        {
                            if (GlobalData.GetStatus(tubeIndex) == TUBE_STATUS.Heating)
                            {
                                EventBus.MainMsg(new MainNotificationMessage()
                                {
                                    Code = MainMessageCode.HeatStop,
                                    TubeIndex = tubeIndex,
                                    Callback = () =>
                                    {
                                        GlobalData.SetStatus(tubeIndex, TUBE_STATUS.LightingCompleted);
                                        EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.StopDetectionTime, TubeIndex = tubeIndex });
                                    }
                                });
                            }
                        }
                    }
                    break;
                case TUBE_STATUS.HeatingCompleted:
                    {
                        // 开始扫描
                        EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.LightStart, TubeIndex = tubeIndex });
                    }
                    break;
                case TUBE_STATUS.Lighting:
                    {
                        string msg = string.Format(Properties.Resources.msg_stop_experiment, Tools.GetDockUnit(tubeIndex));
                        MyMessageBox.CustomMessageBoxResult result =
                        MyMessageBox.Show(msg,
                        MyMessageBox.CustomMessageBoxButton.YesNo,
                        MyMessageBox.CustomMessageBoxIcon.Question);
                        if (result == MyMessageBox.CustomMessageBoxResult.Yes)
                        {
                            // 停止扫描
                            if (GlobalData.GetStatus(tubeIndex) == TUBE_STATUS.Lighting)
                            {
                                EventBus.MainMsg(new MainNotificationMessage()
                                {
                                    Code = MainMessageCode.LightStop,
                                    TubeIndex = tubeIndex,
                                    Callback = () =>
                                    {
                                        GlobalData.SetStatus(tubeIndex, TUBE_STATUS.LightingCompleted);
                                        EventBus.RunMonitor(new NotificationMessage { Code = MessageCode.StopDetectionTime, TubeIndex = tubeIndex });
                                    }
                                });
                                EventBus.RunMonitor(new NotificationMessage() { Code = MessageCode.StopDetectionTime, TubeIndex = tubeIndex });
                            }
                        }
                    }
                    break;
                case TUBE_STATUS.LightingPaused:
                    {
                        // 开始扫描
                        EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.LightStart, TubeIndex = tubeIndex });
                    }
                    break;
                case TUBE_STATUS.LightingCompleted:
                    {
                        // 光扫描轮次完成状态下点击了按钮，则清空数据，再次开始
                        MyMessageBox.CustomMessageBoxResult result =
                        MyMessageBox.Show(Properties.Resources.msg_restart_lighting,
                        MyMessageBox.CustomMessageBoxButton.YesNo,
                        MyMessageBox.CustomMessageBoxIcon.Question);
                        if (result == MyMessageBox.CustomMessageBoxResult.Yes)
                        {
                            EventBus.RunMonitor(new NotificationMessage()
                            {
                                Code = MessageCode.LightClear,
                                TubeIndex = tubeIndex,
                                Callback = () =>
                                {
                                    EventBus.MainMsg(new MainNotificationMessage() { Code = MainMessageCode.HeatingCountdown, TubeIndex = tubeIndex });
                                }
                            });
                        }
                    }
                    break;
            }

            // 刷新
            RefreshButun(s);
        }

        /// <summary>
        /// 重置数据
        /// </summary>
        /// <param name="tubeIndex"></param>
        protected void ResetTubeData(int tubeIndex)
        {
            // 清空数据
            for (int i = 0; i < 6; i++)
            {
                GlobalData.RnYMin[tubeIndex, i] = 0;
                GlobalData.RnYMax[tubeIndex, i] = 0;
            }

            GlobalData.DataFAMX[tubeIndex].Clear();
            GlobalData.DataFAMY[tubeIndex].Clear();
            GlobalData.DataCy5X[tubeIndex].Clear();
            GlobalData.DataCy5Y[tubeIndex].Clear();
            GlobalData.DataVICX[tubeIndex].Clear();
            GlobalData.DataVICY[tubeIndex].Clear();
            GlobalData.DataCy55X[tubeIndex].Clear();
            GlobalData.DataCy55Y[tubeIndex].Clear();
            GlobalData.DataROXX[tubeIndex].Clear();
            GlobalData.DataROXY[tubeIndex].Clear();
            GlobalData.DataMOTX[tubeIndex].Clear();
            GlobalData.DataMOTY[tubeIndex].Clear();
            GlobalData.TubeDatas[tubeIndex].DeleteAll();

            GlobalData.DS.HeatDateSample[tubeIndex] = "";
            GlobalData.DS.HeatPatientID[tubeIndex] = "";
            GlobalData.DS.HeatSampleType[tubeIndex] = 0;
        }

    }
}
