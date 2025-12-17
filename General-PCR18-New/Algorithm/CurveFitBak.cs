using System;
using System.Linq;
using MathNet.Numerics;

namespace General_PCR18.Algorithm
{
    public class CurveFitBak
    {
        // Logistic 4参数函数：Ymin + (Ymax - Ymin) / (1 + (x/Xo)^P)
        private static readonly Func<double, double, double, double, double, double> logistic4 = (A, B, C, D, x) =>
        {
            return A + (B - A) / (1.0 + Math.Pow(x / C, D));
        };

        public static double[] Fitting(double[] y_data, out double Ct)
        {
            // X 轴：Cycle 数（1 到 60）
            double[] x_data = new double[y_data.Length];
            for (int i = 0; i < x_data.Length; i++)
            {
                x_data[i] = i + 1;
            }

            // 初始参数猜测值：A, B, C, D
            double[] p0 = { y_data.Min(), y_data.Max(), x_data[x_data.Length / 2], 2.0 };

            // 拟合曲线
            var params1 = Fit.Curve(x_data, y_data, logistic4, p0[0], p0[1], p0[2], p0[3]);

            // 设定阈值（50%高度处）
            double threshold = p0[0] + (p0[1] - p0[0]) * 0.5;

            // 反推 Ct 值            
            double Ymin = params1.P0;
            double Ymax = params1.P1;
            double Xo = params1.P2;
            double P = params1.P3;
            Ct = Xo * Math.Pow(((Ymax - Ymin) / (threshold - Ymin) - 1), 1.0 / P);

            // 生成拟合曲线
            double[] x_fit = new double[x_data.Length];
            double[] y_fit = new double[x_data.Length];
            for (int i = 0; i < x_data.Length; i++)
            {
                x_fit[i] = i + 1;
                y_fit[i] = logistic4(params1.P0, params1.P1, params1.P2, params1.P3, x_fit[i]);
            }

            return y_fit;
        }
    }
}
