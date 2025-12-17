using General_PCR18.Common;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Media;
using System.Linq;

namespace General_PCR18.Util
{
    public class Tools
    {
        /// <summary>
        /// Hex 颜色代码转换 Brush
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static SolidColorBrush HexToBrush(string hex)
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        /// <summary>
        /// Hex 颜色转 Drawing Color
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static System.Drawing.Color HexToColor(string hex)
        {
            return System.Drawing.ColorTranslator.FromHtml(hex);
        }

        /// <summary>
        /// 验证输入是否是非负整数
        /// </summary>
        /// <param name="input">需要验证的字符串</param>
        /// <returns>如果输入是有效非负整数，返回 true；否则返回 false</returns>
        public static bool IsNonnegativeInt(string input, out int result)
        {
            result = 0;
            string pattern = @"^\d*$";
            if (!Regex.IsMatch(input, pattern))
            {
                return false;
            }

            return int.TryParse(input, out result) && result >= 0;
        }

        /// <summary>
        /// 验证小数
        /// </summary>
        /// <param name="input"></param>
        /// <param name="decimalPlaces"></param>
        /// <returns></returns>
        public static bool IsValidDouble(string input, int decimalPlaces, out double number)
        {
            number = 0;
            string pattern = $"^\\d+(\\.\\d{{{decimalPlaces}}})?$";
            if (!Regex.IsMatch(input, pattern))
            {
                return false;
            }

            if (!double.TryParse(input, out number))
            {
                return false;
            }

            //if (decimalPlaces > 0)
            //{
            //    string[] parts = input.Split('.');
            //    if (parts.Length == 2 && parts[1].Length > decimalPlaces)
            //    {
            //        return false;
            //    }
            //}

            return true;
        }

        /// <summary>
        /// 将秒数转换为 h:mm:ss 格式的字符串
        /// </summary>
        /// <param name="totalSeconds">总秒数</param>
        /// <returns>格式化的 h:mm:ss 字符串</returns>
        public static string SecondsToHms(int totalSeconds)
        {
            // 使用 TimeSpan.FromSeconds 将秒数转换为 TimeSpan
            TimeSpan timeSpan = TimeSpan.FromSeconds(totalSeconds);

            // 将 TimeSpan 格式化为 h:mm:ss 格式
            return string.Format("{0}:{1:mm}:{1:ss}", (int)timeSpan.TotalHours, timeSpan);
        }

        /// <summary>
        /// 根据试管序号返回对应的管号
        /// </summary>
        /// <param name="tubeIdnex"></param>
        /// <returns></returns>
        public static string GetDockUnit(int tubeIdnex)
        {
            int x = tubeIdnex % 6;
            int y = tubeIdnex / 6;
            string dockUnit = VarDef.SampleAxisCharList[y + 6] + VarDef.SampleAxisCharList[x];
            return dockUnit;
        }

        /// <summary>
        /// 根据管号返回试管序号
        /// </summary>
        /// <param name="tubeName"></param>
        /// <returns></returns>
        public static int GetDockIndex(string tubeName)
        {
            char rowChar = tubeName[0];
            int colNum = int.Parse(tubeName.Substring(1));

            int rowIndex;
            switch (rowChar)
            {
                case 'A':
                    rowIndex = 1;
                    break;
                case 'B':
                    rowIndex = 2;
                    break;
                case 'C':
                    rowIndex = 3;
                    break;
                default:
                    return -1;
            }
            int index = (rowIndex - 1) * 6 + (colNum - 1);
            return index;
        }

        /// <summary>
        /// 计算 Y 轴最小值
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static double CalculateYMin(double number)
        {
            // 正数
            if (number >= 0 || double.IsNaN(number))
            {
                return -1;
            }

            // 处理负数  

            if (number > -1.0)
            {
                return -1.0;
            }
            if (number > -0.5)
            {
                return -0.5;
            }

            int a = (int)number;
            long p = GetPositionValue(a);

            return (int)(a / p) * p - p;
        }

        public static int GetNumberLengthLogarithmic(int number)
        {
            if (number == 0) return 1; // 特殊处理 0 的情况
            number = Math.Abs(number); // 去掉负号
            return (int)Math.Floor(Math.Log10(number)) + 1;
        }

        public static long GetPositionValue(long number)
        {
            if (number == 0) return 1; // 0 的位置值是 1

            number = Math.Abs(number); // 去掉负号

            // 使用对数计算数字的位数
            int digits = (int)Math.Floor(Math.Log10(number)) + 1;

            // 返回对应的位置值
            return (long)Math.Pow(10, digits - 1);
        }

        /// <summary>
        /// 将字符串写入文件流
        /// </summary>
        /// <param name="fileStream"></param>
        /// <param name="input"></param>
        public static void WriteStringToFile(FileStream fileStream, string input)
        {
            // 获取字符串的长度
            int length = input.Length;

            // 写入字符串长度
            byte[] lengthBytes = BitConverter.GetBytes(length);
            fileStream.Write(lengthBytes, 0, lengthBytes.Length);

            // 如果字符串长度大于 0，则写入字符串内容
            if (length > 0)
            {
                // 假设使用 UTF-16 编码（类似于 TCHAR 在 Unicode 模式下的大小）
                byte[] stringBytes = Encoding.Unicode.GetBytes(input);
                fileStream.Write(stringBytes, 0, stringBytes.Length);
            }
        }

        /// <summary>
        /// 从文件流中读取字符串
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        public static string ReadStringFromFile(FileStream fileStream)
        {
            // 读取字符串长度
            byte[] lengthBytes = new byte[sizeof(int)];
            fileStream.Read(lengthBytes, 0, lengthBytes.Length);
            int length = BitConverter.ToInt32(lengthBytes, 0);

            // 如果字符串长度大于 0，则读取字符串内容
            string result = "";
            if (length > 0)
            {
                // 使用 UTF-16 编码读取字符串
                byte[] stringBytes = new byte[length * sizeof(char)];
                fileStream.Read(stringBytes, 0, stringBytes.Length);

                result = Encoding.Unicode.GetString(stringBytes);
            }

            return result;
        }

		/// <summary>
		/// 清理用于文件名的字符串，移除非法字符并裁剪长度
		/// </summary>
		/// <param name="name">原始名称</param>
		/// <returns>可用于文件名的安全字符串</returns>
		public static string SanitizeFileName(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
			{
				return string.Empty;
			}

			// 替换无效文件名字符为下划线
			char[] invalidChars = Path.GetInvalidFileNameChars();
			var sanitized = new StringBuilder(name.Length);
			foreach (char c in name)
			{
				if (invalidChars.Contains(c) || char.IsControl(c))
				{
					sanitized.Append('_');
				}
				else
				{
					sanitized.Append(c);
				}
			}

			// 去除首尾空白并将连续空白压缩为单个下划线
			string trimmed = sanitized.ToString().Trim();
			string collapsed = Regex.Replace(trimmed, @"\s+", "_");

			// 为避免过长的文件名，限制到 80 个字符
			if (collapsed.Length > 80)
			{
				collapsed = collapsed.Substring(0, 80);
			}

			return collapsed;
		}
    }
}
