using General_PCR18.Common;
using General_PCR18.Util;
using NPOI.SS.Formula.Functions;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace General_PCR18.Communication
{
    public class Pcr18Client
    {
        public delegate void DataReceivedEventHandler(string hex);
        public event DataReceivedEventHandler DataReceived;

        private readonly SerialPortClient portClient = new SerialPortClient();
        private string portName = "COM1";//串口号，默认COM1
        private BaudRates baudRate = BaudRates.BR_9600;

        private readonly BlockingCollection<byte[]> cmdQueue = new BlockingCollection<byte[]>(100);
        private readonly ManualResetEventSlim _responseReceivedEvent = new ManualResetEventSlim(false);
        private readonly object _responseLock = new object();
        private readonly int waitTimeout = 3 * 60 * 1000;

        private string lastCmd = null;

        public string PortName
        {
            get { return portName; }
            set { portName = value; }
        }

        /// <summary>
        /// 波特率
        /// </summary>
        public BaudRates BaudRate
        {
            get { return baudRate; }
            set { baudRate = value; }
        }

        public bool IsOpen
        {
            get => portClient.IsOpen;
        }

        public Pcr18Client()
        {
            BoundEvents();
        }

        public Pcr18Client(string portName)
        {
            this.portName = portName;
            BoundEvents();
        }

        private void BoundEvents()
        {
            portClient.DataReceived += PortClient_DataReceived;

            // 启动发送线程            
            Task.Run(() => { SendCmdThread(); });
        }

        /// <summary>
        /// 顺序下发指令
        /// </summary>
        private void SendCmdThread()
        {
            foreach (var cmd in cmdQueue.GetConsumingEnumerable())
            {
                try
                {
                    // 重置响应状态
                    lock (_responseLock)
                    {
                        _responseReceivedEvent.Reset();
                    }

                    LogHelper.Debug("发送->：{0}", StringUtils.FormatHex(StringUtils.ByteToHexString(cmd)));

                    portClient.Write(cmd, 0, cmd.Length);

                    lastCmd = StringUtils.ByteToHexString(cmd);

                    //Thread.Sleep(100);
                    //autoResetEvent.WaitOne();  // 当前线程阻塞

                    // 等待响应或超时
                    bool responseReceived = _responseReceivedEvent.Wait(waitTimeout);

                    if (responseReceived)
                    {
                        // 成功收到响应
                        LogHelper.Debug($"收到响应");
                    }
                    else if (!responseReceived)
                    {
                        // 超时
                        LogHelper.Debug($"命令超时: {StringUtils.FormatHex(StringUtils.ByteToHexString(cmd))}");
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Error($"处理命令异常: {ex.Message}");
                }
            }
        }

        private readonly StringBuilder packData = new StringBuilder();
        private int packLen = 0;
        private readonly object _receivedLock = new object();

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PortClient_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            lock (_receivedLock)
            {
                var serialPort = (SerialPortClient)sender;
                List<byte> tempBytes = new List<byte>();
                while (serialPort.BytesToRead > 0)
                {
                    byte[] buffer = new byte[serialPort.BytesToRead];
                    serialPort.Read(buffer, 0, buffer.Length);
                    foreach (byte b in buffer)
                    {
                        tempBytes.Add(b);
                    }
                    Thread.Sleep(20);//超时时间
                }

                LogHelper.Debug("byte: {0}", tempBytes.Count);
                if (tempBytes.Count == 0)
                {
                    return;
                }

                byte[] dataBuf = tempBytes.ToArray();
                string value = StringUtils.ByteToHexString(dataBuf);

                LogHelper.Debug("收到<-：{0}", StringUtils.FormatHex(value));

                // 兼容两种上报形式：
                // 1) 纯 HEX 帧（例如：5E000EA600）
                // 2) ASCII 文本行（例如：RX ... data:
                // 0E A6 00）
                try
                {
                    // 从纯 HEX 串中直接提取 5 字节裂解温度帧
                    var shortHexMatches = System.Text.RegularExpressions.Regex.Matches(value, @"5E0[0-2][0-9A-Fa-f]{4}00");
                    foreach (System.Text.RegularExpressions.Match m in shortHexMatches)
                    {
                        try { DataReceived?.Invoke(m.Value.ToUpper()); } catch { }
                    }

                    // 从 ASCII 文本中提取 "5E 0X HH LL 00"
                    string ascii = System.Text.Encoding.ASCII.GetString(dataBuf);
                    var asciiMatches = System.Text.RegularExpressions.Regex.Matches(ascii, @"5E\s+0([0-2])\s+([0-9A-Fa-f]{2})\s+([0-9A-Fa-f]{2})\s+00");
                    foreach (System.Text.RegularExpressions.Match m in asciiMatches)
                    {
                        string row = m.Groups[1].Value.ToUpper();
                        string hi = m.Groups[2].Value.ToUpper();
                        string lo = m.Groups[3].Value.ToUpper();
                        string normalized = $"5E0{row}{hi}{lo}00";
                        try { DataReceived?.Invoke(normalized); } catch { }
                    }
                }
                catch { }

                packData.Append(value);

                // 包头
                if (packLen == 0 && value.StartsWith("5E"))
                {
                    packLen = GetDataLength(value) * 2;
                }

                // 检查长度
                if (packLen > packData.Length)
                {
                    LogHelper.Debug("拆包处理。包总长: {0}", packLen);

                    return;
                }

                string packReal = packData.ToString().Substring(0, packLen);

                DataReceived?.Invoke(packReal);

                packData.Clear();
                packLen = 0;
            }

            // 收到回复，可以下发下一条指令
            lastCmd = null;
            // autoResetEvent.Set();
            lock (_responseLock)
            {
                _responseReceivedEvent.Set();
            }
        }

        public bool Open()
        {
            try
            {
                portClient.PortName = portName;
                portClient.BaudRate = baudRate;
                portClient.Open();
                portClient.DiscardBuffer();

                return true;
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.Message);
            }

            return false;
        }

        public void Close()
        {
            try
            {
                portClient.Close();
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.Message);
            }
        }

        /// <summary>
        /// 向队列发命令
        /// </summary>
        /// <param name="byteCmd"></param>
        /// <param name="description"></param>
        private void SendData(byte[] byteCmd, string description = "")
        {
            try
            {
                if (byteCmd == null)
                {
                    LogHelper.Debug("字符指令为空！");
                    return;
                }
                if (!IsOpen)
                {
                    LogHelper.Debug("串口未连接！");
                    return;
                }

                bool res = cmdQueue.TryAdd(byteCmd);
                if (!res)
                {
                    LogHelper.Debug("命令加入队列失败：{0}", StringUtils.FormatHex(StringUtils.ByteToHexString(byteCmd)));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Debug(ex.Message);
            }
        }

        /// <summary>
        /// 命令字节（1字节） 数据长度（1字节） 数据流（若干字节，字节数等于数据长度）
        /// </summary>
        /// <param name="cmd"></param>
        public void SendData(string cmd, string description = "")
        {
            string hex = cmd.Replace(" ", "").Replace("H", "");
            byte[] data = StringUtils.HexStringToByte(hex);
            SendData(data, description);
        }

        /// <summary>
        /// 计算检验和
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
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

        #region 命令

        /// <summary>
        /// 1.读取设备型号，硬件版本，软件版本
        /// 返回：5E 91 00 16 31 30 30 31 32 32 30 34 32 35 32 33 30 37 30 33 00 36
        /// </summary>
        public void DevicePCRReadDeviceID()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x00;
            bytesValve[6] = 0x20;
            bytesValve[7] = 0x10;
            bytesValve[8] = 0xA8;

            SendData(bytesValve);
        }

        /// <summary>
        /// 2.读取仪器型号
        /// 返回：5E 91 00 1C 69 41 4D 50 2D 50 53 39 36 20 20 20 20 20 20 20 50 53 32 33 35 30 00 DE 
        /// </summary>
        public void DevicePCRReadInstrumentID()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x00;
            bytesValve[6] = 0x00;
            bytesValve[7] = 0x16;
            bytesValve[8] = 0x8E;

            SendData(bytesValve);
        }

        /// <summary>
        /// 3.读取SN
        /// 返回：5E 91 00 16 50 53 32 33 35 30 31 30 30 30 30 35 32 20 20 20 00 2A
        /// </summary>
        public void DevicePCRReadSN()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x00;
            bytesValve[6] = 0x10;
            bytesValve[7] = 0x10;
            bytesValve[8] = 0x98;

            SendData(bytesValve);
        }

        /// <summary>
        /// 4.握手 Hanshake
        /// 返回：5E 81 00 06 00 E5
        /// </summary>
        /// <param name="output"></param>
        /// <returns></returns>
        public void DevicePCRHandshake()
        {
            // 握手指令 5E 01 00 05 64
            byte[] bytesValve = new byte[5];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x01;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x05;
            bytesValve[4] = 0x64;

            SendData(bytesValve);
        }

        /// <summary>
        /// 5.读取10次地址
        /// </summary>
        public void DeviceTenTime()
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
                    SendData(bytesValve, "DeviceTenTime");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                iAddress += 0xF900;
            }
        }

        /// <summary>
        /// 6.不知道是什么
        /// 下发：5E 11 00 09 00 09 BA 46 81
        /// </summary>
        public void DevicePCRNothing()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x09;
            bytesValve[6] = 0xBA;
            bytesValve[7] = 0x46;
            bytesValve[8] = 0x81;

            SendData(bytesValve);
        }

        /// <summary>
        /// 7.读取结构化数据
        /// </summary>
        public void DeviceStructPara()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;

            //计算并赋值到字节，
            int iAddress = 0x2000F9;
            for (int i = 0; i < 3; i++)
            {
                bytesValve[5] = (byte)((iAddress & 0x00FF0000) >> 16);
                bytesValve[6] = (byte)((iAddress & 0x0000FF00) >> 8);
                if (i == 2)
                {
                    bytesValve[7] = 0x0E;
                }
                else
                {
                    bytesValve[7] = (byte)(iAddress & 0x000000FF);
                }
                bytesValve[8] = CalculateChecksum(bytesValve);
                try
                {
                    SendData(bytesValve);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                iAddress += 0xF900;
            }
        }

        /// <summary>
        /// 8.读取试管开关状态
        ///  5E 68 00 05 CB
        /// </summary>
        public void ReadHeatKeyStatus()
        {
            byte[] bytesValve = new byte[5];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x68;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x05;
            bytesValve[4] = CalculateChecksum(bytesValve);

            SendData(bytesValve);
        }

        /// <summary>
        /// 读取热盖温度
        /// </summary>
        public void DeviceHeatHatTemp()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x20;
            bytesValve[6] = 0x27;
            bytesValve[7] = 0x01;
            bytesValve[8] = 0xC0;

            SendData(bytesValve);
        }

        /// <summary>
        /// 读取实际使用热盖温度
        /// </summary>
        public void DeviceCurHeatHatTemp()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x05;
            bytesValve[6] = 0x50;
            bytesValve[7] = 0x02;
            bytesValve[8] = 0xCF;

            SendData(bytesValve);
        }

        /// <summary>
        /// 写入E2数据
        /// </summary>
        public void DeviceWriteE2Data()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x12;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x20;
            bytesValve[6] = 0x27;
            bytesValve[7] = 0x5F;
            bytesValve[8] = 0x1F;

            SendData(bytesValve);
        }

        /// <summary>
        /// 读取所有温度
        /// </summary>
        public void DeviceReadAllTemp()
        {
            byte[] bytesValve = new byte[5];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x43;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x05;
            bytesValve[4] = 0xA6;

            SendData(bytesValve);
        }

        /// <summary>
        /// 荧光值自检（5次）
        /// </summary>
        public void DevicePCRSelfCheck()
        {
            byte[] bytesValve = new byte[9];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x11;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x09;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x13;

            //计算并赋值到字节，
            int iAddress = 0x00;
            for (int i = 0; i < 5; i++)
            {
                bytesValve[6] = (byte)iAddress;
                bytesValve[7] = 0x02;
                bytesValve[8] = CalculateChecksum(bytesValve);
                try
                {
                    SendData(bytesValve);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                iAddress += 0x20;
            }
        }

        /// <summary>
        /// 增益设置
        /// </summary>
        public void DeviceSetGain()
        {
            byte[] bytesValve = new byte[7];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x0D;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x07;
            bytesValve[4] = 0x0D;
            bytesValve[5] = 0xAC;
            bytesValve[6] = 0x2B;

            SendData(bytesValve);
        }

        /// <summary>
        /// 设置加热温度
        /// </summary>
        /// <param name="inTubeID"></param>
        /// <param name="inHeatTempH1">真实温度x10后的值</param>
        /// <param name="inHeatTempH3">真实温度x10后的值</param>
        public void SetHeatTemp(byte inTubeID, int inHeatTempH1, int inHeatTempH3)
        {
            byte[] bytesValve = new byte[12];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x65;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x0C;
            bytesValve[4] = inTubeID;
            bytesValve[5] = (byte)((inHeatTempH1 & 0xFF00) >> 8);
            bytesValve[6] = (byte)(inHeatTempH1 & 0xFF);
            bytesValve[7] = 0x00;
            bytesValve[8] = 0x50;
            bytesValve[9] = (byte)((inHeatTempH3 & 0xFF00) >> 8);
            bytesValve[10] = (byte)(inHeatTempH3 & 0xFF);
            bytesValve[11] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "SetHeatTemp");
        }

        /// <summary>
        /// 设置加热时间
        /// </summary>
        public void SetHeatTime(byte inTubeID, int inHeatTimeH1, int inHeatTimeH3)
        {
            byte[] bytesValve = new byte[12];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x67;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x0C;
            bytesValve[4] = inTubeID;
            bytesValve[5] = (byte)((inHeatTimeH1 & 0xFF00) >> 8);
            bytesValve[6] = (byte)(inHeatTimeH1 & 0xFF);
            bytesValve[7] = 0x00;
            bytesValve[8] = 0x07;
            bytesValve[9] = (byte)((inHeatTimeH3 & 0xFF00) >> 8);
            bytesValve[10] = (byte)(inHeatTimeH3 & 0xFF);
            bytesValve[11] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "SetHeatTime");
        }

        /// <summary>
        /// 开始加热   
        /// </summary>
        public void StartHeat(byte inTubeID)
        {
            byte[] bytesValve = new byte[6];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x61;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x06;
            bytesValve[4] = inTubeID;
            bytesValve[5] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "StartHeat");
        }

        /// <summary>
        /// 读取加热时间
        /// </summary>
        public void ReadHeatTime(byte inTubeID)
        {
            byte[] bytesValve = new byte[6];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x66;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x06;
            bytesValve[4] = inTubeID;
            bytesValve[5] = CalculateChecksum(bytesValve);

            SendData(bytesValve);
        }

        /// <summary>
        /// 停止加热
        /// </summary>
        /// <param name="inTubeID"></param>
        public void StopHeat(byte inTubeID)
        {
            byte[] bytesValve = new byte[6];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x62;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x06;
            bytesValve[4] = inTubeID;
            bytesValve[5] = CalculateChecksum(bytesValve);

            SendData(bytesValve);
        }


        /// <summary>
        /// 获取环境温度, 每分钟调用一次. 5E 05 00 06 00 69
        /// 返回：5E 85 00 09 00 0D 03 01 FD
        /// 5E 85 00 09 [Flag_STATUS] [TempHi] [TempLo] [Valid] [Chk]
        /// </summary>
        public void EnvTemp()
        {
            byte[] bytesValve = new byte[6];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x05;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x06;
            bytesValve[4] = 0x00;
            bytesValve[5] = 0x69;

            SendData(bytesValve);
        }

        /// <summary>
        /// 获取热盖温度, 每分钟调用一次. 5E 09 00 06 04 71
        /// 返回：5E 89 00 09 1C 40 00 00 4C
        /// 5E 89 00 09 [Flag_STATUS] [TempHi] [TempLo] [Valid] [Chk]
        /// </summary>
        public void HotCoverTemp()
        {
            byte[] bytesValve = new byte[6];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x09;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x06;
            bytesValve[4] = 0x04;
            bytesValve[5] = 0x71;

            SendData(bytesValve);
        }

        /// <summary>
        /// LED设置
        /// </summary>
        public void DeviceSetLED()
        {
            byte[] bytesValve = new byte[7];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x08;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x07;
            bytesValve[4] = 0x02;
            bytesValve[5] = 0xFF;
            bytesValve[6] = 0x6E;

            SendData(bytesValve);
        }

        /// <summary>
        /// 读取温度
        /// <param name="tubeIndexs">需要读取温度的试管序号</param>
        /// </summary>
        public void ReadHeatTemp(List<int> tubeIndexs)
        {
            try
            {
                // 计算长度 头3byte 长度1byte 数据Nbyte 校验1byte
                int lenght = 3 + 1 + tubeIndexs.Count + 1;
                byte bLen = StringUtils.IntToOneByte(lenght);

                byte[] bytesValve = new byte[lenght];
                bytesValve[0] = 0x5E;
                bytesValve[1] = 0x63;
                bytesValve[2] = 0x00;
                bytesValve[3] = bLen;
                for (int i = 0; i < tubeIndexs.Count; i++)
                {
                    byte b = StringUtils.IntToOneByte(tubeIndexs[i]);
                    bytesValve[4 + i] = b;
                }
                bytesValve[4 + tubeIndexs.Count] = CalculateChecksum(bytesValve);

                SendData(bytesValve, "ReadHeatTemp");
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 21 F2 0E 99
        /// </summary>
        public void ReadEEPROM1()
        {
            SendData("5E 11 00 09 00 21 F2 0E 99", "ReadEEPROM1");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 20 27 01 C0
        /// </summary>
        public void ReadEEPROM2()
        {
            SendData("5E 11 00 09 00 20 27 01 C0", "ReadEEPROM2");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 05 50 02 CF
        /// </summary>
        public void ReadEEPROM3()
        {
            SendData("5E 11 00 09 00 05 50 02 CF", "ReadEEPROM3");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 12 00 09 00 20 27 5A 1A
        /// </summary>
        public void ReadEEPROM4()
        {
            SendData("5E 12 00 09 00 20 27 5A 1A", "ReadEEPROM4");
        }

        /// <summary>
        /// 在 EEPROM 4 和 5 之间
        /// 5E 43 00 05 A6
        /// </summary>
        public void ReadTemp()
        {
            SendData("5E 43 00 05 A6", "ReadTemp");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 13 00 02 8D
        /// </summary>
        public void ReadEEPROM5()
        {
            SendData("5E 11 00 09 00 13 00 02 8D", "ReadEEPROM5");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 13 20 02 AD
        /// </summary>
        public void ReadEEPROM6()
        {
            SendData("5E 11 00 09 00 13 20 02 AD", "ReadEEPROM6");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 13 40 02 CD
        /// </summary>
        public void ReadEEPROM7()
        {
            SendData("5E 11 00 09 00 13 40 02 CD", "ReadEEPROM7");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 13 60 02 ED
        /// </summary>
        public void ReadEEPROM8()
        {
            SendData("5E 11 00 09 00 13 60 02 ED", "ReadEEPROM8");
        }

        /// <summary>
        /// 读取EEPROM
        /// 5E 11 00 09 00 13 80 02 0D
        /// </summary>
        public void ReadEEPROM9()
        {
            SendData("5E 11 00 09 00 13 80 02 0D", "ReadEEPROM9");
        }

        /// <summary>
        /// 电机归位
        /// </summary>
        public void MoveMotorHome()
        {
            SendData("5E 07 00 06 23 8E", "MoveMotorHome"); // 发送 Y 轴归位命令
            Thread.Sleep(100);
            SendData("5E 07 00 06 13 7E", "MoveMotorHome"); // 发送 X 轴归位命令
        }

        /// <summary>
        /// 电机归位
        /// </summary>
        public void MoveMotorYHome()
        {
            SendData("5E 07 00 06 23 8E", "MoveMotorYHome"); // 发送 Y 轴归位命令
        }

        /// <summary>
        /// 电机归位X
        /// </summary>
        public void MoveMotorXHome()
        {
            SendData("5E 07 00 06 13 7E", "MoveMotorXHome"); // 发送 X 轴归位命令
        }

        /// <summary>
        /// 电机控制 5E 07 00 0A 11 27 10 23 28 02
        /// </summary>
        public void MotorControl1()
        {
            SendData("5E 07 00 0A 11 27 10 23 28 02");
        }

        /// <summary>
        /// 电机控制 5E 07 00 0A 10 27 10 23 28 01
        /// </summary>
        public void MotorControl2()
        {
            SendData("5E 07 00 0A 10 27 10 23 28 01");
        }

        /// <summary>
        /// 电机控制 5E 07 00 0B 15 27 10 00 23 28 07
        /// </summary>
        public void MotorControl3()
        {
            SendData("5E 07 00 0B 15 27 10 00 23 28 07");
        }

        /// <summary>
        /// 电机控制 5E 07 00 0A 24 00 00 3E 80 51
        /// </summary>
        public void MotorControl4()
        {
            SendData("5E 07 00 0A 24 00 00 3E 80 51");
        }

        /// <summary>
        /// 电机控制 5E 07 00 0A 14 00 00 23 28 CE
        /// </summary>
        public void MotorControl5()
        {
            SendData("5E 07 00 0A 14 00 00 23 28 CE");
        }

        /// <summary>
        /// 电机预设，读取光数据前调用
        /// </summary>
        /// <returns></returns>
        public void MotorPreCommand()
        {
            byte[] preCommand = new byte[7] { 0x5E, 0x08, 0x00, 0x07, 0x02, 0xFF, 0x6E };

            SendData(preCommand, "MotorPreCommand");
            Thread.Sleep(100);
        }

        /// <summary>
        /// 电机预设命令，移动前需要发送
        /// </summary>
        /// <returns></returns>
        public void MotorPreScanCommands()
        {
            byte[] command1 = new byte[7] { 0x5E, 0x0D, 0x00, 0x07, 0x0D, 0xAC, 0x2B };
            byte[] command2 = new byte[7] { 0x5E, 0x08, 0x00, 0x07, 0x02, 0xFF, 0x6E };

            SendData(command1, "MotorPreScanCommands");
            Thread.Sleep(100);
            SendData(command2, "MotorPreScanCommands");
            Thread.Sleep(100);
        }

        /// <summary>
        /// 电机控制命令-Y轴
        /// </summary>
        /// <param name="numRows">移动行数</param>
        public void MotorControlY(double numRows)
        {
            //MotorPreScanCommands(); // 先发送预设命令

            byte[] bytesValve = new byte[10];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x07;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x0A;
            bytesValve[4] = 0x24;

            // 计算 Y 轴步数
            int iYAxis = (int)(numRows * 0x171D);
            bytesValve[5] = (byte)((iYAxis >> 8) & 0xFF);
            bytesValve[6] = (byte)(iYAxis & 0xFF);
            bytesValve[7] = 0x3E;
            bytesValve[8] = 0x80;
            bytesValve[9] = CalculateChecksum(bytesValve);

            Console.WriteLine($"[DEBUG] Y 轴移动: {numRows} 行, 计算步数: {iYAxis}");

            SendData(bytesValve, "MotorControlY");
            Thread.Sleep(100);
        }

        /// <summary>
        /// 电机控制命令-Y轴移动4Byte
        /// </summary>
        /// <param name="numRows">格数</param>
        public void MotorControlYAbsolute4Byte(double numRows)
        {
            byte[] bytesValve = new byte[12];
            bytesValve[0] = 0x5E;
            bytesValve[1] = 0x07;
            bytesValve[2] = 0x00;
            bytesValve[3] = 0x0C;
            bytesValve[4] = 0x28;

            // 4-byte Pulse value (big-endian)
            // 若是“按格数/步长”移动：脉冲 = 每格脉冲数 × 格数。
            // 默认 Y 每格脉冲 wYMotorScanPulse = 5905
            // X 每行扫描脉冲 wXMotorScanPulse = 9500（实际以设备EEPROM为准）
            int pulse = (int)(numRows * 5905);
            bytesValve[5] = (byte)((pulse >> 24) & 0xFF);
            bytesValve[6] = (byte)((pulse >> 16) & 0xFF);
            bytesValve[7] = (byte)((pulse >> 8) & 0xFF);
            bytesValve[8] = (byte)(pulse & 0xFF);

            // 2-byte Speed value (big-endian)
            // 默认 X=11000，Y=16000（运行时可能被EEPROM覆盖）。
            int speed = 16000;
            bytesValve[9] = (byte)((speed >> 8) & 0xFF);
            bytesValve[10] = (byte)(speed & 0xFF);

            // Calculate checksum
            bytesValve[11] = CalculateChecksum(bytesValve);

            Console.WriteLine($"[DEBUG] Y 轴移动: {numRows} , 速度: {speed}");

            SendData(bytesValve, "MotorControlYAbsolute4Byte");
            Thread.Sleep(100);
        }

        /// <summary>
        /// 电机控制命令-正转, X 轴正向扫描指令
        /// </summary>
        public void MotorControlForeward()
        {
            //MotorPreScanCommands(); // 先发送预设命令

            byte[] bytesValve = new byte[11] { 0x5E, 0x07, 0x00, 0x0B, 0x15, 0x27, 0x10, 0x01, 0x23, 0x28, 0x00 };
            bytesValve[10] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "MotorControlForeward");
        }

        /// <summary>
        /// 电机控制命令-反转, X 轴反向扫描指令
        /// </summary>
        public void MotorControlReversal()
        {
            //MotorPreScanCommands(); // 先发送预设命令

            byte[] bytesValve = new byte[11] { 0x5E, 0x07, 0x00, 0x0B, 0x15, 0x27, 0x10, 0x00, 0x23, 0x28, 0x00 };
            bytesValve[10] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "MotorControlReversal");
        }

        /// <summary>
        /// 读取FAM荧光数据
        /// </summary>
        public void ReadFAMData()
        {
            //MotorPreCommand(); // 先发送预设命令

            byte[] bytesValve = new byte[] { 0x5E, 0x0C, 0x00, 0x07, 0x01, 0x01, 0x00 };
            bytesValve[6] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "ReadPCRFAMData");
        }

        /// <summary>
        /// 读取Cy5荧光数据
        /// </summary>
        public void ReadCy5Data()
        {
            //MotorPreCommand(); // 先发送预设命令

            byte[] bytesValve = new byte[] { 0x5E, 0x0C, 0x00, 0x07, 0x02, 0x01, 0x00 };
            bytesValve[6] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "ReadPCRCy5Data");
        }

        /// <summary>
        /// 读取VIC荧光数据
        /// </summary>
        public void ReadVICData()
        {
            //MotorPreCommand(); // 先发送预设命令

            byte[] bytesValve = new byte[] { 0x5E, 0x0C, 0x00, 0x07, 0x03, 0x01, 0x00 };
            bytesValve[6] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "ReadPCRVICData");
        }

        /// <summary>
        /// 读取Cy5.5荧光数据
        /// </summary>
        public void ReadCy55Data()
        {
            //MotorPreCommand(); // 先发送预设命令

            byte[] bytesValve = new byte[] { 0x5E, 0x0C, 0x00, 0x07, 0x04, 0x01, 0x00 };
            bytesValve[6] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "ReadCy55Data");
        }

        /// <summary>
        /// 读取ROX荧光数据
        /// </summary>
        public void ReadRoxData()
        {
            //MotorPreCommand(); // 先发送预设命令

            byte[] bytesValve = new byte[] { 0x5E, 0x0C, 0x00, 0x07, 0x05, 0x01, 0x00 };
            bytesValve[6] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "ReadRoxData");
        }

        /// <summary>
        /// 读取6荧光数据
        /// </summary>
        public void ReadFittingData()
        {
            //MotorPreCommand(); // 先发送预设命令

            byte[] bytesValve = new byte[] { 0x5E, 0x0C, 0x00, 0x07, 0x06, 0x01, 0x00 };
            bytesValve[6] = CalculateChecksum(bytesValve);

            SendData(bytesValve, "ReadFittingData");
        }

        #endregion

        // ========================================================= 

        #region 数据处理

        /// <summary>
        /// 获取长度
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public int GetDataLength(string hex)
        {
            hex = hex.Replace(" ", "");
            int len = StringUtils.HexStringToInt(hex.Substring(6, 2));  // 整个 hex 的长度

            return len;
        }

        /// <summary>
        /// 获取数据区域，除去头，状态，校验
        /// </summary>
        /// <param name="hex"></param>
        /// <param name="status">返回状态</param>
        /// <returns></returns>
        private string GetDataPart(string hex, out int status)
        {
            hex = hex.Replace(" ", "");
            int len = StringUtils.HexStringToInt(hex.Substring(6, 2));  // 整个 hex 的长度
            if (hex.Length < len * 2)
            {
                hex += new string('0', len * 2 - hex.Length);
            }
            string dataHex = hex.Substring(8, (len - 4 - 2) * 2);
            string statusHex = hex.Substring((len - 2) * 2, 2);
            status = StringUtils.HexStringToInt(statusHex);

            return dataHex;
        }

        /// <summary>
        /// 试管插入开关处理
        /// </summary>
        /// <param name="hex"></param>
        public Dictionary<int, bool> ProcessKeyStatus(string hex)
        {
            // 5E E8 00 18 01 02 04 06 08 0A 0C 0E 10 12 14 16 18 1A 1C 1E 20 22 00 6B
            string data = GetDataPart(hex, out int status);

            Dictionary<int, bool> keys = new Dictionary<int, bool>();
            for (int i = 0; i < data.Length / 2; i++)
            {
                string d = data.Substring(i * 2, 2);

                byte[] bytes = StringUtils.HexStringToByte(d);
                int iKeyNumber = (bytes[0] & 0xFE) >> 1;
                if (iKeyNumber > 17)
                {
                    continue;
                }
                bool bKeyStatus = Convert.ToBoolean(bytes[0] & 0x01);

                keys[iKeyNumber] = bKeyStatus;
            }

            return keys;
        }

        private readonly Dictionary<byte, int> lightCorrection = new Dictionary<byte, int>()
        {
            {1, 1 },
            {2, 1 },
            {3, 582 },
            {4, 1 },
            {5, 137 },
            {6, 1 },
        };

        /// <summary>
        /// 处理光数据
        /// 一个报文返回18个管的一种光数据
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>返回dict类型，Key为试管编号，Value为光数据: FAM Cy5 VIC Cy5.5 ROX</returns>
        public Dictionary<int, double[]> ProcessLightData(string hex)
        {
            Dictionary<int, double[]> monitorData = new Dictionary<int, double[]>();
            string data = GetDataPart(hex, out int status);

            // 光类型，整个包第5字节: 通道号1-6
            byte lightType = StringUtils.HexStringToOneByte(data.Substring(0, 2));

            // 该获取的该通道的第几帧数据，实际应用已经固定了，数值为1。这里返回所有的数据。
            byte frameN = StringUtils.HexStringToOneByte(data.Substring(2, 2));

            // 试管序号
            int tubeIndex = 0;
            data = data.Substring(4);
            for (int i = 0; i < data.Length / 4; i++)
            {
                if (tubeIndex < 18)
                {
                    // 两个字节，颠倒，组成荧光数据
                    byte high = StringUtils.HexStringToOneByte(data.Substring(i * 4, 2));
                    byte low = StringUtils.HexStringToOneByte(data.Substring(i * 4 + 2, 2));
                    int iValue = low << 8 | high;  // 可能需要减一个特定的数字： FAM - 1, Cy5 - 1, VIC - 582, Cy55 - 1, ROX - 137
                    if (lightType >= 1 && lightType <= 6)
                    {
                        double[] lightData = new double[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
                        lightData[lightType - 1] = iValue - lightCorrection[lightType];

                        monitorData[tubeIndex] = lightData;
                    }
                }
                else
                {
                    break;
                }

                tubeIndex++;
            }

            return monitorData;
        }

        /// <summary>
        /// 处理光数据 96 格式
        /// 一个报文返回18个管的一种光数据
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>返回dict类型，Key为试管编号，Value为光数据: FAM Cy5 VIC Cy5.5 ROX</returns>
        public Dictionary<int, double[]> ProcessLightData96(string hex)
        {
            Dictionary<int, double[]> monitorData = new Dictionary<int, double[]>();
            string data = GetDataPart(hex, out int status);

            // 光类型, 通道号1-6
            byte lightType = 1;
            if (GlobalData.LightQueue.Count > 0)
            {
                lightType = GlobalData.LightQueue.Dequeue();
            }

            // 试管序号, 对应从右到左，
            // 0102=5， 0304=4， 0506=3，0708=2，0910=1，1112=0
            // 2122=6， 1920=7， 18196=8，1617=9，1415=10，1314=11
            // 3536=12， 3334=13， 3132=14，2930=15，2738=16，2526=17
            int tubeIndex = 0;
            for (int i = 0; i < data.Length / 4; i++)
            {
                if (tubeIndex < 18)
                {
                    int row = i / 6;
                    int col = i % 6; // 计算列索引
                    var readIndex = 6 * (row + 1) - col - 1;

                    // 两个字节，颠倒，组成荧光数据
                    byte high = StringUtils.HexStringToOneByte(data.Substring(i * 4, 2));
                    byte low = StringUtils.HexStringToOneByte(data.Substring(i * 4 + 2, 2));
                    int iValue = low << 8 | high;  // 可能需要减一个特定的数字： FAM - 1, Cy5 - 1, VIC - 582, Cy55 - 1, ROX - 137
                    if (lightType >= 1 && lightType <= 6)
                    {
                        double[] lightData = new double[] { double.NaN, double.NaN, double.NaN, double.NaN, double.NaN, double.NaN };
                        lightData[lightType - 1] = iValue - lightCorrection[lightType];

                        monitorData[readIndex] = lightData;
                    }
                }
                else
                {
                    break;
                }

                tubeIndex++;
            }

            return monitorData;
        }

        /// <summary>
        /// 处理温度
        /// </summary>
        /// <param name="hex"></param>
        /// <returns>返回dict类型，Key为试管编号，Value为数组 H1 H2 H3</returns>
        public Dictionary<int, double[]> ProcessTempData(string hex)
        {
            Dictionary<int, double[]> tempData = new Dictionary<int, double[]>();
            string data = GetDataPart(hex, out int status);

            for (int i = 0; i < data.Length / 14; i++)
            {
                // 0004B0028003E8 0204B0028003E8 0C04B0028003E8 1004B0028003E8 0504B0028003E8
                // 管号 H1高 H1低 H2高 H2低 H3高 H3低
                // 温度除以 10

                // 试管序号
                int tubeIndex = StringUtils.HexStringToOneByte(data.Substring(i * 14, 2));
                if (tubeIndex > 17)
                {
                    continue;
                }

                // 温度按 0.01°C 解析（÷100）—原始正确模式
                int h1 = StringUtils.HexStringToInt(data.Substring(i * 14 + 2, 4));
                double h1Value = h1 / 100.0;

                int h2 = StringUtils.HexStringToInt(data.Substring(i * 14 + 6, 4));
                double h2Value = h2 / 100.0;

                int h3 = StringUtils.HexStringToInt(data.Substring(i * 14 + 10, 4));
                double h3Value = h3 / 100.0;

                tempData[tubeIndex] = new double[] { h1Value, h2Value, h3Value };
            }

            return tempData;
        }

        #endregion

    }
}
