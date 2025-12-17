using General_PCR18.Algorithm;
using General_PCR18.PageUi;
using MathNet.Numerics;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static SQLite.SQLite3;

namespace TestProject1
{
    public class AlgorithmTest
    {
        /// <summary>
        /// 获取单元格类型
        /// </summary>
        /// <param name="cell">目标单元格</param>
        /// <returns></returns>
        private static object GetValueType(ICell cell)
        {
            if (cell == null)
                return null;
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return null;
                case CellType.Boolean:
                    return cell.BooleanCellValue;
                case CellType.Numeric:
                    return cell.NumericCellValue;
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Error:
                    return cell.ErrorCellValue;
                case CellType.Formula:
                    // 这里只能处理数值公式。其他公式将会被转换成数值类型，如 日期公式。
                    return cell.NumericCellValue;
                default:
                    return cell.StringCellValue;
            }
        }

        private DataTable ReadExcelFile()
        {
            DataTable dt = new DataTable();
            IWorkbook workbook;

            try
            {
                string path = AppDomain.CurrentDomain.BaseDirectory + @"..\..\..\CT Comparison PS96 vs Powergene.xlsx";
                string fileExt = Path.GetExtension(path).ToLower();
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    if (fileExt == ".xlsx")
                    {
                        workbook = new XSSFWorkbook(fs);
                    }
                    else if (fileExt == ".xls")
                    {
                        workbook = new HSSFWorkbook(fs);
                    }
                    else
                    {
                        workbook = null;
                    }

                    ISheet sheet = workbook.GetSheetAt(0);

                    //表头  
                    IRow header = sheet.GetRow(sheet.FirstRowNum);
                    List<int> columns = new List<int>();
                    for (int i = 0; i < header.LastCellNum; i++)
                    {
                        object obj = GetValueType(header.GetCell(i));
                        if (obj == null || obj.ToString() == string.Empty)
                        {
                            dt.Columns.Add(new DataColumn("Columns" + i.ToString()));
                        }
                        else
                            dt.Columns.Add(new DataColumn(obj.ToString()));
                        columns.Add(i);
                    }

                    //数据  
                    for (int i = sheet.FirstRowNum + 1; i <= sheet.LastRowNum; i++)
                    {
                        DataRow dr = dt.NewRow();
                        bool hasValue = false;
                        foreach (int j in columns)
                        {
                            dr[j] = GetValueType(sheet.GetRow(i).GetCell(j));
                            if (dr[j] != null && dr[j].ToString() != string.Empty)
                            {
                                hasValue = true;
                            }
                        }
                        if (hasValue)
                        {
                            dt.Rows.Add(dr);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return dt;
        }

        [Test]
        public void PrintData()
        {

            DataTable dt = ReadExcelFile();

            int num = dt.Rows.Count;
            double[] pdx = new double[num];
            double[] pdyInput = new double[num];

            for (int i = 0; i < num; i++)
            {
                pdx[i] = double.Parse(dt.Rows[i][0].ToString());
                pdyInput[i] = double.Parse(dt.Rows[i][1].ToString());
            }

            foreach (var d in pdyInput)
            {
                Console.Write(d + ", ");
            }
        }

        [Test]
        public void CalcCtPos()
        {

            DataTable dt = ReadExcelFile();

            int num = dt.Rows.Count;
            double[] pdx = new double[num];
            double[] pdyInput = new double[num];

            for (int i = 0; i < num; i++)
            {
                pdx[i] = double.Parse(dt.Rows[i][0].ToString());
                pdyInput[i] = double.Parse(dt.Rows[i][1].ToString());
            }

            // 计算ct位置
            double ct = DataAlgorithmPCR.CalcCtPos(num, pdx, pdyInput);
            Console.WriteLine("Ct1={0}", ct);
        }

        [Test]
        public void NormalizedAnalysisBySndDerivative()
        {

            DataTable dt = ReadExcelFile();

            int num = dt.Rows.Count;
            double[] pdx = new double[num];
            double[] pdyInput = new double[num];
            double[] pdyOutput = new double[num];

            for (int i = 0; i < num; i++)
            {
                pdx[i] = double.Parse(dt.Rows[i][0].ToString());
                pdyInput[i] = double.Parse(dt.Rows[i][1].ToString());
            }

            DataAlgorithmPCR.NormalizedAnalysisBySndDerivative(num, pdx, pdyInput, pdyOutput, DataAlgorithmPCR.tagFunAmpNormalizedAnaParamInfo);
            foreach (var d in pdyOutput)
            {
                Console.Write(d);
                Console.Write(", ");
            }
        }

        [Test]
        public void DeltaRnAnalysisBySndDerivative()
        {
            DataTable dt = ReadExcelFile();

            int num = dt.Rows.Count;
            double[] pdx = new double[num];
            double[] pdyInput = new double[num];
            double[] pdyOutput = new double[num];

            List<double> dataY = new List<double>();

            for (int i = 0; i < num; i++)
            {
                pdx[i] = double.Parse(dt.Rows[i][0].ToString());
                pdyInput[i] = double.Parse(dt.Rows[i][1].ToString());

                dataY.Add(double.Parse(dt.Rows[i][1].ToString()));
            }

            DataAlgorithmPCR.DeltaRnAnalysisBySndDerivative(num, pdx, pdyInput, pdyOutput, DataAlgorithmPCR.tagFunAmpNormalizedAnaParamInfo);

            foreach (var d in pdyInput)
            {
                Console.Write(d);
                Console.Write(", ");
            }
            Console.WriteLine();

            foreach (var d in pdyOutput)
            {
                Console.Write(d);
                Console.Write(", ");
            }
            Console.WriteLine();

            // 获取最大值，所在位置就是Ct
            double maxValue = pdyOutput.Max();
            int maxIndex = Array.IndexOf(pdyOutput, maxValue);
            Console.WriteLine("Max: {0}, Index: {1}", maxValue, maxIndex);

            // 计算
            //PcrAlgorigthm.Normalized(dataY, maxValue);
        }

        [Test]
        public void CurveFitTest()
        {
            // 60个归一化数据
            double[] y_data = {
                0.05, 0.05, 0.06, 0.06, 0.05, 0.05, 0.06, 0.05, 0.06, 0.06,
                0.06, 0.07, 0.07, 0.08, 0.10, 0.13, 0.18, 0.25, 0.35, 0.45,
                0.55, 0.65, 0.74, 0.81, 0.87, 0.91, 0.94, 0.96, 0.98, 0.99,
                1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00,
                1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00,
                1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00, 1.00
            };

            double[] y_fit = CurveFitBak.Fitting(y_data, out double Ct);

            // 输出结果
            Console.WriteLine($"Ct: {Ct}");

            for (int i = 0; i < y_fit.Length; i++)
            {
                Console.WriteLine(y_fit[i]);
            }
        }

        double[] c_crosstalk_fam = new double[] { 4933, 4944, 4935, 4957, 4938, 4921, 4951, 4943, 4930, 4925, 4944, 4969, 4988, 5009, 5096, 5162, 5279, 5487, 5754, 6137, 6733, 7490, 8597, 9952, 11388, 12928, 14326, 15594, 16604, 17327, 17913, 18306, 18617, 18883, 18967, 19170, 19246, 19446, 19541, 19670, 19709, 19830, 19953, 19966, 19970, 20078, 20199, 20269, 20290, 20362, 20490, 20340, 20401, 20507, 20402, 20488, 20555, 20684, 20674, 20662 };
        double[] c_filtering_fam = new double[] { 4937.5, 4937.7, 4938, 4938, 4938, 4943, 4938, 4930, 4943, 4943, 4944, 4969, 4988, 5009, 5096, 5162, 5279, 5487, 5754, 6137, 6733, 7490, 8597, 9952, 11388, 12928, 14326, 15594, 16604, 17327, 17913, 18306, 18617, 18883, 18967, 19170, 19246, 19446, 19541, 19670, 19709, 19830, 19953, 19966, 19970, 20078, 20199, 20269, 20290, 20340, 20362, 20401, 20402, 20402, 20488, 20507, 20555, 20662, 20674, 20662 };
        double[] c_baseline_fam = new double[] { 4937.187273, 4937.074545, 4937.061818, 4936.749091, 4936.436364, 4941.123636, 4935.810909, 4927.498182, 4940.185455, 4939.872727, 4940.56, 4965.247273, 4983.934545, 5004.621818, 5091.309091, 5156.996364, 5273.683636, 5481.370909, 5748.058182, 6130.745455, 6726.432727, 7483.12, 8589.807273, 9944.494545, 11380.18182, 12919.86909, 14317.55636, 15585.24364, 16594.93091, 17317.61818, 17903.30545, 18295.99273, 18606.68, 18872.36727, 18956.05455, 19158.74182, 19234.42909, 19434.11636, 19528.80364, 19657.49091, 19696.17818, 19816.86545, 19939.55273, 19952.24, 19955.92727, 20063.61455, 20184.30182, 20253.98909, 20274.67636, 20324.36364, 20346.05091, 20384.73818, 20385.42545, 20385.11273, 20470.8, 20489.48727, 20537.17455, 20643.86182, 20655.54909, 20643.23636 };
        double[] c_mot_calibrated_fam = new double[] { 4937.187273, 4937.074545, 4937.061818, 4936.749091, 4936.436364, 4941.123636, 4935.810909, 4927.498182, 4940.185455, 4939.872727, 4940.56, 4965.247273, 4983.934545, 5004.621818, 5091.309091, 5156.996364, 5273.683636, 5481.370909, 5748.058182, 6130.745455, 6726.432727, 7483.12, 8589.807273, 9944.494545, 11380.18182, 12919.86909, 14317.55636, 15585.24364, 16594.93091, 17317.61818, 17903.30545, 18295.99273, 18606.68, 18872.36727, 18956.05455, 19158.74182, 19234.42909, 19434.11636, 19528.80364, 19657.49091, 19696.17818, 19816.86545, 19939.55273, 19952.24, 19955.92727, 20063.61455, 20184.30182, 20253.98909, 20274.67636, 20324.36364, 20346.05091, 20384.73818, 20385.42545, 20385.39418, 20470.8, 20489.48727, 20537.17455, 20643.86182, 20655.54909, 20654.31782 };
        double[] c_smooth_filtering = new double[] { 4951.579834, 4951.572856, 4951.579993, 4951.667825, 4951.881433, 4952.336, 4953.208727, 4968.104, 4992.167273, 5030.342545, 5088.549818, 5175.877091, 5305.300364, 5494.179636, 5764.706909, 6143.458182, 6656.601455, 7325.928727, 8161.648, 9155.767273, 10280.11055, 11488.00582, 12719.60509, 13912.02036, 15010.63564, 15975.55491, 16786.63418, 17442.40145, 17957.09673, 18352.736, 18656.21527, 18891.61455, 19080.40582, 19237.56509, 19374.19636, 19494.57164, 19603.07491, 19700.83418, 19788.89745, 19868.32073, 19942.888, 20013.27927, 20079.87855, 20142.74982, 20201.00734, 20252.31512, 20296.24115, 20333.73743, 20367.28396, 20399.84998, 20433.50816, 20469.54164, 20508.05207, 20546.03962, 20580.06755, 20608.37106, 20629.46036, 20642.59699, 20649.76481, 20653.12504 };
        double[] c_normalize = new double[] { -0.000112065, -0.000113474, -0.000112033, -9.42967E-05, -5.11624E-05, 4.06298E-05, 0.000216862, 0.003224711, 0.008083883, 0.015792734, 0.02754671, 0.045180978, 0.071315823, 0.109456803, 0.164085217, 0.240567641, 0.34418826, 0.4793476, 0.648106996, 0.848852596, 1.075894734, 1.319808788, 1.568509464, 1.809297593, 2.031144396, 2.22599355, 2.389777313, 2.522198441, 2.62613246, 2.70602513, 2.767307645, 2.814842553, 2.852965762, 2.884701424, 2.912291802, 2.936599556, 2.958509959, 2.978250794, 2.996033685, 3.012071873, 3.027129475, 3.041343804, 3.054792402, 3.067488194, 3.079252318, 3.089613057, 3.098483176, 3.106054917, 3.112829072, 3.119405231, 3.126201932, 3.133478284, 3.141254816, 3.14892576, 3.155797126, 3.161512542, 3.165771169, 3.168423891, 3.16987131, 3.170549852 };
        double[] c_deltarn = new double[] { 0, 0, 0, 0, 0, 0, 0, 15, 40, 78, 136, 223, 353, 542, 812, 1191, 1704, 2373, 3209, 4203, 5327, 6535, 7767, 8959, 10058, 11023, 11834, 12490, 13004, 13400, 13704, 13939, 14128, 14285, 14422, 14542, 14650, 14748, 14836, 14916, 14990, 15061, 15127, 15190, 15248, 15300, 15344, 15381, 15415, 15447, 15481, 15517, 15555, 15593, 15627, 15656, 15677, 15690, 15697, 15700 };

        [Test]
        public void NormalizedTest()
        {
            // 光源的数据准备：FAM、Cy5、VIC、Cy5.5、ROX、MOT
            List<double> fam = new List<double>() {
                4933,4944,4935,4957,4938,4921,4951,4943,4930,4925,4944,4969,4988,5009,5096,5162,5279,5487,5754,6137,6733,7490,8597,9952,11388,12928,14326,15594,16604,17327,17913,18306,18617,18883,18967,19170,19246,19446,19541,19670,19709,19830,19953,19966,19970,20078,20199,20269,20290,20362,20490,20340,20401,20507,20402,20488,20555,20684,20674,20662
            };

            List<double> cy5 = new List<double>() {
                4609,4557,4621,4599,4570,4581,4589,4580,4595,4624,4614,4618,4635,4646,4678,4717,4766,4880,5019,5257,5569,5926,6481,7100,7750,8332,8863,9280,9699,10005,10213,10269,10431,10507,10560,10668,10763,10768,10859,10904,10969,11028,11060,11042,11087,11100,11151,11148,11193,11222,11303,11244,11258,11306,11328,11345,11319,11381,11388,11383
            };

            List<double> vic = new List<double>() {
                5362,5357,5405,5372,5423,5423,5398,5431,5451,5465,5493,5539,5646,5768,5931,6242,6656,7259,8213,9606,11463,13747,16330,18873,20932,22222,22976,23383,23698,23881,24092,24330,24378,24499,24562,24608,24740,24859,24943,25050,25003,24996,25176,25117,25166,25128,25135,25310,25267,25334,25431,25407,25377,25490,25386,25450,25450,25521,25580,25446
            };

            List<double> cy55 = new List<double>() {
                7719,7789,7959,8042,8015,7991,8056,8083,8078,8099,8205,8217,8276,8458,8725,9125,9677,10538,11866,13676,16196,19713,23870,28330,32950,36877,39848,41791,43313,43586,44226,44420,44602,44855,44970,44953,45161,45071,45291,45362,45497,45486,45651,45542,45581,45766,45744,45892,45735,45877,46028,45905,45863,45899,46069,45947,46088,46172,46190,46170
            };

            List<double> rox = new List<double>() {
                4644,4613,4725,4685,4745,4760,4730,4768,4774,4804,4851,4881,4968,5153,5366,5680,6159,6885,7986,9433,11465,13893,16652,19200,20886,21962,22584,22691,22792,22952,23043,23218,23186,23321,23187,23352,23353,23340,23393,23377,23492,23560,23559,23558,23500,23461,23521,23434,23423,23458,23657,23493,23503,23473,23526,23488,23547,23361,23684,23499
            };

            List<double> mot = new List<double>() {
                28204,28323,28354,28375,28316,28431,28396,28360,28357,28372,28449,28364,28384,28366,28342,28488,28391,28396,28412,28353,28452,28316,28396,28350,28455,28332,28347,28415,28351,28389,28324,28426,28308,28356,28347,28357,28329,28466,28365,28289,28413,28388,28460,28346,28340,28378,28348,28428,28352,28392,28368,28367,28365,28327,28377,28281,28367,28326,28414,28338
            };

            Console.WriteLine("Crosstalk===================================================");
            var (c1, c2, c3, c4, c5, motCorrect) = PcrAlgorigthm.CrosstlkCorrection(fam.ToArray(),
                    cy5.ToArray(),
                    vic.ToArray(),
                    cy55.ToArray(),
                    rox.ToArray(), mot.ToArray());

            for (int i = 0; i < c1.Length; i++)
            {
                Console.WriteLine(c1[i] - c_crosstalk_fam[i]);
            }

            Console.WriteLine();
            Console.WriteLine("Filtring===================================================");
            var (f1, f2, f3, f4, f5, filteredMot) = PcrAlgorigthm.MedianFiltering(c1, c2, c3, c4, c5, motCorrect);
            for (int i = 0; i < f1.Length; i++)
            {
                Console.WriteLine(f1[i] - c_filtering_fam[i]);
            }

            Console.WriteLine();
            Console.WriteLine("Baseline===================================================");
            var (b1, b2, b3, b4, b5, ct) = PcrAlgorigthm.BaselineAdjust(f1, f2, f3, f4, f5, motCorrect.ToArray());
            Console.WriteLine("===>CT:" + string.Join(",", ct));
            for (int i = 0; i < b1.Length; i++)
            {
                double d = b1[i] - c_baseline_fam[i];
                d = Math.Truncate(d * 1000000) / 1000000;
                string result = string.Format("{0:0.######}", d);

                Console.WriteLine(result);
            }

            Console.WriteLine();
            Console.WriteLine("MOT Calibration===================================================");
            var (m1, m2, m3, m4, m5, famCt) = PcrAlgorigthm.MotCalibration(b1, b2, b3, b4, b5, motCorrect.ToArray(), ct);
            for (int i = 0; i < m1.Length; i++)
            {
                double d = m1[i] - c_mot_calibrated_fam[i];
                d = Math.Truncate(d * 1000000) / 1000000;
                string result = string.Format("{0:0.######}", d);

                Console.WriteLine(result);
            }

            Console.WriteLine();
            Console.WriteLine("Smooth Filtering===================================================");
            var (sm1, sm2, sm3, sm4, sm5) = PcrAlgorigthm.SmoothData(m1, m2, m3, m4, m5);
            for (int i = 0; i < sm1.Length; i++)
            {
                double d = sm1[i] - c_smooth_filtering[i];
                d = Math.Truncate(d * 1000000) / 1000000;
                string result = string.Format("{0:0.######}", d);

                Console.WriteLine(result);
            }

            Console.WriteLine();
            Console.WriteLine("Normalize===================================================");
            double[] nFAM = NormalizationProcessor.ProcessNormalization(sm1, ct[0]);
            double[] nCy5 = NormalizationProcessor.ProcessNormalization(sm2, ct[1]);
            double[] nHEX = NormalizationProcessor.ProcessNormalization(sm3, ct[2]);
            double[] nCy5_5 = NormalizationProcessor.ProcessNormalization(sm4, ct[3]);
            double[] nROX = NormalizationProcessor.ProcessNormalization(sm5, ct[4]);
            for (int i = 0; i < nFAM.Length; i++)
            {
                double d = nFAM[i] - c_normalize[i];
                d = Math.Truncate(d * 1000000000) / 1000000000;
                string result = string.Format("{0:0.#########}", d);

                Console.WriteLine(result);
            }

            Console.WriteLine();
            Console.WriteLine("DeltaRn===================================================");
            var (dtFAM, dtCy5, dtHex, dtCy5_5, dtROX) = PcrAlgorigthm.DeltaRn(fam.ToArray(), cy5.ToArray(), vic.ToArray(),
                cy55.ToArray(), rox.ToArray(), mot.ToArray());
            for (int i = 0; i < dtFAM.Length; i++)
            {
                double d = dtFAM[i] - c_deltarn[i];
                d = Math.Truncate(d * 1000000000) / 1000000000;
                string result = string.Format("{0:0.#########}", d);

                Console.WriteLine(result);
            }

        }
    }
}
