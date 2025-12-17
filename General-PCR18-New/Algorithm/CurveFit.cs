using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General_PCR18.Algorithm
{
    public class CurveFit
    {
        public static double[] Fitting(double[] yValues, out double Ct, double threshold = 0.1)
        {
            List<double> xValues = Enumerable.Range(1, yValues.Length).Select(i => (double)i).ToList();
            List<double> yList = yValues.ToList();

            var fitter = new LogisticFitter(xValues, yList);
            fitter.Fit();

            double[] parameters = fitter.GetParams();
            double A0 = parameters[0];
            double A1 = parameters[1];
            double X0 = parameters[2];
            double p = parameters[3];

            // 计算 threshold 处的 Ct
            if (threshold <= A0 || threshold >= A1)
            {
                Ct = double.NaN; // 阈值超出曲线范围
            }
            else
            {
                Ct = X0 * Math.Pow((A1 - A0) / (threshold - A0) - 1, 1.0 / p);
            }

            return fitter.GetFittedValues().ToArray();
        }


        private class LogisticFitter
        {
            private List<double> XList, YList;
            private double[] LogisticY, LnX;
            private double[,] PartialMatrix, WeightMatrix;
            private double[] ResidualY;
            private double[] Params = new double[4];
            private double Ymin, Ymax, Ymean, Xmin;
            private const double Zero = 1e-10;
            private double BestR2 = -1e9;

            public LogisticFitter(List<double> x, List<double> y)
            {
                XList = new List<double>(x);
                YList = new List<double>(y);
                InitStats();
            }

            private void InitStats()
            {
                Ymin = YList.Min();
                Ymax = YList.Max();
                Ymean = YList.Average();
                Xmin = XList.Where(v => v > 0).Min();
            }

            private void PrepareLnXAndLogitY()
            {
                int n = XList.Count;
                LnX = new double[n];
                LogisticY = new double[n];
                double ymin = Ymin - 0.1;
                double ymax = Ymax + 1.0;

                for (int i = 0; i < n; i++)
                {
                    double y = YList[i];
                    double x = XList[i];
                    double temp = (y - ymin) / (ymax - y);
                    LogisticY[i] = Math.Log(temp);
                    LnX[i] = Math.Log(Math.Max(x, Zero));
                }
            }

            private void LinearFitInitialGuess()
            {
                int n = LnX.Length;
                double sumX = LnX.Sum();
                double sumY = LogisticY.Sum();
                double sumXY = LnX.Zip(LogisticY, (a, b) => a * b).Sum();
                double sumXX = LnX.Sum(x => x * x);

                double slope = (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
                double intercept = (sumY - slope * sumX) / n;

                Params[0] = Ymin - 0.1;
                Params[1] = Ymax + 1.0;
                Params[3] = -slope;
                Params[2] = Math.Exp(-intercept / slope);
            }

            private double Logistic(double x)
            {
                double z = Math.Pow(x / Params[2], Params[3]);
                return Params[0] + (Params[1] - Params[0]) / (1 + z);
            }

            private void FormJacobianAndResiduals()
            {
                int n = XList.Count;
                PartialMatrix = new double[n, 4];
                ResidualY = new double[n];
                WeightMatrix = new double[n, n];

                for (int i = 0; i < n; i++)
                {
                    double x = Math.Max(XList[i], Zero);
                    double z = Math.Pow(x / Params[2], Params[3]);
                    double denom = 1 + z;
                    double denom2 = denom * denom;
                    double delta = Params[1] - Params[0];
                    double yfit = Params[0] + delta / denom;
                    ResidualY[i] = YList[i] - yfit;

                    PartialMatrix[i, 0] = 1 - 1.0 / denom;
                    PartialMatrix[i, 1] = 1.0 / denom;
                    PartialMatrix[i, 2] = (Params[3] * z * delta) / (Params[2] * denom2);
                    PartialMatrix[i, 3] = -delta * z * Math.Log(x / Params[2]) / denom2;

                    for (int j = 0; j < n; j++)
                        WeightMatrix[i, j] = (i == j) ? 1.0 : 0.0;
                }
            }

            private double[] GaussNewtonStep()
            {
                int n = XList.Count;
                double[,] JTJ = new double[4, 4];
                double[] JTr = new double[4];

                for (int i = 0; i < n; i++)
                {
                    for (int a = 0; a < 4; a++)
                    {
                        JTr[a] += PartialMatrix[i, a] * ResidualY[i];
                        for (int b = 0; b < 4; b++)
                            JTJ[a, b] += PartialMatrix[i, a] * PartialMatrix[i, b];
                    }
                }

                return SolveLinearSystem(JTJ, JTr);
            }

            public void Fit(int maxIter = 100)
            {
                PrepareLnXAndLogitY();
                LinearFitInitialGuess();

                double[] bestParams = (double[])Params.Clone();

                for (int iter = 0; iter < maxIter; iter++)
                {
                    FormJacobianAndResiduals();
                    double[] delta = GaussNewtonStep();

                    for (double factor = 2; factor >= 1e-3; factor /= 2.0)
                    {
                        double[] tempParams = new double[4];
                        for (int j = 0; j < 4; j++)
                            tempParams[j] = Params[j] + delta[j] * factor;

                        double r2 = ComputeR2(tempParams);
                        if (r2 > BestR2)
                        {
                            BestR2 = r2;
                            bestParams = tempParams;
                        }
                    }

                    double improvement = Math.Abs(BestR2 - ComputeR2(Params));
                    if (improvement < 1e-6) break;

                    Params = bestParams;
                }
            }

            private double ComputeR2(double[] param)
            {
                double ssRes = 0, ssTot = 0;
                double mean = YList.Average();
                for (int i = 0; i < XList.Count; i++)
                {
                    double z = Math.Pow(XList[i] / param[2], param[3]);
                    double pred = param[0] + (param[1] - param[0]) / (1 + z);
                    ssRes += Math.Pow(YList[i] - pred, 2);
                    ssTot += Math.Pow(YList[i] - mean, 2);
                }
                return 1 - ssRes / (ssTot + Zero);
            }

            private double[] SolveLinearSystem(double[,] A, double[] b)
            {
                int n = b.Length;
                double[,] mat = new double[n, n + 1];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++) mat[i, j] = A[i, j];
                    mat[i, n] = b[i];
                }

                for (int k = 0; k < n; k++)
                {
                    double pivot = mat[k, k];
                    for (int j = k; j <= n; j++) mat[k, j] /= pivot;
                    for (int i = 0; i < n; i++)
                    {
                        if (i == k) continue;
                        double f = mat[i, k];
                        for (int j = k; j <= n; j++)
                            mat[i, j] -= f * mat[k, j];
                    }
                }

                double[] x = new double[n];
                for (int i = 0; i < n; i++) x[i] = mat[i, n];
                return x;
            }

            public double[] GetParams() => Params;
            public List<double> GetFittedValues() => XList.Select(x => Logistic(x)).ToList();
        }
    }
}
