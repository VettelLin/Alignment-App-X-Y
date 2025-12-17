using Org.BouncyCastle.Asn1.Pkcs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public class NormalizationParams
    {
        public int AvgNum { get; set; } = 3;
        public bool Minus1 { get; set; } = true;
    }

    public static class NormalizationProcessor
    {
        // 归一化参数
        private static NormalizationParams _params = new NormalizationParams();

        // 主归一化处理函数
        public static double[] ProcessNormalization(double[] inputData, double ct)
        {
            if (inputData == null || inputData.Length == 0)
                return inputData;

            return NormalizedAnalysis(inputData, _params.AvgNum, 0, _params.Minus1, ct);
        }

        // 归一化分析 - 严格按照源代码实现
        private static double[] NormalizedAnalysis(double[] pInput, int nAvgNum, int nNormType, bool bMinus1, double ct)
        {
            int nCycleCount = pInput.Length;
            if (nCycleCount < 1)
                return pInput;

            if (nAvgNum >= nCycleCount || nAvgNum <= 0)
            {
                return pInput;
            }

            double[] pOutput = new double[nCycleCount];
            Array.Copy(pInput, pOutput, nCycleCount);

            // 对数据进行排序
            Array.Sort(pOutput);

            // TODO: 差异。C++ 重新计算基线的开始和结束

            double dNormThre = 0, dsum = 0;

            //switch (nNormType)
            //{
            //    case 0: // 取前nAvgNum个点的平均值
            //        for (int i = 0; i < nAvgNum; i++)
            //            dsum += pOutput[i];
            //        dNormThre = dsum / nAvgNum;
            //        break;
            //    case 1: // 取后nAvgNum个点的平均值
            //        for (int i = nCycleCount - nAvgNum; i < nCycleCount; i++)
            //            dsum += pOutput[i];
            //        dNormThre = dsum / nAvgNum;
            //        break;
            //    default:
            //        break;
            //}

            double[] dFilter = new double[nCycleCount];
            dNormThre = FluorescenceUtils.BaselineAverage(pInput, dFilter, ct, out int iCurStart, out int iCurEnd);

            /*
            Console.WriteLine();
            Console.WriteLine("Filtering===================================================");
            double[] c_smooth_filtering = new double[] { 4951.579834, 4951.572856, 4951.579993, 4951.667825, 4951.881433, 4952.336, 4953.208727, 4968.104, 4992.167273, 5030.342545, 5088.549818, 5175.877091, 5305.300364, 5494.179636, 5764.706909, 6143.458182, 6656.601455, 7325.928727, 8161.648, 9155.767273, 10280.11055, 11488.00582, 12719.60509, 13912.02036, 15010.63564, 15975.55491, 16786.63418, 17442.40145, 17957.09673, 18352.736, 18656.21527, 18891.61455, 19080.40582, 19237.56509, 19374.19636, 19494.57164, 19603.07491, 19700.83418, 19788.89745, 19868.32073, 19942.888, 20013.27927, 20079.87855, 20142.74982, 20201.00734, 20252.31512, 20296.24115, 20333.73743, 20367.28396, 20399.84998, 20433.50816, 20469.54164, 20508.05207, 20546.03962, 20580.06755, 20608.37106, 20629.46036, 20642.59699, 20649.76481, 20653.12504 };
            for (int i = 0; i < dFilter.Length; i++)
            {
                double d = dFilter[i] - c_smooth_filtering[i];
                d = Math.Truncate(d * 1000000000) / 1000000000;
                string result = string.Format("{0:0.##########}", d);

                Console.WriteLine(result);
            }
            */

            if (dNormThre == 0)
            {
                return pInput;
            }

            // 如果需要减1
            if (bMinus1)
            {
                for (int i = 0; i < nCycleCount; i++)
                {
                    pOutput[i] = dFilter[i] / dNormThre - 1;
                }
            }
            else
            {
                // 进行归一化计算
                for (int i = 0; i < nCycleCount; i++)
                {
                    pOutput[i] = dFilter[i] / dNormThre;
                }
            }
           

            

            return pOutput;
        }
    }
}
