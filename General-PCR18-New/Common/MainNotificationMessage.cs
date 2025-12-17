using System.Collections.Generic;

namespace General_PCR18.Common
{
    public enum MainMessageCode
    {
        /// <summary>
        /// 保存患者信息
        /// </summary>
        SavePatienInfo = 50,

        /// <summary>
        /// 点击开始加热按钮
        /// </summary>
        HeatStart = 200,

        /// <summary>
        /// 点击停止加热
        /// </summary>
        HeatStop = 201,

        /// <summary>
        /// 开始加热倒计时
        /// </summary>
        HeatingCountdown = 202,

        /// <summary>
        /// 开始获取光
        /// </summary>
        LightStart = 300,

        /// <summary>
        /// 暂停光扫描
        /// </summary>
        LightPause = 301,

        /// <summary>
        /// 停止光扫描
        /// </summary>
        LightStop = 302,     

        /// <summary>
        /// 自动导出光数据
        /// </summary>
        AutoExportLight = 405,
    }

    public delegate void MainNotificationCallback();

    public class MainNotificationMessage
    {
        /// <summary>
        /// 消息Code
        /// </summary>
        public MainMessageCode Code { get; set; }

        /// <summary>
        /// 数据，Key=试管序号，Value=值
        /// </summary>
        public Dictionary<int, double[]> Data { get; set; }

        /// <summary>
        /// 试管序号
        /// </summary>
        public int TubeIndex { get; set; }

        /// <summary>
        /// 回调
        /// </summary>
        public MainNotificationCallback Callback { get; set; }
    }
}
