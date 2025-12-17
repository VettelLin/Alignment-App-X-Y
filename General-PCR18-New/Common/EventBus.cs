using System;

namespace General_PCR18.Common
{
    public class EventBus
    {
        /// <summary>
        /// 主界面
        /// </summary>
        public static event Action<MainNotificationMessage> OnMainMessageReceived;

        public static void MainMsg(MainNotificationMessage message)
        {
            OnMainMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// 数据分析页面
        /// </summary>
        public static event Action<NotificationMessage> OnDataAnalyseMessageReceived;
        public static void DataAnalyse(NotificationMessage message)
        {
            OnDataAnalyseMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// 加热页面
        /// </summary>
        public static event Action<NotificationMessage> OnHeatingDectionMessageReceived;
        public static void HeatingDection(NotificationMessage message)
        {
            OnHeatingDectionMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// 运行监控
        /// </summary>
        public static event Action<NotificationMessage> OnRunMonitorMessageReceived;
        public static void RunMonitor(NotificationMessage message)
        {
            OnRunMonitorMessageReceived?.Invoke(message);
        }

        /// <summary>
        /// 样本编辑
        /// </summary>
        public static event Action<NotificationMessage> OnSampleRegistrationMessageReceived;
        public static void SampleRegistration(NotificationMessage message)
        {
            OnSampleRegistrationMessageReceived?.Invoke(message);
        }
    }
}
