using System;

namespace General_PCR18.Common
{
    /// <summary>
    /// 缓存配置
    /// </summary>
    [Serializable]
    public class ConfigCache
    {

        /// <summary>
        /// 语言
        /// </summary>
        public string Lang { get; set; }

        /// <summary>
        /// 试管检测时长
        /// </summary>
        public string DetectionTime { get; set; }

        /// <summary>
        /// 试管检测结果存储路径
        /// </summary>
        public string DataPath { get; set; }
    }
}
