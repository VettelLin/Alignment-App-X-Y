using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static General_PCR18.Algorithm.DataAlgorithm;

namespace General_PCR18.Algorithm
{
    public struct TagFunAmpNormalizedAnaParamInfo
    {
        public bool bAbleToCalcu;
        public int nAvgNum;  // 平均点数（平滑处理）
        public int nNormalizeStartPreXPos;
        public int nFiltNum;

        public bool bBaseCorrection; // 是否执行基线校正
        public double dBCK;
        public double dBCR2;  // 基线拟合相关参数 
        public int nBCXPosThre;
        public int nBCStart;
        public int nBCEndPreXPos;
        public int nBCEndPreNumPos;

        public bool bMinus1;  // 是否减1处理

        public bool bBaseSuppression;  // 是否进行低值抑制（Base Suppression）
        public double dBSRatioAllData;  // 低值压制比例参数
        public double dBSRatioBaseLine;  // 低值压制比例参数
    }

    public enum DigitalFilterType
    {
        FILTERTYPE_CFMEAN3 = 0,            ///< 中点滑动平均3点
		FILTERTYPE_CFMEAN5 = 1,			   ///< 中点滑动平均5点	
		FILTERTYPE_CFMEAN7 = 2,			   ///< 中点滑动平均7点
		FILTERTYPE_CFMEAN9 = 14,
        FILTERTYPE_CFMEAN11 = 15,
        FILTERTYPE_CFMEAN13 = 16,
        FILTERTYPE_CFMEAN15 = 17,
        FILTERTYPE_CFMEAN17 = 18,
        FILTERTYPE_CFMEAN19 = 19,
        FILTERTYPE_CFMEAN21 = 20,
        FILTERTYPE_CFMEAN23 = 21,
        FILTERTYPE_CFMEAN25 = 22,
        FILTERTYPE_CFMEAN27 = 23,
        FILTERTYPE_CFMEAN29 = 24,
        FILTERTYPE_CFMEAN31 = 25,

        FILTERTYPE_MEDIAN = 26,

        FILTERTYPE_QuadraticSmooth_5 = 3,	///< 五点两次
		FILTERTYPE_CubicSmooth_5 = 4,		///< 五点三次
		FILTERTYPE_QuadraticSmooth_7 = 5,	///< 七点两次
		FILTERTYPE_CubicSmooth_7 = 6,       ///< 七点三次

        FILTERTYPE_DTMEAN3 = 7,				///< 前置滑动平均3点
		FILTERTYPE_DTMEAN4 = 8,				///< 前置滑动平均4点
		FILTERTYPE_DTMEAN5 = 9,				///< 前置滑动平均5点
		FILTERTYPE_DTMEAN6 = 10,				///< 前置滑动平均6点
		FILTERTYPE_DTMEAN7 = 11,                ///< 前置滑动平均7点

        FILTERTYPE_TriangularSmooths_29 = 12,///< 29点三角滤波
		FILTERTYPE_GaussianSmooths_31 = 13  ///< 31点伪高斯
	};


    public class DataAlgorithmPCR
    {
        static float m_dd2MaxXThre;    //二阶导最大值对应X轴位置小于dd2MaxXThreshold * 循环数(默认值为35)时，比较二阶导最大值是否大于某个数，如果大于，则认为是扩增，反之认为不是扩增
        static float m_dd2MaxYThre;	//二阶导最大值阈值

        public static TagFunAmpNormalizedAnaParamInfo tagFunAmpNormalizedAnaParamInfo = new TagFunAmpNormalizedAnaParamInfo()
        {
            bAbleToCalcu = false,
            nAvgNum = 3,
            nNormalizeStartPreXPos = 5,
            nFiltNum = 4,
            bBaseCorrection = true,
            dBCK = 0xFFFF,
            dBCR2 = 0.5,
            nBCXPosThre = 10,
            nBCStart = 1,
            nBCEndPreXPos = 3,
            nBCEndPreNumPos = 0,
            bMinus1 = false,
            bBaseSuppression = true,
            dBSRatioAllData = 0.5,
            dBSRatioBaseLine = 0.5
        };

        /// <summary>
        /// 数字滤波器
        /// </summary>
        /// <param name="x"></param>
        /// <param name="dReturn"></param>
        /// <param name="nNum"></param>
        /// <param name="eType"></param>
        /// <param name="nWnd"></param>
        public static void DigitalFilter(double[] x, double[] dReturn, int nNum, DigitalFilterType eType = DigitalFilterType.FILTERTYPE_CubicSmooth_5, int nWnd = 3)
        {
            switch (eType)
            {
                case DigitalFilterType.FILTERTYPE_CubicSmooth_5:
                    Kdspt(nNum, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CubicSmooth_7:
                    Kdspt7_3(nNum, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_QuadraticSmooth_5:
                    Kdspt5_2(nNum, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_QuadraticSmooth_7:
                    Kdspt7_2(nNum, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_TriangularSmooths_29:
                    Kdspt_TriangularSmooths_29(nNum, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_GaussianSmooths_31:
                    Kdspt_GaussianSmooths_31(nNum, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_DTMEAN3:
                    Kdspt_dtmean(nNum, 3, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_DTMEAN4:
                    Kdspt_dtmean(nNum, 4, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_DTMEAN5:
                    Kdspt_dtmean(nNum, 5, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_DTMEAN6:
                    Kdspt_dtmean(nNum, 6, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_DTMEAN7:
                    Kdspt_dtmean(nNum, 7, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN3:
                    Kdspt_cfmean(nNum, 3, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN5:
                    Kdspt_cfmean(nNum, 5, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN7:
                    Kdspt_cfmean(nNum, 7, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN9:
                    Kdspt_cfmean(nNum, 9, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN11:
                    Kdspt_cfmean(nNum, 11, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN13:
                    Kdspt_cfmean(nNum, 13, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN15:
                    Kdspt_cfmean(nNum, 15, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN17:
                    Kdspt_cfmean(nNum, 17, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN19:
                    Kdspt_cfmean(nNum, 19, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN21:
                    Kdspt_cfmean(nNum, 21, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN23:
                    Kdspt_cfmean(nNum, 23, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN25:
                    Kdspt_cfmean(nNum, 25, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN27:
                    Kdspt_cfmean(nNum, 27, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN29:
                    Kdspt_cfmean(nNum, 29, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_CFMEAN31:
                    Kdspt_cfmean(nNum, 31, x, dReturn);
                    break;
                case DigitalFilterType.FILTERTYPE_MEDIAN:
                    Kdspt_Median(nNum, nWnd, x, dReturn);
                    break;
                default:
                    Kdspt(nNum, x, dReturn);
                    break;
            }
        }

        /// <summary>
        /// 计算一阶, 二阶导数	
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="nNum">数组长度</param>
        /// <param name="nDev">1一阶 2二阶</param>
        /// <param name="dReturn"></param>
        public static void CalDerivative(double[] x, double[] y, int nNum, int nDev, double[] dReturn)
        {
            int n = nNum;
            double[] dy = new double[n];
            double[] ddy = new double[n];

            if (x[0] == x[1])
                dy[0] = 0;
            else
                dy[0] = (y[1] - y[0]) / (x[1] - x[0]);

            if (x[n - 2] == x[n - 1])
                dy[n - 1] = 0;
            else
                dy[n - 1] = (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);

            DataAlgorithm.Amspl(x, y, n, dy, ddy);

            if (nDev == 1)
            {
                for (int i = 0; i < n; i++)
                    dReturn[i] = dy[i];
            }
            else if (nDev == 2)
            {
                for (int i = 0; i < n; i++)
                    dReturn[i] = ddy[i];
            }
        }

        /// <summary>
        /// 计算一阶, 二阶导数	
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="nNum"></param>
        /// <param name="nDev"></param>
        /// <param name="dReturn"></param>
        public static void NewCalDerivative(double[] x, double[] y, int nNum, int nDev, double[] dReturn)
        {
            if (nNum < 1 || null == x || null == y)
            {
                return;
            }

            int n = nNum;
            double[] dy = new double[n];
            double[] ddy = new double[n];

            dy[n - 1] = 0;
            for (int i = 0; i < n - 1; ++i)
            {
                dy[i] = y[i + 1] - y[i];
            }

            ////求一阶导数后进行五点三次滤波（首尾不收缩）
            //double* pReturn = new double[n];
            //memset(pReturn,0,sizeof(double) * n);
            //KdsptForwardMBackN_NoShrink(n,dy,pReturn,2,2);
            //memcpy(dy,pReturn,sizeof(double) * n);

            ddy[0] = 0;
            ddy[n - 1] = 0;
            for (int i = 1; i < n - 1; ++i)
            {
                ddy[i] = dy[i] - dy[i - 1];
            }

            ////求二阶导数后进行五点三次滤波（首尾不收缩）
            //memset(pReturn,0,sizeof(double) * n);
            //KdsptForwardMBackN_NoShrink(n,ddy,pReturn,2,2);
            //memcpy(ddy,pReturn,sizeof(double) * n);

            if (nDev == 1)
            {
                for (int i = 0; i < n; i++)
                    dReturn[i] = dy[i];
            }
            else if (nDev == 2)
            {
                for (int i = 0; i < n; i++)
                    dReturn[i] = ddy[i];
            }
        }

        public static void Spline(double[] x, double[] y, int nNum, double xCur, out double yCur)
        {
            if (nNum < 1)
            {
                yCur = 0;
                return;
            }

            int k = -1;
            double[] s = new double[5];

            double dXMin = x[0];
            int nIndex = 0;
            //for( int i=1; i< nNum; i++)
            //{
            //	dXMin = x[i] < dXMin ? x[i],nIndex = i : dXMin;
            //}

            int nTempNum = nNum - nIndex;
            double[] dX = new double[nTempNum];
            double[] dY = new double[nTempNum];
            for (int i = 0; i < nTempNum; i++)
            {
                dX[i] = x[i + nIndex];
                dY[i] = y[i + nIndex];
            }
            Akspl(dX, dY, nTempNum, k, xCur, s, true);
            yCur = s[4];
        }

        /// <summary>
        /// 归一化处理（内部判别参数开放，二阶导找Ct值起峰点，取起峰点之前的3-5点荧光信号值的均值作为背景值；
        /// 若不满足条件则取3个(缺省)最小值的均值作为背景值；
        /// 依据背景值进一步做归一化处理）
        /// </summary>
        /// <param name="nNum"></param>
        /// <param name="pdx"></param>
        /// <param name="pdyInput"></param>
        /// <param name="pdyOutput"></param>
        /// <param name="paraminfo"></param>
        public static void NormalizedAnalysisBySndDerivative(int nNum, double[] pdx, double[] pdyInput, double[] pdyOutput, TagFunAmpNormalizedAnaParamInfo paraminfo)
        {
            if ((nNum > 3) && (nNum <= paraminfo.nAvgNum))
            {
                paraminfo.nAvgNum = 3;
            }
            if (paraminfo.nAvgNum >= nNum || paraminfo.nAvgNum <= 0)
            {
                for (int i = 0; i < nNum; i++)
                    pdyOutput[i] = pdyInput[i];
                return;
            }

            double backval = 0;
            double dbegin = 1, dend = 1;
            bool bAble = paraminfo.bAbleToCalcu;
            if (bAble)
            {
                int nxpos_max = 0, nxpos_min = 0; double dmax = 0, dmin = 0;
                //二阶导数最大值对应的位置点，向前移动5点作为起始点，向后取连续3点的均值作为背景值
                {
                    double[] dFiltered = new double[nNum];
                    double[] dReturn = new double[nNum];
                    double[] dTemp = new double[nNum];
                    for (int k = 0; k < nNum; k++)
                        dTemp[k] = pdyInput[k];

                    //滤波
                    for (int itemp = 0; itemp < paraminfo.nFiltNum; itemp++)
                    {
                        DigitalFilter(dTemp, dFiltered, nNum);
                        //memcpy(dTemp, dFiltered, sizeof(double) * nNum);
                        Buffer.BlockCopy(dFiltered, 0, dTemp, 0, nNum * sizeof(double));
                    }
                    //计算二阶导数
                    CalDerivative(pdx, dFiltered, nNum, 2, dReturn);
                    //计算最大值
                    dmax = dReturn[0]; dmin = dReturn[0];
                    for (int t = 0; t < nNum; t++)
                    {
                        if (dReturn[t] < 0) dReturn[t] = 0;
                    }
                    dmax = FindCrestWithParabola(pdx, dReturn, nNum, 6);
                    nxpos_max = (int)(dmax + 0.5);

                    if (paraminfo.bBaseCorrection)
                    {
                        if ((nxpos_max > paraminfo.nBCXPosThre) && (nxpos_max < nNum))
                        {
                            //归一前数据进行基线矫正
                            int nTemp = nxpos_max - paraminfo.nBCEndPreXPos;
                            nTemp -= paraminfo.nBCStart;
                            if (nTemp >= 2)
                            {
                                double[] dCoef = new double[3];
                                double[] dTemp2 = new double[nTemp];
                                //memcpy(dTemp2, pdyInput + (paraminfo.nBCStart - 1), sizeof(double) * nTemp);
                                Array.Copy(pdyInput, (paraminfo.nBCStart - 1), dTemp2, 0, nTemp);
                                LinearFit(pdx, dTemp2, nTemp, dCoef);

                                //CFitting fit;
                                //double dR2 = fit.SolutionCLEG(pdx, dTemp2, dCoef, nTemp, 3);
                                if ((dCoef[2] * dCoef[2] > paraminfo.dBCR2) && (dCoef[1] < paraminfo.dBCK) && (dCoef[1] > paraminfo.dBCK * -1))
                                {
                                    for (int i = 0; i < nNum; i++)
                                    {
                                        pdyInput[i] -= (pdx[i] * dCoef[1]/*+pdx[i]*pdx[i]*dCoef[2]*/);
                                        dFiltered[i] -= (pdx[i] * dCoef[1]/*+pdx[i]*pdx[i]*dCoef[2]*/);
                                    }
                                }
                            }

                            //重新计算二阶导最大值
                            CalDerivative(pdx, dFiltered, nNum, 2, dReturn);
                            //计算最大值
                            dmax = dReturn[0]; dmin = dReturn[0];
                            for (int t = 0; t < nNum; t++)
                            {
                                if (dReturn[t] < 0) dReturn[t] = 0;
                            }
                            dmax = FindCrestWithParabola(pdx, dReturn, nNum, 6);
                            nxpos_max = (int)(dmax + 0.5);
                        }
                    }

                }

                //提取有效数据源
                dbegin = dmax - paraminfo.nNormalizeStartPreXPos;
                if ((dbegin < 1) || (dbegin > nNum))//数据点从1开始计数
                {
                    dbegin = 1;
                }
                dend = dbegin + paraminfo.nAvgNum - 1;
                if (dend > nNum)
                {
                    dend = nNum;
                }

                double sum = 0;
                for (double i = dbegin - 1; i < dend; i++)
                {
                    double dytemp = 0;
                    Spline(pdx, pdyInput, nNum, i, out dytemp);
                    sum += dytemp;
                }
                backval = sum / (dend - dbegin + 1);
            }
            else
            {
                if (paraminfo.bBaseCorrection)
                {
                    int nTemp = nNum - paraminfo.nBCEndPreNumPos;
                    if (nTemp >= 2)
                    {
                        double[] dCoef = new double[3];
                        LinearFit(pdx, pdyInput, nTemp, dCoef);
                        if (dCoef[2] * dCoef[2] > paraminfo.dBCR2)
                        {
                            for (int i = 0; i < nNum; i++)
                            {
                                pdyInput[i] -= (pdx[i] * dCoef[1]);
                            }
                        }
                    }
                }
                //除去 nAvgNum个最小值的均值
                SortDouble(nNum, pdyInput, pdyOutput);
                double sum = 0;
                for (int i = 0; i < paraminfo.nAvgNum; i++)
                    sum += pdyOutput[i];
                backval = sum / paraminfo.nAvgNum;
            }

            //减1操作,便于对数曲线表现
            if (paraminfo.bMinus1)
            {
                for (int i = 0; i < nNum; i++)
                    pdyOutput[i] = pdyInput[i] / backval - 1;
            }
            else
            {
                for (int i = 0; i < nNum; i++)
                    pdyOutput[i] = pdyInput[i] / backval;
            }
            //背景信号压制
            if (paraminfo.bBaseSuppression)
            {
                if (!bAble)
                {
                    if (paraminfo.bMinus1)
                    {
                        for (int i = 0; i < nNum; i++)
                            pdyOutput[i] += (0 - pdyOutput[i]) * paraminfo.dBSRatioAllData;
                    }
                    else
                    {
                        for (int i = 0; i < nNum; i++)
                            pdyOutput[i] += (1 - pdyOutput[i]) * paraminfo.dBSRatioAllData;
                    }
                }
                else
                {
                    if (paraminfo.bMinus1)
                    {
                        for (int i = 0; i < dbegin - 1; i++)
                            pdyOutput[i] += (0 - pdyOutput[i]) * paraminfo.dBSRatioBaseLine;
                    }
                    else
                    {
                        for (int i = 0; i < dbegin - 1; i++)
                            pdyOutput[i] += (1 - pdyOutput[i]) * paraminfo.dBSRatioBaseLine;
                    }
                }
            }
        }

        public static double FindCrestWithParabola(double[] x, double[] y, int nNum, int nWndWidth)
        {
            if (nNum < 1 || null == x || null == y)
            {
                return -1;
            }

            if (nNum < nWndWidth || nWndWidth < 3)
            {
                return -1;
            }

            double[] dTempY = new double[nNum];
            //memcpy(dTempY, y, nNum * sizeof(double));
            Buffer.BlockCopy(y, 0, dTempY, 0, nNum * sizeof(double));

            for (int k = 1; k < nNum - 1; k++)
            {
                if (dTempY[k] < y[k - 1] && dTempY[k] < y[k + 1])
                    dTempY[k] = (y[k - 1] + y[k + 1]) / 2;
            }

            double dSum, dMax = -1000, dMaxCrest = -1000;
            int nCrestPos = 0;

            for (int i = 0; i < nNum - nWndWidth; i++)
            {
                dSum = 0;
                for (int j = 0; j < nWndWidth; j++)
                {
                    dSum += dTempY[i + j];
                }
                if (dMax < dSum)
                {
                    dMax = dSum;
                    nCrestPos = i;
                }
                dMaxCrest = dMaxCrest < dTempY[i] ? dTempY[i] : dMaxCrest;
            }

            dMaxCrest /= 10;
            int end, start = 0;
            start = nCrestPos;
            end = nCrestPos + nWndWidth - 1;

            int sumn = (int)(end - start) + 1;

            if (sumn < 3)
            {
                return -1;
            }

            double sumx = 0.0, sumx2 = 0.0, sumx3 = 0.0, sumx4 = 0.0;
            double sumy = 0.0, sumxy = 0.0, sumx2y = 0.0;

            double doubj;
            for (int j = start; j <= end; j++)
            {
                doubj = (double)j - start;

                sumx += doubj;
                sumx2 += doubj * doubj;
                sumx3 += doubj * doubj * doubj;
                sumx4 += doubj * doubj * doubj * doubj;
                sumy += dTempY[j] / dMaxCrest;                      // Division is to normalize data
                sumxy += doubj * (dTempY[j] / dMaxCrest);
                sumx2y += doubj * doubj * (dTempY[j] / dMaxCrest);  // Division is to normalize data
            }

            double dXPos = 0.0;

            double determ = sumn * (sumx2 * sumx4 - sumx3 * sumx3) - sumx * (sumx * sumx4 - sumx2 * sumx3) + sumx2 * (sumx * sumx3 - sumx2 * sumx2);
            if (Math.Abs(determ) < 1.0E-12)
            {
                return -1;
            }
            double parba = sumy * (sumx2 * sumx4 - sumx3 * sumx3) - sumxy * (sumx * sumx4 - sumx2 * sumx3) + sumx2y * (sumx * sumx3 - sumx2 * sumx2);

            parba /= determ;
            double parbb = -sumy * (sumx * sumx4 - sumx2 * sumx3) + sumxy * (sumn * sumx4 - sumx2 * sumx2) - sumx2y * (sumn * sumx3 - sumx * sumx2);

            parbb /= determ;
            double parbc = sumy * (sumx * sumx3 - sumx2 * sumx2) - sumxy * (sumn * sumx3 - sumx * sumx2) + sumx2y * (sumn * sumx2 - sumx * sumx);

            parbc /= determ;
            dXPos = -(parbb) / (2.0 * (parbc));

            dXPos += start;

            //该函数有bug，尚未修复
            if ((int)(dXPos + 0.5) >= nNum)
            {
                dXPos = nNum - 1;
            }
            else if ((int)(dXPos + 0.5) <= 1)
            {
                dXPos = start + sumn / 2.0;
            }

            return dXPos;
        }

        /// <summary>
        /// 线性拟合
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="nNum"></param>
        /// <param name="dReturn"></param>
        public static void LinearFit(double[] x, double[] y, int nNum, double[] dReturn)
        {
            double[] dt = new double[6];
            double[] a = new double[2];
            Jbsqt(x, y, nNum, a, dt);
            dReturn[0] = a[0];
            dReturn[1] = a[1];

            /////////////////R2////////////////////
            double xMean = 0, yMean = 0;
            for (int i = 0; i < nNum; i++)
            {
                xMean += x[i];
                yMean += y[i];
            } // for(int i=0; i<nNum; i++)
            xMean /= nNum;
            yMean /= nNum;
            double aUp = 0, bDown = 0, cDown = 0;
            for (int i = 0; i < nNum; i++)
            {
                aUp += Math.Abs((x[i] - xMean) * (y[i] - yMean));
                bDown += (x[i] - xMean) * (x[i] - xMean);
                cDown += (y[i] - yMean) * (y[i] - yMean);
            } // for(i=0; i<nNum; i++)

            //////////////////R///////////////////////
            double dSumXY = 0, dSumX = 0, dSumY = 0, dSumX2 = 0, dSumY2 = 0;
            for (int i = 0; i < nNum; i++)
            {
                dSumXY += x[i] * y[i];
                dSumX += x[i];
                dSumY += y[i];
                dSumX2 += x[i] * x[i];
                dSumY2 += y[i] * y[i];
            }
            double dUp = nNum * dSumXY - dSumX * dSumY;
            double dDown = Math.Sqrt(nNum * dSumX2 - dSumX * dSumX) * Math.Sqrt(nNum * dSumY2 - dSumY * dSumY);

            //////////////////////////////////////////
            //	if( bDown!= 0 && cDown!= 0)
            //		dReturn[2] = aUp/(sqrt(bDown*cDown));
            if (dDown != 0)
                dReturn[2] = dUp / dDown;
            else
                dReturn[2] = 0;
        }

        /// <summary>
        /// 中位值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="nNum"></param>
        /// <returns></returns>
        public static double Median(double[] x, int nNum)
        {
            double dVal = 0;
            if (nNum <= 0) return dVal;
            double[] temp = new double[nNum];
            SortDouble(nNum, x, temp);
            if (nNum % 2 == 0)//若为偶数，取中间的两个数做平均
            {
                dVal = (temp[nNum / 2] + temp[nNum / 2 - 1]) / 2;
            }
            else
            {
                dVal = temp[nNum / 2];
            }

            return dVal;
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
        /// 计算DeltaRn处理（内部判别参数开放，二阶导找Ct值起峰点，起峰点前推5点，前后各取3点共7点荧光信号值的均值作为背景值；若不满足条件则取全体数据的中位值作为背景值；依据背景值进一步做△Rn处理）
        /// </summary>
        /// <param name="nNum"></param>
        /// <param name="pdx"></param>
        /// <param name="pdyInput"></param>
        /// <param name="pdyOutput"></param>
        /// <param name="paraminfo"></param>
        public static void DeltaRnAnalysisBySndDerivative(int nNum, double[] pdx, double[] pdyInput, double[] pdyOutput, TagFunAmpNormalizedAnaParamInfo paraminfo)
        {
            if ((nNum > 3) && (nNum <= paraminfo.nAvgNum))
            {
                paraminfo.nAvgNum = 3;
            }
            if (paraminfo.nAvgNum >= nNum || paraminfo.nAvgNum <= 0)
            {
                for (int i = 0; i < nNum; i++)
                    pdyOutput[i] = pdyInput[i];
                return;
            }

            double backval = 0;
            double dbegin = 1, dend = 1;
            int nbegin = 1;
            bool bAble = paraminfo.bAbleToCalcu /*AbleToCalculate(pdx,pdyInput,nNum,false)*/;
            if (bAble)
            {
                int nxpos_max = 0, nxpos_min = 0; double dmax = 0, dmin = 0;
                //二阶导数最大值对应的位置点，向前移动5点作为起始点，前后各取连续3点的均值作为背景值
                {
                    double[] dFiltered = new double[nNum];
                    double[] dReturn = new double[nNum];
                    double[] dTemp = new double[nNum];
                    for (int k = 0; k < nNum; k++)
                        dTemp[k] = pdyInput[k];

                    //滤波
                    for (int itemp = 0; itemp < 1; itemp++)
                    {
                        DigitalFilter(dTemp, dFiltered, nNum, DigitalFilterType.FILTERTYPE_CFMEAN7);
                        //memcpy(dTemp, dFiltered, sizeof(double) * nNum);
                        Buffer.BlockCopy(dFiltered, 0, dTemp, 0, nNum * sizeof(double));
                    }
                    for (int itemp = 0; itemp < paraminfo.nFiltNum; itemp++)
                    {
                        DigitalFilter(dTemp, dFiltered, nNum);
                        //memcpy(dTemp, dFiltered, sizeof(double) * nNum);
                        Buffer.BlockCopy(dFiltered, 0, dTemp, 0, nNum * sizeof(double));
                    }
                    //计算二阶导数
                    CalDerivative(pdx, dFiltered, nNum, 2, dReturn);
                    //计算最大值
                    dmax = dReturn[0]; dmin = dReturn[0];
                    for (int t = 0; t < nNum; t++)
                    {
                        if (dReturn[t] < 0) dReturn[t] = 0;
                    }
                    dmax = FindCrestWithParabola(pdx, dReturn, nNum, 6);
                    nxpos_max = (int)(dmax + 0.5);

                    if (paraminfo.bBaseCorrection)
                    {
                        if ((nxpos_max > paraminfo.nBCXPosThre) && (nxpos_max < nNum))
                        {
                            //归一前数据进行基线矫正
                            int nTemp = nxpos_max - paraminfo.nBCEndPreXPos;
                            nTemp -= paraminfo.nBCStart;
                            if (nTemp >= 2)
                            {
                                double[] dCoef = new double[3];
                                double[] dTemp2 = new double[nTemp];
                                // memcpy(dTemp, pdyInput + (paraminfo.nBCStart - 1), sizeof(double) * nTemp);
                                Buffer.BlockCopy(pdyInput, (paraminfo.nBCStart - 1) * sizeof(double), dTemp2, 0, nTemp * sizeof(double));

                                LinearFit(pdx, dTemp2, nTemp, dCoef);
                                //CFitting fit;
                                //double dR2 = fit.SolutionCLEG(pdx, dTemp2, dCoef, nTemp, 3);
                                if ((dCoef[2] * dCoef[2] > paraminfo.dBCR2) && (dCoef[1] < paraminfo.dBCK) && (dCoef[1] > paraminfo.dBCK * -1))
                                {
                                    for (int i = 0; i < nNum; i++)
                                    {
                                        pdyInput[i] -= (pdx[i] * dCoef[1]/*+pdx[i]*pdx[i]*dCoef[2]*/);
                                        dFiltered[i] -= (pdx[i] * dCoef[1]/*+pdx[i]*pdx[i]*dCoef[2]*/);
                                    }
                                }
                            }

                            //重新计算二阶导最大值
                            CalDerivative(pdx, dFiltered, nNum, 2, dReturn);
                            //计算最大值
                            dmax = dReturn[0]; dmin = dReturn[0];
                            for (int t = 0; t < nNum; t++)
                            {
                                if (dReturn[t] < 0) dReturn[t] = 0;
                            }
                            dmax = FindCrestWithParabola(pdx, dReturn, nNum, 6);
                            nxpos_max = (int)(dmax + 0.5);
                        }
                    }

                }

                //提取有效数据源
                nbegin = nxpos_max - paraminfo.nNormalizeStartPreXPos;
                if ((nbegin < 1) || (nbegin > nNum))//数据点从1开始计数
                {
                    nbegin = 1;
                }
                int nend = nbegin + paraminfo.nAvgNum - 1;
                if (nend > nNum)
                {
                    nend = nNum;
                }

                double sum = 0;
                for (int i = nbegin - 1; i < nend; i++)
                    sum += pdyInput[i];
                backval = sum / (nend - nbegin + 1);
            }
            else
            {
                if (paraminfo.bBaseCorrection)
                {
                    int nTemp = nNum - paraminfo.nBCEndPreNumPos;
                    if (nTemp >= 2)
                    {
                        double[] dCoef = new double[3];
                        LinearFit(pdx, pdyInput, nTemp/*nNum*/, dCoef);
                        if (dCoef[2] * dCoef[2] > paraminfo.dBCR2)
                        {
                            for (int i = 0; i < nNum; i++)
                            {
                                pdyInput[i] -= (pdx[i] * dCoef[1]);
                            }
                        }
                    }
                }
                //中位值
                backval = Median(pdyInput, nNum);
                //SortDouble(nNum, pdyInput, pdyOutput);
                //double sum = 0;
                //for(int i=0; i< paraminfo.nAvgNum; i++)
                //	sum += pdyOutput[i];
                //backval = sum /paraminfo.nAvgNum;
            }

            //计算△Rn
            for (int i = 0; i < nNum; i++)
                pdyOutput[i] = pdyInput[i] - backval;
        }

        /// <summary>
        /// 计算一阶导数（增量）
        /// </summary>
        /// <param name="iPtCount"></param>
        /// <param name="pdInputY"></param>
        /// <param name="pdOutputY"></param>
        public static void CalcFirstDerivative(int iPtCount, double[] pdInputY, double[] pdOutputY)
        {
            pdOutputY[0] = 0;
            for (int i = 1; i < iPtCount; i++)
            {
                pdOutputY[i] = pdInputY[i] - pdInputY[i - 1];
            }
        }

        /// <summary>
        /// 计算二阶导数（二次增量）
        /// </summary>
        /// <param name="iPtCount"></param>
        /// <param name="pdInputY"></param>
        /// <param name="pdOutputY"></param>
        public static void CalcSecondDerivative(int iPtCount, double[] pdInputY, double[] pdOutputY)
        {
            double[] dy = new double[iPtCount];

            CalcFirstDerivative(iPtCount, pdInputY, dy);

            pdOutputY[0] = 0;
            for (int i = 1; i < iPtCount; i++)
            {
                pdOutputY[i] = dy[i] - dy[i - 1];
            }
        }

        public static double FindCtPosWithParabola(int iPtCount, double[] pdInputX, double[] pdInputY, double[] pdFirstDer, double[] pdSecondDer)
        {
            double dMaxFirstDer = pdFirstDer[6];
            int iMaxPos = 0;
            for (int i = 6; i < iPtCount; i++)
            {
                if (pdFirstDer[i] > dMaxFirstDer)
                {
                    dMaxFirstDer = pdFirstDer[i];
                    iMaxPos = i;
                }
            }
            int iMinPos = 6;
            for (int i = iMaxPos - 1; i >= 6; i--)
            {
                if (pdFirstDer[i] > pdFirstDer[i + 1])
                {
                    iMinPos = i;
                    break;
                }
            }

            double dmaxTemp = pdSecondDer[0];
            for (int t = 0; t < iPtCount; t++)
            {
                if (pdSecondDer[t] < 0)
                {
                    pdSecondDer[t] = 0;
                }
            }

            double dMaxPosX = 0;
            int iStartPos = 3;
            do
            {
                dMaxPosX = FindCrestWithParabola(pdInputX.AsSpan().Slice(iStartPos).ToArray(),
                    pdSecondDer.AsSpan().Slice(iStartPos).ToArray(), iPtCount - iStartPos, 6);

                int iPosMax = (int)(dMaxPosX + 0.5) + iStartPos;
                if (pdFirstDer[iPosMax] > 0 && iPosMax > iMinPos /* && IsLegalCtByFluo(iPtCount, pdInputX, pdInputY, iPosMax)*/)
                {
                    dMaxPosX += iStartPos;
                    break;
                }
                else
                {
                    iStartPos = iPosMax;
                    dMaxPosX = 0;
                }
            } while (iStartPos < iPtCount - 5);


            return dMaxPosX;
        }

        public static double CalcCtPosBySndDerivative(int iPtCount, double[] pdInputX, double[] pdInputY)
        {
            double[] pdTempTemp = new double[iPtCount];
            for (int k = 0; k < iPtCount; k++)
            {
                pdTempTemp[k] = pdInputY[k];
            }

            //滤波
            //double *pdFilteredTemp = new double [iPtCount];
            //for (int itemp=0; itemp<3; itemp++)
            //{
            //	DigitalFilter(pdTempTemp, pdFilteredTemp, iPtCount);
            //	memcpy(pdTempTemp, pdFilteredTemp, sizeof(double)*iPtCount);
            //}
            //delete [] pdFilteredTemp;


            //计算一阶导数和二阶导数
            double[] pdFirstDer = new double[iPtCount];
            CalcFirstDerivative(iPtCount, pdTempTemp, pdFirstDer);
            double[] pdSecondDer = new double[iPtCount];
            CalcSecondDerivative(iPtCount, pdTempTemp, pdSecondDer);

            //计算二阶导数正值的最大值
            double dMaxPosX = FindCtPosWithParabola(iPtCount, pdInputX, pdInputY, pdFirstDer, pdSecondDer);

            int iPosMax = (int)(dMaxPosX + 0.5);
            if (iPosMax < 1) iPosMax = 1;  // fixed
            double dMaxY = pdSecondDer[iPosMax - 1];

            bool bAble = false;
            if (iPosMax > 6 && (dMaxPosX < m_dd2MaxXThre * iPtCount) && dMaxY > m_dd2MaxYThre)
            {
                if (iPtCount > 8)
                {
                    bAble = true;
                    //if(pdInputY[iPtCount-1] > pdInputY[0] && pdInputY[iPtCount-1] > pdInputY[2] && pdInputY[iPtCount-2] > pdInputY[0] && pdInputY[iPtCount-2] > pdInputY[2])
                    //{
                    //	bAble = TRUE;
                    //}
                }
            }

            if (!bAble)
                dMaxPosX = 0;

            return dMaxPosX;
        }

        public static int FindCtPosBySlope(int nPtCount, double[] pInputX, double[] pInputY, int iWndWidth = 5)
        {
            int iFindCt = 0;
            double[] dPreCoef = new double[3];
            double[] dCurCoef = new double[3];
            double[] pdTempX = new double[iWndWidth];
            double[] pdTempY = new double[iWndWidth];
            double dMaxSlope = 0;


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
                    if (dCurCoef[1] < 50)
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


                    if (dCurCoef[1] - dPreCoef[1] > dMaxSlope)
                    {
                        dMaxSlope = dCurCoef[1] - dPreCoef[1];
                        iFindCt = i + 3;
                    }
                }
            }

            if (dMaxSlope < 50)
                iFindCt = 0;

            return iFindCt;
        }

        public static double CalcCtPos(int iPtCount, double[] pdInputX, double[] pdInputY)
        {
            double dCt = CalcCtPosBySndDerivative(iPtCount, pdInputX, pdInputY);
            if (dCt < 7 || dCt > iPtCount - 3)
            {
                dCt = FindCtPosBySlope(iPtCount, pdInputX, pdInputY);
            }

            return dCt;
        }

        /// <summary>
        /// 根据Ct位置计算基线位置
        /// </summary>
        /// <param name="nPosMax"></param>
        /// <param name="iOutStart"></param>
        /// <param name="iOutEnd"></param>
        public static void CalcBaselinePosByCt(int nPosMax, out int iOutStart, out int iOutEnd)
        {
            int iTempStart = 3;
            int iTempEnd = nPosMax - 5;
            if (iTempEnd <= iTempStart)
            {
                iTempStart = 1;
                if (iTempEnd - iTempStart < 3)
                {
                    iTempEnd += 3;
                }
                if (iTempEnd < iTempStart)
                {
                    iTempEnd = iTempStart;
                }
            }
            else if (iTempEnd - iTempStart < 3)
            {
                iTempStart = 1;
                if (iTempEnd - iTempStart < 5)
                {
                    iTempEnd += 1;
                }
            }
            else if (iTempEnd - iTempStart < 5)
            {
                iTempStart -= 1;
            }

            iOutStart = iTempStart;
            iOutEnd = iTempEnd;
        }

        /// <summary>
        /// 计算自动基线的位置，通过二阶导数找Ct
        /// </summary>
        /// <param name="nPtCount"></param>
        /// <param name="pdInputX"></param>
        /// <param name="pdInputY"></param>
        /// <param name="iOutStart"></param>
        /// <param name="iOutEnd"></param>
        public static void CalcAutoBaselinePos(int nPtCount, double[] pdInputX, double[] pdInputY, out int iOutStart, out int iOutEnd)
        {
            double dMaxPosX = CalcCtPos(nPtCount, pdInputX, pdInputY);
            // 计算基线位置
            if (dMaxPosX > 7) // 发现Ct
            {
                int iPosMax = (int)(dMaxPosX + 0.5);
                CalcBaselinePosByCt(iPosMax, out iOutStart, out iOutEnd);

                if (iOutEnd - iOutStart > 10)
                {
                    bool bFindPlateau = false;

                    double[] dCurCoef = new double[3];
                    double[] pdTempX = new double[iOutEnd - iOutStart + 1];
                    double[] pdTempY = new double[iOutEnd - iOutStart + 1];
                    int iWndWidth = (iOutEnd - iOutStart) / 3;
                    double fFirstSlope = 0;
                    double dMinSlope = 0;
                    int iMinStart = 0;
                    bool bFindFirstSlope = false;

                    for (int i = iOutEnd; i >= iOutStart + iWndWidth; i--)
                    {
                        for (int j = 0; j < iWndWidth; j++)
                        {
                            pdTempX[j] = pdInputX[i - iWndWidth + 1 + j];
                            pdTempY[j] = pdInputY[i - iWndWidth + 1 + j];
                        }
                        LinearFit(pdTempX, pdTempY, iWndWidth, dCurCoef);

                        if (i == iOutEnd)
                        {
                            dMinSlope = dCurCoef[1];
                            iMinStart = i;
                        }
                        else
                        {
                            if (Math.Abs(dMinSlope) > Math.Abs(dCurCoef[1]))
                            {
                                dMinSlope = dCurCoef[1];
                                iMinStart = i;
                            }
                        }

                        if (!bFindFirstSlope)
                        {
                            if (dCurCoef[1] < 20)
                            {
                                fFirstSlope = dCurCoef[1];
                                iOutEnd = i;
                                bFindFirstSlope = true;
                                bFindPlateau = true;
                            }
                        }
                        else
                        {
                            if (Math.Abs(dCurCoef[1]) < 10)
                                continue;

                            if (Math.Abs(dCurCoef[1] - fFirstSlope) > Math.Abs(fFirstSlope) * 0.5)
                            {
                                iOutStart = i - iWndWidth + 1;
                                break;
                            }
                        }
                    }

                    if (!bFindPlateau)
                    {
                        // 根据最小斜率的位置向前向后找接近斜率的位
                        bool bFindStart = false;
                        for (int i = iMinStart - 1; i >= iOutStart + iWndWidth; i--)
                        {
                            for (int j = 0; j < iWndWidth; j++)
                            {
                                pdTempX[j] = pdInputX[i - iWndWidth + 1 + j];
                                pdTempY[j] = pdInputY[i - iWndWidth + 1 + j];
                            }
                            LinearFit(pdTempX, pdTempY, iWndWidth, dCurCoef);
                            if (Math.Abs(dCurCoef[1] - dMinSlope) > Math.Abs(dMinSlope) * 0.5)
                            {
                                iOutStart = i - iWndWidth + 1;
                                bFindStart = true;
                                break;
                            }
                        }
                        if (!bFindStart)
                            iOutStart = iMinStart - iWndWidth + 1;

                        bool bFindEnd = false;
                        for (int i = iMinStart + 1; i <= iOutEnd; i++)
                        {
                            for (int j = 0; j < iWndWidth; j++)
                            {
                                pdTempX[j] = pdInputX[i - iWndWidth + 1 + j];
                                pdTempY[j] = pdInputY[i - iWndWidth + 1 + j];
                            }
                            LinearFit(pdTempX, pdTempY, iWndWidth, dCurCoef);
                            if (Math.Abs(dCurCoef[1] - dMinSlope) > Math.Abs(dMinSlope) * 0.5)
                            {
                                iOutEnd = i;
                                bFindEnd = true;
                                break;
                            }
                        }
                        if (!bFindEnd)
                            iOutEnd = iMinStart;
                    }

                }
            }
            else
            {
                if (nPtCount > 20)
                {
                    iOutStart = 3;
                    iOutEnd = nPtCount - 3;
                    iOutStart = iOutEnd - nPtCount * 2 / 3;
                }
                else
                {
                    iOutStart = 1;
                    iOutEnd = nPtCount;
                }
            }
        }
    }
}
