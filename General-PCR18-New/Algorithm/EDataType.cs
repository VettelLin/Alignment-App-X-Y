using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    /// <summary>
    ///  处理数据的顺序，先计算通道串扰、滤波、浊度调整，同时保存
    /// </summary>
    public enum EDataType
    {
        /// <summary>
        /// 原始荧光值
        /// </summary>
        FLU_ORIGINAL = 0,
        /// <summary>
        /// 串扰修正后数据
        /// </summary>
        FLU_CROSSTALK = 1,
        /// <summary>
        /// 中值滤波数据
        /// </summary>
        FLU_FILTER_MEDIAN = 2,
        /// <summary>
        /// 基线修正后数据，使用浊度调整参数
        /// </summary>
        FLU_BASELINE_ADJUST = 3,
        /// <summary>
        /// 浊度调整后数据
        /// </summary>
        FLU_TURBIDITY = 4,
        /// <summary>
        /// 均值滤波数据
        /// </summary>
        FLU_FILTER = 5,
        /// <summary>
        /// 归一化数据，扩增曲线
        /// </summary>
        FLU_NORMALIZATE = 6,
        /// <summary>
        /// DeltaRn数据
        /// </summary>
        FLU_DELTARN = 7,
        /// <summary>
        /// 滤波数据的对数值
        /// </summary>
        LOG_FLU_FILTER = 8,
        /// <summary>
        /// 浊度调整后数据的对数值，如果无效，则等同于滤波数据的对数值
        /// </summary>
        LOG_FLU_TURBIDITY = 9,
        /// <summary>
        /// 归一化数据的对数值
        /// </summary>
        LOG_FLU_NORMALIZATE = 10,
        /// <summary>
        /// DeltaRn数据的对数值
        /// </summary>
        LOG_FLU_DELTARN = 11,
        /// <summary>
        /// 归一化处理之后的熔曲数据
        /// </summary>
        MELT_NORMALIZE = 12,
        /// <summary>
        /// 一阶负导数处理之后的数据
        /// </summary>
        MELT_FSTNEGRECIPROCAL = 13,
        /// <summary>
        /// 增益处理之后的熔曲数据
        /// </summary>
        MELT_GAINDATA = 14,
    }
}
