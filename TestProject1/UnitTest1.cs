using General_PCR18;
using General_PCR18.Common;
using General_PCR18.Communication;
using General_PCR18.Util;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Web.UI.WebControls;

namespace TestProject1
{
    public class Tests
    {
        Pcr18Client pcr;

        [SetUp]
        public void Setup()
        {
            pcr = new Pcr18Client();
        }

        [Test]
        public void PasswordTest()
        {
            string hash = CryptUtil.Crypt("123456");
            Console.WriteLine("pwd:" + hash);
        }

        [Test]
        public void TestDataLen()
        {
            string hex = "5E 91 00 14 FF FF FF FF FF FF FF FF FF FF FF FF FF FF 00 F5";

            int len = pcr.GetDataLength(hex) * 2;
            Console.WriteLine("len:" + len);
        }

        [Test]
        public void TestRCC()
        {
            //string hex = "5E 01 00 05 64";
            //string hex = "5E 11 00 09 00 00 20 10 A8";
            string hex = "5E 91 00 14 FF FF FF FF FF FF FF FF FF FF FF FF FF FF 00 F5";
            byte a = pcr.CalculateChecksum(StringUtils.HexStringToByte(hex));
            Console.WriteLine("hex:" + hex);
            Console.WriteLine("byte rcc:" + a);
            Console.WriteLine("hex rcc:" + StringUtils.ByteToHexString(new byte[] { a }));

            hex = "5E 61 00 06 00 5E";
            a = pcr.CalculateChecksum(StringUtils.HexStringToByte(hex));
            Console.WriteLine("hex:" + hex);
            Console.WriteLine("byte rcc:" + a);
            Console.WriteLine("hex rcc:" + StringUtils.ByteToHexString(new byte[] { a }));
        }

        [Test]
        public void TestPRCKeyStasus()
        {
            string hex = "5E E8 00 18 00 02 04 06 08 0A 0C 0E 10 12 14 16 18 1A 1C 1E 20 22 00 6B";

            pcr.ProcessKeyStatus(hex);

            for (int i = 0; i < GlobalData.DS.PCRKeyStatus.Length; i++)
            {
                Console.WriteLine(GlobalData.DS.PCRKeyStatus[i]);
            }
        }

        [Test]
        public void TestLightData()
        {
            string hex = "5E 8C 00 2C 01 01 BF 41 AC 2C 77 40 E8 3E 3A 38 DA 30 AA 41 79 2C 2C 40 B4 3E FD 37 C8 30 BD 41 BF 2C 61 40 C6 3E 3B 38 D8 30 00 9F";
            Dictionary<int, double[]> data = pcr.ProcessLightData(hex);

            foreach (var d in data)
            {
                Console.WriteLine(d.Key + " " + string.Join(",", d.Value));
            }

        }

        [Test]
        public void TestLightData96()
        {
            string hex = "5E 8C 00 C6 9A 42 6D 42 B7 42 95 42 87 42 94 42 9E 42 46 42 5B 42 56 42 13 42 59 42 7A 45 96 45 A3 45 4D 45 4C 45 94 45 B4 45 96 45 72 45 85 45 84 45 84 45 81 45 AF 45 C9 45 95 45 3F 45 49 45 6F 45 4C 45 78 45 82 45 3E 45 4E 45 F2 07 E6 07 F4 07 E1 07 EF 07 DC 07 E0 07 DD 07 D3 07 BC 07 CC 07 C6 07 62 07 EC 07 E8 07 E7 07 E3 07 F0 5E 07 91 C6 00 07 14 D4 FF 07 FF DC FF 07 FF E5 FF 07 FF CC FF 07 FF AB FF 07 FF E7 FF 07 FF E1 FF 07 FF DB 00 07 F5 CF 07 E0 07 D4 07 E2 07 CE 07 D8 07 D6 07 A1 07 C9 07 D1 07 F4 07 CF 07 D7 07 E3 07 DD 07 DE 07 EA 07 A4 07 D1 07 CD 07 96 07 DD 07 E9 07 CD 07 D9 07 B1 07 DF 07 D5 07 E2 07 C1 07 C3 07 B3 07 D5 07 00 63";
            hex = hex.Replace(" ", "");

            int len = pcr.GetDataLength(hex);
            Console.WriteLine("字节数 {0}，字符串长度 {1}", len, hex.Length);

            Dictionary<int, double[]> data = pcr.ProcessLightData96(hex);

            foreach (var d in data)
            {
                Console.WriteLine(d.Key + " " + string.Join(",", d.Value));
            }

        }

        [Test]
        public void TestTempData()
        {
            string hex = "5E E3 00 0D 02 FC 61 00 00 FC 61 00 42";
            Dictionary<int, double[]> data = pcr.ProcessTempData(hex);
            foreach (var d in data)
            {
                Console.WriteLine(d.Key + "=" + string.Join(",", d.Value));
            }
        }

        [Test]
        public void TestReadHeatTemp()
        {
            List<int> tubeIndexs = new List<int>() { 1, 3, 4, 17 };
            pcr.ReadHeatTemp(tubeIndexs);
        }

        [Test]
        public void TestRowCol()
        {
            for (int i = 0; i < 18; i++)
            {
                int row = i / 6;
                int col = i % 6; // 计算列索引
                var readIndex = 6 * (row + 1) - col - 1;

                Console.Write(readIndex + " ");
                if (col == 5)
                {
                    Console.WriteLine();
                }
            }
        }

        public byte CalculateChecksum(byte[] data)
        {
            int sum = 0;

            // 计算前 n-1 个字节的和
            for (int i = 0; i < data.Length - 1; i++)
            {
                sum += data[i];
            }
            // Console.WriteLine("校验和：" + sum);

            // 返回和的低 8 位
            return (byte)(sum & 0xFF);
        }

        [Test]
        public void Test10()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;

            //计算并赋值到字节，
            int iAddress = 0xF9;
            for (int i = 0; i < 10; i++)
            {
                bytesValve[5] = (byte)((iAddress & 0x00FF0000) >> 16);
                bytesValve[6] = (byte)((iAddress & 0x0000FF00) >> 8);
                bytesValve[7] = (byte)(iAddress & 0x000000FF);
                bytesValve[8] = CalculateChecksum(bytesValve);
                try
                {
                    Console.WriteLine(StringUtils.FormatHex(StringUtils.ByteToHexString(bytesValve)));
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                iAddress += 0xF900;
            }
        }
    }
}