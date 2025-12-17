using General_PCR18.Common;
using General_PCR18.Util;
using NPOI.HSSF.Record.CF;
using NPOI.SS.Formula.Functions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace General_PCR18.Algorithm
{
    public class PcrAlgorigthm
    {
        private static double ComputeBaselineAverageWithOverride(double[] smoothed, double ct, int channelIndex, double[] outArray, out int iCurStart, out int iCurEnd)
        {
            iCurStart = 0; iCurEnd = 0;
            try
            {
                // 优先使用 Basic Parameters 中的手动基线
                if (Common.GlobalData.BasicParameters != null && channelIndex >= 0 && channelIndex < Common.GlobalData.BasicParameters.Count)
                {
                    var p = Common.GlobalData.BasicParameters[channelIndex];
                    if (p != null && !p.AutoBaseline && p.BaselineEnd >= p.BaselineStart)
                    {
                        int n = smoothed.Length;
                        iCurStart = Math.Max(1, Math.Min(p.BaselineStart, n));
                        iCurEnd = Math.Max(iCurStart, Math.Min(p.BaselineEnd, n));
                        // 复制并按要求在窗口内做线性拟合，得到斜率b，用 y = y - b*x 去斜率
                        Array.Copy(smoothed, outArray, n);
                        double[] x = new double[n];
                        for (int i = 0; i < n; i++) x[i] = i + 1;
                        // 直接复用已有的去斜率实现
                        FluorescenceUtils.AdjustFluValueByBaseline(n, x, outArray, iCurStart, iCurEnd);

                        // 基线均值在相同窗口上计算
                        double avg = FluorescenceUtils.CalcBaselineAverage(n, x, outArray, iCurStart, iCurEnd);
                        return avg;
                    }
                }
            }
            catch { }

            // 默认：走自动计算
            return FluorescenceUtils.BaselineAverage(smoothed, outArray, ct, out iCurStart, out iCurEnd);
        }
        private static double ComputeRmse(double[] a, double[] b)
        {
            if (a == null || b == null) return 0;
            int n = Math.Min(a.Length, b.Length);
            if (n == 0) return 0;
            double s = 0;
            for (int i = 0; i < n; i++)
            {
                double d = a[i] - b[i];
                s += d * d;
            }
            return Math.Sqrt(s / n);
        }
        /// <summary>
        /// 阴性的归一化（改进版，支持背景压制、线性拟合、减1操作）
        /// </summary>
        /// <param name="data">原始荧光数据</param>
        /// <param name="xAxis">横轴：循环数（例如 1,2,3...）</param>
        /// <returns>归一化后的结果</returns>
        public static List<double> NormalizedNoPlateau(List<double> data, int baseStart, int baseEnd)
        {
            List<double> result = new List<double>();

            // 第一步：基线修正（线性拟合后去趋势）可跳过，直接进入备用路径
            // 第二步：计算 backval，取 baseStart~baseEnd 区间内的最小 4个值的平均值
            var baseWindow = data.GetRange(baseStart, baseEnd - baseStart + 1);
            var sorted = baseWindow.OrderBy(x => x).Take(4).ToList();
            double backval = sorted.Average();

            // 第三步：归一化计算
            foreach (var d in data)
            {
                double norm = d / backval;
                if (double.IsNaN(norm))
                {
                    norm = 1;
                }

                // 第四步：背景信号压制（无导数所以用全体压制）
                norm += (1 - norm) * 0.5;

                // 第五步：减1操作（便于对数显示）
                result.Add(norm - 1);
            }

            return result;
        }



        /// <summary>
        /// 简单线性拟合（只拟合一次线性项）
        /// </summary>
        private static double[] LinearFit(List<double> x, List<double> y)
        {
            int n = x.Count;
            double sumX = 0, sumY = 0, sumXY = 0, sumXX = 0;
            for (int i = 0; i < n; i++)
            {
                sumX += x[i];
                sumY += y[i];
                sumXY += x[i] * y[i];
                sumXX += x[i] * x[i];
            }
            double slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
            double intercept = (sumY - slope * sumX) / n;
            return new double[] { intercept, slope, 0 }; // 拟合出 a + b*x，二次项默认 0
        }

        /// <summary>
        /// 获取曲线CT值
        /// </summary>
        /// <param name="tubeIndex"></param>
        /// <param name="channelId"></param>
        /// <returns></returns>
        public static double GetCt(int tubeIndex, int channelId)
        {

            return GlobalData.TubeDatas[tubeIndex].GetCT(channelId);
        }

        /// <summary>
        /// 获取曲线CT值
        /// </summary>
        /// <param name="dataX"></param>
        /// <param name="dataY"></param>
        /// <returns></returns>
        public static double GetCt(List<double> dataX, List<double> dataY)
        {
            try
            {
                int num = dataX.Count;
                double[] pdx = dataX.ToArray();
                double[] pdyInput = dataY.ToArray();

                // ct
                double ct = DataAlgorithmPCR.CalcCtPos(num, pdx, pdyInput);
                return ct;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return 0;
            }
        }

        /// <summary>
        /// DeltaRn
        /// </summary>
        /// <param name="tubeIndex">试管下标</param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[]) DeltaRn(int tubeIndex, int threshold = 60)
        {
            TubeData tubeData = GlobalData.TubeDatas[tubeIndex];
            int nCycleCount = tubeData.GetPointCout();
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 0, nCycleCount, out double[] rawFAMX, out double[] rawFAM);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 1, nCycleCount, out double[] rawCy5X, out double[] rawCy5);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 2, nCycleCount, out double[] rawHEXX, out double[] rawHEX);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 3, nCycleCount, out double[] rawCy5_5X, out double[] rawCy5_5);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 4, nCycleCount, out double[] rawROXX, out double[] rawROX);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 5, nCycleCount, out double[] rawMOTX, out double[] rawMOT);

            LogHelper.Debug("计算DeltaRn：FAM={0}, Cy5={1}, HEX={2}, Cy55={3}, ROX={4}, MOT={5}, nCycleCount={6}",
                rawFAM.Length, rawCy5.Length, rawHEX.Length, rawCy5_5.Length, rawROX.Length, rawMOT.Length, nCycleCount);

            return DeltaRn(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, rawMOT, tubeIndex, threshold);
        }

        /// <summary>
        /// DeltaRn
        /// </summary>
        /// <param name="rawFAM"></param>
        /// <param name="rawCy5"></param>
        /// <param name="rawHEX"></param>
        /// <param name="rawCy5_5"></param>
        /// <param name="rawROX"></param>
        /// <param name="rawMOT"></param>
        /// <param name="tubeIndex"></param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[]) DeltaRn(double[] rawFAM,
            double[] rawCy5, double[] rawHEX, double[] rawCy5_5, double[] rawROX, double[] rawMOT, int tubeIndex = -1,
            int threshold = 60)
        {
            int nCycleCount = rawFAM.Length;
            double[] outputFAM = new double[nCycleCount];
            double[] outputCy5 = new double[nCycleCount];
            double[] outputHEX = new double[nCycleCount];
            double[] outputCy5_5 = new double[nCycleCount];
            double[] outputROX = new double[nCycleCount];

            if (nCycleCount < 3)
            {
                //int nStart = 1;
                //int nEnd = nCycleCount;
                //if (nEnd > 10)
                //{
                //    nEnd = 10;
                //}

                //CalculatedDeltaRn(nCycleCount, nStart, nEnd, rawFAM, outputFAM);
                //CalculatedDeltaRn(nCycleCount, nStart, nEnd, rawFAM, outputCy5);
                //CalculatedDeltaRn(nCycleCount, nStart, nEnd, rawFAM, outputHEX);
                //CalculatedDeltaRn(nCycleCount, nStart, nEnd, rawFAM, outputCy5_5);
                //CalculatedDeltaRn(nCycleCount, nStart, nEnd, rawFAM, outputROX);

                return (outputFAM, outputCy5, outputHEX, outputCy5_5, outputROX);
            }

            // === Step 1: Crosstalk Correction ===         
            var (correctedFAM,
                correctedCy5,
                correctedHEX,
                correctedCy5_5,
                correctedROX,
                correctedMOT
                ) = CrosstlkCorrection(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, rawMOT);
            // 应用用户可调的串扰矩阵（简单线性扣除）
            try
            {
                double[,] m = Common.GlobalData.CrosstalkMatrix; // 0=FAM,1=Cy5,2=VIC,3=Cy5.5,4=ROX
                int n = Math.Min(correctedFAM.Length, Math.Min(correctedCy5.Length, Math.Min(correctedHEX.Length, Math.Min(correctedCy5_5.Length, correctedROX.Length))));
                for (int i = 0; i < n; i++)
                {
                    double fam = correctedFAM[i];
                    double cy5 = correctedCy5[i];
                    double hex = correctedHEX[i];
                    double cy55 = correctedCy5_5[i];
                    double rox = correctedROX[i];
                    // 行=被影响通道：FAM
                    correctedFAM[i] = fam - (m[0, 1] * cy5 + m[0, 2] * hex + m[0, 3] * cy55 + m[0, 4] * rox);
                    // Cy5
                    correctedCy5[i] = cy5 - (m[1, 0] * fam + m[1, 2] * hex + m[1, 3] * cy55 + m[1, 4] * rox);
                    // HEX(VIC)
                    correctedHEX[i] = hex - (m[2, 0] * fam + m[2, 1] * cy5 + m[2, 3] * cy55 + m[2, 4] * rox);
                    // Cy5.5
                    correctedCy5_5[i] = cy55 - (m[3, 0] * fam + m[3, 1] * cy5 + m[3, 2] * hex + m[3, 4] * rox);
                    // ROX
                    correctedROX[i] = rox - (m[4, 0] * fam + m[4, 1] * cy5 + m[4, 2] * hex + m[4, 3] * cy55);
                }
            }
            catch { }

            // === Step 2: Median Filtering ===
            var (filteredFAM, filteredCy5, filteredHEX,
                filteredCy5_5, filteredROX, filtermot
                ) = MedianFiltering(correctedFAM, correctedCy5, correctedHEX, correctedCy5_5, correctedROX, correctedMOT);

            // === Step 3: Baseline Correction ===
            var (
                baselineCorrectedFAM,
                baselineCorrectedCy5,
                baselineCorrectedHEX,
                baselineCorrectedCy5_5,
                baselineCorrectedROX,
                ct
                ) = BaselineAdjust(filteredFAM, filteredCy5, filteredHEX, filteredCy5_5, filteredROX, filtermot, threshold);

            // 保存CT值（包含 A1 -> 下标 0）
            if (tubeIndex >= 0)
            {
                TubeData tubeData = GlobalData.TubeDatas[tubeIndex];
                for (int i = 0; i < ct.Length; i++)
                {
                    tubeData.SetCT(i, ct[i]);
                }
            }

            // === Step 4: MOT Calibration OR Skip Turbidity ===
            bool[] turb = Common.GlobalData.TurbidityEnabled;
            Util.LogHelper.Debug("DeltaRn Turbility flags: FAM={0}, HEX={1}, ROX={2}, Cy5={3}, Cy5.5={4}", turb[0], turb[1], turb[2], turb[3], turb[4]);
            // 先统一计算一次 MOT 校准结果，便于对比
            var motTuple = MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5,
                                          baselineCorrectedROX, correctedMOT, ct);
            // 统计输出：CT 值
            try
            {
                Util.LogHelper.Info("DeltaRn CTs: FAM={0:0.###}, Cy5={1:0.###}, HEX={2:0.###}, Cy5.5={3:0.###}, ROX={4:0.###}", ct[0], ct[1], ct[2], ct[3], ct[4]);
            }
            catch { }
            // 统计输出：MOT 信号统计（使用滤波后 MOT）
            try
            {
                double motMin = filtermot.Min();
                double motMax = filtermot.Max();
                double motMean = filtermot.Average();
                double motVar = filtermot.Select(v => (v - motMean) * (v - motMean)).Average();
                double motStd = Math.Sqrt(motVar);
                Util.LogHelper.Info("MOT stats: min={0:0.###}, max={1:0.###}, mean={2:0.###}, std={3:0.###}", motMin, motMax, motMean, motStd);
            }
            catch { }
            // 统计输出：各通道 RMSE（MOT校准 vs 基线校正）
            try
            {
                double rmseF = ComputeRmse(motTuple.Item1, baselineCorrectedFAM);
                double rmseCy5 = ComputeRmse(motTuple.Item2, baselineCorrectedCy5);
                double rmseHEX = ComputeRmse(motTuple.Item3, baselineCorrectedHEX);
                double rmseCy55 = ComputeRmse(motTuple.Item4, baselineCorrectedCy5_5);
                double rmseROX = ComputeRmse(motTuple.Item5, baselineCorrectedROX);
                Util.LogHelper.Info("RMSE (MOT vs Base): FAM={0:0.###}, Cy5={1:0.###}, HEX={2:0.###}, Cy5.5={3:0.###}, ROX={4:0.###}", rmseF, rmseCy5, rmseHEX, rmseCy55, rmseROX);
            }
            catch { }
            double[] motCalibratedFAM = turb[0] ? motTuple.Item1 : (double[])baselineCorrectedFAM.Clone();
            double[] motCalibratedCy5 = turb[3] ? motTuple.Item2 : (double[])baselineCorrectedCy5.Clone();
            double[] motCalibratedHEX = turb[1] ? motTuple.Item3 : (double[])baselineCorrectedHEX.Clone();
            double[] motCalibratedCy5_5 = turb[4] ? motTuple.Item4 : (double[])baselineCorrectedCy5_5.Clone();
            double[] motCalibratedROX = turb[2] ? motTuple.Item5 : (double[])baselineCorrectedROX.Clone();
            if (!turb[4])
            {
                try
                {
                    int n = Math.Min(motTuple.Item4.Length, baselineCorrectedCy5_5.Length);
                    double rmse = 0; for (int i = 0; i < n; i++) { double d = motTuple.Item4[i] - baselineCorrectedCy5_5[i]; rmse += d * d; }
                    rmse = n > 0 ? Math.Sqrt(rmse / n) : 0;
                    Util.LogHelper.Info("DeltaRn Cy5.5 skip: RMSE vs MOT={0}, base[10]={1}, mot[10]={2}",
                        rmse,
                        baselineCorrectedCy5_5.Length > 10 ? baselineCorrectedCy5_5[10] : 0,
                        motTuple.Item4.Length > 10 ? motTuple.Item4[10] : 0);
                }
                catch { }
            }

            // === Step 5: Smoothed ===
            Util.LogHelper.Debug("DeltaRn Before Smooth len: FAM={0}, Cy5={1}, HEX={2}, Cy5.5={3}, ROX={4}",
                motCalibratedFAM.Length, motCalibratedCy5.Length, motCalibratedHEX.Length, motCalibratedCy5_5.Length, motCalibratedROX.Length);
            var (smoothedFAM, smoothedCy5, smoothedHEX, smoothedCy5_5, smoothedROX)
                = SmoothData(motCalibratedFAM, motCalibratedCy5, motCalibratedHEX, motCalibratedCy5_5, motCalibratedROX);
            Util.LogHelper.Debug("DeltaRn After Smooth sample: Cy5.5[10] before={0} after={1}",
                motCalibratedCy5_5.Length > 10 ? motCalibratedCy5_5[10] : 0,
                smoothedCy5_5.Length > 10 ? smoothedCy5_5[10] : 0);

            double[] dFilterFAM = new double[nCycleCount];
            double[] dFilterCy5 = new double[nCycleCount];
            double[] dFilterHEX = new double[nCycleCount];
            double[] dFilterCy5_5 = new double[nCycleCount];
            double[] dFilterROX = new double[nCycleCount];
            Util.LogHelper.Debug("BaselineAverage sample: Cy5.5[10]={0}", smoothedCy5_5.Length > 10 ? smoothedCy5_5[10] : 0);
            double avgFAM = ComputeBaselineAverageWithOverride(smoothedFAM, ct[0], 0, dFilterFAM, out int iCurStartFAM, out int iCurEndFAM);
            double avgCy5 = ComputeBaselineAverageWithOverride(smoothedCy5, ct[1], 1, dFilterCy5, out int iCurStartCy5, out int iCurEndCy5);
            double avgHEX = ComputeBaselineAverageWithOverride(smoothedHEX, ct[2], 2, dFilterHEX, out int iCurStartHEX, out int iCurEndHEX);
            double avgCy5_5 = ComputeBaselineAverageWithOverride(smoothedCy5_5, ct[3], 3, dFilterCy5_5, out int iCurStartCy5_5, out int iCurEndCy5_5);
            double avgROX = ComputeBaselineAverageWithOverride(smoothedROX, ct[4], 4, dFilterROX, out int iCurStartROX, out int iCurEndROX);

            // DeltaRn = yBase - avg；并将 i < E 置零
            CalculatedDeltaRn(nCycleCount, dFilterFAM, outputFAM, avgFAM);
            // 将平台期的数据都置零
            for (int i = 0; i <= iCurEndFAM && i < outputFAM.Length; i++)
            {
                outputFAM[i] = 0;
            }

            CalculatedDeltaRn(nCycleCount, dFilterCy5, outputCy5, avgCy5);
            // 将平台期的数据都置零
            for (int i = 0; i <= iCurEndCy5 && i < outputCy5.Length; i++)
            {
                outputCy5[i] = 0;
            }

            CalculatedDeltaRn(nCycleCount, dFilterHEX, outputHEX, avgHEX);
            // 将平台期的数据都置零
            for (int i = 0; i <= iCurEndHEX && i < outputHEX.Length; i++)
            {
                outputHEX[i] = 0;
            }

            CalculatedDeltaRn(nCycleCount, dFilterCy5_5, outputCy5_5, avgCy5_5);
            // 将平台期的数据都置零
            for (int i = 0; i <= iCurEndCy5_5 && i < outputCy5_5.Length; i++)
            {
                outputCy5_5[i] = 0;
            }

            CalculatedDeltaRn(nCycleCount, dFilterROX, outputROX, avgROX);
            // 将平台期的数据都置零
            for (int i = 0; i <= iCurEndROX && i < outputROX.Length; i++)
            {
                outputROX[i] = 0;
            }

            return (outputFAM, outputCy5, outputHEX, outputCy5_5, outputROX);
        }

        /// <summary>
        /// 计算△Rn(减去前nAvgNum个数据的均值)
        /// </summary>
        /// <param name="nCycleCount"></param>
        /// <param name="pInput"></param>
        /// <param name="pOutput"></param>
        /// <param name="dAverage"></param>
        public static void CalculatedDeltaRn(int nCycleCount, double[] pInput, double[] pOutput, double dAverage)
        {

            for (int i = 0; i < nCycleCount; i++)
            {
                pOutput[i] = pInput[i] - dAverage;
            }
        }

        /// <summary>
        /// 计算△Rn(减去前nAvgNum个数据的均值)
        /// </summary>
        /// <param name="nNum"></param>
        /// <param name="nStart">以1开始</param>
        /// <param name="nEnd"></param>
        /// <param name="pInput"></param>
        /// <param name="pOutput"></param>
        public static void CalculatedDeltaRn(int nNum, int nStart, int nEnd, double[] pInput, double[] pOutput)
        {
            int nAvgNum = nEnd - nStart + 1;
            if (nAvgNum > nNum || nAvgNum <= 1)
            {
                for (int i = 0; i < nNum; i++)
                    pOutput[i] = pInput[i];
                return;
            }
            double sum = 0;
            for (int i = nStart - 1; i < nEnd; i++)
                sum += pInput[i];
            sum /= nAvgNum;

            for (int i = 0; i < nNum; i++)
                pOutput[i] = pInput[i] - sum;
        }

        /// <summary>
        /// Step 1: Crosstalk Correction 串扰校正
        /// </summary>
        /// <param name="rawFAM"></param>
        /// <param name="rawCy5"></param>
        /// <param name="rawHEX"></param>
        /// <param name="rawCy5_5"></param>
        /// <param name="rawROX"></param>
        /// <param name="rawMOT"></param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[], double[]) CrosstlkCorrection(
            double[] rawFAM,
            double[] rawCy5,
            double[] rawHEX,
            double[] rawCy5_5,
            double[] rawROX,
            double[] rawMOT)
        {
            List<int> numArr = new List<int>{rawFAM.Length, rawCy5.Length,
                rawHEX.Length, rawCy5_5.Length,
                rawROX.Length, rawMOT.Length};
            int num = numArr.Min();

            double[] correctedFAM = (double[])rawFAM.Clone();
            double[] correctedCy5 = (double[])rawCy5.Clone();
            double[] correctedHEX = new double[num];
            double[] correctedCy5_5 = new double[num];
            double[] correctedROX = new double[num];
            double[] correctedMOT = new double[num];

            for (int i = 0; i < num; i++)
            {
                correctedHEX[i] = Math.Max(10, rawHEX[i] - rawFAM[i] * 0.14);
                correctedCy5_5[i] = Math.Max(10, rawCy5_5[i] - rawCy5[i] * 0.15);
                correctedROX[i] = Math.Max(10, rawROX[i] - rawHEX[i] * 0.03);
                correctedMOT[i] = Math.Max(10, rawMOT[i] - correctedCy5_5[i] * 0);
            }

            return (correctedFAM, correctedCy5, correctedHEX, correctedCy5_5, correctedROX, correctedMOT);
        }

        /// <summary>
        /// Step 2: Median Filtering 中值波波
        /// </summary>
        /// <param name="correctedFAM"></param>
        /// <param name="correctedCy5"></param>
        /// <param name="correctedHEX"></param>
        /// <param name="correctedCy5_5"></param>
        /// <param name="correctedROX"></param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[], double[]) MedianFiltering(
            double[] correctedFAM,
            double[] correctedCy5,
            double[] correctedHEX,
            double[] correctedCy5_5,
            double[] correctedROX,
            double[] correctedMot)
        {
            int num = correctedFAM.Length;

            double[] filteredFAM = new double[num];
            double[] filteredCy5 = new double[num];
            double[] filteredHEX = new double[num];
            double[] filteredCy5_5 = new double[num];
            double[] filteredROX = new double[num];
            double[] filteredMot = new double[num];

            // TODO: 替换为C++的中值滤波， 窗口为5，窗口是来源配置文件

            int wnd = Math.Max(3, Common.GlobalData.FilterParams.MedianWindow);
            if (wnd % 2 == 0) wnd += 1;
            KdsptMedian(num, wnd, correctedFAM, filteredFAM);
            KdsptMedian(num, wnd, correctedCy5, filteredCy5);
            KdsptMedian(num, wnd, correctedHEX, filteredHEX);
            KdsptMedian(num, wnd, correctedCy5_5, filteredCy5_5);
            KdsptMedian(num, wnd, correctedROX, filteredROX);
            KdsptMedian(num, wnd, correctedMot, filteredMot);

            //ApplyMedianFilter(correctedFAM, filteredFAM, "FAM");
            //ApplyMedianFilter(correctedCy5, filteredCy5, "CY5");
            //ApplyMedianFilter(correctedHEX, filteredHEX, "HEX");
            //ApplyMedianFilter(correctedCy5_5, filteredCy5_5, "CY5.5");
            //ApplyMedianFilter(correctedROX, filteredROX, "ROX");
            //ApplyMedianFilter(correctedMot, filteredROX, "MOT");

            return (filteredFAM, filteredCy5, filteredHEX, filteredCy5_5, filteredROX, filteredMot);
        }

        /// <summary>
        /// Step 3: Baseline Adjust 基线校正
        /// </summary>
        /// <param name="filteredFAM"></param>
        /// <param name="filteredCy5"></param>
        /// <param name="filteredHEX"></param>
        /// <param name="filteredCy5_5"></param>
        /// <param name="filteredROX"></param>
        /// <param name="mot"></param>
        /// <returns>FAM,Cy5,HEX,Cy5.5,ROX,CT</returns>
        public static (double[], double[], double[], double[], double[], double[]) BaselineAdjust(
            double[] filteredFAM,
            double[] filteredCy5,
            double[] filteredHEX,
            double[] filteredCy5_5,
            double[] filteredROX,
            double[] mot,
            int threshold = 60)
        {
            int num = filteredFAM.Length;

            double[] x = new double[num];
            for (int i = 0; i < num; i++)
                x[i] = i + 1;

            double[] baselineCorrectedFAM = new double[num];
            double[] baselineCorrectedCy5 = new double[num];
            double[] baselineCorrectedHEX = new double[num];
            double[] baselineCorrectedCy5_5 = new double[num];
            double[] baselineCorrectedROX = new double[num];

            int baselineStart, baselineEnd;
            double[] ct = new double[5];

            // TODO: 5.0 改为 0.05
            // 对每个通道进行基线校正
            FluorescenceUtils.FluoAdjustBaselineStandalone(x, filteredFAM, mot, out baselineCorrectedFAM, out baselineStart, out baselineEnd, out ct[0], true, 0.05, threshold);
            FluorescenceUtils.FluoAdjustBaselineStandalone(x, filteredCy5, mot, out baselineCorrectedCy5, out baselineStart, out baselineEnd, out ct[1], true, 0.05, threshold);
            FluorescenceUtils.FluoAdjustBaselineStandalone(x, filteredHEX, mot, out baselineCorrectedHEX, out baselineStart, out baselineEnd, out ct[2], true, 0.05, threshold);
            FluorescenceUtils.FluoAdjustBaselineStandalone(x, filteredCy5_5, mot, out baselineCorrectedCy5_5, out baselineStart, out baselineEnd, out ct[3], true, 0.05, threshold);
            FluorescenceUtils.FluoAdjustBaselineStandalone(x, filteredROX, mot, out baselineCorrectedROX, out baselineStart, out baselineEnd, out ct[4], true, 0.05, threshold);

            return (baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX,
                baselineCorrectedCy5_5, baselineCorrectedROX, ct);
        }

        /// <summary>
        /// Step 4: MOT Calibration  MOT 校准
        /// </summary>
        /// <param name="baselineCorrectedFAM"></param>
        /// <param name="baselineCorrectedCy5"></param>
        /// <param name="baselineCorrectedHEX"></param>
        /// <param name="baselineCorrectedCy5_5"></param>
        /// <param name="baselineCorrectedROX"></param>
        /// <param name="mot"></param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[], double) MotCalibration(
            double[] baselineCorrectedFAM,
            double[] baselineCorrectedCy5,
            double[] baselineCorrectedHEX,
            double[] baselineCorrectedCy5_5,
            double[] baselineCorrectedROX,
            double[] mot, double[] CT)
        {
            MOTCalibration calibrator = new MOTCalibration();
            var (motCalibratedFAM, ctValue) = calibrator.ProcessData(baselineCorrectedFAM, mot, "FAM", CT[0]);
            var (motCalibratedCy5, _) = calibrator.ProcessData(baselineCorrectedCy5, mot, "Cy5", CT[1]);
            var (motCalibratedHEX, _) = calibrator.ProcessData(baselineCorrectedHEX, mot, "VIC", CT[2]);
            var (motCalibratedCy5_5, _) = calibrator.ProcessData(baselineCorrectedCy5_5, mot, "Cy5.5", CT[3]);
            var (motCalibratedROX, _) = calibrator.ProcessData(baselineCorrectedROX, mot, "ROX", CT[4]);

            return (motCalibratedFAM, motCalibratedCy5, motCalibratedHEX, motCalibratedCy5_5, motCalibratedROX, ctValue);
        }

        /// <summary>
        /// Smoothed 平滑滤波
        /// </summary>
        /// <param name="motCalibratedFAM"></param>
        /// <param name="motCalibratedCy5"></param>
        /// <param name="motCalibratedHEX"></param>
        /// <param name="motCalibratedCy5_5"></param>
        /// <param name="motCalibratedROX"></param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[]) SmoothData(
            double[] motCalibratedFAM,
           double[] motCalibratedCy5,
           double[] motCalibratedHEX,
           double[] motCalibratedCy5_5,
           double[] motCalibratedROX)
        {
            int iCycle = motCalibratedFAM.Length;
            double[] arrSmoothedFAM = new double[iCycle];
            double[] arrSmoothedcY5 = new double[iCycle];
            double[] arrSmoothedHEX = new double[iCycle];
            double[] arrSmoothedCy5_5 = new double[iCycle];
            double[] arrSmoothedROX = new double[iCycle];

            //均值滤波窗口是前1后3, 进行3次
            int passes = Math.Max(0, Common.GlobalData.FilterParams.SmoothPasses);
            int m = Math.Max(0, Common.GlobalData.FilterParams.SmoothForwardM);
            int n = Math.Max(0, Common.GlobalData.FilterParams.SmoothBackwardN);
            for (int i = 0; i < passes; i++)
            {
                FluorescenceUtils.KdsptForwardMBackN_New(iCycle, motCalibratedFAM, arrSmoothedFAM, m, n);
                FluorescenceUtils.KdsptForwardMBackN_New(iCycle, motCalibratedCy5, arrSmoothedcY5, m, n);
                FluorescenceUtils.KdsptForwardMBackN_New(iCycle, motCalibratedHEX, arrSmoothedHEX, m, n);
                FluorescenceUtils.KdsptForwardMBackN_New(iCycle, motCalibratedCy5_5, arrSmoothedCy5_5, m, n);
                FluorescenceUtils.KdsptForwardMBackN_New(iCycle, motCalibratedROX, arrSmoothedROX, m, n);
                Array.Copy(arrSmoothedFAM, motCalibratedFAM, iCycle);
                Array.Copy(arrSmoothedcY5, motCalibratedCy5, iCycle);
                Array.Copy(arrSmoothedHEX, motCalibratedHEX, iCycle);
                Array.Copy(arrSmoothedCy5_5, motCalibratedCy5_5, iCycle);
                Array.Copy(arrSmoothedROX, motCalibratedROX, iCycle);

            }

            return (arrSmoothedFAM, arrSmoothedcY5, arrSmoothedHEX, arrSmoothedCy5_5, arrSmoothedROX);

            //// First pass
            //List<double> smoothedFAM = ApplyMovingAverage(motCalibratedFAM.ToList());
            //List<double> smoothedCy5 = ApplyMovingAverage(motCalibratedCy5.ToList());
            //List<double> smoothedHEX = ApplyMovingAverage(motCalibratedHEX.ToList());
            //List<double> smoothedCy5_5 = ApplyMovingAverage(motCalibratedCy5_5.ToList());
            //List<double> smoothedROX = ApplyMovingAverage(motCalibratedROX.ToList());

            ////Second pass
            //smoothedFAM = ApplyMovingAverage(smoothedFAM);
            //smoothedCy5 = ApplyMovingAverage(smoothedCy5);
            //smoothedHEX = ApplyMovingAverage(smoothedHEX);
            //smoothedCy5_5 = ApplyMovingAverage(smoothedCy5_5);
            //smoothedROX = ApplyMovingAverage(smoothedROX);

            ////Third pass
            //smoothedFAM = ApplyMovingAverage(smoothedFAM);
            //smoothedCy5 = ApplyMovingAverage(smoothedCy5);
            //smoothedHEX = ApplyMovingAverage(smoothedHEX);
            //smoothedCy5_5 = ApplyMovingAverage(smoothedCy5_5);
            //smoothedROX = ApplyMovingAverage(smoothedROX);

            //return (smoothedFAM.ToArray(), smoothedCy5.ToArray(), smoothedHEX.ToArray(), smoothedCy5_5.ToArray(), smoothedROX.ToArray());
        }

        /// <summary>
        /// 归一化
        /// </summary>
        /// <param name="tubeIndex">试管下标</param>
        /// <returns></returns>
        public static (List<double>, List<double>, List<double>, List<double>, List<double>) Normalized(int tubeIndex, int threshold = 60)
        {
            TubeData tubeData = GlobalData.TubeDatas[tubeIndex];
            int nCycleCount = tubeData.GetPointCout();
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 0, nCycleCount, out double[] rawFAMX, out double[] rawFAM);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 1, nCycleCount, out double[] rawCy5X, out double[] rawCy5);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 2, nCycleCount, out double[] rawHEXX, out double[] rawHEX);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 3, nCycleCount, out double[] rawCy5_5X, out double[] rawCy5_5);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 4, nCycleCount, out double[] rawROXX, out double[] rawROX);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 5, nCycleCount, out double[] rawMOTX, out double[] rawMOT);

            return Normalized(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, rawMOT, threshold);
        }

        /// <summary>
        /// 归一化
        /// </summary>
        /// <param name="rawFAM"></param>
        /// <param name="rawCy5"></param>
        /// <param name="rawHEX"></param>
        /// <param name="rawCy5_5"></param>
        /// <param name="rawROX"></param>
        /// <param name="mot"></param>
        /// <returns></returns>
        public static (List<double>, List<double>, List<double>, List<double>, List<double>) Normalized(
            double[] rawFAM,
            double[] rawCy5,
            double[] rawHEX,
            double[] rawCy5_5,
            double[] rawROX,
            double[] mot,
            int threshold = 60)
        {
            // === Step 1: Crosstalk Correction ===         
            var (correctedFAM,
                correctedCy5,
                correctedHEX,
                correctedCy5_5,
                correctedROX,
                correctedMOT
                ) = CrosstlkCorrection(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, mot);

            // TODO: MOT 数据也做中值滤波
            // === Step 2: Median Filtering ===
            var (filteredFAM, filteredCy5, filteredHEX,
                filteredCy5_5, filteredROX, filtermot
                ) = MedianFiltering(correctedFAM, correctedCy5, correctedHEX, correctedCy5_5, correctedROX, correctedMOT);

            // === Step 3: Baseline Correction ===
            var (
                baselineCorrectedFAM,
                baselineCorrectedCy5,
                baselineCorrectedHEX,
                baselineCorrectedCy5_5,
                baselineCorrectedROX,
                ct
                ) = BaselineAdjust(filteredFAM, filteredCy5, filteredHEX, filteredCy5_5, filteredROX, filtermot, threshold);

            // === Step 4: MOT Calibration OR Skip Turbidity ===
            bool[] turb2 = Common.GlobalData.TurbidityEnabled;
            Util.LogHelper.Debug("Normalized Turbility flags: FAM={0}, HEX={1}, ROX={2}, Cy5={3}, Cy5.5={4}", turb2[0], turb2[1], turb2[2], turb2[3], turb2[4]);
            double[] motCalibratedFAM2 = turb2[0]
                ? MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5, baselineCorrectedROX, correctedMOT, ct).Item1
                : (double[])baselineCorrectedFAM.Clone();
            double[] motCalibratedCy52 = turb2[3]
                ? MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5, baselineCorrectedROX, correctedMOT, ct).Item2
                : (double[])baselineCorrectedCy5.Clone();
            double[] motCalibratedHEX2 = turb2[1]
                ? MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5, baselineCorrectedROX, correctedMOT, ct).Item3
                : (double[])baselineCorrectedHEX.Clone();
            double[] motCalibratedCy5_52 = turb2[4]
                ? MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5, baselineCorrectedROX, correctedMOT, ct).Item4
                : (double[])baselineCorrectedCy5_5.Clone();
            double[] motCalibratedROX2 = turb2[2]
                ? MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5, baselineCorrectedROX, correctedMOT, ct).Item5
                : (double[])baselineCorrectedROX.Clone();

            // === Step 5: Smoothed ===
            Util.LogHelper.Debug("Before Smooth len: FAM={0}, Cy5={1}, HEX={2}, Cy5.5={3}, ROX={4}",
                motCalibratedFAM2.Length, motCalibratedCy52.Length, motCalibratedHEX2.Length, motCalibratedCy5_52.Length, motCalibratedROX2.Length);
            var (smoothedFAM, smoothedCy5, smoothedHEX, smoothedCy5_5, smoothedROX)
                = SmoothData(motCalibratedFAM2, motCalibratedCy52, motCalibratedHEX2, motCalibratedCy5_52, motCalibratedROX2);
            Util.LogHelper.Debug("After Smooth sample: Cy5.5[10] before={0} after={1}",
                motCalibratedCy5_52.Length > 10 ? motCalibratedCy5_52[10] : 0,
                smoothedCy5_5.Length > 10 ? smoothedCy5_5[10] : 0);

            // === Step 6: Normalized ===
            double[] nFAM = NormalizationProcessor.ProcessNormalization(smoothedFAM, ct[0]);
            double[] nCy5 = NormalizationProcessor.ProcessNormalization(smoothedCy5, ct[1]);
            double[] nHEX = NormalizationProcessor.ProcessNormalization(smoothedHEX, ct[2]);
            double[] nCy5_5 = NormalizationProcessor.ProcessNormalization(smoothedCy5_5, ct[3]);
            double[] nROX = NormalizationProcessor.ProcessNormalization(smoothedROX, ct[4]);

            return (nFAM.ToList(), nCy5.ToList(), nHEX.ToList(), nCy5_5.ToList(), nROX.ToList());
        }

        /// <summary>
        /// 原始扩增曲线
        /// </summary>
        /// <param name="tubeIndex">试管下标</param>
        /// <returns></returns>
        public static (double[], double[], double[], double[], double[]) Amplify(int tubeIndex)
        {
            TubeData tubeData = GlobalData.TubeDatas[tubeIndex];
            int nCycleCount = tubeData.GetPointCout();
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 0, nCycleCount, out double[] rawFAMX, out double[] rawFAM);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 1, nCycleCount, out double[] rawCy5X, out double[] rawCy5);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 2, nCycleCount, out double[] rawHEXX, out double[] rawHEX);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 3, nCycleCount, out double[] rawCy5_5X, out double[] rawCy5_5);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 4, nCycleCount, out double[] rawROXX, out double[] rawROX);
            tubeData.GetChannelFlu(EDataType.FLU_ORIGINAL, 5, nCycleCount, out double[] rawMOTX, out double[] rawMOT);

            if (nCycleCount < 3)
            {
                return (rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX);
            }

            // === Step 1: Crosstalk Correction ===         
            var (correctedFAM,
                correctedCy5,
                correctedHEX,
                correctedCy5_5,
                correctedROX,
                correctedMOT
                ) = CrosstlkCorrection(rawFAM, rawCy5, rawHEX, rawCy5_5, rawROX, rawMOT);

            // === Step 2: Median Filtering ===
            var (filteredFAM, filteredCy5, filteredHEX,
                filteredCy5_5, filteredROX, filtermot
                ) = MedianFiltering(correctedFAM, correctedCy5, correctedHEX, correctedCy5_5, correctedROX, correctedMOT);

            // === Step 3: Baseline Correction ===
            var (
                baselineCorrectedFAM,
                baselineCorrectedCy5,
                baselineCorrectedHEX,
                baselineCorrectedCy5_5,
                baselineCorrectedROX,
                ct
                ) = BaselineAdjust(filteredFAM, filteredCy5, filteredHEX, filteredCy5_5, filteredROX, filtermot);

            // === Step 4: MOT Calibration OR Skip Turbidity ===
            bool[] turbA = Common.GlobalData.TurbidityEnabled;
            Util.LogHelper.Debug("Amplify Turbility flags: FAM={0}, HEX={1}, ROX={2}, Cy5={3}, Cy5.5={4}", turbA[0], turbA[1], turbA[2], turbA[3], turbA[4]);
            double[] motCalibratedFAM;
            double[] motCalibratedCy5Arr;
            double[] motCalibratedHEXArr;
            double[] motCalibratedCy55Arr;
            double[] motCalibratedROXArr;
            if (turbA[0] || turbA[1] || turbA[2] || turbA[3] || turbA[4])
            {
                var tuple = MotCalibration(baselineCorrectedFAM, baselineCorrectedCy5, baselineCorrectedHEX, baselineCorrectedCy5_5,
                    baselineCorrectedROX, correctedMOT, ct);
                motCalibratedFAM = turbA[0] ? tuple.Item1 : (double[])baselineCorrectedFAM.Clone();
                motCalibratedCy5Arr = turbA[3] ? tuple.Item2 : (double[])baselineCorrectedCy5.Clone();
                motCalibratedHEXArr = turbA[1] ? tuple.Item3 : (double[])baselineCorrectedHEX.Clone();
                motCalibratedCy55Arr = turbA[4] ? tuple.Item4 : (double[])baselineCorrectedCy5_5.Clone();
                motCalibratedROXArr = turbA[2] ? tuple.Item5 : (double[])baselineCorrectedROX.Clone();
            }
            else
            {
                motCalibratedFAM = (double[])baselineCorrectedFAM.Clone();
                motCalibratedCy5Arr = (double[])baselineCorrectedCy5.Clone();
                motCalibratedHEXArr = (double[])baselineCorrectedHEX.Clone();
                motCalibratedCy55Arr = (double[])baselineCorrectedCy5_5.Clone();
                motCalibratedROXArr = (double[])baselineCorrectedROX.Clone();
            }

            // === Step 5: Smoothed ===
            var (smoothedFAM, smoothedCy5, smoothedHEX, smoothedCy5_5, smoothedROX)
                = SmoothData(motCalibratedFAM, motCalibratedCy5Arr, motCalibratedHEXArr, motCalibratedCy55Arr, motCalibratedROXArr);

            double[] dFilterFAM = new double[nCycleCount];
            double[] dFilterCy5 = new double[nCycleCount];
            double[] dFilterHEX = new double[nCycleCount];
            double[] dFilterCy5_5 = new double[nCycleCount];
            double[] dFilterROX = new double[nCycleCount];
            ComputeBaselineAverageWithOverride(smoothedFAM, ct[0], 0, dFilterFAM, out int iCurStartFAM, out int iCurEndFAM);
            ComputeBaselineAverageWithOverride(smoothedCy5, ct[1], 1, dFilterCy5, out int iCurStartCy5, out int iCurEndCy5);
            ComputeBaselineAverageWithOverride(smoothedHEX, ct[2], 2, dFilterHEX, out int iCurStartHEX, out int iCurEndHEX);
            ComputeBaselineAverageWithOverride(smoothedCy5_5, ct[3], 3, dFilterCy5_5, out int iCurStartCy5_5, out int iCurEndCy5_5);
            ComputeBaselineAverageWithOverride(smoothedROX, ct[4], 4, dFilterROX, out int iCurStartROX, out int iCurEndROX);

            return (dFilterFAM, dFilterCy5, dFilterHEX, dFilterCy5_5, dFilterROX);
        }

        // TODO: C++是配置的。 这里是固定的前一后三

        /// <summary>
        /// Applies moving average smoothing to the input data
        /// </summary>
        /// <param name="data">Input data list</param>
        /// <returns>Smoothed data list</returns>
        private static List<double> ApplyMovingAverage(List<double> data)
        {
            int n = data.Count;
            List<double> result = new List<double>();

            for (int i = 0; i < n; i++)
            {
                List<double> window = new List<double>();

                if (i == 0)
                {
                    for (int j = i; j <= i + 3 && j < n; j++)
                    {
                        window.Add(data[j]);
                    }
                }
                else
                {
                    for (int j = i - 1; j <= i + 3; j++)
                    {
                        if (j < n)
                            window.Add(data[j]);
                        else
                            window.Add(data[n - 1]); // padding with last value
                    }
                }

                double avg = window.Average();
                result.Add(avg);
            }

            return result;
        }

        /// <summary>
        /// 中位数滤波算法
        /// </summary>
        /// <param name="n">输入数组长度</param>
        /// <param name="cfnum">窗口大小（需为≥3的奇数）</param>
        /// <param name="y">输入数组</param>
        /// <param name="yy">输出数组（存放滤波结果）</param>
        public static void KdsptMedian(int n, int cfnum, double[] y, double[] yy)
        {
            if (n > y.Length)
            {
                n = y.Length;
            }
            // 校验窗口参数，不符合要求时直接复制原数据
            if (n < cfnum || cfnum < 3 || cfnum % 2 != 1)
            {
                for (int i = 0; i < n; i++)
                {
                    yy[i] = y[i];
                }
                return;
            }

            int halfWindow = cfnum / 2;
            int beginIndex = 0;
            int endIndex = 0;

            for (int i = 0; i < n; i++)
            {
                // 当数据长度小于窗口大小时的处理逻辑
                if (n < cfnum)
                {
                    int k = (i < cfnum - 1) ? i : cfnum - 1;
                    double[] temp = new double[k + 1];

                    LogHelper.Debug("中位数滤波: y={0}, i={1}, k={2}", y.Length, i, k);

                    // 复制窗口内数据
                    Array.Copy(y, i - k, temp, 0, k + 1);
                    // 排序
                    Array.Sort(temp);
                    // 取中位数（中间位置）
                    yy[i] = temp[(k + 1) / 2];
                }
                // 当数据长度大于等于窗口大小时的处理逻辑
                else
                {
                    // 计算窗口起始索引（左边界处理）
                    beginIndex = i >= halfWindow ? i - halfWindow : 0;
                    // 计算窗口结束索引（右边界处理）
                    endIndex = (i + halfWindow <= n - 1) ? i + halfWindow : n - 1;

                    // 调整窗口保持对称性
                    int preCount = i - beginIndex;
                    int behindCount = endIndex - i;
                    if (preCount > behindCount)
                    {
                        beginIndex = i - behindCount;
                    }
                    else
                    {
                        endIndex = i + preCount;
                    }

                    // 计算窗口内元素数量
                    int count = endIndex - beginIndex + 1;
                    double[] sortArray = new double[count];

                    LogHelper.Debug("中位数滤波算法: y={0}, beginIndex={1}, count={2}", string.Join(",", y), beginIndex, count);

                    // 复制窗口内数据并排序
                    Array.Copy(y, beginIndex, sortArray, 0, count);
                    Array.Sort(sortArray);

                    // 取排序后的中间值作为结果
                    yy[i] = sortArray[count / 2];
                }
            }
            // 处理开始两点的数据，保证偏差较小
            yy[0] = yy[2] + 0.1 * (yy[0] - yy[2]);
            yy[1] = yy[2] + 0.1 * (yy[1] - yy[2]);
        }

        private static void ApplyMedianFilter(double[] input, double[] output, string channel)
        {
            int count = input.Length;

            double[] temp = new double[count];

            temp[0] = input[0];
            var win2 = new double[] { input[0], input[1], input[2], input[3] };
            temp[1] = GetChannelMedian(win2, channel);

            for (int i = 2; i <= count - 3; i++)
            {
                var win = new double[] { input[i - 2], input[i - 1], input[i], input[i + 1], input[i + 2] };
                temp[i] = win.OrderBy(x => x).ElementAt(2); // 中位数
            }

            // 56,57,58,59
            var win59 = new double[] { input[count - 4], input[count - 3], input[count - 2], input[count - 1] };
            temp[count - 2] = win59.OrderBy(x => x).ElementAt(2);
            temp[count - 1] = input[count - 1];

            Array.Copy(temp, output, count);
            output[0] = temp[2] + 0.1 * (temp[0] - temp[2]);
            output[1] = temp[2] + 0.1 * (temp[1] - temp[2]);
        }

        private static double GetChannelMedian(double[] arr, string channel)
        {
            var sorted = arr.OrderBy(x => x).ToArray();
            return channel == "CY5" ? sorted[2] : sorted[1];
        }
    }
}
