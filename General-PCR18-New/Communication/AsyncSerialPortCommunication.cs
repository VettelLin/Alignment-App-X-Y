using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace General_PCR18.Communication
{
    public class AsyncSerialPortCommunication
    {
        private SerialPort _serialPort;
        private Queue<byte[]> _sendQueue;
        private readonly object _queueLock = new object();
        private bool _isSending = false;
        private TaskCompletionSource<bool> _responseReceivedTcs;

        public AsyncSerialPortCommunication(string portName, int baudRate)
        {
            _serialPort = new SerialPort(portName, baudRate);
            _sendQueue = new Queue<byte[]>();
        }

        public void Open()
        {
            _serialPort.Open();
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void Close()
        {
            _serialPort.Close();
        }

        public async Task SendDataAsync(byte[] data)
        {
            lock (_queueLock)
            {
                _sendQueue.Enqueue(data);
                if (!_isSending)
                {
                    _isSending = true;
                    Task.Run(() => SendNextDataAsync());
                }
            }

            // 等待当前数据的响应
            if (_responseReceivedTcs != null)
            {
                await _responseReceivedTcs.Task;
            }
        }

        private async Task SendNextDataAsync()
        {
            byte[] dataToSend = null;
            lock (_queueLock)
            {
                if (_sendQueue.Count > 0)
                {
                    dataToSend = _sendQueue.Dequeue();
                }
                else
                {
                    _isSending = false;
                    return;
                }
            }

            // 发送数据
            await _serialPort.BaseStream.WriteAsync(dataToSend, 0, dataToSend.Length);

            // 等待响应
            _responseReceivedTcs = new TaskCompletionSource<bool>();
            await _responseReceivedTcs.Task;

            // 继续发送下一条数据
            await SendNextDataAsync();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 读取响应数据
            byte[] buffer = new byte[_serialPort.BytesToRead];
            _serialPort.Read(buffer, 0, buffer.Length);

            // 处理响应数据
            ProcessResponse(buffer);

            // 通知发送线程响应已收到
            _responseReceivedTcs?.TrySetResult(true);
        }

        private void ProcessResponse(byte[] response)
        {
            // 在这里处理接收到的响应数据
            Console.WriteLine("Response received: " + BitConverter.ToString(response));
        }
    }
}
