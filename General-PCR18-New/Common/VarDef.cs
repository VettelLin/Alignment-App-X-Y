using System.Collections.Generic;

namespace General_PCR18.Common
{
    public struct DeviceStatus
    {
        /// <summary>
        /// 串口是否打开
        /// </summary>
        public bool PCRStatus;

        //设备公共信息
        public int DeviceBeStatus;//当前设备处于的状态
        public int SelectTube;//保存当前选中的管号，从0开始
        public bool[] SelectTubeIndex;//保存当前试管是否选中
        /// <summary>
        /// 保存试管开关的状态，是否已经插入
        /// </summary>
        public bool[] PCRKeyStatus;

        //运行监控变量
        public bool[] RunMonitorTubeStatus;//保存运行监控所有按钮的运行状态
        public int[] RunMonitorDataFAM;//保存荧光值Cy5
        public int RunMonitorDataIndex;//保存荧光值Cy5的数据索引
        public int[] RunMonitorDataCy5;//保存荧光值Cy5
        public int[] RunMonitorDataHEX;//保存荧光值Cy5
        public int[] RunMonitorDataCy55;//保存荧光值Cy5
        public int[] RunMonitorDataRox;//保存荧光值Cy5
        public byte RunMonitorHighByte;//荧光数据高字节
        public byte RunMonitorLowByte;//荧光数据低字节
        public int RunMonitorMotorYIndex;//电机Y轴行数索引

        // 加热
        public int HeatTubeCount;//保存当前加热管的总数（孔位有管子）

        /// <summary>
        /// 保存样本类型 0 未设置, 1 HPV, 2 DNA, 3 RNA
        /// </summary>
        public int[] HeatSampleType;

        /// <summary>
        /// H1温度，x10后的值
        /// </summary>
        public int[] HeatH1Temp;

        /// <summary>
        /// H3温度，x10后的值
        /// </summary>
        public int[] HeatH3Temp;

        public int[] HeatH1Time;//保存对于管号的H1时间
        public int[] HeatH3Time;//保存对于管号的H3时间
        public string[] HeatPatientID;//保存病人ID
        public string[] HeatDateSample;//保存采样日期
        public bool HeatStatus;//当前加热串口状态
        public bool HeatSendEnable;//保存加热命令可发送状态
        public byte HeatSendCmd;//保存发送的命令
        public string[] HeatDockUnit;//保存管号

        /// <summary>
        /// 保存按钮全局状态
        /// </summary>
        public TUBE_STATUS[] TubeCurrentStatus;

        public string[] HeatSampleID; // 保存样本ID
    }

    public enum TUBE_STATUS
    {
        /// <summary>
        /// 没有样本
        /// </summary>
        NoSample = 0,

        /// <summary>
        /// 未设置参数
        /// </summary>
        NoParameters = 1,

        /// <summary>
        /// 已设置参数
        /// </summary>
        ParametersSet = 2,

        /// <summary>
        /// 加热中
        /// </summary>
        Heating = 3,

        /// <summary>
        /// 暂停加热
        /// </summary>
        HeatingPaused = 4,

        /// <summary>
        /// 加热完成
        /// </summary>
        HeatingCompleted = 5,

        /// <summary>
        /// 扫描中
        /// </summary>
        Lighting = 6,

        /// <summary>
        /// 暂停扫描
        /// </summary>
        LightingPaused = 7,

        /// <summary>
        /// 扫描完成
        /// </summary>
        LightingCompleted = 8,
    }

    public class VarDef
    {
        /// <summary>
        /// 样本类型: Key=类型ID，Value=[类型文本, 边框颜色, 选中颜色]
        /// </summary>
        public readonly static Dictionary<int, string[]> SampleType = new Dictionary<int, string[]>()
        {
            {0, new string[] {"UN", "#c6c6c6", "#eeeeee" } },
            {1, new string[] {"HPV", "#9b51e0", "#ebdcf9" } },
            {2, new string[] {"RNA", "#219653", "#d3eadd" } },
            {3, new string[] {"DNA", "#eb5757", "#fbdddd" } },
        };

        /// <summary>
        ///  默认值: Key=类型ID，Value=[H1温度, H1时间, H3温度, H3时间]
        /// </summary>
        public readonly static Dictionary<int, string[]> DefaultValues = new Dictionary<int, string[]>()
        {
            {0, new string[] {"95", "15", "60", "15" } },
            {1, new string[] {"95", "15", "60", "15" } },
            {2, new string[] {"30", "15", "61", "15" } },
            {3, new string[] {"95", "15", "61", "15" } },
        };

        /// <summary>
        /// 样本xy轴字符
        /// </summary>
        public static readonly IReadOnlyList<string> SampleAxisCharList = new List<string>()
        {
            "1", "2", "3", "4", "5", "6", "A", "B", "C"
        };

        public float[,] CrosstalkMatrix = new float[6, 6];
    }
}
