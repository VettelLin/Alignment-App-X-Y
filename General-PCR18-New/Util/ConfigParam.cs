namespace General_PCR18.Util
{
    public class ConfigParam
    {
        public enum LogLevelEnum
        {
            Debug = 0,
            Info = 1,
            Warn = 2,
            Error = 3,
            Fatal = 4
        }

        /// <summary>
        /// 当前保存日志级别
        /// </summary>
        public static LogLevelEnum LogLevel;

        /// <summary>
        /// 日志存放路径
        /// </summary>
        public static string LogFilePath;

        /// <summary>
        /// 日志存放天数
        /// </summary>
        public static int LogFileExistDay;

        /// <summary>
        /// 设备串口号
        /// </summary>
        public static string DevicePort;
    }
}
