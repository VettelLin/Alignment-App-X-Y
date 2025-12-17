using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public class DataAnalysisPCR
    {
        /// <summary>
        /// 计算△Rn
        /// </summary>
        /// <param name="nPtCount"></param>
        /// <param name="pInputY"></param>
        /// <param name="pOutputY"></param>
        /// <param name="dAverage"></param>
        public static void CalcDeltaRn(int nPtCount, double[] pInputY, double[] pOutputY, double dAverage)
        {
            for (int i = 0; i < nPtCount; i++)
            {
                pOutputY[i] = pInputY[i] - dAverage;
            }
        }

        /// <summary>
        /// 根据基线区间拟合直线斜率对荧光值进行修正
        /// </summary>
        /// <param name="nPtCount"></param>
        /// <param name="pInputX"></param>
        /// <param name="pInputY"></param>
        /// <param name="iStart"></param>
        /// <param name="iEnd"></param>
        public static void AdjustFluValueByBaseline(int nPtCount, double[] pInputX, double[] pInputY, int iStart, int iEnd)
        {
            if (iStart > nPtCount)
            {
                iStart = nPtCount;
            }
            if (iEnd > nPtCount)
            {
                iEnd = nPtCount;
            }
            int iStep = iEnd - iStart + 1;

            if (iStep >= 2) // 基线区间包括2个点，进行线性拟合调整
            {
                double[] dCoef = { 0, 0, 0 };
                double[] dTemp = new double[iStep];
                //memcpy(dTemp, pInputY + (iStart - 1), sizeof(double) * iStep);
                Array.Copy(pInputY, iStart - 1, dTemp, 0, iStep);
                DataAlgorithmPCR.LinearFit(pInputX, dTemp, iStep, dCoef);

                if (Math.Abs(dCoef[1]) < 100 || (iEnd - iStart > nPtCount / 3 || iEnd - iStart > 10))
                {
                    for (int i = 0; i < nPtCount; ++i)
                    {
                        pInputY[i] -= (pInputX[i] * dCoef[1]);
                    }
                }
            }
            return;
        }

        public static int FindStartPlateauCycle(int iCycleCount, double[] pdInputX, double[] pdInputY, double dCt)
        {
            int iReturn = -1;
            int iStartCycle = (int)dCt;
            double dStartBase = pdInputY[iStartCycle];

            for (int i = iStartCycle + 1; i < iCycleCount; i++)
            {
                if (pdInputY[i] > dStartBase)
                {
                    dStartBase = pdInputY[i];
                }
                else // 开始下降
                {
                    iReturn = i;
                    break;
                }
            }

            return iReturn;
        }
    }
}
