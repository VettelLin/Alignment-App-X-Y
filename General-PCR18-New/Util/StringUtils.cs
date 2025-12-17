using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace General_PCR18.Util
{
    public class StringUtils
    {
        /// <summary>
        /// 空格分隔16进制字符串
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static string FormatHex(string hex)
        {
            if (hex == null)
            {
                return null;
            }

            hex = hex.Trim().Replace(" ", "");
            string formattedHex = string.Join(" ", Enumerable.Range(0, hex.Length / 2)
                                                        .Select(i => hex.Substring(i * 2, 2)))
                                      .TrimEnd();
            return formattedHex;
        }

        public static byte[] HexStringToByte(string hex)
        {
            hex = hex.Replace(" ", "");
            byte[] b = new byte[hex.Length / 2];
            for (int i = 0; i < hex.Length / 2; i++)
            {
                string strTemp = hex.Substring(i * 2, 2);
                b[i] = Convert.ToByte(strTemp, 16);
            }
            return b;
        }

        public static byte HexStringToOneByte(string hex)
        {
            hex = hex.Replace(" ", "");

            string strTemp = hex.Substring(0, 2);
            byte b = Convert.ToByte(strTemp, 16);

            return b;
        }

        public static string ByteToHexString(byte[] data)
        {
            string strTemp = "";
            for (int i = 0; i < data.Length; i++)
            {
                string a = Convert.ToString(data[i], 16).PadLeft(2, '0');
                strTemp += a;
            }
            return strTemp.ToUpper();
        }

        public static int HexStringToInt(string hex)
        {
            return int.Parse(hex.Replace(" ", ""), System.Globalization.NumberStyles.HexNumber);
        }

        public static long HexStringToLong(string hex)
        {
            return long.Parse(hex, System.Globalization.NumberStyles.HexNumber);
        }

        // <summary>
        /// 计算按位异或校验和（返回校验和值）
        /// </summary>
        /// <param name="Cmd">命令数组</param>
        /// <returns>校验和值</returns>
        public static byte GetXOR(byte[] Cmd)
        {
            byte check = (byte)(Cmd[0] ^ Cmd[1]);
            for (int i = 2; i < Cmd.Length; i++)
            {
                check = (byte)(check ^ Cmd[i]);
            }
            return check;
        }

        public static string HexXOR(string hex)
        {
            byte[] check = HexStringToByte(hex.Replace(" ", ""));
            byte c = GetXOR(check);
            string cc = ByteToHexString(IntToByte(c));
            return cc.Substring(cc.Length - 2);
        }

        /// <summary>
        /// 16进制转字符串
        /// </summary>
        /// <param name="hex"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HexStringToString(string hex, Encoding encoding)
        {
            return encoding.GetString(HexStringToByte(hex));
        }

        public static string HexStringToString(string hex)
        {
            return HexStringToString(hex, Encoding.UTF8);
        }

        /// <summary>
        /// int整数转换为4字节的byte数组
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] IntToByte(int i)
        {
            byte[] targets = new byte[4];
            targets[3] = (byte)(i & 0xFF);
            targets[2] = (byte)(i >> 8 & 0xFF);
            targets[1] = (byte)(i >> 16 & 0xFF);
            targets[0] = (byte)(i >> 24 & 0xFF);
            return targets;
        }

        /// <summary>
        /// byte数组转换为int整数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int Byte4ToInt(byte[] bytes, int offset)
        {
            int b0 = bytes[offset] & 0xFF;
            int b1 = bytes[offset + 1] & 0xFF;
            int b2 = bytes[offset + 2] & 0xFF;
            int b3 = bytes[offset + 3] & 0xFF;
            return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
        }

        public static int Byte4ToInt(byte[] bytes)
        {
            return Byte4ToInt(bytes, 0);
        }

        /// <summary>
        /// 无符号short整数转换为2字节的byte数组
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static byte[] UnsignedShortToByte(int s)
        {
            byte[] targets = new byte[2];
            targets[0] = (byte)(s >> 8 & 0xFF);
            targets[1] = (byte)(s & 0xFF);
            return targets;
        }

        public static byte IntToOneByte(int s)
        {
            return (byte)(s & 0xFF);
        }

        /// <summary>
        /// byte数组转换为无符号short整数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int ByteToUnsignedShort(byte[] bytes, int offset)
        {
            int high = bytes[offset];
            int low = bytes[offset + 1];
            return (high << 8 & 0xFF00) | (low & 0xFF);
        }

        public static int ByteToUnsignedShort(byte[] bytes)
        {
            return ByteToUnsignedShort(bytes, 0);
        }

        public static string ByteToString(byte[] value)
        {
            string str = System.Text.Encoding.UTF8.GetString(value);
            return str;
        }

        public static byte[] StringToBytes(string value)
        {
            return System.Text.Encoding.UTF8.GetBytes(value);
        }

        public static string StringToHexString(string str)
        {
            return ByteToHexString(StringToBytes(str));
        }

        /// <summary>
        /// 字符串分割
        /// </summary>
        /// <param name="value">原字符串</param>
        /// <param name="sp">分割字符</param>
        /// <returns></returns>
        public static string[] Split(string value, string sp)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new string[1] { value };
            }
            return value.Split(new String[1] { sp }, System.StringSplitOptions.None);
        }

        /// <summary>
        /// 去除byte[]数组缓冲区内的尾部空白区;从末尾向前判断;
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] TrimBytesEnd(byte[] bytes)
        {
            List<byte> list = bytes.ToList();
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                if (bytes[i] == 0x00)
                {
                    list.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
            return list.ToArray();
        }

        /// <summary>
        /// 将字符串按个数分割
        /// </summary>
        /// <param name="str"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static string[] Split(string str, int count)
        {
            var list = new List<string>();

            if (count <= 0)
            {
                list.Add(str);
            }
            else
            {
                int c = (str.Length + count - 1) / count;
                for (int i = 0; i < c; i++)
                {
                    string s = str.Substring(i * count, str.Length >= i * count + count ? count : str.Length - i * count);
                    list.Add(s);
                }
            }

            return list.ToArray();
        }

        /// <summary>
        /// 36进制转换10进制
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int Str36ToInt(string str)
        {
            int d = 0;
            int b;
            char[] ch = str.ToCharArray();
            int j = 0;
            for (int i = ch.Length - 1; i >= 0; i--)
            {
                char c = ch[i];
                b = 1;
                for (int t = 0; t < j; t++)
                    b *= 36;
                j++;
                int cc;
                if (c >= '0' && c <= '9')
                    cc = Convert.ToInt32(c) - 48;
                else
                    cc = Convert.ToInt32(c) - 65 + 10;
                d += cc * b;
            }
            return d;
        }

        public static string HexCC(int i)
        {
            return StringUtils.ByteToHexString(StringUtils.UnsignedShortToByte(i)).Substring(2, 2);
        }

        /// <summary>
        /// Int转换16进制
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string IntToHex(int i)
        {
            return i.ToString("X2");
        }

        /// <summary>
        /// Long转换16进制
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public static string LongToHex(long i)
        {
            return i.ToString("X2");
        }

        /// <summary>
        /// 数字左边补0
        /// </summary>
        /// <param name="number">数字</param>
        /// <param name="w">总长度</param>
        /// <returns></returns>
        public static string PadLeft(int number, int w)
        {
            return number.ToString().PadLeft(w, '0');
        }

        public static string HexStringToBinString(string hexString)
        {
            string result = string.Empty;
            foreach (char c in hexString)
            {
                int v = Convert.ToInt32(c.ToString(), 16);
                int v2 = int.Parse(Convert.ToString(v, 2));
                result += string.Format("{0:d4}", v2);
            }
            return result;
        }

        /// <summary>
        /// 二进制字符串形式转整数
        /// </summary>
        /// <param name="binString">二进制：100101</param>
        /// <returns></returns>
        public static int BinStringToInt(string binString)
        {
            return Convert.ToInt32(binString, 2);
        }

        public static string IntToBinString(int d)
        {
            return Convert.ToString(d, 2);
        }

        public static string BinStringToHexString(string binString)
        {
            return Convert.ToInt32(binString, 2).ToString("X2");
        }

        public static string ToUpper(string s)
        {
            return s == null ? "" : s.ToUpper();
        }

        /// <summary>
        /// 字符串处理  (_切分，首字母大写 （.)去除  )
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToCase(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            string[] array = value.Split('_');
            string str = "";
            foreach (string item in array)
            {
                string newStr = item.Replace("(", "").Replace(".", "").Replace(")", "");
                string firstLetter = newStr.Substring(0, 1);
                string rest = newStr.Substring(1, newStr.Length - 1);
                str += firstLetter.ToUpper() + rest.ToLower();
            }

            return str;
        }
    }
}
