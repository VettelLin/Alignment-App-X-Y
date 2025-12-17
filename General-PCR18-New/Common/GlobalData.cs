using General_PCR18.Algorithm;
using General_PCR18.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace General_PCR18.Common
{
    /// <summary>
    /// 全局数据
    /// </summary>
    public static class GlobalData
    {
        public static string DeviceCode { get; set; }

        public static string SoftVer { get; set; }

        public static MainWindow MainWin { get; set; }

        public static User CurrentUser { get; set; }

        /// <summary>
        /// 样本登记界面选择的临时备份存储路径（仅当前会话有效）
        /// </summary>
        public static string BackupDataPath { get; set; }

        /// <summary>
        /// 最后一次下发读取的光类型, FAM, Cy5, VIC, Cy5.5, ROX, MOT
        /// </summary>
        public static byte LastLightType { get; set; }

        /// <summary>
        /// 记录读取光的通道顺序，1=FAM, 2=Cy5, 3=VIC, 4=Cy5.5, 5=ROX, 6=MOT
        /// </summary>
        public static readonly Queue<byte> LightQueue = new Queue<byte>();

        public static DeviceStatus DS = new DeviceStatus()
        {
            //初始化结构体的数组元素
            PCRKeyStatus = new bool[18],
            //运行监控
            RunMonitorTubeStatus = new bool[18],
            RunMonitorDataFAM = new int[18],
            RunMonitorDataCy5 = new int[18],
            RunMonitorDataHEX = new int[18],
            RunMonitorDataCy55 = new int[18],
            RunMonitorDataRox = new int[18],
            RunMonitorMotorYIndex = 0,

            //加热检测
            HeatH1Temp = new int[18],
            HeatH3Temp = new int[18],
            HeatH1Time = new int[18],
            HeatH3Time = new int[18],
            HeatPatientID = new string[18],
            HeatDateSample = new string[18],
            SelectTubeIndex = new bool[18],
            HeatDockUnit = new string[18],

            HeatSampleID = new string[18],
            HeatSampleType = new int[18],
            TubeCurrentStatus = new TUBE_STATUS[18],
        };

        /// <summary>
        /// 记录 DeltaRn Y轴最小值
        /// </summary>
        public static readonly double[,] RnYMin = new double[18, 6];
        /// <summary>
        /// 记录 DeltaRn Y轴最大值
        /// </summary>
        public static readonly double[,] RnYMax = new double[18, 6];

        /// <summary>
        /// 保存FAM X轴点
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataFAMX = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存FAM值
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataFAMY = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存Cy5 X轴点
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataCy5X = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存Cy5值
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataCy5Y = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存VIC X轴点
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataVICX = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存VIC值
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataVICY = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存Cy5.5 X轴点
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataCy55X = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存Cy5.5值
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataCy55Y = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存ROX X轴 
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataROXX = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存ROX值
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataROXY = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存MOT X轴 
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataMOTX = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存MOT值
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataMOTY = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存H1 X轴点
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataH1X = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存H1温度
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataH1Y = new Dictionary<int, List<double>>();

        /// <summary>
        /// 保存H3 X轴点
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataH3X = new Dictionary<int, List<double>>();
        /// <summary>
        /// 保存H3温度
        /// </summary>
        public static readonly Dictionary<int, List<double>> DataH3Y = new Dictionary<int, List<double>>();

        /// <summary>
        /// 试管各种数据. Key=试管下标 0-17
        /// </summary>
        public static readonly Dictionary<int, TubeData> TubeDatas = new Dictionary<int, TubeData>();

        /// <summary>
        /// Turbidity 开关（按通道：0=FAM,1=Cy5,2=HEX(VIC),3=Cy5.5,4=ROX）
        /// 默认开启
        /// </summary>
        public static bool[] TurbidityEnabled = new bool[] { true, true, true, true, true };

        /// <summary>
        /// Turbidity 调整比例（0.1 表示压缩为原来的10% 等）。
        /// 按通道：0=FAM,1=HEX(VIC),2=ROX,3=Cy5,4=Cy5.5
        /// </summary>
        public static double[] TurbidityAdjustScale = new double[] { 0.1, 0.2, 0.2, 0.2, 0.2 };

        /// <summary>
        /// 串扰矩阵（行=被影响通道，列=来源通道），单位系数
        /// 索引映射：0=FAM,1=Cy5,2=VIC(HEX),3=Cy5.5,4=ROX
        /// </summary>
        public static double[,] CrosstalkMatrix = new double[5,5]
        {
            {0,    0,    0.140,0,    0   },
            {0,    0,    0,    0.150,0   },
            {0,    0,    0,    0,    0   },
            {0,    0,    0,    0,    0   },
            {0,    0,    0.030,0,    0   }
        };

        /// <summary>
        /// Filter parameters (global, adjustable by UI)
        /// </summary>
        public static class FilterParams
        {
            /// <summary>
            /// Median filter window size (odd number >=3)
            /// </summary>
            public static int MedianWindow { get; set; } = 5;

            /// <summary>
            /// Smoothing passes for forward/backward average
            /// </summary>
            public static int SmoothPasses { get; set; } = 3;

            /// <summary>
            /// Forward window M for smoothing
            /// </summary>
            public static int SmoothForwardM { get; set; } = 1;

            /// <summary>
            /// Backward window N for smoothing
            /// </summary>
            public static int SmoothBackwardN { get; set; } = 3;

            /// <summary>
            /// CT detection threshold used in baseline/ct detection
            /// </summary>
            public static int CtThreshold { get; set; } = 60;
        }

        /// <summary>
        /// Basic Parameters per channel/target shown in Basic Parameters dialog
        /// </summary>
        public class BasicParameterItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string name) { PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name)); }

            private int _no;
            public int No { get => _no; set { _no = value; OnPropertyChanged(nameof(No)); } }

            private string _channel; 
            public string Channel { get => _channel; set { _channel = value; OnPropertyChanged(nameof(Channel)); } }

            private string _target;
            public string Target { get => _target; set { _target = value; OnPropertyChanged(nameof(Target)); } }

            private bool _autoBaseline;
            public bool AutoBaseline
            {
                get => _autoBaseline;
                set
                {
                    if (_autoBaseline != value)
                    {
                        _autoBaseline = value;
                        OnPropertyChanged(nameof(AutoBaseline));
                        if (_autoBaseline)
                        {
                            // 勾选后自动重置为 3/15
                            BaselineStart = 3;
                            BaselineEnd = 15;
                        }
                    }
                }
            }

            private int _baselineStart;
            public int BaselineStart { get => _baselineStart; set { _baselineStart = value; OnPropertyChanged(nameof(BaselineStart)); } }

            private int _baselineEnd;
            public int BaselineEnd { get => _baselineEnd; set { _baselineEnd = value; OnPropertyChanged(nameof(BaselineEnd)); } }

            private bool _autoThreshold;
            public bool AutoThreshold { get => _autoThreshold; set { _autoThreshold = value; OnPropertyChanged(nameof(AutoThreshold)); } }

            private double _normalizedThreshold;
            public double NormalizedThreshold { get => _normalizedThreshold; set { _normalizedThreshold = value; OnPropertyChanged(nameof(NormalizedThreshold)); } }

            private double _deltaRnThreshold;
            public double DeltaRnThreshold { get => _deltaRnThreshold; set { _deltaRnThreshold = value; OnPropertyChanged(nameof(DeltaRnThreshold)); } }

            private double _lowerThreshold;
            public double LowerThreshold { get => _lowerThreshold; set { _lowerThreshold = value; OnPropertyChanged(nameof(LowerThreshold)); } }

            private double _upperThreshold;
            public double UpperThreshold { get => _upperThreshold; set { _upperThreshold = value; OnPropertyChanged(nameof(UpperThreshold)); } }
        }

        /// <summary>
        /// Current editable list of basic parameters
        /// </summary>
        public static ObservableCollection<BasicParameterItem> BasicParameters { get; set; } = new ObservableCollection<BasicParameterItem>();

        static GlobalData()
        {
            for (int i = 0; i < 18; i++)
            {

                DataFAMX.Add(i, new List<double>());
                DataFAMY.Add(i, new List<double>());

                DataCy5X.Add(i, new List<double>());
                DataCy5Y.Add(i, new List<double>());

                DataVICX.Add(i, new List<double>());
                DataVICY.Add(i, new List<double>());

                DataCy55X.Add(i, new List<double>());
                DataCy55Y.Add(i, new List<double>());

                DataROXX.Add(i, new List<double>());
                DataROXY.Add(i, new List<double>());

                DataMOTX.Add(i, new List<double>());
                DataMOTY.Add(i, new List<double>());

                DataH1X.Add(i, new List<double>());
                DataH1Y.Add(i, new List<double>());

                DataH3X.Add(i, new List<double>());
                DataH3Y.Add(i, new List<double>());

                TubeDatas.Add(i, new TubeData());

                lockObj[i] = new object();
            }
        }

        private static readonly object[] lockObj = new object[18]; // 锁对象

        /// <summary>
        /// 设置试管当前状态
        /// </summary>
        /// <param name="tubeIndex"></param>
        /// <param name="status"></param>
        public static void SetStatus(int tubeIndex, TUBE_STATUS status)
        {
            lock (lockObj[tubeIndex])
            {
                DS.TubeCurrentStatus[tubeIndex] = status;
            }
        }

        /// <summary>
        /// 获取试管当前状态
        /// </summary>
        /// <param name="tubeIndex"></param>
        /// <returns></returns>
        public static TUBE_STATUS GetStatus(int tubeIndex)
        {
            return DS.TubeCurrentStatus[tubeIndex];
        }
    }
}
