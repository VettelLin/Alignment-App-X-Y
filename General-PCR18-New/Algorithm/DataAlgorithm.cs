using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public static class DataAlgorithm
    {
        /// <summary>
        /// 样条插值求一阶微分——1
        /// </summary>
        /// <param name="x0"></param>
        /// <param name="h"></param>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="s"></param>
        public static void Alspl(double x0, double h, int n, double[] y, int k, double t, double[] s)
        {
            int kk, m, l;
            double[] u = new double[5];
            double p, q;
            s[4] = 0.0; s[0] = 0.0; s[1] = 0.0; s[2] = 0.0; s[3] = 0.0;
            if (n < 1)
                return;
            if (n == 1) { s[0] = y[0]; s[4] = y[0]; return; }
            if (n == 2)
            {
                s[0] = y[0]; s[1] = (y[1] - y[0]) / h;
                if (k < 0)
                    s[4] = (y[1] * (t - x0) - y[0] * (t - x0 - h)) / h;
                return;
            }
            if (k < 0)
            {
                if (t <= x0 + h) kk = 0;
                else if (t >= x0 + (n - 1) * h)
                    kk = n - 2;
                else
                {
                    kk = 1; m = n;
                    while (((kk - m) != 1) && ((kk - m) != -1))
                    {
                        l = (kk + m) / 2;
                        if (t < x0 + (l - 1) * h)
                            m = l;
                        else
                            kk = l;
                    }
                    kk = kk - 1;
                }
            }
            else
                kk = k;
            if (kk >= n - 1)
                kk = n - 2;
            u[2] = (y[kk + 1] - y[kk]) / h;
            if (n == 3)
            {
                if (kk == 0)
                {
                    u[3] = (y[2] - y[1]) / h;
                    u[4] = 2.0 * u[3] - u[2];
                    u[1] = 2.0 * u[2] - u[3];
                    u[0] = 2.0 * u[1] - u[2];
                }
                else
                {
                    u[1] = (y[1] - y[0]) / h;
                    u[0] = 2.0 * u[1] - u[2];
                    u[3] = 2.0 * u[2] - u[1];
                    u[4] = 2.0 * u[3] - u[2];
                }
            }
            else
            {
                if (kk <= 1)
                {
                    u[3] = (y[kk + 2] - y[kk + 1]) / h;
                    if (kk == 1)
                    {
                        u[1] = (y[1] - y[0]) / h;
                        u[0] = 2.0 * u[1] - u[2];
                        if (n == 4) u[4] = 2.0 * u[3] - u[2];
                        else u[4] = (y[4] - y[3]) / h;
                    }
                    else
                    {
                        u[1] = 2.0 * u[2] - u[3];
                        u[0] = 2.0 * u[1] - u[2];
                        u[4] = (y[3] - y[2]) / h;
                    }
                }
                else if (kk >= (n - 3))
                {
                    u[1] = (y[kk] - y[kk - 1]) / h;
                    if (kk == (n - 3))
                    {
                        u[3] = (y[n - 1] - y[n - 2]) / h;
                        u[4] = 2.0 * u[3] - u[2];
                        if (n == 4)
                            u[0] = 2.0 * u[1] - u[2];
                        else
                            u[0] = (y[kk - 1] - y[kk - 2]) / h;
                    }
                    else
                    {
                        u[3] = 2.0 * u[2] - u[1];
                        u[4] = 2.0 * u[3] - u[2];
                        u[0] = (y[kk - 1] - y[kk - 2]) / h;
                    }
                }
                else
                {
                    u[1] = (y[kk] - y[kk - 1]) / h;
                    u[0] = (y[kk - 1] - y[kk - 2]) / h;
                    u[3] = (y[kk + 2] - y[kk + 1]) / h;
                    u[4] = (y[kk + 3] - y[kk + 2]) / h;
                }
            }
            s[0] = Math.Abs(u[3] - u[2]);
            s[1] = Math.Abs(u[0] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                p = (u[1] + u[2]) / 2.0;
            else
                p = (s[0] * u[1] + s[1] * u[2]) / (s[0] + s[1]);
            s[0] = Math.Abs(u[3] - u[4]);
            s[1] = Math.Abs(u[2] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                q = (u[2] + u[3]) / 2.0;
            else
                q = (s[0] * u[2] + s[1] * u[3]) / (s[0] + s[1]);
            s[0] = y[kk];
            s[1] = p;
            s[3] = h;
            s[2] = (3.0 * u[2] - 2.0 * p - q) / s[3];
            s[3] = (q + p - 2.0 * u[2]) / (s[3] * s[3]);
            if (k < 0)
            {
                p = t - (x0 + kk * h);
                s[4] = s[0] + s[1] * p + s[2] * p * p + s[3] * p * p * p;
            }
        }

        /// <summary>
        /// 样条插值求一阶微分——2
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="n"></param>
        /// <param name="dy"></param>
        /// <param name="ddy"></param>
        public static void Amspl(double[] x, double[] y, int n, double[] dy, double[] ddy)
        {
            int j;
            double h0, h1, alpha, beta;
            double[] s = new double[n];
            s[0] = dy[0]; dy[0] = 0.0;
            h0 = x[1] - x[0];
            for (j = 1; j <= n - 2; j++)
            {
                h1 = x[j + 1] - x[j];
                alpha = h0 / (h0 + h1);
                beta = (1.0 - alpha) * (y[j + 1] - y[j]) / h0;
                beta = 3.0 * (beta + alpha * (y[j] - y[j - 1]) / h1);
                dy[j] = -alpha / (2.0 + (1.0 - alpha) * dy[j - 1]);
                s[j] = (beta - (1.0 - alpha) * s[j - 1]);
                s[j] = s[j] / (2.0 + (1.0 - alpha) * dy[j - 1]);
                h0 = h1;
            }
            for (j = n - 2; j >= 0; j--)
                dy[j] = dy[j] * dy[j + 1] + s[j];
            for (j = 0; j <= n - 2; j++)
                s[j] = x[j + 1] - x[j];
            for (j = 0; j <= n - 2; j++)
            {
                h1 = s[j] * s[j];
                ddy[j] = 6.0 * (y[j + 1] - y[j]) / h1 - 2.0 * (2.0 * dy[j] + dy[j + 1]) / s[j];
            }
            h1 = s[n - 2] * s[n - 2];
            ddy[n - 1] = 6.0 * (y[n - 2] - y[n - 1]) / h1 + 2.0 * (2.0 * dy[n - 1] + dy[n - 2]) / s[n - 2];
        }

        /// <summary>
        /// 最小二乘拟合
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="n"></param>
        /// <param name="a"></param>
        /// <param name="dt"></param>
        public static void Jbsqt(double[] x, double[] y, int n, double[] a, double[] dt)
        {
            int i;
            double xx, yy, e, f, q, u, p, umax, umin, s;
            xx = 0; yy = 0;
            for (i = 0; i <= n - 1; i++)
            {
                xx += x[i] / n;
                yy += y[i] / n;
            }
            e = 0.0; f = 0.0;
            for (i = 0; i <= n - 1; i++)
            {
                q = x[i] - xx; e += q * q;
                f += q * (y[i] - yy);
            }
            a[1] = f / e;
            a[0] = yy - a[1] * xx;
            q = 0.0; u = 0.0; p = 0.0;
            umax = 0.0; umin = 1.0e+30;
            for (i = 0; i <= n - 1; i++)
            {
                s = a[1] * x[i] + a[0];
                q += (y[i] - s) * (y[i] - s);
                p += (s - yy) * (s - yy);
                e = Math.Abs(y[i] - s);
                if (e > umax) umax = e;
                if (e < umin) umin = e;
                u += e / n;
            }
            dt[1] = Math.Sqrt(q / n);
            dt[0] = q;
            dt[2] = p;
            dt[3] = umax;
            dt[4] = umin;
            dt[5] = u;
        }

        /// <summary>
        /// 五点三次平滑滤波
        /// </summary>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt(int n, double[] y, double[] yy)
        {
            int i;
            if (n < 5)
            {
                //for (i=0; i<=n-1; i++) yy[i]=y[i];
                Kdspt_dtmean(n, 5, y, yy);
            }
            else
            {
                yy[0] = 69.0 * y[0] + 4.0 * y[1] - 6.0 * y[2] + 4.0 * y[3] - y[4];
                yy[0] = yy[0] / 70.0;
                yy[1] = 2.0 * y[0] + 27.0 * y[1] + 12.0 * y[2] - 8.0 * y[3];
                yy[1] = (yy[1] + 2.0 * y[4]) / 35.0;
                for (i = 2; i <= n - 3; i++)
                {
                    yy[i] = -3.0 * y[i - 2] + 12.0 * y[i - 1] + 17.0 * y[i];
                    yy[i] = (yy[i] + 12.0 * y[i + 1] - 3.0 * y[i + 2]) / 35.0;
                }
                yy[n - 2] = 2.0 * y[n - 5] - 8.0 * y[n - 4] + 12.0 * y[n - 3];
                yy[n - 2] = (yy[n - 2] + 27.0 * y[n - 2] + 2.0 * y[n - 1]) / 35.0;
                yy[n - 1] = -y[n - 5] + 4.0 * y[n - 4] - 6.0 * y[n - 3];
                yy[n - 1] = (yy[n - 1] + 4.0 * y[n - 2] + 69.0 * y[n - 1]) / 70.0;
            }
        }

        /// <summary>
        /// 滑动平均滤波 dtnum(滑动窗口)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="dtnum"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt_dtmean(int n, int dtnum, double[] y, double[] yy)
        {
            int i;
            int k;
            double sum;
            for (i = 0; i <= n - 1; i++)
            {
                if (i < dtnum - 1)
                {
                    k = i;
                }
                else
                {
                    k = dtnum - 1;
                }
                sum = 0;

                for (int j = 0; j <= k; j++)
                {
                    sum += y[i - k + j];
                }
                yy[i] = sum / (k + 1);
            }
        }

        /// <summary>
        /// 七点三次平滑滤波
        /// </summary>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt7_3(int n, double[] y, double[] yy)
        {
            int i;
            if (n < 7)
            {
                //for (i=0; i<=n-1; i++) yy[i]=y[i];
                Kdspt_dtmean(n, 5, y, yy);
            }
            else
            {
                yy[0] = 39.0 * y[0] + 8.0 * y[1] - 4.0 * y[2] - 4.0 * y[3] +
                    1.0 * y[4] + 4.0 * y[5] - 2.0 * y[6];
                yy[0] = yy[0] / 42.0;
                yy[1] = 8.0 * y[0] + 19.0 * y[1] + 16.0 * y[2] + 6.0 * y[3] -
                    4.0 * y[4] - 7.0 * y[5] + 4.0 * y[6];
                yy[1] = yy[1] / 42.0;
                yy[2] = -4.0 * y[0] + 16.0 * y[1] + 19.0 * y[2] + 12.0 * y[3] +
                    2.0 * y[4] - 4.0 * y[5] + 1.0 * y[6];
                yy[2] = yy[2] / 42.0;
                for (i = 3; i <= n - 4; i++)
                {
                    yy[i] = (-2.0 * (y[i - 3] + y[i + 3]) +
                        3.0 * (y[i - 2] + y[i + 2]) +
                        6.0 * (y[i - 1] + y[i + 1]) + 7.0 * y[i]) / 21.0;
                }
                yy[n - 3] = (-4.0 * y[n - 1] + 16.0 * y[n - 2] + 19.0 * y[n - 3] +
                    12.0 * y[n - 4] + 2.0 * y[n - 5] - 4.0 * y[n - 6] + 1.0 * y[n - 7]) / 42.0;
                yy[n - 2] = (8.0 * y[n - 1] + 19.0 * y[n - 2] + 16.0 * y[n - 3] +
                    6.0 * y[n - 4] - 4.0 * y[n - 5] - 7.0 * y[n - 6] + 4.0 * y[n - 7]) / 42.0;
                yy[n - 1] = (39.0 * y[n - 1] + 8.0 * y[n - 2] - 4.0 * y[n - 3] -
                    4.0 * y[n - 4] + 1.0 * y[n - 5] + 4.0 * y[n - 6] - 2.0 * y[n - 7]) / 42.0;
            }
        }

        /// <summary>
        /// 七点二次平滑滤波
        /// </summary>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt7_2(int n, double[] y, double[] yy)
        {
            int i;
            if (n < 7)
            {
                /*for ( i = 0; i <= n - 1; i++ )
                {
                    yy[i] = y[i];
                }*/
                Kdspt_dtmean(n, 5, y, yy);
            }
            else
            {
                yy[0] = (32.0 * y[0] + 15.0 * y[1] + 3.0 * y[2] - 4.0 * y[3] -
                    6.0 * y[4] - 3.0 * y[5] + 5.0 * y[6]) / 42.0;

                yy[1] = (5.0 * y[0] + 4.0 * y[1] + 3.0 * y[2] + 2.0 * y[3] +
                    y[4] - y[6]) / 14.0;

                yy[2] = (1.0 * y[0] + 3.0 * y[1] + 4.0 * y[2] + 4.0 * y[3] +
                    3.0 * y[4] + 1.0 * y[5] - 2.0 * y[6]) / 14.0;
                for (i = 3; i <= n - 4; i++)
                {
                    yy[i] = (-2.0 * (y[i - 3] + y[i + 3]) +
                        3.0 * (y[i - 2] + y[i + 2]) +
                        6.0 * (y[i - 1] + y[i + 1]) + 7.0 * y[i]) / 21.0;
                }
                yy[n - 3] = (1.0 * y[n - 1] + 3.0 * y[n - 2] + 4.0 * y[n - 3] +
                    4.0 * y[n - 4] + 3.0 * y[n - 5] + 1.0 * y[n - 6] - 2.0 * y[n - 7]) / 14.0;

                yy[n - 2] = (5.0 * y[n - 1] + 4.0 * y[n - 2] + 3.0 * y[n - 3] +
                    2.0 * y[n - 4] + y[n - 5] - y[n - 7]) / 14.0;

                yy[n - 1] = (32.0 * y[n - 1] + 15.0 * y[n - 2] + 3.0 * y[n - 3] -
                    4.0 * y[n - 4] - 6.0 * y[n - 5] - 3.0 * y[n - 6] + 5.0 * y[n - 7]) / 42.0;
            }
        }

        /// <summary>
        /// 五点两次平滑滤波
        /// </summary>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt5_2(int n, double[] y, double[] yy)
        {
            int i;
            if (n < 5)
            {
                //for ( i = 0; i <= n - 1; i++ )
                //{
                //	yy[i] = y[i];
                //}
                Kdspt_dtmean(n, 5, y, yy);
            }
            else
            {
                yy[0] = (31.0 * y[0] + 9.0 * y[1] - 3.0 * y[2] - 5.0 * y[3] + 3.0 * y[4]) / 35.0;
                yy[1] = (9.0 * y[0] + 13.0 * y[1] + 12 * y[2] + 6.0 * y[3] - 5.0 * y[4]) / 35.0;
                for (i = 2; i <= n - 3; i++)
                {
                    yy[i] = (-3.0 * (y[i - 2] + y[i + 2]) +
                        12.0 * (y[i - 1] + y[i + 1]) + 17 * y[i]) / 35.0;
                }
                yy[n - 2] = (9.0 * y[n - 1] + 13.0 * y[n - 2] + 12.0 * y[n - 3] + 6.0 * y[n - 4] - 5.0 * y[n - 5]) / 35.0;
                yy[n - 1] = (31.0 * y[n - 1] + 9.0 * y[n - 2] - 3.0 * y[n - 3] - 5.0 * y[n - 4] + 3.0 * y[n - 5]) / 35.0;
            }
        }

        /// <summary>
        /// 排序
        /// </summary>
        /// <param name="nNum"></param>
        /// <param name="pInput"></param>
        /// <param name="pOutput"></param>
        public static void SortDouble(int nNum, double[] pInput, double[] pOutput)
        {
            int sType = 3;
            PcrAnalysisSort<double> s = new PcrAnalysisSort<double>(nNum);
            for (int i = 0; i < nNum; i++)
            {
                pOutput[i] = pInput[i];
            }
            s.SelectSortType(sType, pOutput);
        }

        /// <summary>
        /// 中位数滤波 cfnum(窗口)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="cfnum"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt_Median(int n, int cfnum, double[] y, double[] yy)
        {
            if (cfnum < 3 || cfnum % 2 != 1)
            {
                for (int i = 0; i < n; i++)
                {
                    yy[i] = y[i];
                }
            }
            else
            {
                int halfwindow = cfnum / 2;
                for (int i = 0; i < n; i++)
                {
                    if (n < cfnum)
                    {
                        int k = (i < cfnum - 1) ? i : cfnum - 1;
                        double[] temp = new double[k + 1];
                        Array.Copy(y, i - k, temp, 0, k + 1);
                        SortDouble(k + 1, temp, temp);
                        yy[i] = temp[(k + 1) / 2];
                    }
                    else
                    {
                        int beginindex = (i >= halfwindow) ? i - halfwindow : 0;
                        int endindex = (i + halfwindow <= n - 1) ? i + halfwindow : n - 1;

                        int nPre = i - beginindex;
                        int nBehind = endindex - i;
                        if (nPre > nBehind)
                        {
                            beginindex = i - nBehind;
                        }
                        else
                        {
                            endindex = i + nPre;
                        }

                        int nCount = endindex - beginindex + 1;
                        double[] sortArray = new double[nCount];
                        Array.Copy(y, beginindex, sortArray, 0, nCount);
                        SortDouble(nCount, sortArray, sortArray);
                        yy[i] = sortArray[nCount / 2];
                    }
                }
            }
        }

        /// <summary>
        /// 中心滑动平均滤波 cfnum(滑动窗口为奇数)
        /// </summary>
        /// <param name="n"></param>
        /// <param name="cfnum"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt_cfmean(int n, int cfnum, double[] y, double[] yy)
        {
            if (cfnum < 3 || cfnum % 2 != 1)
            {
                for (int i = 0; i < n; i++)
                {
                    yy[i] = y[i];
                }
            }
            else
            {
                int halfwindow = cfnum / 2;
                for (int i = 0; i < n; i++)
                {
                    if (n < cfnum)
                    {
                        int k = (i < cfnum - 1) ? i : cfnum - 1;
                        double sum = 0;
                        for (int j = 0; j <= k; j++)
                        {
                            sum += y[i - k + j];
                        }
                        yy[i] = sum / (k + 1);
                    }
                    else
                    {
                        int beginindex = (i >= halfwindow) ? i - halfwindow : 0;
                        int endindex = (i + halfwindow <= n - 1) ? i + halfwindow : n - 1;

                        int nPre = i - beginindex;
                        int nBehind = endindex - i;
                        if (nPre > nBehind)
                        {
                            beginindex = i - nBehind;
                        }
                        else
                        {
                            endindex = i + nPre;
                        }

                        double sum = 0;
                        for (int j = beginindex; j <= endindex; j++)
                        {
                            sum += y[j];
                        }

                        int windowSize = endindex - beginindex + 1;
                        if (windowSize == 7)
                        {
                            yy[i] = (y[beginindex] + 3 * y[beginindex + 1] + 6 * y[beginindex + 2] +
                                     7 * y[beginindex + 3] + 6 * y[beginindex + 4] + 3 * y[beginindex + 5] +
                                     y[beginindex + 6]) / 27;
                        }
                        else
                        {
                            yy[i] = sum / windowSize;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 29点三角平滑滤波（前14个数据、后14个数据不做处理）
        /// </summary>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt_TriangularSmooths_29(int n, double[] y, double[] yy)
        {
            double[] dweightcoef = {0.004444444,0.008888889,0.013333333,0.017777778,0.022222222,
            0.026666667,0.031111111,0.035555556,0.04,0.044444444,
            0.048888889,0.053333333,0.057777778,0.062222222,0.066666667,
            0.062222222,0.057777778,0.053333333,0.048888889,0.044444444,
            0.04,0.035555556,0.031111111,0.026666667,0.022222222,
            0.017777778,0.013333333,0.008888889,0.004444444};

            int i;
            if (n < 29)
            {
                for (i = 0; i <= n - 1; i++)
                {
                    yy[i] = y[i];
                }
            }
            else
            {
                for (i = 0; i < 14; i++)
                {
                    yy[i] = y[i];
                }
                for (i = 14; i < n - 14; i++)
                {
                    yy[i] = ((dweightcoef[0] * y[i - 14] + dweightcoef[1] * y[i - 13] + dweightcoef[2] * y[i - 12] + dweightcoef[3] * y[i - 11] + dweightcoef[4] * y[i - 10])
                        + (dweightcoef[5] * y[i - 9] + dweightcoef[6] * y[i - 8] + dweightcoef[7] * y[i - 7] + dweightcoef[8] * y[i - 6] + dweightcoef[9] * y[i - 5])
                        + (dweightcoef[10] * y[i - 4] + dweightcoef[11] * y[i - 3] + dweightcoef[12] * y[i - 2] + dweightcoef[13] * y[i - 1] + dweightcoef[14] * y[i])
                        + (dweightcoef[15] * y[i + 1] + dweightcoef[16] * y[i + 2] + dweightcoef[17] * y[i + 3] + dweightcoef[18] * y[i + 4] + dweightcoef[19] * y[i + 5])
                        + (dweightcoef[20] * y[i + 6] + dweightcoef[21] * y[i + 7] + dweightcoef[22] * y[i + 8] + dweightcoef[23] * y[i + 9] + dweightcoef[24] * y[i + 10])
                        + (dweightcoef[25] * y[i + 11] + dweightcoef[26] * y[i + 12] + dweightcoef[27] * y[i + 13] + dweightcoef[28] * y[i + 14]));
                }
                for (i = n - 14; i < n; i++)
                {
                    yy[i] = y[i];
                }
            }
        }

        /// <summary>
        /// 31点伪高斯平滑滤波（前15个数据、后15个数据不做处理）
        /// </summary>
        /// <param name="n"></param>
        /// <param name="y"></param>
        /// <param name="yy"></param>
        public static void Kdspt_GaussianSmooths_31(int n, double[] y, double[] yy)
        {
            double[] dweightcoef = {0.004603022,  0.006432366,    0.008783666,    0.011720827,    0.015283334,
            0.01947401,     0.024247672,    0.029502724,    0.035077739,    0.040754773,
            0.046270347,    0.051333922,    0.055652353,    0.058957631,    0.061034293,
            0.061742643,    0.061034293,    0.058957631,    0.055652353,    0.051333922,
            0.046270347,    0.040754773,    0.035077739,    0.029502724,    0.024247672,
            0.01947401,     0.015283334,    0.011720827,    0.008783666,    0.006432366,    0.004603022};

            int i;
            if (n < 31)
            {
                for (i = 0; i <= n - 1; i++)
                {
                    yy[i] = y[i];
                }
            }
            else
            {
                for (i = 0; i < 15; i++)
                {
                    yy[i] = y[i];
                }
                for (i = 15; i < n - 15; i++)
                {
                    yy[i] = ((dweightcoef[0] * y[i - 15] + dweightcoef[1] * y[i - 14] + dweightcoef[2] * y[i - 13] + dweightcoef[3] * y[i - 12] + dweightcoef[4] * y[i - 11] + dweightcoef[5] * y[i - 10])
                        + (dweightcoef[6] * y[i - 9] + dweightcoef[7] * y[i - 8] + dweightcoef[8] * y[i - 7] + dweightcoef[9] * y[i - 6] + dweightcoef[10] * y[i - 5])
                        + (dweightcoef[11] * y[i - 4] + dweightcoef[12] * y[i - 3] + dweightcoef[13] * y[i - 2] + dweightcoef[14] * y[i - 1] + dweightcoef[15] * y[i])
                        + (dweightcoef[16] * y[i + 1] + dweightcoef[17] * y[i + 2] + dweightcoef[18] * y[i + 3] + dweightcoef[19] * y[i + 4] + dweightcoef[20] * y[i + 5])
                        + (dweightcoef[21] * y[i + 6] + dweightcoef[22] * y[i + 7] + dweightcoef[23] * y[i + 8] + dweightcoef[24] * y[i + 9] + dweightcoef[25] * y[i + 10])
                        + (dweightcoef[26] * y[i + 11] + dweightcoef[27] * y[i + 12] + dweightcoef[28] * y[i + 13] + dweightcoef[29] * y[i + 14] + dweightcoef[30] * y[i + 15]));
                }
                for (i = n - 15; i < n; i++)
                {
                    yy[i] = y[i];
                }
            }
            return;
        }

        /// <summary>
        /// 样条插值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="s"></param>
        /// <param name="bSpline"></param>
        public static void Akspl(double[] x, double[] y, int n, int k, double t, double[] s, bool bSpline)
        //double t,x[],y[],s[5];
        {
            int kk, m, l;
            double[] u = new double[5];
            double p, q;
            s[4] = 0.0; s[0] = 0.0; s[1] = 0.0; s[2] = 0.0; s[3] = 0.0;
            if (n < 1)
                return;
            if (n == 1) { s[0] = y[0]; s[4] = y[0]; return; }
            if (n == 2)
            {
                s[0] = y[0]; s[1] = (x[1] - x[0]) == 0 ? (y[1] - y[0]) : (y[1] - y[0]) / (x[1] - x[0]);
                if (k < 0)
                    s[4] = (y[0] * (t - x[1]) - y[1] * (t - x[0])) / (x[0] - x[1]);
                return;
            }
            if (k < 0)
            {
                if (t <= x[1])
                    kk = 0;
                else
                    if (t >= x[n - 1] && !bSpline)
                    kk = n - 2;
                else
                {
                    kk = 1; m = n;
                    if (bSpline)
                    {
                        int nTemp = n;
                        for (int i = kk; i < n; i++)/*for( int i=n-1; i>= kk; i--)*/
                        {
                            if (x[i] > t && y[i] > y[nTemp])
                            {
                                nTemp = i;
                                break;
                            }
                        }
                        m = nTemp;
                    }

                    while (((kk - m) != 1) && ((kk - m) != -1) && kk != m)
                    {
                        l = (kk + m) / 2;
                        if (t < x[l - 1]) m = l;
                        else kk = l;
                        /*kk = l;*/
                    }
                    if (!bSpline)
                        kk = kk - 1;
                }
            }
            else kk = k;
            if (kk >= n - 1) kk = n - 2;
            u[2] = (y[kk + 1] - y[kk]) / (x[kk + 1] - x[kk]);
            if (n == 3)
            {
                if (kk == 0)
                {
                    u[3] = (y[2] - y[1]) / (x[2] - x[1]);
                    u[4] = 2.0 * u[3] - u[2];
                    u[1] = 2.0 * u[2] - u[3];
                    u[0] = 2.0 * u[1] - u[2];
                }
                else
                {
                    u[1] = (y[1] - y[0]) / (x[1] - x[0]);
                    u[0] = 2.0 * u[1] - u[2];
                    u[3] = 2.0 * u[2] - u[1];
                    u[4] = 2.0 * u[3] - u[2];
                }
            }
            else
            {
                if (kk <= 1)
                {
                    u[3] = (x[kk + 2] - x[kk + 1]) == 0 ? (y[kk + 2] - y[kk + 1]) : (y[kk + 2] - y[kk + 1]) / (x[kk + 2] - x[kk + 1]);
                    if (kk == 1)
                    {
                        u[1] = (x[1] - x[0]) == 0 ? (y[1] - y[0]) : (y[1] - y[0]) / (x[1] - x[0]);
                        u[0] = 2.0 * u[1] - u[2];
                        if (n == 4) u[4] = 2.0 * u[3] - u[2];
                        else u[4] = (x[4] - x[3]) == 0 ? (y[4] - y[3]) : (y[4] - y[3]) / (x[4] - x[3]);
                    }
                    else
                    {
                        u[1] = 2.0 * u[2] - u[3];
                        u[0] = 2.0 * u[1] - u[2];
                        u[4] = (y[3] - y[2]) / (x[3] - x[2]);
                    }
                }
                else if (kk >= (n - 3))
                {
                    u[1] = (y[kk] - y[kk - 1]) / (x[kk] - x[kk - 1]);
                    if (kk == (n - 3))
                    {
                        u[3] = (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);
                        u[4] = 2.0 * u[3] - u[2];
                        if (n == 4) u[0] = 2.0 * u[1] - u[2];
                        else u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    }
                    else
                    {
                        u[3] = 2.0 * u[2] - u[1];
                        u[4] = 2.0 * u[3] - u[2];
                        u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    }
                }
                else
                {
                    u[1] = (x[kk] - x[kk - 1]) == 0 ? (y[kk] - y[kk - 1]) : (y[kk] - y[kk - 1]) / (x[kk] - x[kk - 1]);
                    u[0] = (x[kk - 1] - x[kk - 2]) == 0 ? (y[kk - 1] - y[kk - 2]) : (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    u[3] = (x[kk + 2] - x[kk + 1]) == 0 ? (y[kk + 2] - y[kk + 1]) : (y[kk + 2] - y[kk + 1]) / (x[kk + 2] - x[kk + 1]);
                    u[4] = (x[kk + 3] - x[kk + 2]) == 0 ? (y[kk + 3] - y[kk + 2]) : (y[kk + 3] - y[kk + 2]) / (x[kk + 3] - x[kk + 2]);
                }
            }
            s[0] = Math.Abs(u[3] - u[2]);
            s[1] = Math.Abs(u[0] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                p = (u[1] + u[2]) / 2.0;
            else p = (s[0] * u[1] + s[1] * u[2]) / (s[0] + s[1]);
            s[0] = Math.Abs(u[3] - u[4]);
            s[1] = Math.Abs(u[2] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                q = (u[2] + u[3]) / 2.0;
            else q = (s[0] * u[2] + s[1] * u[3]) / (s[0] + s[1]);
            s[0] = y[kk];
            s[1] = p;
            s[3] = x[kk + 1] - x[kk];
            s[2] = (3.0 * u[2] - 2.0 * p - q) / s[3];
            s[3] = (q + p - 2.0 * u[2]) / (s[3] * s[3]);
            if (k < 0)
            {
                p = t - x[kk];
                s[4] = s[0] + s[1] * p + s[2] * p * p + s[3] * p * p * p;
            }
        }

        /// <summary>
        /// 样条插值
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <param name="t"></param>
        /// <param name="s"></param>
        /// <param name="bSpline"></param>
        public static void AksplEx(double[] x, double[] y, int n, int k, double t, double[] s, bool bSpline)
        //double t,x[],y[],s[5];
        {
            int kk, m, l;
            double[] u = new double[5];
            double p, q;
            s[4] = 0.0; s[0] = 0.0; s[1] = 0.0; s[2] = 0.0; s[3] = 0.0;
            if (n < 1)
                return;

            if (n == 1) { s[0] = y[0]; s[4] = y[0]; return; }
            if (n == 2)
            {
                s[0] = y[0]; s[1] = (x[1] - x[0]) == 0 ? (y[1] - y[0]) : (y[1] - y[0]) / (x[1] - x[0]);
                if (k < 0)
                    s[4] = (y[0] * (t - x[1]) - y[1] * (t - x[0])) / (x[0] - x[1]);
                return;
            }
            if (k < 0)
            {
                //if (t<=x[1])
                //	kk=0;
                //else 
                if (t >= x[n - 1] && !bSpline)
                    kk = n - 2;
                else
                {
                    kk = 1; m = n;
                    if (bSpline)
                    {
                        int nTemp = n;
                        /*for( int i=kk; i< n; i++)*/
                        for (int i = n - 1; i >= kk; i--)
                        {
                            if (x[i] < t /*&& y[i] > y[nTemp]*/)
                            {
                                nTemp = i + 1;
                                break;
                            }
                        }
                        m = nTemp;
                    }

                    while (((kk - m) != 1) && ((kk - m) != -1) && kk != m)
                    {
                        l = (kk + m) / 2;
                        if (!bSpline)
                        {
                            if (t < x[l - 1]) m = l;
                            else kk = l;
                        }
                        else
                            kk = l;
                    }
                    if (!bSpline)
                        kk = kk - 1;
                }
            }
            else kk = k;
            if (kk >= n - 1) kk = n - 2;
            u[2] = (y[kk + 1] - y[kk]) / (x[kk + 1] - x[kk]);
            if (n == 3)
            {
                if (kk == 0)
                {
                    u[3] = (y[2] - y[1]) / (x[2] - x[1]);
                    u[4] = 2.0 * u[3] - u[2];
                    u[1] = 2.0 * u[2] - u[3];
                    u[0] = 2.0 * u[1] - u[2];
                }
                else
                {
                    u[1] = (y[1] - y[0]) / (x[1] - x[0]);
                    u[0] = 2.0 * u[1] - u[2];
                    u[3] = 2.0 * u[2] - u[1];
                    u[4] = 2.0 * u[3] - u[2];
                }
            }
            else
            {
                if (kk <= 1)
                {
                    u[3] = (x[kk + 2] - x[kk + 1]) == 0 ? (y[kk + 2] - y[kk + 1]) : (y[kk + 2] - y[kk + 1]) / (x[kk + 2] - x[kk + 1]);
                    if (kk == 1)
                    {
                        u[1] = (x[1] - x[0]) == 0 ? (y[1] - y[0]) : (y[1] - y[0]) / (x[1] - x[0]);
                        u[0] = 2.0 * u[1] - u[2];
                        if (n == 4) u[4] = 2.0 * u[3] - u[2];
                        else u[4] = (x[4] - x[3]) == 0 ? (y[4] - y[3]) : (y[4] - y[3]) / (x[4] - x[3]);
                    }
                    else
                    {
                        u[1] = 2.0 * u[2] - u[3];
                        u[0] = 2.0 * u[1] - u[2];
                        u[4] = (y[3] - y[2]) / (x[3] - x[2]);
                    }
                }
                else if (kk >= (n - 3))
                {
                    u[1] = (y[kk] - y[kk - 1]) / (x[kk] - x[kk - 1]);
                    if (kk == (n - 3))
                    {
                        u[3] = (y[n - 1] - y[n - 2]) / (x[n - 1] - x[n - 2]);
                        u[4] = 2.0 * u[3] - u[2];
                        if (n == 4) u[0] = 2.0 * u[1] - u[2];
                        else u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    }
                    else
                    {
                        u[3] = 2.0 * u[2] - u[1];
                        u[4] = 2.0 * u[3] - u[2];
                        u[0] = (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    }
                }
                else
                {
                    u[1] = (x[kk] - x[kk - 1]) == 0 ? (y[kk] - y[kk - 1]) : (y[kk] - y[kk - 1]) / (x[kk] - x[kk - 1]);
                    u[0] = (x[kk - 1] - x[kk - 2]) == 0 ? (y[kk - 1] - y[kk - 2]) : (y[kk - 1] - y[kk - 2]) / (x[kk - 1] - x[kk - 2]);
                    u[3] = (x[kk + 2] - x[kk + 1]) == 0 ? (y[kk + 2] - y[kk + 1]) : (y[kk + 2] - y[kk + 1]) / (x[kk + 2] - x[kk + 1]);
                    u[4] = (x[kk + 3] - x[kk + 2]) == 0 ? (y[kk + 3] - y[kk + 2]) : (y[kk + 3] - y[kk + 2]) / (x[kk + 3] - x[kk + 2]);
                }
            }
            s[0] = Math.Abs(u[3] - u[2]);
            s[1] = Math.Abs(u[0] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                p = (u[1] + u[2]) / 2.0;
            else p = (s[0] * u[1] + s[1] * u[2]) / (s[0] + s[1]);
            s[0] = Math.Abs(u[3] - u[4]);
            s[1] = Math.Abs(u[2] - u[1]);
            if ((s[0] + 1.0 == 1.0) && (s[1] + 1.0 == 1.0))
                q = (u[2] + u[3]) / 2.0;
            else q = (s[0] * u[2] + s[1] * u[3]) / (s[0] + s[1]);
            s[0] = y[kk];
            s[1] = p;
            s[3] = x[kk + 1] - x[kk];
            s[2] = (3.0 * u[2] - 2.0 * p - q) / s[3];
            s[3] = (q + p - 2.0 * u[2]) / (s[3] * s[3]);
            if (k < 0)
            {
                p = t - x[kk];
                s[4] = s[0] + s[1] * p + s[2] * p * p + s[3] * p * p * p;
            }
        }

        /// <summary>
        /// 非方阵求逆，用于多项式拟合
        /// </summary>
        /// <param name="a"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="aa"></param>
        /// <param name="eps"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="ka"></param>
        /// <returns></returns>
        public static int Dginv(double[] a, int m, int n, double[] aa, double eps, double[] u, double[] v, int ka)
        {
            int i, j, k, l, t, p, q, f;
            double[] at = new double[m * n];
            for (i = 0; i < m * n; i++)
                at[i] = a[i];
            i = Dluav(at, m, n, u, v, eps, ka);
            if (i < 0) return (-1);
            j = n;
            if (m < n) j = m;
            j = j - 1;
            k = 0;
            while ((k <= j) && (a[k * n + k] != 0.0)) k = k + 1;
            k = k - 1;
            for (i = 0; i <= n - 1; i++)
            {
                for (j = 0; j <= m - 1; j++)
                {
                    t = i * m + j; aa[t] = 0.0;
                    for (l = 0; l <= k; l++)
                    {
                        f = l * n + i; p = j * m + l; q = l * n + l;
                        aa[t] = aa[t] + v[f] * u[p] / at[q];
                    }
                }
            }
            return (1);
        }

        /// <summary>
        /// Dluav矩阵分解，用于求逆函数
        /// </summary>
        /// <param name="a"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <param name="eps"></param>
        /// <param name="ka"></param>
        /// <returns></returns>
        public static int Dluav(double[] a, int m, int n, double[] u, double[] v, double eps, int ka)
        {
            int i, j, k, l, it, ll, kk, ix, iy, mm, nn, iz, m1, ks;
            double d, dd, t, sm, sm1, em1, sk, ek, b, c, shh;
            double[] fg = new double[2];
            double[] cs = new double[2];
            double[] s, e, w;
            s = new double[ka];
            e = new double[ka];
            w = new double[ka];
            it = 60; k = n;
            if (m - 1 < n) k = m - 1;
            l = m;
            if (n - 2 < m) l = n - 2;
            if (l < 0) l = 0;
            ll = k;
            if (l > k) ll = l;
            if (ll >= 1)
            {
                for (kk = 1; kk <= ll; kk++)
                {
                    if (kk <= k)
                    {
                        d = 0.0;
                        for (i = kk; i <= m; i++)
                        {
                            ix = (i - 1) * n + kk - 1; d = d + a[ix] * a[ix];
                        }
                        s[kk - 1] = Math.Sqrt(d);
                        if (s[kk - 1] != 0.0)
                        {
                            ix = (kk - 1) * n + kk - 1;
                            if (a[ix] != 0.0)
                            {
                                s[kk - 1] = Math.Abs(s[kk - 1]);
                                if (a[ix] < 0.0) s[kk - 1] = -s[kk - 1];
                            }
                            for (i = kk; i <= m; i++)
                            {
                                iy = (i - 1) * n + kk - 1;
                                a[iy] = a[iy] / s[kk - 1];
                            }
                            a[ix] = 1.0 + a[ix];
                        }
                        s[kk - 1] = -s[kk - 1];
                    }
                    if (n >= kk + 1)
                    {
                        for (j = kk + 1; j <= n; j++)
                        {
                            if ((kk <= k) && (s[kk - 1] != 0.0))
                            {
                                d = 0.0;
                                for (i = kk; i <= m; i++)
                                {
                                    ix = (i - 1) * n + kk - 1;
                                    iy = (i - 1) * n + j - 1;
                                    d = d + a[ix] * a[iy];
                                }
                                d = -d / a[(kk - 1) * n + kk - 1];
                                for (i = kk; i <= m; i++)
                                {
                                    ix = (i - 1) * n + j - 1;
                                    iy = (i - 1) * n + kk - 1;
                                    a[ix] = a[ix] + d * a[iy];
                                }
                            }
                            e[j - 1] = a[(kk - 1) * n + j - 1];
                        }
                    }
                    if (kk <= k)
                    {
                        for (i = kk; i <= m; i++)
                        {
                            ix = (i - 1) * m + kk - 1; iy = (i - 1) * n + kk - 1;
                            u[ix] = a[iy];
                        }
                    }
                    if (kk <= l)
                    {
                        d = 0.0;
                        for (i = kk + 1; i <= n; i++)
                            d = d + e[i - 1] * e[i - 1];
                        e[kk - 1] = Math.Sqrt(d);
                        if (e[kk - 1] != 0.0)
                        {
                            if (e[kk] != 0.0)
                            {
                                e[kk - 1] = Math.Abs(e[kk - 1]);
                                if (e[kk] < 0.0) e[kk - 1] = -e[kk - 1];
                            }
                            for (i = kk + 1; i <= n; i++)
                                e[i - 1] = e[i - 1] / e[kk - 1];
                            e[kk] = 1.0 + e[kk];
                        }
                        e[kk - 1] = -e[kk - 1];
                        if ((kk + 1 <= m) && (e[kk - 1] != 0.0))
                        {
                            for (i = kk + 1; i <= m; i++) w[i - 1] = 0.0;
                            for (j = kk + 1; j <= n; j++)
                                for (i = kk + 1; i <= m; i++)
                                    w[i - 1] = w[i - 1] + e[j - 1] * a[(i - 1) * n + j - 1];
                            for (j = kk + 1; j <= n; j++)
                                for (i = kk + 1; i <= m; i++)
                                {
                                    ix = (i - 1) * n + j - 1;
                                    a[ix] = a[ix] - w[i - 1] * e[j - 1] / e[kk];
                                }
                        }
                        for (i = kk + 1; i <= n; i++)
                            v[(i - 1) * n + kk - 1] = e[i - 1];
                    }
                }
            }
            mm = n;
            if (m + 1 < n) mm = m + 1;
            if (k < n) s[k] = a[k * n + k];
            if (m < mm) s[mm - 1] = 0.0;
            if (l + 1 < mm) e[l] = a[l * n + mm - 1];
            e[mm - 1] = 0.0;
            nn = m;
            if (m > n) nn = n;
            if (nn >= k + 1)
            {
                for (j = k + 1; j <= nn; j++)
                {
                    for (i = 1; i <= m; i++)
                        u[(i - 1) * m + j - 1] = 0.0;
                    u[(j - 1) * m + j - 1] = 1.0;
                }
            }
            if (k >= 1)
            {
                for (ll = 1; ll <= k; ll++)
                {
                    kk = k - ll + 1; iz = (kk - 1) * m + kk - 1;
                    if (s[kk - 1] != 0.0)
                    {
                        if (nn >= kk + 1)
                            for (j = kk + 1; j <= nn; j++)
                            {
                                d = 0.0;
                                for (i = kk; i <= m; i++)
                                {
                                    ix = (i - 1) * m + kk - 1;
                                    iy = (i - 1) * m + j - 1;
                                    d = d + u[ix] * u[iy] / u[iz];
                                }
                                d = -d;
                                for (i = kk; i <= m; i++)
                                {
                                    ix = (i - 1) * m + j - 1;
                                    iy = (i - 1) * m + kk - 1;
                                    u[ix] = u[ix] + d * u[iy];
                                }
                            }
                        for (i = kk; i <= m; i++)
                        {
                            ix = (i - 1) * m + kk - 1; u[ix] = -u[ix];
                        }
                        u[iz] = 1.0 + u[iz];
                        if (kk - 1 >= 1)
                            for (i = 1; i <= kk - 1; i++)
                                u[(i - 1) * m + kk - 1] = 0.0;
                    }
                    else
                    {
                        for (i = 1; i <= m; i++)
                            u[(i - 1) * m + kk - 1] = 0.0;
                        u[(kk - 1) * m + kk - 1] = 1.0;
                    }
                }
            }
            for (ll = 1; ll <= n; ll++)
            {
                kk = n - ll + 1; iz = kk * n + kk - 1;
                if ((kk <= l) && (e[kk - 1] != 0.0))
                {
                    for (j = kk + 1; j <= n; j++)
                    {
                        d = 0.0;
                        for (i = kk + 1; i <= n; i++)
                        {
                            ix = (i - 1) * n + kk - 1; iy = (i - 1) * n + j - 1;
                            d = d + v[ix] * v[iy] / v[iz];
                        }
                        d = -d;
                        for (i = kk + 1; i <= n; i++)
                        {
                            ix = (i - 1) * n + j - 1; iy = (i - 1) * n + kk - 1;
                            v[ix] = v[ix] + d * v[iy];
                        }
                    }
                }
                for (i = 1; i <= n; i++)
                    v[(i - 1) * n + kk - 1] = 0.0;
                v[iz - n] = 1.0;
            }
            for (i = 1; i <= m; i++)
            {
                for (j = 1; j <= n; j++)
                    a[(i - 1) * n + j - 1] = 0.0;
            }
            m1 = mm; it = 60;
            while (1 == 1)
            {
                if (mm == 0)
                {
                    Ppp(a, e, s, v, m, n);
                    return (1);
                }
                if (it == 0)
                {
                    Ppp(a, e, s, v, m, n);
                    return (-1);
                }
                kk = mm - 1;
                while ((kk != 0) && (Math.Abs(e[kk - 1]) != 0.0))
                {
                    d = Math.Abs(s[kk - 1]) + Math.Abs(s[kk]);
                    dd = Math.Abs(e[kk - 1]);
                    if (dd > eps * d) kk = kk - 1;
                    else e[kk - 1] = 0.0;
                }
                if (kk == mm - 1)
                {
                    kk = kk + 1;
                    if (s[kk - 1] < 0.0)
                    {
                        s[kk - 1] = -s[kk - 1];
                        for (i = 1; i <= n; i++)
                        {
                            ix = (i - 1) * n + kk - 1; v[ix] = -v[ix];
                        }
                    }
                    while ((kk != m1) && (s[kk - 1] < s[kk]))
                    {
                        d = s[kk - 1]; s[kk - 1] = s[kk]; s[kk] = d;
                        if (kk < n)
                            for (i = 1; i <= n; i++)
                            {
                                ix = (i - 1) * n + kk - 1; iy = (i - 1) * n + kk;
                                d = v[ix]; v[ix] = v[iy]; v[iy] = d;
                            }
                        if (kk < m)
                            for (i = 1; i <= m; i++)
                            {
                                ix = (i - 1) * m + kk - 1; iy = (i - 1) * m + kk;
                                d = u[ix]; u[ix] = u[iy]; u[iy] = d;
                            }
                        kk = kk + 1;
                    }
                    it = 60;
                    mm = mm - 1;
                }
                else
                {
                    ks = mm;
                    while ((ks > kk) && (Math.Abs(s[ks - 1]) != 0.0))
                    {
                        d = 0.0;
                        if (ks != mm) d = d + Math.Abs(e[ks - 1]);
                        if (ks != kk + 1) d = d + Math.Abs(e[ks - 2]);
                        dd = Math.Abs(s[ks - 1]);
                        if (dd > eps * d) ks = ks - 1;
                        else s[ks - 1] = 0.0;
                    }
                    if (ks == kk)
                    {
                        kk = kk + 1;
                        d = Math.Abs(s[mm - 1]);
                        t = Math.Abs(s[mm - 2]);
                        if (t > d) d = t;
                        t = Math.Abs(e[mm - 2]);
                        if (t > d) d = t;
                        t = Math.Abs(s[kk - 1]);
                        if (t > d) d = t;
                        t = Math.Abs(e[kk - 1]);
                        if (t > d) d = t;
                        sm = s[mm - 1] / d; sm1 = s[mm - 2] / d;
                        em1 = e[mm - 2] / d;
                        sk = s[kk - 1] / d; ek = e[kk - 1] / d;
                        b = ((sm1 + sm) * (sm1 - sm) + em1 * em1) / 2.0;
                        c = sm * em1; c = c * c; shh = 0.0;
                        if ((b != 0.0) || (c != 0.0))
                        {
                            shh = Math.Sqrt(b * b + c);
                            if (b < 0.0) shh = -shh;
                            shh = c / (b + shh);
                        }
                        fg[0] = (sk + sm) * (sk - sm) - shh;
                        fg[1] = sk * ek;
                        for (i = kk; i <= mm - 1; i++)
                        {
                            Sss(fg, cs);
                            if (i != kk) e[i - 2] = fg[0];
                            fg[0] = cs[0] * s[i - 1] + cs[1] * e[i - 1];
                            e[i - 1] = cs[0] * e[i - 1] - cs[1] * s[i - 1];
                            fg[1] = cs[1] * s[i];
                            s[i] = cs[0] * s[i];
                            if ((cs[0] != 1.0) || (cs[1] != 0.0))
                                for (j = 1; j <= n; j++)
                                {
                                    ix = (j - 1) * n + i - 1;
                                    iy = (j - 1) * n + i;
                                    d = cs[0] * v[ix] + cs[1] * v[iy];
                                    v[iy] = -cs[1] * v[ix] + cs[0] * v[iy];
                                    v[ix] = d;
                                }
                            Sss(fg, cs);
                            s[i - 1] = fg[0];
                            fg[0] = cs[0] * e[i - 1] + cs[1] * s[i];
                            s[i] = -cs[1] * e[i - 1] + cs[0] * s[i];
                            fg[1] = cs[1] * e[i];
                            e[i] = cs[0] * e[i];
                            if (i < m)
                                if ((cs[0] != 1.0) || (cs[1] != 0.0))
                                    for (j = 1; j <= m; j++)
                                    {
                                        ix = (j - 1) * m + i - 1;
                                        iy = (j - 1) * m + i;
                                        d = cs[0] * u[ix] + cs[1] * u[iy];
                                        u[iy] = -cs[1] * u[ix] + cs[0] * u[iy];
                                        u[ix] = d;
                                    }
                        }
                        e[mm - 2] = fg[0];
                        it = it - 1;
                    }
                    else
                    {
                        if (ks == mm)
                        {
                            kk = kk + 1;
                            fg[1] = e[mm - 2]; e[mm - 2] = 0.0;
                            for (ll = kk; ll <= mm - 1; ll++)
                            {
                                i = mm + kk - ll - 1;
                                fg[0] = s[i - 1];
                                Sss(fg, cs);
                                s[i - 1] = fg[0];
                                if (i != kk)
                                {
                                    fg[1] = -cs[1] * e[i - 2];
                                    e[i - 2] = cs[0] * e[i - 2];
                                }
                                if ((cs[0] != 1.0) || (cs[1] != 0.0))
                                    for (j = 1; j <= n; j++)
                                    {
                                        ix = (j - 1) * n + i - 1;
                                        iy = (j - 1) * n + mm - 1;
                                        d = cs[0] * v[ix] + cs[1] * v[iy];
                                        v[iy] = -cs[1] * v[ix] + cs[0] * v[iy];
                                        v[ix] = d;
                                    }
                            }
                        }
                        else
                        {
                            kk = ks + 1;
                            fg[1] = e[kk - 2];
                            e[kk - 2] = 0.0;
                            for (i = kk; i <= mm; i++)
                            {
                                fg[0] = s[i - 1];
                                Sss(fg, cs);
                                s[i - 1] = fg[0];
                                fg[1] = -cs[1] * e[i - 1];
                                e[i - 1] = cs[0] * e[i - 1];
                                if ((cs[0] != 1.0) || (cs[1] != 0.0))
                                    for (j = 1; j <= m; j++)
                                    {
                                        ix = (j - 1) * m + i - 1;
                                        iy = (j - 1) * m + kk - 2;
                                        d = cs[0] * u[ix] + cs[1] * u[iy];
                                        u[iy] = -cs[1] * u[ix] + cs[0] * u[iy];
                                        u[ix] = d;
                                    }
                            }
                        }
                    }
                }
            }
            return (1);

        }

        /// <summary>
        /// Ppp矩阵分解，用于求逆函数
        /// </summary>
        /// <param name="a"></param>
        /// <param name="e"></param>
        /// <param name="s"></param>
        /// <param name="v"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        public static void Ppp(double[] a, double[] e, double[] s, double[] v, int m, int n)
        {
            int i, j, p, q;
            double d;
            if (m >= n) i = n;
            else i = m;
            for (j = 1; j <= i - 1; j++)
            {
                a[(j - 1) * n + j - 1] = s[j - 1];
                a[(j - 1) * n + j] = e[j - 1];
            }
            a[(i - 1) * n + i - 1] = s[i - 1];
            if (m < n) a[(i - 1) * n + i] = e[i - 1];
            for (i = 1; i <= n - 1; i++)
                for (j = i + 1; j <= n; j++)
                {
                    p = (i - 1) * n + j - 1; q = (j - 1) * n + i - 1;
                    d = v[p]; v[p] = v[q]; v[q] = d;
                }
            return;
        }

        /// <summary>
        /// Sss矩阵分解，用于求逆函数
        /// </summary>
        /// <param name="fg"></param>
        /// <param name="cs"></param>
        public static void Sss(double[] fg, double[] cs)
        {
            double r, d;
            if ((Math.Abs(fg[0]) + Math.Abs(fg[1])) == 0.0)
            {
                cs[0] = 1.0; cs[1] = 0.0; d = 0.0;
            }
            else
            {
                d = Math.Sqrt(fg[0] * fg[0] + fg[1] * fg[1]);
                if (Math.Abs(fg[0]) > Math.Abs(fg[1]))
                {
                    d = Math.Abs(d);
                    if (fg[0] < 0.0) d = -d;
                }
                if (Math.Abs(fg[1]) >= Math.Abs(fg[0]))
                {
                    d = Math.Abs(d);
                    if (fg[1] < 0.0) d = -d;
                }
                cs[0] = fg[0] / d; cs[1] = fg[1] / d;
            }
            r = 1.0;
            if (Math.Abs(fg[0]) > Math.Abs(fg[1]))
                r = cs[1];
            else
                if (cs[0] != 0.0)
                r = 1.0 / cs[0];
            fg[0] = d; fg[1] = r;
        }

        /// <summary>
        /// 矩阵乘法
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="m"></param>
        /// <param name="n"></param>
        /// <param name="k"></param>
        /// <param name="c"></param>
        public static void Damul(double[] a, double[] b, int m, int n, int k, double[] c)
        {
            int i, j, l, u;
            for (i = 0; i <= m - 1; i++)
                for (j = 0; j <= k - 1; j++)
                {
                    u = i * k + j; c[u] = 0.0;
                    for (l = 0; l <= n - 1; l++)
                        c[u] = c[u] + a[i * n + l] * b[l * k + j];
                }
            return;
        }

    }
}
