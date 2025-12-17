using General_PCR18.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public enum EDigitalFilterTypeBak
    {
        None = 0,
        Median = 1
    }

    public class PeakAmlCheckParamBak
    {
        public double ratioThreshold = 1.1;
        public double m_dStdThreshold = 45.0;
        public int m_forwardNum = 3;
        public int m_start = 1;
        public int m_bkCalcDotNum = 3;
        public double m_sdMultiples = 1.5;
        public double m_dCrestBkRatio = 0.001;
    }

    public class TurbidityConfigBak
    {
        public float MinRatio { get; set; }
        public int Count { get; set; }
        public List<TurbidityDyeConfig> DyeConfigs { get; set; }

        public TurbidityConfigBak()
        {
            MinRatio = 0.05f;
            Count = 5;
            DyeConfigs = new List<TurbidityDyeConfig>
        {
            new TurbidityDyeConfig { DyeName = "FAM", Channel = 1, UseMOT = true, AheadCycle = 10, MinFlu = 100, Ratio = 0.100f },
            new TurbidityDyeConfig { DyeName = "Cy5", Channel = 2, UseMOT = true, AheadCycle = 10, MinFlu = 100, Ratio = 0.200f },
            new TurbidityDyeConfig { DyeName = "VIC", Channel = 3, UseMOT = true, AheadCycle = 10, MinFlu = 100, Ratio = 0.200f },
            new TurbidityDyeConfig { DyeName = "Cy5.5", Channel = 4, UseMOT = true, AheadCycle = 10, MinFlu = 200, Ratio = 0.200f },
            new TurbidityDyeConfig { DyeName = "ROX", Channel = 5, UseMOT = true, AheadCycle = 10, MinFlu = 100, Ratio = 0.200f }
        };
        }
    }

    public class TurbidityDyeConfigBak
    {
        public string DyeName { get; set; }
        public int Channel { get; set; }
        public bool UseMOT { get; set; }
        public int AheadCycle { get; set; }
        public int MinFlu { get; set; }
        public float Ratio { get; set; }
    }

    /// <summary>
    /// MOT 校准
    /// </summary>
    public class MOTCalibrationBak
    {
        private TurbidityConfig config;

        public MOTCalibrationBak()
        {
            config = new TurbidityConfig();
        }

        // 计算Ct值的方法
        public double CalculateCt(double[] famData)
        {
            int dataLength = famData.Length;
            if (dataLength < 5) return 0;

            // 计算二阶导数
            double[] secondDerivative = new double[dataLength];
            for (int i = 2; i < dataLength - 2; i++)
            {
                // 使用五点法计算二阶导数
                secondDerivative[i] = (famData[i + 2] - 2 * famData[i] + famData[i - 2]) / 4.0;
            }

            // 找到二阶导数的最大值位置
            double maxDerivative = double.MinValue;
            int maxDerivativeIndex = 0;
            for (int i = 2; i < dataLength - 2; i++)
            {
                if (secondDerivative[i] > maxDerivative)
                {
                    maxDerivative = secondDerivative[i];
                    maxDerivativeIndex = i;
                }
            }

            // 在最大值附近进行插值，得到更精确的Ct值
            if (maxDerivativeIndex > 0 && maxDerivativeIndex < dataLength - 1)
            {
                double x1 = maxDerivativeIndex - 1;
                double x2 = maxDerivativeIndex;
                double x3 = maxDerivativeIndex + 1;
                double y1 = secondDerivative[maxDerivativeIndex - 1];
                double y2 = secondDerivative[maxDerivativeIndex];
                double y3 = secondDerivative[maxDerivativeIndex + 1];

                // 使用抛物线插值
                double a = (y3 - 2 * y2 + y1) / 2.0;
                double b = (y3 - y1) / 2.0;
                double c = y2;

                if (Math.Abs(a) > 1e-10)
                {
                    double x = -b / (2 * a);
                    return maxDerivativeIndex + x;
                }
            }

            return 0; // 如果找不到有效的Ct值，返回0表示阴性
        }

        // 严格按照C++代码实现的CalcFluDataByCtAndMOT方法
        private void CalcFluDataByCtAndMOT(double[] inputY, double[] outputY, int startNo, int cycleCount, float adjustRatio)
        {
            double startBase = inputY[startNo];
            for (int j = startNo + 1; j < cycleCount; j++)
            {
                if (inputY[j] > startBase)
                {
                    startBase = inputY[j];
                }
                else
                {
                    outputY[j] = startBase + (inputY[j] - startBase) * adjustRatio;
                }
            }
        }

        public (double[] processedData, double ctValue) ProcessData(double[] famData, double[] motData, string channelName, double CT = 0)
        {
            if (famData == null || motData == null || famData.Length != motData.Length)
            {
                LogHelper.Debug("fam:{0} | mot:{1}", famData, motData);
                return (famData, 0);
            }

            var dyeConfig = config.DyeConfigs.FirstOrDefault(d => d.DyeName == channelName);
            if (dyeConfig == null)
            {
                LogHelper.Debug("DyeConfig is null:{0}", channelName);
                return (famData, 0);
            }

            int dataLength = famData.Length;
            double[] processedData = new double[dataLength];
            Array.Copy(famData, processedData, dataLength);

            // TODO: 这里重新计算了CT值，C++用基线调整中的CT值

            double ctPosition = CT;
            if (ctPosition <= 0)
            {
                // 自动计算Ct值
                ctPosition = CalculateCt(famData);
            }
            // 找到MOT数据的起始点
            int startPos = FindStableStartPosition(motData);
            if (startPos < 0) startPos = 0;

            // 检查是否需要进行基线拟合
            if (startPos < 3)
            {
                // 阴性样本处理（Ct = 0）
                if (ctPosition == 0)
                {
                    double startBase = processedData[startPos];

                    // 严格按照C++代码实现
                    for (int j = 0; j < dataLength; j++)
                    {
                        if (j > startPos)
                        {
                            processedData[j] = startBase + (processedData[j] - startBase) * dyeConfig.Ratio;
                        }
                        else
                        {
                            processedData[j] = famData[j];
                        }
                    }
                }
                else
                {
                    // 阳性样本处理
                    CalcFluDataByCtAndMOT(famData, processedData, startPos, dataLength, dyeConfig.Ratio);
                }
            }
            else
            {
                // 需要进行基线拟合的情况
                if (ctPosition == 0)
                {
                    double startBase = processedData[startPos];
                    for (int j = 0; j < dataLength; j++)
                    {
                        if (j > startPos)
                        {
                            double motRatio = motData[j] / motData[startPos];
                            if (Math.Abs(motRatio - 1.0) > config.MinRatio)
                            {
                                processedData[j] = startBase + (processedData[j] - startBase) * dyeConfig.Ratio;
                            }
                        }
                    }
                }
                else
                {
                    int startDescendPos = FindStartDescendPosition(motData, ctPosition);
                    if (startDescendPos < 0) startDescendPos = 0;
                    CalcFluDataByCtAndMOT(famData, processedData, startDescendPos, dataLength, dyeConfig.Ratio);
                }
            }

            return (processedData, ctPosition);
        }

        private int FindStartDescendPosition(double[] motData, double ctPosition)
        {
            int startPos = (int)Math.Floor(ctPosition);
            if (startPos < 0 || startPos >= motData.Length)
                return 0;

            double maxMot = motData[startPos];
            int maxPos = startPos;

            // 向前查找MOT最大值
            for (int i = startPos; i >= 0; i--)
            {
                if (motData[i] > maxMot)
                {
                    maxMot = motData[i];
                    maxPos = i;
                }
            }

            // 向后查找MOT开始下降的位置
            for (int i = maxPos; i < motData.Length; i++)
            {
                if (motData[i] < maxMot * (1 - config.MinRatio))
                {
                    return i;
                }
            }

            return startPos;
        }

        private int FindStableStartPosition(double[] motData)
        {
            if (motData.Length < 3) return 0;

            // 计算前几个点的平均值
            double sum = motData[0] + motData[1] + motData[2];
            double avg = sum / 3;

            // 找到第一个稳定点
            for (int i = 3; i < motData.Length; i++)
            {
                if (Math.Abs(motData[i] - avg) > avg * config.MinRatio)
                {
                    return i - 1;
                }
                avg = (avg * 2 + motData[i]) / 3; // 更新移动平均
            }

            return motData.Length > 1 ? motData.Length - 2 : 0;
        }
    }

    public static class FluorescenceUtilsBak
    {
        public static void SortDouble(int nNum, double[] pInput, double[] pOutput)
        {
            Array.Copy(pInput, pOutput, nNum);
            Array.Sort(pOutput, 0, nNum);
        }

        public static void Kdspt_Median(int n, int cfnum, double[] y, double[] yy)
        {
            if (cfnum < 3 || cfnum % 2 != 1)
            {
                Array.Copy(y, yy, n);
                return;
            }
            int halfWindow = cfnum / 2;
            double[] temp = new double[cfnum];
            for (int i = 0; i < n; i++)
            {
                int beginIndex = Math.Max(0, i - halfWindow);
                int endIndex = Math.Min(n - 1, i + halfWindow);
                int count = endIndex - beginIndex + 1;
                for (int j = 0; j < count; j++)
                    temp[j] = y[beginIndex + j];
                Array.Sort(temp, 0, count);
                yy[i] = temp[count / 2];
            }
        }

        public static void DigitalFilter(double[] x, double[] dReturn, int nNum, EDigitalFilterType eType, int nWnd)
        {
            switch (eType)
            {
                case EDigitalFilterType.Median:
                    Kdspt_Median(nNum, nWnd, x, dReturn);
                    break;
                default:
                    Array.Copy(x, dReturn, nNum);
                    break;
            }
        }

        public static void KdsptForwardMBackN_New(int count, double[] inputY, double[] outputY, int forwardM, int backwardN)
        {
            if (inputY == null || outputY == null || forwardM < 0 || backwardN < 0 || count <= 0)
                return;
            if (count < forwardM + backwardN + 1)
            {
                Array.Copy(inputY, outputY, count);
                return;
            }
            for (int i = 0; i < count; ++i)
            {
                int start = Math.Max(0, i - forwardM);
                int end = Math.Min(count - 1, i + backwardN);
                int nPre = i - start;
                int nBehind = end - i;
                if (forwardM == backwardN)
                {
                    if (nPre > nBehind)
                        start = i - nBehind;
                    else
                        end = i + nPre;
                }
                double sum = 0.0;
                int actualCount = 0;
                for (int k = start; k <= end; ++k)
                {
                    sum += inputY[k];
                    ++actualCount;
                }
                if (forwardM != backwardN && i > (count - 1 - backwardN))
                {
                    for (int k = i + backwardN; k > count - 1; --k)
                    {
                        sum += inputY[count - 1];
                        ++actualCount;
                    }
                }
                outputY[i] = (actualCount > 0) ? (sum / actualCount) : inputY[i];
            }
        }

        public static void CalcFirstDerivative(int count, double[] inputY, double[] outputY)
        {
            if (count <= 0) return;
            outputY[0] = 0.0;
            for (int i = 1; i < count; ++i)
                outputY[i] = inputY[i] - inputY[i - 1];
        }

        public static void CalcSecondDerivative(int count, double[] inputY, double[] outputY)
        {
            if (count <= 0) return;
            double[] dy = new double[count];
            CalcFirstDerivative(count, inputY, dy);
            outputY[0] = 0.0;
            for (int i = 1; i < count; ++i)
                outputY[i] = dy[i] - dy[i - 1];
        }

        public static double CalculateSD(int count, double[] inputData)
        {
            if (count < 2) return 0.0;
            double sum = inputData.Take(count).Sum();
            double avg = sum / count;
            double variance = inputData.Take(count).Select(v => (v - avg) * (v - avg)).Sum();
            return Math.Sqrt(variance / (count - 1));
        }

        public static void LinearFit(double[] x, double[] y, int nNum, double[] dReturn)
        {
            double[] dt = new double[6];
            double[] a = new double[2];
            Jbsqt(x, y, nNum, a, dt);
            dReturn[0] = a[0];
            dReturn[1] = a[1];
            double dSumXY = 0.0, dSumX = 0.0, dSumY = 0.0, dSumX2 = 0.0, dSumY2 = 0.0;
            for (int i = 0; i < nNum; i++)
            {
                dSumXY += x[i] * y[i];
                dSumX += x[i];
                dSumY += y[i];
                dSumX2 += x[i] * x[i];
                dSumY2 += y[i] * y[i];
            }
            double numerator = nNum * dSumXY - dSumX * dSumY;
            double denominator = Math.Sqrt(nNum * dSumX2 - dSumX * dSumX) * Math.Sqrt(nNum * dSumY2 - dSumY * dSumY);
            dReturn[2] = (denominator != 0) ? numerator / denominator : 0;
        }

        public static void Jbsqt(double[] x, double[] y, int n, double[] a, double[] dt)
        {
            double xx = 0.0, yy = 0.0;
            for (int i = 0; i < n; i++)
            {
                xx += x[i] / n;
                yy += y[i] / n;
            }
            double e = 0.0, f = 0.0;
            for (int i = 0; i < n; i++)
            {
                double q = x[i] - xx;
                e += q * q;
                f += q * (y[i] - yy);
            }
            a[1] = f / e;
            a[0] = yy - a[1] * xx;
            double q2 = 0.0, u = 0.0, p = 0.0;
            double umax = 0.0, umin = 1.0e+30;
            for (int i = 0; i < n; i++)
            {
                double s = a[1] * x[i] + a[0];
                double diff = y[i] - s;
                q2 += diff * diff;
                p += (s - yy) * (s - yy);
                double abs_diff = Math.Abs(diff);
                if (abs_diff > umax) umax = abs_diff;
                if (abs_diff < umin) umin = abs_diff;
                u += abs_diff / n;
            }
            dt[0] = q2;
            dt[1] = Math.Sqrt(q2 / n);
            dt[2] = p;
            dt[3] = umax;
            dt[4] = umin;
            dt[5] = u;
        }

        public static double FindCrestWithParabola(double[] x, double[] y, int count, int windowWidth)
        {
            if (count < windowWidth || windowWidth < 3 || x == null || y == null) return -1;
            double[] dTempY = new double[count];
            Array.Copy(y, dTempY, count);
            // Noise suppression
            for (int k = 1; k < count - 1; ++k)
            {
                if (dTempY[k] < y[k - 1] && dTempY[k] < y[k + 1])
                    dTempY[k] = (y[k - 1] + y[k + 1]) / 2.0;
            }
            int maxSumStart = 0;
            double maxSum = -1000;
            for (int i = 0; i <= count - windowWidth; ++i)
            {
                double sum = 0;
                for (int j = 0; j < windowWidth; ++j)
                    sum += dTempY[i + j];
                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxSumStart = i;
                }
            }
            double maxCrest = dTempY.Max() / 10.0;
            int start = maxSumStart;
            int end = start + windowWidth - 1;
            int N = end - start + 1;
            if (N < 3) return -1;
            double sumx = 0, sumx2 = 0, sumx3 = 0, sumx4 = 0;
            double sumy = 0, sumxy = 0, sumx2y = 0;
            for (int j = start; j <= end; ++j)
            {
                double dj = j - start;
                double normY = dTempY[j] / maxCrest;
                sumx += dj;
                sumx2 += dj * dj;
                sumx3 += dj * dj * dj;
                sumx4 += dj * dj * dj * dj;
                sumy += normY;
                sumxy += dj * normY;
                sumx2y += dj * dj * normY;
            }
            double det = N * (sumx2 * sumx4 - sumx3 * sumx3)
                       - sumx * (sumx * sumx4 - sumx2 * sumx3)
                       + sumx2 * (sumx * sumx3 - sumx2 * sumx2);
            if (Math.Abs(det) < 1e-12) return -1;
            double a = (sumy * (sumx2 * sumx4 - sumx3 * sumx3)
                      - sumxy * (sumx * sumx4 - sumx2 * sumx3)
                      + sumx2y * (sumx * sumx3 - sumx2 * sumx2)) / det;
            double b = (-sumy * (sumx * sumx4 - sumx2 * sumx3)
                      + sumxy * (N * sumx4 - sumx2 * sumx2)
                      - sumx2y * (N * sumx3 - sumx * sumx2)) / det;
            double c = (sumy * (sumx * sumx3 - sumx2 * sumx2)
                      - sumxy * (N * sumx3 - sumx * sumx2)
                      + sumx2y * (N * sumx2 - sumx * sumx)) / det;
            double xPeak = -b / (2 * c);
            xPeak += start;
            if ((int)(xPeak + 0.5) >= count)
                xPeak = count - 1;
            else if ((int)(xPeak + 0.5) <= 1)
                xPeak = start + N / 2.0;
            if (xPeak > end)
                xPeak = end - 1 + xPeak - (int)xPeak;
            else if (xPeak < start)
                xPeak = start + xPeak - (int)xPeak;
            return xPeak;
        }

        public static double FindCtPosWithParabola(int count, double[] x, double[] y, double[] firstDer, double[] secondDer)
        {
            const int winWidth = 6;
            int maxSumStart = -1;
            double maxSum = 0;
            for (int i = 0; i < count - winWidth; ++i)
            {
                double sum = 0;
                for (int j = 0; j < winWidth; ++j)
                    sum += secondDer[i + j];
                if (sum > maxSum)
                {
                    maxSum = sum;
                    maxSumStart = i;
                }
            }
            if (maxSumStart == -1)
                return 0.0;
            double maxFirstDer = firstDer[maxSumStart];
            int maxPos = maxSumStart;
            for (int i = maxSumStart; i < maxSumStart + winWidth && i < count; ++i)
            {
                if (firstDer[i] < 0) break;
                if (firstDer[i] > maxFirstDer)
                {
                    maxFirstDer = firstDer[i];
                    maxPos = i;
                }
            }
            int minPos = maxSumStart;
            for (int i = maxPos - 1; i >= maxSumStart; --i)
            {
                if (firstDer[i] > firstDer[i + 1])
                {
                    minPos = i;
                    break;
                }
            }
            double[] zeroedSecond = new double[count];
            for (int i = 0; i < count; ++i)
                zeroedSecond[i] = secondDer[i] < 0 ? 0 : secondDer[i];
            double maxPosX = 0;
            int startPos = minPos;
            do
            {
                maxPosX = FindCrestWithParabola(x.Skip(startPos).ToArray(), zeroedSecond.Skip(startPos).ToArray(), count - startPos, winWidth);
                int posMax = (int)(maxPosX + 0.5) + startPos;
                if (posMax < count && firstDer[posMax] > 0)
                {
                    double sum = 0;
                    for (int i = posMax - winWidth / 2; i < posMax + winWidth / 2 && i < count; ++i)
                        sum += zeroedSecond[i];
                    if (sum > 100)
                    {
                        maxPosX += startPos;
                        break;
                    }
                    else
                    {
                        startPos = posMax;
                        maxPosX = 0;
                    }
                }
                else
                {
                    startPos = posMax;
                    maxPosX = 0;
                }
            } while (startPos < count - 5);
            return maxPosX;
        }

        public static bool MeetForeAvgAndEndAvgCondition(int count, double[] inputData, PeakAmlCheckParam param)
        {
            if (count <= 0) return false;
            double dForeAvg = 0.0, dEndAvg = 0.0;
            int nForeID = (int)(count / 3.0 + 0.5);
            int nEndID = (int)(count * 8.0 / 9.0 + 0.5);
            for (int i = 0; i < nForeID; ++i)
                dForeAvg += inputData[i];
            if (nEndID >= 10)
                nEndID = count - 3;
            for (int i = nEndID; i < count; ++i)
                dEndAvg += inputData[i];
            dForeAvg = (nForeID > 0) ? dForeAvg / nForeID : 0;
            dEndAvg = (nEndID > 0) ? dEndAvg / (count - nEndID) : 0;
            return dForeAvg * param.ratioThreshold < dEndAvg;
        }

        /// <summary>
        /// 通过斜率查找峰值
        /// </summary>
        /// <param name="nPtCount">点的数量</param>
        /// <param name="pInputX">X坐标数组</param>
        /// <param name="pInputY">Y坐标数组</param>
        /// <param name="iWndWidth">窗口宽度</param>
        /// <returns>是否找到峰值</returns>
        public static bool FindPeakAmlBySlope(int nPtCount, double[] pInputX, double[] pInputY, int iWndWidth = 5)
        {
            bool bFindPeak = false;

            double[] dPreCoef = new double[3];
            double[] dCurCoef = new double[3];
            double[] pdTempX = new double[iWndWidth];
            double[] pdTempY = new double[iWndWidth];

            for (int i = 3; i < nPtCount - iWndWidth + 1; i++)
            {
                bool bContinueRise = true;
                for (int j = 0; j < iWndWidth - 1; j++)
                {
                    if (pInputY[j + i + 1] < pInputY[j + i])
                    {
                        bContinueRise = false;
                        break;
                    }
                }

                if (bContinueRise) // 计算前后点的斜率
                {
                    int iTempCount = iWndWidth;
                    for (int ii = 0; ii < iTempCount; ii++)
                    {
                        pdTempX[ii] = pInputX[ii + i];
                        pdTempY[ii] = pInputY[ii + i];
                    }
                    LinearFit(pdTempX, pdTempY, iTempCount, dCurCoef);
                    if (dCurCoef[1] < 80)
                        continue;

                    if (i < iWndWidth)
                    {
                        iTempCount = i + 1;
                    }

                    for (int ii = 0; ii < iTempCount; ii++)
                    {
                        pdTempX[ii] = pInputX[i - iTempCount + 1 + ii];
                        pdTempY[ii] = pInputY[i - iTempCount + 1 + ii];
                    }
                    LinearFit(pdTempX, pdTempY, iTempCount, dPreCoef);

                    if (dCurCoef[1] - dPreCoef[1] > 50)
                    {
                        bFindPeak = true;
                        break;
                    }
                }
            }

            return bFindPeak;
        }

        public static bool IsPeakAml(double[] pDX, double[] pDY, int nDataNum, PeakAmlCheckParam peakAmlChkParam, out double dOutCt)
        {
            double max = Math.Abs(pDY[0]);
            for (int i = 1; i < nDataNum; ++i)
                max = Math.Max(max, Math.Abs(pDY[i]));
            if (max == 0) { dOutCt = 0; return false; }
            double dSD = CalculateSD(nDataNum, pDY);
            if (dSD <= peakAmlChkParam.m_dStdThreshold) { dOutCt = 0; return false; }
            bool bMeetAvg = MeetForeAvgAndEndAvgCondition(nDataNum, pDY, peakAmlChkParam);
            bool bFindNormalD2Max = false;
            double[] pDTemp = (double[])pDY.Clone();
            double[] pDFiltered = new double[nDataNum];
            double[] pDReturn = new double[nDataNum];
            DigitalFilter(pDTemp, pDFiltered, nDataNum, EDigitalFilterType.Median, 5);
            Array.Copy(pDFiltered, pDTemp, nDataNum);
            for (int i = 0; i < 3; ++i)
            {
                KdsptForwardMBackN_New(nDataNum, pDTemp, pDFiltered, 2, 2);
                Array.Copy(pDFiltered, pDTemp, nDataNum);
            }
            CalcSecondDerivative(nDataNum, pDFiltered, pDReturn);
            double dXPosMax = FindCtPosWithParabola(nDataNum, pDX, pDFiltered, pDTemp, pDReturn);
            dOutCt = dXPosMax;
            int nxpos_max = (int)(dXPosMax + 0.5);
            double dmax = (nxpos_max > 0 && nxpos_max < nDataNum) ? pDReturn[nxpos_max - 1] : 0;
            int blDotNum = nxpos_max - peakAmlChkParam.m_forwardNum - peakAmlChkParam.m_start + 1;
            if (blDotNum > 1)
            {
                double dBLAvg = 0, dBLSD = 0;
                for (int i = 0; i < blDotNum; ++i)
                    dBLAvg += pDReturn[i];
                dBLAvg /= blDotNum;
                for (int i = 0; i < nxpos_max - 3; ++i)
                    dBLSD += Math.Pow(pDReturn[i] - dBLAvg, 2);
                dBLSD = Math.Sqrt(dBLSD / (blDotNum - 1));
                if (dmax > (dBLAvg + peakAmlChkParam.m_sdMultiples * dBLSD))
                    bFindNormalD2Max = true;
            }
            double dCrestBkRatio = 0, dBkSum = 0, dBkAvg = 0;
            for (int i = 0; i < peakAmlChkParam.m_bkCalcDotNum && i < nDataNum; ++i)
                dBkSum += pDY[i];
            if (peakAmlChkParam.m_bkCalcDotNum > 0)
                dBkAvg = dBkSum / peakAmlChkParam.m_bkCalcDotNum;
            if (dBkAvg != 0)
                dCrestBkRatio = Math.Abs(dmax / dBkAvg);
            bool bFindPeak = false;
            if (bFindNormalD2Max && dCrestBkRatio > peakAmlChkParam.m_dCrestBkRatio)
            {
                if (bMeetAvg)
                {
                    bFindPeak = true;
                }
                //TODO: 省略部分分支，按需补全
                else if (dXPosMax > 1)
                {
                    int iCurStart, iCurEnd;
                    Array.Copy(pDFiltered, pDTemp, nDataNum);

                    CalcAutoBaselinePosBySndDrvCt(dXPosMax, nDataNum, pDX, pDFiltered, out iCurStart, out iCurEnd);
                    if (iCurEnd - iCurStart < dXPosMax / 2)
                    {
                        iCurStart = iCurEnd - (int)(dXPosMax / 2);
                        if (iCurStart < 1)
                        {
                            iCurStart = 1;
                        }
                    }

                    Array.Copy(pDFiltered, pDTemp, nDataNum);
                    AdjustFluValueByBaseline(nDataNum, pDX, pDFiltered, iCurStart, iCurEnd);
                    bFindPeak = MeetForeAvgAndEndAvgCondition(nDataNum, pDFiltered, peakAmlChkParam);
                }
                else
                {
                    bFindPeak = false;
                }
            }
            else
            {
                bFindPeak = false;
            }

            dOutCt = bFindPeak ? dOutCt : 0;

            // TODO: 没有找到使用斜率找CT，补救方法

            if (!bFindPeak && dXPosMax > 3)
            {
                bFindPeak = FindPeakAmlBySlope(nDataNum, pDX, pDY);
            }

            return bFindPeak;
        }

        /// <summary>
        /// 基线调整
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="motY"></param>
        /// <param name="outCorrectedY"></param>
        /// <param name="baselineStart"></param>
        /// <param name="baselineEnd"></param>
        /// <param name="outCt"></param>
        /// <param name="autoBaseline"></param>
        /// <param name="minMOTRatio"></param>
        public static void FluoAdjustBaselineStandalone(
            double[] x,
            double[] y,
            double[] motY,
            out double[] outCorrectedY,
            out int baselineStart,
            out int baselineEnd,
            out double outCt,
            bool autoBaseline = true,
            double minMOTRatio = 100
        )
        {
            int nCycleCount = x.Length;
            outCorrectedY = new double[nCycleCount];
            baselineStart = 0;
            baselineEnd = 0;
            outCt = 0;
            if (nCycleCount < 8) return;

            double[] tempX = (double[])x.Clone();
            double[] tempY = (double[])y.Clone();
            double[] filteredY = new double[nCycleCount];

            // Step 1: MOT-based cycle limit
            int motMaxCycle = CalcCycleOfMaxMOTRatioBy(tempX, motY, minMOTRatio);
            if (motMaxCycle < 5) motMaxCycle = nCycleCount;

            bool hasCt = false;
            double dSndDrvCt = 0;

            //TODO:  第2步去掉
            //Step 2: Smooth data
            //if (autoBaseline)
            //{
            //    DigitalFilter(tempY, filteredY, nCycleCount, EDigitalFilterType.Median, 5);
            //    tempY = (double[])filteredY.Clone();

            //    for (int i = 0; i < 3; ++i)
            //    {
            //        KdsptForwardMBackN_New(nCycleCount, tempY, filteredY, 2, 2);
            //        tempY = (double[])filteredY.Clone();
            //    }
            //}

            // Step 3: Peak detection
            PeakAmlCheckParam peakParam = new PeakAmlCheckParam();
            hasCt = IsPeakAml(tempX, tempY, nCycleCount, peakParam, out dSndDrvCt);

            //TODO: 第4步和第5步对调

            // Step 5: Re-fetch median-filtered signal before baseline adjust
            if (autoBaseline)
            {
                DigitalFilter(y, tempY, nCycleCount, EDigitalFilterType.Median, 5);
                for (int i = 0; i < 3; ++i)
                {
                    KdsptForwardMBackN_New(nCycleCount, tempY, filteredY, 2, 2);
                    tempY = (double[])filteredY.Clone();
                }
            }

            // Step 4: Baseline start/end detection
            int iCurStart = 3;  // Changed from 1 to 3 to match C++
            int iCurEnd = 8;
            if (autoBaseline)
            {
                if (hasCt)
                {
                    CalcAutoBaselinePosBySndDrvCt(dSndDrvCt, nCycleCount, tempX, tempY, out iCurStart, out iCurEnd);
                    if (iCurEnd - iCurStart < dSndDrvCt / 2)
                    {
                        iCurStart = iCurEnd - (int)(dSndDrvCt / 2);
                        if (iCurStart < 1) iCurStart = 1;
                    }

                    if (iCurEnd > motMaxCycle)
                        iCurEnd = motMaxCycle;
                }
                else
                {
                    iCurStart = 1;
                    iCurEnd = nCycleCount;
                    CalcBaselinePosOfNoCt(tempX, tempY, out iCurStart, out iCurEnd);
                    if (nCycleCount > 20 && iCurEnd - iCurStart > nCycleCount * 2 / 3)
                    {
                        iCurStart = iCurEnd - nCycleCount * 2 / 3;
                    }
                }
            }

            // Step 6: Final baseline adjustment
            outCorrectedY = (double[])y.Clone();
            AdjustFluValueByBaseline(nCycleCount, x, outCorrectedY, iCurStart, iCurEnd);
            baselineStart = iCurStart;
            baselineEnd = iCurEnd;
            outCt = dSndDrvCt;
        }

        public static void CalMeltFirstDerivative(double[] x, double[] y, bool bBackMinusFront, int stepNum, double[] dReturn)
        {
            int n = y.Length;
            if (stepNum < 1) stepNum = 1;

            if (bBackMinusFront)
            {
                for (int i = 0; i < n - stepNum; ++i)
                {
                    dReturn[i] = y[i + stepNum] - y[i]; // forward difference
                }
                dReturn[n - 1] = 0.0;
            }
            else
            {
                for (int i = stepNum; i < n; ++i)
                {
                    dReturn[i] = y[i - stepNum] - y[i]; // backward difference
                }
                dReturn[0] = 0.0;
            }
        }

        public static int CalcCycleOfMaxMOTRatioBy(double[] x, double[] y, double dMinRatio)
        {
            int nCycleCount = y.Length;
            int nCalcCycle = -1;

            if (nCycleCount == 0) return -1;

            // Step 1: Find min and max
            double dMaxValue = y[0];
            double dMinValue = y[0];
            for (int i = 1; i < nCycleCount; ++i)
            {
                if (y[i] > dMaxValue) dMaxValue = y[i];
                if (y[i] < dMinValue) dMinValue = y[i];
            }

            // Step 2: Handle invalid or flat data
            if (dMaxValue < 1 || dMinValue < 1 || dMaxValue == dMinValue)
                return -1;

            // Step 3: Derivative of unnormalized values
            double[] derivateValue = new double[nCycleCount];
            CalMeltFirstDerivative(x, y, true, 1, derivateValue);

            // Step 4: Find max derivative value
            double dMaxValueY = derivateValue[0];
            for (int i = 1; i < nCycleCount; ++i)
            {
                if (derivateValue[i] > dMaxValueY)
                {
                    dMaxValueY = derivateValue[i];
                    nCalcCycle = (int)x[i];
                }
            }

            // Step 5: Validate against minimum threshold
            if (dMaxValueY < dMinRatio)
                nCalcCycle = -1;

            return nCalcCycle;
        }

        public static int CalcCycleOfMaxMOTRatio(double[] x, double[] y, double minRatio)
        {
            if (x == null || y == null || x.Length != y.Length)
                return -1;

            return CalcCycleOfMaxMOTRatioBy(x, y, minRatio);
        }

        public static void CalcBaselinePosByCt(int ctIndex, out int outStart, out int outEnd)
        {
            int tempStart = 3;
            int tempEnd = ctIndex - 5;

            if (tempEnd <= tempStart)
            {
                tempStart = 1;
                if (tempEnd - tempStart < 3)
                    tempEnd += 3;
                if (tempEnd < tempStart)
                    tempEnd = tempStart;
            }
            else if (tempEnd - tempStart < 3)
            {
                tempStart = 1;
                if (tempEnd - tempStart < 5)
                    tempEnd += 1;
            }
            else if (tempEnd - tempStart < 5)
            {
                tempStart -= 1;
            }

            outStart = tempStart;
            outEnd = tempEnd;
        }

        public static void CalcBaselinePosOfNoCt(double[] x, double[] y, out int outStart, out int outEnd)
        {
            int nPtCount = y.Length;
            if (nPtCount < 2)
            {
                outStart = 0;
                outEnd = nPtCount - 1;
                return;
            }

            double dSum = 0;
            for (int i = nPtCount / 2; i < nPtCount; ++i)
            {
                dSum += y[i];
            }

            double dAverage = dSum / (nPtCount / 2);
            outStart = 1;
            outEnd = nPtCount;

            for (int i = 1; i < nPtCount; ++i)
            {
                if (Math.Abs(y[i] - y[i - 1]) > dAverage * 0.1)
                {
                    outStart = i;
                }
                else
                {
                    break;
                }
            }

            for (int i = nPtCount - 1; i > outStart; --i)
            {
                if (Math.Abs(y[i] - y[i - 1]) > dAverage * 0.1)
                {
                    outEnd = i - 1;
                }
                else
                {
                    break;
                }
            }

            if (outStart > nPtCount / 2 || outEnd < nPtCount / 2 || (outEnd - outStart) < nPtCount / 2)
            {
                outStart = 1;
                outEnd = nPtCount;
            }
        }

        public static void CalcAutoBaselinePosBySndDrvCt(double ct, int count, double[] x, double[] y, out int outStart, out int outEnd)
        {
            if (ct > 4)
            {
                int posCt = (int)ct;
                CalcBaselinePosByCt(posCt, out outStart, out outEnd);

                if (outEnd - outStart > 4)
                {
                    bool plateauFound = false;
                    int winWidth = (outEnd - outStart) / 3;
                    if (winWidth < 3) winWidth = 3;

                    double[] tempX = new double[winWidth];
                    double[] tempY = new double[winWidth];
                    double[] curCoef = new double[3];
                    double firstSlope = 0, minSlope = 0;
                    int minStart = 0;
                    bool foundFirstSlope = false;

                    for (int i = outEnd; i >= outStart + winWidth; --i)
                    {
                        for (int j = 0; j < winWidth; ++j)
                        {
                            tempX[j] = x[i - winWidth + 1 + j];
                            tempY[j] = y[i - winWidth + 1 + j];
                        }

                        LinearFit(tempX, tempY, winWidth, curCoef);

                        if (i == outEnd)
                        {
                            minSlope = curCoef[1];
                            minStart = i;
                        }
                        else if (Math.Abs(curCoef[1]) < Math.Abs(minSlope))
                        {
                            minSlope = curCoef[1];
                            minStart = i;
                        }

                        if (!foundFirstSlope && curCoef[1] < 10)
                        {
                            firstSlope = curCoef[1];
                            outEnd = i;
                            foundFirstSlope = true;
                            plateauFound = true;
                        }
                        else if (foundFirstSlope && Math.Abs(curCoef[1] - firstSlope) > Math.Abs(firstSlope) * 0.5)
                        {
                            outStart = i - winWidth + 1;
                            break;
                        }
                    }

                    if (!plateauFound)
                    {
                        bool foundStart = false;
                        for (int i = minStart - 1; i >= outStart + winWidth; --i)
                        {
                            for (int j = 0; j < winWidth; ++j)
                            {
                                tempX[j] = x[i - winWidth + 1 + j];
                                tempY[j] = y[i - winWidth + 1 + j];
                            }
                            LinearFit(tempX, tempY, winWidth, curCoef);
                            if (Math.Abs(curCoef[1] - minSlope) > Math.Abs(minSlope) * 0.5)
                            {
                                outStart = i - winWidth + 1;
                                foundStart = true;
                                break;
                            }
                        }
                        if (!foundStart) outStart = minStart - winWidth + 1;

                        bool foundEnd = false;
                        for (int i = minStart + 1; i <= outEnd; ++i)
                        {
                            for (int j = 0; j < winWidth; ++j)
                            {
                                tempX[j] = x[i - winWidth + 1 + j];
                                tempY[j] = y[i - winWidth + 1 + j];
                            }
                            LinearFit(tempX, tempY, winWidth, curCoef);
                            if (Math.Abs(curCoef[1] - minSlope) > Math.Abs(minSlope) * 0.5)
                            {
                                outEnd = i;
                                foundEnd = true;
                                break;
                            }
                        }
                        if (!foundEnd) outEnd = minStart;
                    }

                    // Refine again forward
                    foundFirstSlope = false;
                    winWidth = (outEnd - outStart) / 3;
                    if (winWidth < 3) winWidth = 3;

                    for (int i = outStart; i <= outEnd - winWidth + 1; ++i)
                    {
                        for (int j = 0; j < winWidth; ++j)
                        {
                            tempX[j] = x[i + j];
                            tempY[j] = y[i + j];
                        }

                        LinearFit(tempX, tempY, winWidth, curCoef);

                        if (i == outStart)
                        {
                            firstSlope = curCoef[1];
                        }
                        else if (Math.Abs(curCoef[1]) >= 10 &&
                                 Math.Abs(curCoef[1] - firstSlope) > Math.Abs(firstSlope) * 0.5)
                        {
                            outEnd = i + winWidth - 1;
                            break;
                        }
                    }
                }
            }
            else
            {
                if (count > 20)
                {
                    outEnd = count - 3;
                    outStart = outEnd - (count * 2 / 3);
                }
                else
                {
                    outStart = 1;
                    outEnd = count;
                }
            }
        }

        public static void AdjustFluValueByBaseline(int count, double[] x, double[] y, int start, int end)
        {
            if (start > count) start = count;
            if (end > count) end = count;

            int step = end - start + 1;
            if (step >= 2)
            {
                double[] tempY = new double[step];
                Array.Copy(y, start - 1, tempY, 0, step);

                double[] tempXstep = new double[step];
                Array.Copy(x, start - 1, tempXstep, 0, step);

                double[] coef = new double[3];
                LinearFit(tempXstep, tempY, step, coef);

                if (Math.Abs(coef[1]) < 100 || (step > count / 3 || step > 10))
                {
                    for (int i = 0; i < count; ++i)
                    {
                        y[i] -= x[i] * coef[1];  // Subtract baseline trend
                    }
                }
            }
        }
    }
}
