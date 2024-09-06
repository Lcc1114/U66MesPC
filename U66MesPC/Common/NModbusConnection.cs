
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Modbus.Device;

namespace U66MesPC.Common
{
    public class NModbusConnection
    {
        private ModbusIpMaster _master;
        private TcpClient _tcpClient;
        private string _ip;
        private int _port;
        public bool Initialized { get; private set; }
        public NModbusConnection(string IP,int Port)
        {
            _ip = IP;
            _port = Port;
        }
        public void Initialzie()
        {
            try
            {
                if (Initialized) return;
                _tcpClient = new TcpClient(_ip, _port);
                _tcpClient.ReceiveTimeout = 3000;
                _tcpClient.SendTimeout = 3000;
                _master =ModbusIpMaster.CreateIp(_tcpClient) ;
                if (_tcpClient.Connected)
                    Initialized = true;

            }catch(Exception ex)
            {
                LogsUtil.Instance.WriteError($"Modbus TCP(IP:{_ip},Port:{_port})：{ex.Message}");
                throw;
            }
        }
        public void CheckConnectStatus()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient = new TcpClient();
                _tcpClient.ReceiveTimeout = 500;
                _tcpClient.SendTimeout = 500;
                _tcpClient.Connect(_ip, _port);
                _master = ModbusIpMaster.CreateIp(_tcpClient);
                LogsUtil.Instance.WriteInfo($"modbus tcp reconnect,IP:{_ip},Port:{_port}");
            }
            if (!Initialized) throw new Exception($"modbus tcp({_ip},{_port}) not initialized!");
        }
        
        /// <summary>
        ///  读取离散输入状态，即输入线圈的状态
        /// </summary>
        public bool[] ReadDiscreteCoils(ushort addr,ushort quantity)
        {
            lock (this)
            {
                CheckConnectStatus();
                return _master?.ReadInputs(addr, quantity);
            }
        }
        /// <summary>
        /// 读取线圈状态，即输出线圈的状态
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public bool[] ReadCoils(ushort addr, ushort quantity)
        {
            lock (this)
            {
                CheckConnectStatus();
                return _master?.ReadCoils(addr, quantity);
            }
        }
        /// <summary>
        /// 读取保持寄存器的内容 
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public ushort[] ReadHoldingRegisters(ushort addr, ushort quantity)
        {
            lock (this)
            {
                CheckConnectStatus();
                return _master?.ReadHoldingRegisters(addr, quantity);
            }
        }
        /// <summary>
        /// 读取输入寄存器的内容
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public ushort[] ReadInputRegisters(ushort addr, ushort quantity)
        {
            lock (this)
            {
                CheckConnectStatus();
                return _master?.ReadInputRegisters(addr, quantity);
            }
        }
        /// <summary>
        /// 写入单个线圈的状态
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public void WriteSingleCoil(ushort addr,bool value)
        {
            lock (this)
            {
                CheckConnectStatus();
                _master.WriteSingleCoil(addr, value);
            }
        }
        /// <summary>
        /// 写单个寄存器
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="value"></param>
        public void WriteRegister(ushort addr, ushort value)
        {
            lock (this)
            {
                CheckConnectStatus();
                _master.WriteSingleRegister(addr, value);
            }
        }
        /// <summary>
        /// 写多个线圈
        /// </summary>
        /// <param name="startAddr"></param>
        /// <param name="values"></param>
        public void WriteMultiCoils(ushort startAddr, bool[] values)
        {
            lock (this)
            {
                CheckConnectStatus();
                _master.WriteMultipleCoils(startAddr, values);
            }
        }
        /// <summary>
        /// 写多个寄存器
        /// </summary>
        /// <param name="startAddr"></param>
        /// <param name="values"></param>
        public void WriteMultiRegisters(ushort startAddr, ushort[] values)
        {
            lock (this)
            {
                CheckConnectStatus();
                _master.WriteMultipleRegisters(startAddr, values);
            }
        }
#if true
        public static ushort[] SetString(ushort[] src,int startAddr,string value)
        {
            byte[] bytesTemp = Encoding.ASCII.GetBytes(value);
            ushort[] dest = Bytes2Ushorts(bytesTemp);
            //dest.CopyTo(src, startAddr);
            return dest;
        }
        public static string GetString(ushort[] src,int start,int len)
        {
            ushort[] temp = new ushort[len];
            for(int i=0;i<len;i++)
            {
                temp[i] = src[i + start];
            }
            byte[] bytesTemp = Ushorts2Bytes(temp);
            string res = Encoding.ASCII.GetString(bytesTemp).Trim(new char[] { '\0' });
            return res;
        }
        public static ushort[] Bytes2Ushorts(byte[] src,bool reverse=false)
        {
            int len = src.Length;
            byte[] srcPlus = new byte[len + 1];
            src.CopyTo(srcPlus, 0);
            int count=len >> 1;
            if(len%2!=0)
            {
                count += 1;
            }
            ushort[] dest = new ushort[count];
            if(reverse)
            {
                for(int i=0;i<count;i++)
                {
                    dest[i] = (ushort)(srcPlus[i * 2] << 8 | srcPlus[2 * i + 1] & 0xff);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    dest[i] = (ushort)(srcPlus[i * 2] & 0xff | srcPlus[2 * i + 1]<<8 );
                }
            }
            return dest;
        }
        public static byte[] Ushorts2Bytes(ushort[] src, bool reverse=false)
        {
            int count = src.Length;
            byte[] dest = new byte[count << 1];
            if (reverse)
            {
                for (int i = 0; i < count; i++)
                {
                    dest[i * 2] = (byte)(src[i] >> 8);
                    dest[i * 2+1] = (byte)(src[i] >> 0);
                }
            }
            else
            {
                for (int i = 0; i < count; i++)
                {
                    dest[i * 2] = (byte)(src[i] >> 0);
                    dest[i * 2 + 1] = (byte)(src[i] >> 8);
                }
            }
            return dest;
        }
#endif
        public void Release()
        {
            if (!Initialized) return;
            Initialized = false;
            lock (this)
            {
                _tcpClient?.Close();
                _master?.Dispose();
                _master = null;
            }
        }
    }
}
