using System.Collections.Generic;

namespace General_PCR18.Common
{
    public enum MessageCode
    {
        /// <summary>
        /// 刷新UI
        /// </summary>
        RefreshUI = 60,

        /// <summary>
        /// 试管开关状态更新
        /// </summary>
        PcrKeyStatus = 100,

        /// <summary>
        /// 收到温度值更新
        /// </summary>
        TempUpdate = 101,

        /// <summary>
        /// 收到光值更新
        /// </summary>
        LightUpdate = 102,

        /// <summary>
        /// 收到环境温度值更新
        /// </summary>
        EnvTempUpdate = 103,

        /// <summary>
        /// 隐藏环境温度标签
        /// </summary>
        HideEnvTempTag = 104,

        /// <summary>
        /// 显示环境温度标签
        /// </summary>
        ShowEnvTempTag = 105,

        /// <summary>
        /// 收到热盖温度值更新
        /// </summary>
        HotCoverTempUpdate = 106,

        /// <summary>
        /// 裂解温度（A/B/C行）更新
        /// </summary>
        LysisTempUpdate = 107,

        /// <summary>
        /// 清空光的数据, 轮次
        /// </summary>
        LightClear = 303,

        /// <summary>
        /// 开始倒计时
        /// </summary>
        StartDetectionTime = 401,

        /// <summary>
        /// 停止倒计时
        /// </summary>
        StopDetectionTime = 402,

        /// <summary>
        /// 阶段倒计时
        /// </summary>
        StageCountdown = 403,

    }

    public delegate void NotificationCallback();

    public class NotificationMessage
    {
        /// <summary>
        /// 消息Code
        /// </summary>
        public MessageCode Code { get; set; }

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
        public NotificationCallback Callback { get; set; }

        /// <summary>
        /// 倒计时总秒数
        /// </summary>
        public int CountdownSeconds { get; set; }

        /// <summary>
        /// 倒计时标题
        /// </summary>
        public string CountdownTitle { get; set; }
    }
}
