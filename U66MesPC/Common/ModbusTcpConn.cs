using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace U66MesPC.Common
{
    public class ModbusTcpConn
    {
        private ModbusFactory _modbusFactory;
        private IModbusMaster _master;
        private TcpClient _tcpClient;
        public bool Initialized { get; private set; }

        private string _ip;
        private int _port;

        public bool Connected
        {
            get => _tcpClient.Connected;
        }

        public ModbusTcpConn(string ip, int port)
        {
            _ip = ip;
            _port = port;
            _modbusFactory = new ModbusFactory();

        }
        public void Initialzie()
        {
            try
            {
                
                _tcpClient = new TcpClient(_ip, _port);
                _master = _modbusFactory.CreateMaster(_tcpClient);
                _master.Transport.ReadTimeout = 2000;
                _master.Transport.Retries = 10;
                Initialized = true;
            }
            catch (Exception ex)
            {
                LogsUtil.Instance.WriteError($"Modbus TCP(IP:{_ip},Port:{_port})：{ex.Message}");
                throw;
            }
        }
        public void CheckConnStatus()
        {
            if(!Connected)
            {
                _tcpClient = new TcpClient(_ip, _port);
                _master = _modbusFactory.CreateMaster(_tcpClient);
                _master.Transport.ReadTimeout = 2000;
                _master.Transport.Retries = 10;
                Initialized = true;
            }
            if (!Initialized) throw new Exception($"modbus tcp({_ip},{_port}) not initialized!");
        }
        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort num)
        {
            lock(this)
            {
                CheckConnStatus();
                return _master.ReadCoils(slaveAddress, startAddress, num);
            }
        }

        public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort num)
        {
            lock (this)
            {
                CheckConnStatus();
                return _master.ReadInputs(slaveAddress, startAddress, num);
            }
        }

        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort num)
        {
            lock (this)
            {
                CheckConnStatus();
                return _master.ReadHoldingRegisters(slaveAddress, startAddress, num);
            }
        }

        public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort num)
        {
            lock (this)
            {
                CheckConnStatus();
                return _master.ReadInputRegisters(slaveAddress, startAddress, num);
            }
        }

        public void WriteSingleCoil(byte slaveAddress, ushort startAddress, bool value)
        {
            lock (this)
            {
                CheckConnStatus();
                _master.WriteSingleCoil(slaveAddress, startAddress, value);
            }
        }

        public void WriteSingleRegister(byte slaveAddress, ushort startAddress, ushort value)
        {
            lock (this)
            {
                CheckConnStatus();
                _master.WriteSingleRegister(slaveAddress, startAddress, value);
            }
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] value)
        {
            lock (this)
            {
                CheckConnStatus();
                _master.WriteMultipleCoils(slaveAddress, startAddress, value);
            }
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] value)
        {
            lock (this)
            {
                CheckConnStatus();
                _master.WriteMultipleRegisters(slaveAddress, startAddress, value);
            }
        }
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
