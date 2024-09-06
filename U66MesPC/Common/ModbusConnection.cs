using EasyModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static EasyModbus.ModbusClient;

namespace U66MesPC.Common
{
    public class ModbusConnection
    {
        private ModbusClient _master;
        private string _ip;
        private int _port;
        public bool Initialized { get; set; }
        public ModbusConnection(string IP, int Port)
        {
            _ip = IP;
            _port = Port;
        }
        public void Initialzie()
        {
            try
            {
                if (Initialized) { return; }
                _master = new ModbusClient(_ip, _port) { ConnectionTimeout = 5000 };
                _master.Connect();
                if (_master.Connected) { Initialized = true; }
            }
            catch(Exception ex)
            {
                LogsUtil.Instance.WriteError($"Modbus TCP(IP:{_ip},Port:{_port})：{ex.Message}");
                //throw new Exception($"连接PLC失败，IP:{_ip},端口号：{_port};");
            }
        }
        public string GetIP()
        {
            return _ip;
        }
        public int GetPort()
        {
            return _port;
        }
        public void CheckConnectStatus()
        {
            if (!_master.Connected)
                _master.Connect();
            if (!Initialized)
            {
                Initialzie();
                throw new Exception($"modbus tcp({_ip},{_port}) not initialized!");
            }
            if (!_master.Connected)
            {
                throw new Exception($"modbus tcp({_ip},{_port}) not connected!");
            }
        }

        /// <summary>
        ///  读取离散输入状态，即输入线圈的状态
        /// </summary>
        public bool[] ReadDiscreteCoils(int addr,int quantity)
        {
            lock(this)
            {
                CheckConnectStatus();
                return _master?.ReadDiscreteInputs(addr, quantity);
            }
        }
        /// <summary>
        /// 读取线圈状态，即输出线圈的状态
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public bool[] ReadCoils(int addr, int quantity)
        {
            lock(this)
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
        public int[] ReadHoldingRegisters(int addr, int quantity)
        {
            lock (this)
            {
                CheckConnectStatus();
                return _master?.ReadHoldingRegisters(addr, quantity);
            }
           
        }
        public int GetRegisterValAndClear(int addr,int quantity)
        {
            lock (this)
            {
                CheckConnectStatus();
                int val = _master.ReadHoldingRegisters(addr, quantity)[0];
                _master.WriteSingleRegister(addr, 0);
                return val;
            }
        }
       
        /// <summary>
        /// 读取输入寄存器的内容
        /// </summary>
        /// <param name="addr"></param>
        /// <param name="quantity"></param>
        /// <returns></returns>
        public int[] ReadInputRegisters(int addr, int quantity)
        {
            lock(this)
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
        public void WriteSingleCoil(int addr,bool value)
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
        public void WriteRegister(int addr, int value)
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
        public void WriteMultiCoils(int startAddr, bool[] values)
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
        public void WriteMultiRegisters(int startAddr, int[] values)
        {
            lock (this)
            {
                CheckConnectStatus();
                _master.WriteMultipleRegisters(startAddr, values);
            }
        }
        public static Int32 ConvertRegistersToInt(int[] registers, RegisterOrder registerOrder)
        {
            if (registers.Length != 2)
                throw new ArgumentException("Input Array length invalid - Array langth must be '2'");
            int[] swappedRegisters = { registers[0], registers[1] };
            if (registerOrder == RegisterOrder.HighLow)
                swappedRegisters = new int[] { registers[1], registers[0] };
            return ConvertRegistersToInt(swappedRegisters);
        }
        public static Int32 ConvertRegistersToInt(int[] registers)
        {
            if (registers.Length != 2)
                throw new ArgumentException("Input Array length invalid - Array langth must be '2'");
            int highRegister = registers[1];
            int lowRegister = registers[0];
            byte[] highRegisterBytes = BitConverter.GetBytes(highRegister);
            byte[] lowRegisterBytes = BitConverter.GetBytes(lowRegister);
            byte[] doubleBytes = {
                                    lowRegisterBytes[0],
                                    lowRegisterBytes[1],
                                    highRegisterBytes[0],
                                    highRegisterBytes[1]
                                };
            return BitConverter.ToInt32(doubleBytes, 0);
        }
        /// <summary>
        /// Converts 16 - Bit Register values to String
        /// </summary>
        /// <param name="registers">Register array received via Modbus</param>
        /// <param name="offset">First Register containing the String to convert</param>
        /// <param name="stringLength">number of characters in String (must be even)</param>
        /// <returns>Converted String</returns>
        public static string ConvertRegistersToString(int[] registers, int offset=0, int stringLength=0)
        {
      
            stringLength = registers.Length + 2;
            byte[] result = new byte[stringLength];
            byte[] registerResult = new byte[2];

            for (int i = 0; i < stringLength / 2; i++)
            {
                   registerResult = BitConverter.GetBytes(registers[offset + i]);
                //result[i * 2] = registerResult[0];
                //result[i * 2 + 1] = registerResult[1];
                if (registerResult[0] == 0)
                {
                    result[i * 2] = 0;
                    result[i * 2 + 1] = 0;
                    break;
                }
                else if (registerResult[1] == 0)
                {

                    result[i * 2] = registerResult[0];
                    result[i * 2 + 1] = 0;
                    result[i * 2 + 2] = 0;
                    break;
                }
                else
                {
                    result[i * 2] = registerResult[0];
                    result[i * 2 + 1] = registerResult[1];
                }
            }
            return System.Text.Encoding.Default.GetString(result);
        }
        /// <summary>
        /// Converts a String to 16 - Bit Registers
        /// </summary>
        /// <param name="registers">Register array received via Modbus</param>
        /// <returns>Converted String</returns>
        public static int[] ConvertStringToRegisters(string stringToConvert)
        {
            byte[] array =Encoding.ASCII.GetBytes(stringToConvert);
            int[] returnarray = new int[stringToConvert.Length / 2 + stringToConvert.Length % 2];
            for (int i = 0; i < returnarray.Length; i++)
            {
                returnarray[i] = array[i * 2];
                if (i * 2 + 1 < array.Length)
                {
                    returnarray[i] = returnarray[i] | ((int)array[i * 2 + 1] << 8);
                }
            }
            return returnarray;
        }
        public string GetStringFromMaster(int addr,int quantity)
        {
            var val = ReadHoldingRegisters(addr, quantity);
            string msg = ConvertRegistersToString(val).Trim(new char[] { '\0'});
            int[] vals = new int[64];
            WriteMultiRegisters(addr, vals);
            return msg;
        }
        public string GetStringFromMasterAndNotClear(int addr, int quantity)
        {
            var val = ReadHoldingRegisters(addr, quantity);
            string msg = ConvertRegistersToString(val).Trim(new char[] { '\0' });
            return msg;
        }
        public void WriteStringToMaster(string text,int addr)
        {
            int[] val=ConvertStringToRegisters(text);
            WriteMultiRegisters(addr, val);
        }
#if false
        public static void SetString(ushort[] src,int startAddr,string value)
        {
            byte[] bytesTemp = Encoding.ASCII.GetBytes(value);
            ushort[] dest = Bytes2Ushorts(bytesTemp);
            dest.CopyTo(src, startAddr);
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
                _master?.Disconnect();
                _master = null;
            }
        }
    }
}
