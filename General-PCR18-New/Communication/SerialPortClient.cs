using General_PCR18.Util;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace General_PCR18.Communication
{
    public class SerialPortClient
    {
        public event SerialDataReceivedEventHandler DataReceived;
        public event SerialErrorReceivedEventHandler ErrorReceived;

        private readonly SerialPort comPort = new SerialPort();
        private string portName = "COM1";//串口号，默认COM1
        private BaudRates baudRate = BaudRates.BR_9600;//波特率
        private Parity parity = Parity.None;//校验位
        private StopBits stopBits = StopBits.One;//停止位
        private DataBits dataBits = DataBits.Eight;//数据位    
        private Handshake flowControl = Handshake.None;  // 数据流

        private readonly object LockObj = new object();

        /// <summary>
        /// 串口号
        /// </summary>
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

        /// <summary>
        /// 奇偶校验位
        /// </summary>
        public Parity Parity
        {
            get { return parity; }
            set { parity = value; }
        }

        /// <summary>
        /// 数据位
        /// </summary>
        public DataBits DataBits
        {
            get { return dataBits; }
            set { dataBits = value; }
        }

        /// <summary>
        /// 停止位
        /// </summary>
        public StopBits StopBits
        {
            get { return stopBits; }
            set { stopBits = value; }
        }

        /// <summary>
        /// 数据流
        /// </summary>
        public Handshake FlowControl
        {
            get => flowControl;
            set => flowControl = value;
        }

        #region 构造函数
        /// <summary>
        /// 无参构造函数
        /// </summary>
        public SerialPortClient()
        {
            BoundEvents();
        }

        void BoundEvents()
        {
            comPort.DataReceived += new SerialDataReceivedEventHandler(comPort_DataReceived);
            comPort.ErrorReceived += new SerialErrorReceivedEventHandler(comPort_ErrorReceived);
        }

        /// <summary>
        /// 参数构造函数（使用枚举参数构造）
        /// </summary>
        /// <param name="name">串口号</param>
        /// <param name="baud">波特率</param>
        /// <param name="par">奇偶校验位</param>
        /// <param name="dBits">数据位</param>
        /// <param name="sBits">停止位</param>
        /// <param name="fControl">数据流</param>
        public SerialPortClient(string name, BaudRates baud, Parity par, DataBits dBits, StopBits sBits, Handshake fControl)
        {
            this.portName = name;
            this.baudRate = baud;
            this.parity = par;
            this.dataBits = dBits;
            this.stopBits = sBits;
            this.FlowControl = fControl;
            BoundEvents();
        }

        /// <summary>
        /// 参数构造函数（使用字符串参数构造）
        /// </summary>
        /// <param name="name">串口号</param>
        /// <param name="baud">波特率</param>
        /// <param name="par">奇偶校验位</param>
        /// <param name="dBits">数据位</param>
        /// <param name="sBits">停止位</param>
        /// <param name="fControl">数据流</param>
        public SerialPortClient(string name, string baud, string par, string dBits, string sBits, string fControl)
        {
            this.portName = name;
            this.baudRate = (BaudRates)Enum.Parse(typeof(BaudRates), baud);
            this.parity = (Parity)Enum.Parse(typeof(Parity), par);
            this.dataBits = (DataBits)Enum.Parse(typeof(DataBits), dBits);
            this.stopBits = (StopBits)Enum.Parse(typeof(StopBits), sBits);
            this.flowControl = (Handshake)Enum.Parse(typeof(Handshake), fControl);
            BoundEvents();
        }
        #endregion
        #region 事件处理函数

        /// <summary>
        /// 数据接收处理
        /// </summary>
        void comPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (comPort.IsOpen)     //判断是否打开串口
            {
                DataReceived?.Invoke(this, e);
            }
            else
            {
                Console.WriteLine("请打开某个串口", "Error");
            }
        }
        /// <summary>
        /// 错误处理函数
        /// </summary>
        void comPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            ErrorReceived?.Invoke(this, e);
        }
        #endregion

        #region 串口关闭/打开
        /// <summary>
        /// 端口是否已经打开
        /// </summary>
        public bool IsOpen
        {
            get => comPort.IsOpen;
        }

        public int BytesToRead
        {
            get => comPort.BytesToRead;
        }

        /// <summary>
        /// 打开端口
        /// </summary>
        /// <returns></returns>
        public void Open()
        {
            try
            {
                if (comPort.IsOpen) comPort.Close();

                comPort.PortName = portName;
                comPort.BaudRate = (int)baudRate;
                comPort.Parity = parity;
                comPort.DataBits = (int)dataBits;
                comPort.StopBits = stopBits;
                comPort.Handshake = flowControl;

                comPort.Open();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        /// <summary>
        /// 关闭端口
        /// </summary>
        public void Close()
        {
            try
            {
                if (comPort.IsOpen) comPort.Close();
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        /// <summary>
        /// 丢弃来自串行驱动程序的接收和发送缓冲区的数据
        /// </summary>
        public void DiscardBuffer()
        {
            comPort.DiscardInBuffer();
            comPort.DiscardOutBuffer();
        }
        #endregion

        #region 写入数据
        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="buffer"></param>
        public void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }

                comPort.BaseStream.WriteAsync(buffer, offset, count);
            }
            catch (Exception ex) { LogHelper.Error(ex.Message); }
        }

        /// <summary>
        /// 写入数据
        /// </summary>
        /// <param name="buffer">写入端口的字节数组</param>
        public void Write(byte[] buffer)
        {
            try
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }
                comPort.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        /// <summary>
        /// 异步写数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public async Task WriteAsync(byte[] buffer, int offset, int count)
        {
            try
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }

                await comPort.BaseStream.WriteAsync(buffer, offset, count);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        /// <summary>
        /// 异步写数据
        /// </summary>
        /// <param name="buffer"></param>
        public async Task WriteAsync(byte[] buffer)
        {
            try
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }
                await comPort.BaseStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }
        #endregion

        public void Read(byte[] buffer, int offset, int count)
        {
            try
            {
                if (!comPort.IsOpen)
                {
                    comPort.Open();
                }

                comPort.Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public string ReadExisting()
        {
            lock (LockObj)
            {
                try
                {
                    if (!comPort.IsOpen) { return string.Empty; }
                    System.Threading.Thread.Sleep(100);//延缓一会，用于防止硬件发送速率跟不上缓存数据导致的缓存数据杂乱               
                    string value = comPort.ReadExisting();
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [SerialPortClient] ReadExisting: " + value);

                    return value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    return string.Empty;
                }
            }
        }

        public string ReadHexData()
        {
            lock (LockObj)
            {
                try
                {
                    if (!comPort.IsOpen) { return string.Empty; }
                    System.Threading.Thread.Sleep(100);//延缓一会，用于防止硬件发送速率跟不上缓存数据导致的缓存数据杂乱
                    int n = comPort.BytesToRead;//先记录下来，避免某种原因，人为的原因，操作几次之间时间长，缓存不一致  
                    byte[] buf = new byte[n];//声明一个临时数组存储当前来的串口数据           
                    comPort.Read(buf, 0, n);//读取缓冲数据                
                    string value = StringUtils.ByteToHexString(buf);
                    Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " [SerialPortClient] ReadHexData: " + value);

                    return value;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ReadHexData, error:{0}", ex.Message);
                    return string.Empty;
                }
            }
        }
    }

    #region 波特率、数据位的枚举
    /// <summary>
    /// 串口数据位列表（5,6,7,8）
    /// </summary>
    public enum DataBits : int
    {
        Five = 5,
        Six = 6,
        Sevent = 7,
        Eight = 8
    }

    /// <summary>
    /// 串口波特率列表。
    /// 75,110,150,300,600,1200,2400,4800,9600,14400,19200,28800,38400,56000,57600,
    /// 115200,128000,230400,256000
    /// </summary>
    public enum BaudRates : int
    {
        BR_75 = 75,
        BR_110 = 110,
        BR_150 = 150,
        BR_300 = 300,
        BR_600 = 600,
        BR_1200 = 1200,
        BR_2400 = 2400,
        BR_4800 = 4800,
        BR_9600 = 9600,
        BR_14400 = 14400,
        BR_19200 = 19200,
        BR_28800 = 28800,
        BR_38400 = 38400,
        BR_56000 = 56000,
        BR_57600 = 57600,
        BR_115200 = 115200,
        BR_128000 = 128000,
        BR_230400 = 230400,
        BR_256000 = 256000
    }
    #endregion
}
