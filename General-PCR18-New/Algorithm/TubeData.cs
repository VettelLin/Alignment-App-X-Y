using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;

namespace General_PCR18.Algorithm
{
    public class TubeData
    {
        /// <summary>
        /// 原始数据
        /// </summary>
        private readonly Dictionary<int, List<uint>> m_pFluOriginal;
        /// <summary>
        /// 交叉干扰参数，扩增分析使用
        /// </summary>
        private readonly Dictionary<int, List<double>> m_ppFCrosstalkPara;
        /// <summary>
        /// 交叉干扰参数，熔曲分析使用
        /// </summary>
        private readonly Dictionary<int, List<double>> m_ppFMeltCrosstalk;
        /// <summary>
        /// 串扰
        /// </summary>
        private readonly Dictionary<int, List<double>> m_pFluCrossTalk;
        /// <summary>
        /// 中线滤波
        /// </summary>
        private readonly Dictionary<int, List<double>> m_pFluFilterMedian;
        /// <summary>
        /// 基线调整
        /// </summary>
        private readonly Dictionary<int, List<double>> m_pFluBaselineAdjust;
        /// <summary>
        /// 滤波
        /// </summary>
        private readonly Dictionary<int, List<double>> m_pFluFilter;

        private readonly Dictionary<int, List<double>> m_pFluTuibitidy;
        /// <summary>
        /// 自动调整荧光数据
        /// </summary>
        private readonly List<bool> m_arrayAutoAdjust;

        /// <summary>
        /// 6种光, 0 FAM, 1 Cy5, 2 VIX, 3 Cy55, 4 ROX, 5 MOT
        /// </summary>
        private readonly static int nChannelCount = 6;

        /// <summary>
        /// CT值
        /// </summary>
        private readonly double[] ct = new double[nChannelCount];

        public TubeData()
        {
            m_pFluOriginal = new Dictionary<int, List<uint>>();
            m_ppFCrosstalkPara = new Dictionary<int, List<double>>();
            m_ppFMeltCrosstalk = new Dictionary<int, List<double>>();
            m_pFluCrossTalk = new Dictionary<int, List<double>>();
            m_pFluFilterMedian = new Dictionary<int, List<double>>();
            m_pFluBaselineAdjust = new Dictionary<int, List<double>>();
            m_pFluFilter = new Dictionary<int, List<double>>();
            m_pFluTuibitidy = new Dictionary<int, List<double>>();

            m_arrayAutoAdjust = new List<bool>();

            AllocateMemory();
        }

        /// <summary>
        /// 获取CT值。0 FAM, 1 Cy5, 2 VIX, 3 Cy55, 4 ROX, 5 MOT
        /// </summary>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public double GetCT(int channelId) { return ct[channelId]; }

        /// <summary>
        /// 设置CT
        /// </summary>
        /// <param name="channelId"></param>
        /// <param name="CT"></param>
        public void SetCT(int channelId, double CT) { ct[channelId] = CT; }

        private void AllocateMemory()
        {
            for (int i = 0; i < nChannelCount; i++)
            {
                m_pFluOriginal[i] = new List<uint>();
                m_ppFCrosstalkPara[i] = new List<double>();
                m_ppFMeltCrosstalk[i] = new List<double>();
                m_pFluCrossTalk[i] = new List<double>();
                m_pFluFilterMedian[i] = new List<double>();
                m_pFluBaselineAdjust[i] = new List<double>();
                m_pFluFilter[i] = new List<double>();
                m_pFluTuibitidy[i] = new List<double>();

                m_arrayAutoAdjust.Add(false);
            }
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void DeleteAll()
        {
            for (int i = 0; i < nChannelCount; i++)
            {
                m_pFluOriginal[i].Clear();
                m_ppFCrosstalkPara[i].Clear();
                m_ppFMeltCrosstalk[i].Clear();
                m_pFluCrossTalk[i].Clear();
                m_pFluFilterMedian[i].Clear();
                m_pFluBaselineAdjust[i].Clear();
                m_pFluFilter[i].Clear();
                m_pFluTuibitidy[i].Clear();

                ct[i] = 0;
            }
        }

        /// <summary>
        /// 添加原始数据
        /// </summary>
        /// <param name="nChannelNo">0 FAM, 1 Cy5, 2 VIX, 3 Cy55, 4 ROX, 5 MOT</param>
        /// <param name="nYValue"></param>
        public void AddOriginalData(int nChannelNo, uint nYValue)
        {
            m_pFluOriginal[nChannelNo].Add(nYValue);
            m_pFluCrossTalk[nChannelNo].Add(nYValue);
            m_pFluFilterMedian[nChannelNo].Add(nYValue);
            m_pFluBaselineAdjust[nChannelNo].Add(nYValue);
            m_pFluFilter[nChannelNo].Add(nYValue);
            m_pFluTuibitidy[nChannelNo].Add(nYValue);
        }

        /// <summary>
        /// 获取原始数据
        /// </summary>
        /// <param name="nChannelNo">0 FAM, 1 Cy5, 2 VIX, 3 Cy55, 4 ROX, 5 MOT</param>
        /// <returns></returns>
        public List<uint> GetOriginalData(int nChannelNo)
        {
            return m_pFluOriginal[nChannelNo];
        }

        /// <summary>
        /// 数据个数
        /// </summary>
        /// <returns></returns>
        public int GetPointCout()
        {
            int[] arr = new int[] {
                m_pFluOriginal[0].Count(),
                m_pFluOriginal[1].Count(),
                m_pFluOriginal[2].Count(),
                m_pFluOriginal[3].Count(),
                m_pFluOriginal[4].Count(),
                m_pFluOriginal[5].Count()
            };
            return arr.Min();
        }

        /// <summary>
        /// 获取某个点的数据
        /// </summary>
        /// <param name="nType"></param>
        /// <param name="nChannelNo"></param>
        /// <param name="nPointNo"></param>
        /// <returns></returns>
        public double GetFluYValueBy(EDataType nType, int nChannelNo, int nPointNo)
        {
            double dReturn = 0;
            try
            {
                switch (nType)
                {
                    case EDataType.FLU_ORIGINAL:
                        dReturn = m_pFluOriginal[nChannelNo][nPointNo];
                        break;
                    case EDataType.FLU_CROSSTALK:
                        dReturn = m_pFluCrossTalk[nChannelNo][nPointNo];
                        break;
                    case EDataType.FLU_FILTER_MEDIAN:
                        dReturn = m_pFluFilterMedian[nChannelNo][nPointNo];
                        break;
                    case EDataType.FLU_BASELINE_ADJUST:
                        dReturn = m_pFluBaselineAdjust[nChannelNo][nPointNo];
                        break;
                    case EDataType.FLU_FILTER:
                        dReturn = m_pFluFilter[nChannelNo][nPointNo];
                        break;
                    case EDataType.FLU_TURBIDITY:
                        dReturn = m_pFluTuibitidy[nChannelNo][nPointNo];
                        break;
                    case EDataType.LOG_FLU_TURBIDITY:
                        dReturn = m_pFluTuibitidy[nChannelNo][nPointNo];
                        dReturn = ConvertToLog10(dReturn);
                        break;

                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                // ignore
            }

            return dReturn;
        }

        /// <summary>
        /// 设置某个点的数据
        /// </summary>
        /// <param name="nType"></param>
        /// <param name="nChannelNo"></param>
        /// <param name="nPointNo"></param>
        /// <param name="dYValue"></param>
        void SetFluYValueBy(EDataType nType, int nChannelNo, int nPointNo, double dYValue)
        {
            switch (nType)
            {
                case EDataType.FLU_ORIGINAL:
                    m_pFluOriginal[nChannelNo][nPointNo] = (uint)dYValue;
                    break;
                case EDataType.FLU_CROSSTALK:
                    m_pFluCrossTalk[nChannelNo][nPointNo] = dYValue;
                    break;
                case EDataType.FLU_FILTER_MEDIAN:
                    m_pFluFilterMedian[nChannelNo][nPointNo] = dYValue;
                    break;
                case EDataType.FLU_BASELINE_ADJUST:
                    m_pFluBaselineAdjust[nChannelNo][nPointNo] = dYValue;
                    break;
                case EDataType.FLU_FILTER:
                    m_pFluFilter[nChannelNo][nPointNo] = dYValue;
                    break;
                case EDataType.FLU_TURBIDITY:
                    m_pFluTuibitidy[nChannelNo][nPointNo] = dYValue;
                    break;
                default:
                    break;
            }
        }

        double ConvertToLog10(double dInput)
        {
            if (dInput < 0)
            {
                return Math.Log10(-dInput);
            }
            else if (dInput > 0)
            {
                return Math.Log10(dInput);
            }
            else
                return 0;
        }

        /// <summary>
        /// 获取通道数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nChannel">0 FAM, 1 Cy5, 2 VIX, 3 Cy55, 4 ROX, 5 MOT</param>
        /// <param name="nCycleCount"></param>
        /// <param name="pdXValue"></param>
        /// <param name="pdYValue"></param>
        public void GetChannelFlu(EDataType type, int nChannel, int nCycleCount, out double[] pdXValue, out double[] pdYValue)
        {
            pdXValue = new double[nCycleCount];
            pdYValue = new double[nCycleCount];

            for (int i = 0; i < nCycleCount; i++)
            {
                if (pdXValue != null)
                {
                    pdXValue[i] = i + 1;
                }
                pdYValue[i] = GetFluYValueBy(type, nChannel, i);
            }
        }

        /// <summary>
        /// 设置通道数据
        /// </summary>
        /// <param name="type"></param>
        /// <param name="nChannel"></param>
        /// <param name="nCycleCount"></param>
        /// <param name="pdYValue"></param>
        public void SetChannelFlu(EDataType type, int nChannel, int nCycleCount, double[] pdYValue)
        {
            for (int i = 0; i < nCycleCount; i++)
            {
                SetFluYValueBy(type, nChannel, i, pdYValue[i]);
            }
        }
    }

}
