using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using U66MesPC.Common.Exceptions;
using U66MesPC.Dal.Tool;
using U66MesPC.Model;
using Timer = System.Timers.Timer;

namespace U66MesPC.Common.Station
{
    public enum StationType
    {
        None,
        CapInput,
        PCBALoad,
        PCBAUnload,
        EFlexLoad,
        EFlexUnload,
        TrimInput,
        FlowCheck,
        PSA1Load,
        PSA2Load,
        PSAPress,
        ChangeBoat4,
        PSA2Unload,
        Binning,
        SortFlow
    }
    class GlobalPLCAddress
    {
        public static int StatusAddr = 10936;
        public static int AlarmAddr = 11000;  //报警的起始地址
        public static int HeartbeatAddr = 19;
        public static int HeartbeatResponseAddr = 1019;

    }

    public class StationBase : ViewModelBase
    {
        public SysConfigs Configs { get; set; }
        public ModbusConnection Master { get; set; }
        public ModbusConnection Master2 { get; set; }
        public NModbusConnection NMaster { get; set; }
        public ModbusTcpConn MasterTcp { get; set; }
        public Thread ThreadCheck1 { get; set; }
        public Thread ThreadCheck2 { get; set; }
        public bool bRunning { get; set; }
        private object syncObject1;
        private object syncObject2;
        private static readonly object syncObject3 = new object();
        public Stopwatch swHeartbeat; //机台状态连接计时
        public Stopwatch swPLCbeat; //PLC自动连接计时
        public static bool bSimulation = Convert.ToInt32(ConfigurationManager.AppSettings["RealResult"]) == 0;
        public bool bPLCHeartbeat { get; set; }
        public bool bMESHeartbeat { get; set; }

        private Dictionary<int, int> dicAlarm1 = new Dictionary<int, int>();
        private Dictionary<int, int> dicAlarm2 = new Dictionary<int, int>();
        public Dictionary<string, int> dicColor = new Dictionary<string, int>();
        public List<string> PressHreadls = new List<string>();
        public List<string> PSA2Unloadls = new List<string>();
        public StationBase(SysConfigs sysConfigs)
        {
            Configs = sysConfigs;
            Line = Configs?.Line;
            StationID = Configs?.StationID;
            MachineID = Configs?.MachineID;

            bRunning = true;
            syncObject1 = new object();
            syncObject2 = new object();

            InitModbusTcp();
            bPLCHeartbeat = Convert.ToInt32(ConfigurationManager.AppSettings["PLCHeartbeat"]) == 1;
            bMESHeartbeat = Convert.ToInt32(ConfigurationManager.AppSettings["MESHeartbeat"]) == 1;
            swHeartbeat = Stopwatch.StartNew();
            swPLCbeat = Stopwatch.StartNew();
            InitDicAlarm();
            InitDicColor();

        }
        public void PressHreadReadToolingSN()
        {
            #region
            //每次打开软件ToolingSN检
            string fileCode = "_" + ConfigurationManager.AppSettings["LineID"] + "_" + ConfigurationManager.AppSettings["Tooling"];
            string dirPath = Directory.GetCurrentDirectory() + $"\\PressHreadToolingSN";
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            string path = dirPath + $"\\PressHreadToolingSN{fileCode}.txt";
            if (File.Exists(path))
            {
                foreach (string item in File.ReadAllLines(path))
                {
                    if (!string.IsNullOrEmpty(item))
                        PressHreadls.Add(item);
                }
            }
            else
            {
                File.Create(path).Close();
                throw new ArgumentErrorException($"请检查程序目录下是否存在PressHreadToolingSN文件", "初始化程序");
            }
            if (PressHreadls.Count < 20)
            {
                throw new ArgumentErrorException($"请检查程序目录下是否存在PressHreadToolingSN文件配置的SN个数是否正确", "初始化程序");
            }
            #endregion
        }
        public void PSA2UnloadReadToolingSN()
        {
            #region
            //每次打开软件ToolingSN检
            string fileCode = "_" + ConfigurationManager.AppSettings["LineID"] + "_" + ConfigurationManager.AppSettings["Tooling"];
            string dirPath = Directory.GetCurrentDirectory() + $"\\PSA2UnloadToolingSN";
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);
            string path = dirPath + $"\\PSA2UnloadToolingSN{fileCode}.txt";
            if (File.Exists(path))
            {
                foreach (string item in File.ReadAllLines(path))
                {
                    if (!string.IsNullOrEmpty(item))
                        PSA2Unloadls.Add(item);
                }
            }
            else
            {
                File.Create(path).Close();
                throw new ArgumentErrorException($"请检查程序目录下是否存在PSA2UnloadToolingSN文件", "初始化程序");
            }
            if (PSA2Unloadls.Count < 5)
            {
                throw new ArgumentErrorException($"请检查程序目录下是否存在PSA2UnloadToolingSN文件配置的SN个数是否正确", "初始化程序");
            }
            #endregion
        }
        #region properties
        private string _line;
        public string Line
        {
            get
            {
                return _line;
            }
            set
            {
                _line = value;
                OnPropertyChanged();
            }
        }
        private string _stationID;
        public string StationID
        {
            get
            {
                return _stationID;
            }
            set
            {
                _stationID = value;
                OnPropertyChanged();
            }
        }
        private string _machineID;
        public string MachineID
        {
            get
            {
                return _machineID;
            }
            set
            {
                _machineID = value;
                OnPropertyChanged();
            }
        }
        private string _sn;
        public string SN
        {
            get
            {
                return _sn;
            }
            set
            {
                _sn = value;
                OnPropertyChanged();
            }
        }
        private string _carrierID;
        public string CarrierID
        {
            get
            {
                return _carrierID;
            }
            set
            {
                _carrierID = value;
                OnPropertyChanged();
            }
        }
        private bool _connected;
        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                if (_connected == value) return;
                _connected = value;
                OnPropertyChanged();
            }
        }
        private string _stationType;
        public string StationType
        {
            get
            {
                return _stationType;
            }
            set
            {
                _stationType = value;
                OnPropertyChanged();
            }
        }
        #endregion
        public List<string> AlarmCodeList = new List<string>();

        /// <summary>
        /// 初始化dicAlarm键值对
        /// </summary>
        public void InitDicAlarm()
        {
            int num1 = GetAlarmAddrNum(1) * 2;
            for (int i = 0; i < num1; i++)
            {
                if (!dicAlarm1.ContainsKey(i + 1))
                {
                    dicAlarm1.Add(i + 1, 0);
                }
            }

            int num2 = GetAlarmAddrNum(2) * 2;
            for (int i = 0; i < num2; i++)
            {
                if (!dicAlarm2.ContainsKey(i + 1))
                {
                    dicAlarm2.Add(i + 1, 0);
                }
            }
        }

        private void InitDicColor()
        {
            string str = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            for (int i = 0; i < str.Length; i++)
            {
                char a = str.ElementAt(i);
                dicColor.Add(a.ToString(), i + 1);
            }
        }
        public void InitModbusTcp()
        {
            try
            {
                string[] ip = Configs?.PlcIP.Split(';');
                string[] port = Configs?.PlcPort.Split(';');
                string[] alarmCode = Configs.AlarmCode.Split(';');
                for (int i = 0; i < ip.Length; i++)
                {
                    if (i == 0)
                        Master = new ModbusConnection(ip[i], Convert.ToInt32(port[i]));
                    else if (i == 1)
                        Master2 = new ModbusConnection(ip[i], Convert.ToInt32(port[i]));
                }
                for (int i = 0; i < alarmCode.Length; i++)
                {
                    AlarmCodeList.Add(alarmCode[i]);
                }
            }
            catch (Exception ex)
            {
                HttpClientHelper.AddErrorLogInfo(Configs.StationID, "连接PLC", null, $"连接PLC失败:{ex.Message}");
            }
        }
        public int HeartbeatError1 { get; set; }
        public int HeartbeatError2 { get; set; }
        public void CheckHeartbeat()
        {
            try
            {
                if (Master.Initialized)
                {
                    int[] val = Master.ReadHoldingRegisters(GlobalPLCAddress.HeartbeatAddr, 1);
                    //AddReadLogInfo(Configs.StationID, "读取心跳", $" 心跳：{val[0]}，{Master.GetIP()}");
                    if (val[0] == 1)
                    {
                        Master.WriteRegister(GlobalPLCAddress.HeartbeatResponseAddr, 1);
                        Master.WriteRegister(GlobalPLCAddress.HeartbeatAddr, 0);
                    }
                    else
                    {
                        HeartbeatError1++;
                        if (HeartbeatError1 == 5)
                        {
                            HeartbeatError1 = 0;
                            Master.Initialized = false;
                            Master.Initialzie();
                            if (Master.Initialized)
                            {
                                Master.WriteRegister(GlobalPLCAddress.HeartbeatResponseAddr, 1);
                                //Master.WriteRegister(GlobalPLCAddress.HeartbeatAddr, 1);
                                ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.信息, $"与PLC({Master.GetIP()})通讯异常,自动重连成功");
                            }
                            else
                            {
                                ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.错误, $"与PLC({Master.GetIP()})通讯异常,自动重连中......");
                            }
                        }
                    }
                }
                else
                {
                    Master.Initialzie();
                    if (Master.Initialized)
                    {
                        ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.信息, $"0与PLC({Master.GetIP()})通讯异常,自动重连成功");
                        Master.WriteRegister(GlobalPLCAddress.HeartbeatResponseAddr, 1);
                    }
                    else
                        ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.错误, $"0与PLC({Master.GetIP()})通讯异常,自动重连中......");
                }
                if (Configs.StationType == Station.StationType.CapInput || Configs.StationType == Station.StationType.TrimInput
                    || Configs.StationType == Station.StationType.PSA2Load || Configs.StationType == Station.StationType.ChangeBoat4
                    || Configs.StationType == Station.StationType.PSA1Load || Configs.StationType == Station.StationType.PSAPress
                    || Configs.StationType == Station.StationType.PSA2Unload)
                {
                    if (Master2.Initialized)
                    {
                        int[] val2 = Master2.ReadHoldingRegisters(GlobalPLCAddress.HeartbeatAddr, 1);
                        if (val2[0] == 1)
                        {
                            Master2.WriteRegister(GlobalPLCAddress.HeartbeatResponseAddr, 1);
                            Master2.WriteRegister(GlobalPLCAddress.HeartbeatAddr, 0);
                        }
                        else
                        {
                            HeartbeatError2++;
                            if (HeartbeatError2 == 5)
                            {
                                HeartbeatError2 = 0;
                                Master2.Initialized = false;
                                Master2.Initialzie();
                                if (Master2.Initialized)
                                {
                                    ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.信息, $"与PLC({Master2.GetIP()})通讯异常,自动重连成功");
                                    Master2.WriteRegister(GlobalPLCAddress.HeartbeatResponseAddr, 1);
                                }
                                else
                                    ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.错误, $"与PLC({Master2.GetIP()})通讯异常,自动重连中......");
                            }
                        }
                    }
                    else
                    {
                        Master2.Initialzie();
                        if (Master2.Initialized)
                        {
                            ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.信息, $"1与PLC({Master2.GetIP()})通讯异常,自动重连成功");
                            Master.WriteRegister(GlobalPLCAddress.HeartbeatResponseAddr, 1);
                        }
                        else
                            ShowLogInfo(Configs.StationID, "Heartbeat", EventIO.错误, $"1与PLC({Master2.GetIP()})通讯异常,自动重连中......");
                    }
                }
            }
            catch (Exception ex)
            {
                HttpClientHelper.AddErrorLogInfo(Configs.StationID, "Heartbeat", null, $"心跳与PLC通讯异常:{ex.Message}");
            }
            finally
            {
                swPLCbeat.Restart();
            }
        }
        public void InitNewThread()
        {
            ThreadCheck2 = new Thread(() =>
            {
                try
                {
                    while (bRunning)
                    {
                        lock (syncObject2)
                        {
                            CheckOut();
                        }
                        Thread.Sleep(100);
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    LogsUtil.Instance.WriteWarn($"{MachineID} thread abort!");
                }
                catch (Exception ex)
                {
                    LogsUtil.Instance.WriteException(ex.InnerException ?? ex);
                }
            });
            ThreadCheck2.IsBackground = true;
            ThreadCheck2.Start();
        }
        public virtual void InitThread()
        {
            ThreadCheck1 = new Thread(() =>
            {
                try
                {
                    while (bRunning)
                    {
                        try
                        {
                            lock (syncObject1)
                            {
                                CheckIn();
                            }
                            Thread.Sleep(100);
                        }
                        catch (ThreadAbortException)
                        {
                            Thread.ResetAbort();
                            LogsUtil.Instance.WriteWarn($"循环内{MachineID} thread abort!");
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                    Thread.ResetAbort();
                    LogsUtil.Instance.WriteWarn($"{MachineID} thread abort!");

                }
                catch (Exception ex)
                {
                    LogsUtil.Instance.WriteException(ex.InnerException ?? ex);
                }
            });
            ThreadCheck1.IsBackground = true;
            ThreadCheck1.Start();
            Task.Run(async () =>
           {
               while (bPLCHeartbeat)
               {
                   if (swPLCbeat.ElapsedMilliseconds >= 3000 && Environment.MachineName != "SXJM009") //3秒置位一次心跳
                   {
                       CheckHeartbeat(); //
                   }
                   //if (swHeartbeat.ElapsedMilliseconds >= 15000) //机台状态上传
                   //{
                   PostStatus();
                   //}

                   //if (bMESHeartbeat)
                   //{
                   //    PostAlarm();
                   //}
                   await Task.Delay(1000);
                   //Thread.Sleep(1000);
               }
           });
        }
        [DllImport("user32.dll")]
        public static extern int MessageBoxTimeoutA(IntPtr intPtr, string content, string title, int cap, int type, int timeout);
        public void MessageBoxMsg(string msg)
        {
            Task.Run(() => { MessageBoxTimeoutA(IntPtr.Zero, DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "\n" + msg, "错误", 0, 0, 1200000); });
        }
        public virtual bool CheckIn()
        {
            return true;
        }
        public virtual bool CheckOut()
        {
            return true;
        }

        public virtual void PostStatus()
        {
            //1. Run  运行   2. Stop  停机  3. Maintain  保养  4. Idle  等待(物料或者载具)  5. Standby  休息/吃饭
            try
            {
                bool flag = false;
                if (Master.Initialized)
                {
                    #region
                    //var statusID = Master.ReadHoldingRegisters(GlobalPLCAddress.StatusAddr, 1)[0];
                    //StringBuilder sbAlarm = new StringBuilder();
                    //if (statusID == 2)
                    //{
                    //    List<int> val = new List<int>();
                    //    val = GetAlarmVal(1, out string alarmCode);
                    //    if (val != null && val.Any(v => v != 0))
                    //    {
                    //        for (int i = 0; i < val.Count; i++)
                    //        {
                    //            int index = i * 2 + 1; //两个地址中的前一个
                    //            if (val[i] == 1)
                    //                sbAlarm.Append($"_{alarmCode}{index.ToString("D3")}");
                    //            else if (val[i] == 256)
                    //                sbAlarm.Append($"_{alarmCode}{index + 1.ToString("D3")}");
                    //            else if (val[i] == 257)
                    //                sbAlarm.Append($"_{alarmCode}{index.ToString("D3")}" + $"_{alarmCode}{index + 1.ToString("D3")}");
                    //        }
                    //    }
                    //}
                    //StatusRequest req = new StatusRequest(Configs, statusID.ToString() + sbAlarm);
                    //Task<StatusResponse> resp = HttpClientHelper.StatusAsync(req, Configs.Url);
                    //resp.Wait();
                    //CheckMesConnectStatus(resp.Result);
                    #endregion
                    #region
                    int statusID = Master.ReadHoldingRegisters(GlobalPLCAddress.StatusAddr, 1)[0];
                    PLC_Status pLC_Status = PLC_StatusTool.Query(Configs.MachineID);
                    if (pLC_Status != null)
                    {
                        if (pLC_Status.Status == statusID)
                        {
                            if (swHeartbeat.ElapsedMilliseconds < 15000)
                            {
                                goto two;//状态未变更并且不到15s就不上传
                            }
                            else flag = true;
                        }
                        else
                        {
                            PLC_StatusTool.Update(Configs.MachineID, new PLC_Status() { MachineID = Configs.MachineID, Status = statusID });
                            flag = true;
                        }
                    }
                    else
                    {
                        PLC_StatusTool.Insert(new PLC_Status() { MachineID = Configs.MachineID, Status = statusID });
                        flag = true;
                    }

                    if (statusID == 2)
                    {
                        List<int> val = GetAlarmVal(1, out string alarmCode);
                        if (val != null && val.Any(v => v != 0))
                        {
                            #region 应MES要求只传一次
                            //string strAlarm = null;
                            //int index = 0 * 2 + 1; //两个地址中的前一个 11000  val[0]=257 000 001  MES:001 002  003 004 
                            //if (val[0] == 1 || val[0] == 257)
                            //    strAlarm = $"_{alarmCode}{index.ToString("D3")}";
                            //else if (val[0] == 256)
                            //    strAlarm = $"_{alarmCode}{(index + 1).ToString("D3")}";
                            //if (val[0] != 0)
                            //{
                            //    Task<StatusResponse> resp = StatusPost(Configs, statusID.ToString() + strAlarm);
                            //    CheckMesConnectStatus(resp.Result);
                            //}
                            //if (val[0] == 257)
                            //{
                            //    strAlarm = $"_{alarmCode}{(index + 1).ToString("D3")}";
                            //    StatusPost(Configs, statusID.ToString() + strAlarm);
                            //}
                            #endregion
                            #region 传所有机故信息
                            for (int i = 0; i < val.Count; i++)
                            {
                                string strAlarm = null;
                                int index = i * 2 + 1; //两个地址中的前一个 11000  val[0]=257 000 001  MES:001 002  003 004 
                                if (val[i] == 1 || val[i] == 257)
                                    strAlarm = $"_{alarmCode}{index.ToString("D3")}";
                                else if (val[i] == 256)
                                    strAlarm = $"_{alarmCode}{(index + 1).ToString("D3")}";
                                if (val[i] != 0)
                                {
                                    Task<StatusResponse> resp = StatusPost(Configs, statusID.ToString() + strAlarm);
                                    CheckMesConnectStatus(resp.Result);
                                }
                                if (val[i] == 257)
                                {
                                    strAlarm = $"_{alarmCode}{(index + 1).ToString("D3")}";
                                    StatusPost(Configs, statusID.ToString() + strAlarm);
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            Task<StatusResponse> resp = StatusPost(Configs, statusID.ToString());
                            CheckMesConnectStatus(resp?.Result);
                            LogsUtil.Instance.WriteLogStatus(Configs.StationID + "，读取报警", "未匹配到机台报警信息！");
                        }
                    }
                    else
                    {
                        Task<StatusResponse> resp = StatusPost(Configs, statusID.ToString());
                        CheckMesConnectStatus(resp?.Result);
                    }
                    #endregion
                }
            two:
                if (Configs.StationType == Station.StationType.CapInput
                    || Configs.StationType == Station.StationType.TrimInput
                    || Configs.StationType == Station.StationType.PSA1Load
                    || Configs.StationType == Station.StationType.PSA2Load
                    || Configs.StationType == Station.StationType.PSAPress
                    || Configs.StationType == Station.StationType.ChangeBoat4
                    || Configs.StationType == Station.StationType.PSA2Unload)
                {
                    if (Master2.Initialized)
                    {
                        #region
                        int statusID = Master2.ReadHoldingRegisters(GlobalPLCAddress.StatusAddr, 1)[0];
                        SysConfigs config = Configs.CloneSysConfigs();
                        if (config.AlarmCode.Split(';').Length >= 2)
                        {
                            //string alarmCode = config.AlarmCode.Split(';')[1];
                            //int index = alarmCode.IndexOf('M');
                            //string id = alarmCode.Substring(index + 2);
                            //config.MachineID = config.MachineID.Substring(0, config.MachineID.Length - 2) + id;
                            config.MachineID = config.MachineID.Substring(0, config.MachineID.Length - 2) + "02";
                        }
                        #region 表数据查询
                        PLC_Status pLC_Status1 = PLC_StatusTool.Query(config.MachineID);
                        if (pLC_Status1 != null)
                        {
                            if (pLC_Status1.Status == statusID)
                            {
                                if (swHeartbeat.ElapsedMilliseconds < 15000)
                                {
                                    return;//状态未变更并且不到15s就不上传
                                }
                                else flag = true;
                            }
                            else
                            {
                                PLC_StatusTool.Update(config.MachineID, new PLC_Status() { MachineID = config.MachineID, Status = statusID });
                                flag = true;
                            }
                        }
                        else
                        {
                            PLC_StatusTool.Insert(new PLC_Status() { MachineID = config.MachineID, Status = statusID });
                            flag = true;
                        }
                        #endregion
                        if (statusID == 2)
                        {
                            var val = GetAlarmVal(2, out string alarmCode);
                            if (val != null && val.Any(v => v != 0))
                            {
                                for (int i = 0; i < val.Count; i++)
                                {
                                    string strAlarm = null;
                                    int index = i * 2 + 1; //两个地址中的前一个
                                    if (val[i] == 1 || val[i] == 257)
                                        strAlarm = $"_{alarmCode}{index.ToString("D3")}";
                                    else if (val[i] == 256)
                                        strAlarm = $"_{alarmCode}{(index + 1).ToString("D3")}";
                                    if (val[i] != 0)
                                    {
                                        Task<StatusResponse> resp = StatusPost(config, statusID.ToString() + strAlarm);
                                        CheckMesConnectStatus(resp.Result);
                                    }
                                    if (val[i] == 257)
                                    {
                                        strAlarm = $"_{alarmCode}{(index + 1).ToString("D3")}";
                                        StatusPost(config, statusID.ToString() + strAlarm);
                                    }
                                }
                            }
                            else
                            {
                                Task<StatusResponse> resp = StatusPost(config, statusID.ToString());
                                CheckMesConnectStatus(resp?.Result);
                                LogsUtil.Instance.WriteLogStatus(Configs.StationID + "，读取报警", "未匹配到机台报警信息！");
                            }
                        }
                        else
                        {
                            Task<StatusResponse> resp = StatusPost(config, statusID.ToString());
                            CheckMesConnectStatus(resp?.Result);
                        }
                        #endregion
                    }
                }
                if (flag)
                    swHeartbeat.Restart();
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
            finally { }
        }
        private Task<StatusResponse> StatusPost(SysConfigs config, string status)
        {
            StatusRequest req = new StatusRequest(config, status);
            Task<StatusResponse> resp = HttpClientHelper.StatusAsync(req, config.Url);
            resp.Wait();
            return resp;
        }
        public enum AlarmStatus
        {
            Start,
            End
        }

        public bool PostAlarm(string alarmCode, AlarmStatus status)
        {

            string sendTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); ;
            string resetTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"); ;
            if (status == AlarmStatus.Start)
            {
                sendTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }
            else if (status == AlarmStatus.End)
            {
                resetTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            }
            AlarmRequest req = new AlarmRequest(Configs, alarmCode, sendTime, resetTime);
            Task<AlarmResponse> resp = HttpClientHelper.AlarmAsync(req, Configs.Url);
            return resp.Result?.Result == "PASS";
        }

        /// <summary>
        /// 获取报警地址的使用个数
        /// </summary>
        /// <param name="plcID"></param>
        /// <returns></returns>
        public int GetAlarmAddrNum(int plcID)
        {
            int num = 250;
            if ((Configs.StationType == Station.StationType.CapInput || Configs.StationType == Station.StationType.TrimInput) && plcID == 2)
            {
                num = 150;
            }
            else if (Configs.StationType == Station.StationType.PSA2Unload)
                num = 500;
            else if (Configs.StationType == Station.StationType.PSA2Load)
            {
                if (plcID == 1)
                    num = 500;
                else if (plcID == 2)
                    num = 400;
            }
            else if (Configs.StationType == Station.StationType.PSAPress)
                num = 150;
            return num;
        }
        /// <summary>
        /// 获取PLC报警地址中的数组信息
        /// </summary>
        /// <param name="plcID"></param>
        /// <param name="alarmCode"></param>
        /// <returns></returns>
        public virtual List<int> GetAlarmVal(int plcID, out string alarmCode)
        {
            List<int> listAlarm = new List<int>();
            int num = GetAlarmAddrNum(plcID);//250
            int count = 123;//每次最大读取123长度
            int split = num / count;
            int last = num % count;
            int startAdr = GlobalPLCAddress.AlarmAddr;
            if (plcID == 1)
            {
                alarmCode = AlarmCodeList[0];
                //int split = num / 100;
                //int last = num % 100;
                //int count = 100;
                //int startAdr = GlobalPLCAddress.AlarmAddr;
                for (int i = 0; i < split; i++)
                {
                    startAdr = startAdr + i * count;
                    listAlarm.AddRange(Master.ReadHoldingRegisters(startAdr, count).ToList());
                }
                if (last > 0)
                {
                    startAdr = startAdr + count;
                    listAlarm.AddRange(Master.ReadHoldingRegisters(startAdr, last).ToList());
                }
                return listAlarm;
            }
            else
            {
                alarmCode = AlarmCodeList.Count == 2 ? AlarmCodeList[1] : string.Empty;
                for (int i = 0; i < split; i++)
                {
                    startAdr = startAdr + i * count;
                    List<int> list = Master2.ReadHoldingRegisters(startAdr, count).ToList();
                    listAlarm.AddRange(list);
                }
                if (last > 0)
                {
                    startAdr = startAdr + count;
                    listAlarm.AddRange(Master2.ReadHoldingRegisters(startAdr, last).ToList());
                }
                return listAlarm;
            }
        }
        //public virtual List<int> GetAlarmVal(int plcID, out string alarmCode)
        //{
        //    List<int> listAlarm = new List<int>();
        //    int num = GetAlarmAddrNum(plcID);
        //    if (plcID == 1)
        //    {
        //        alarmCode = AlarmCodeList[0];
        //        int split = num / 125;
        //        int last = num % 125;
        //        int count = 50;
        //        //int count = 125;
        //        int startAdr = GlobalPLCAddress.AlarmAddr;
        //        for (int i = 0; i < split; i++)
        //        {
        //            startAdr = startAdr + i * count;
        //            listAlarm.AddRange(Master.ReadHoldingRegisters(startAdr, count).ToList());
        //        }
        //        if (last > 0)
        //        {
        //            startAdr = startAdr + count;
        //            listAlarm.AddRange(Master.ReadHoldingRegisters(startAdr, last).ToList());
        //        }
        //        return listAlarm;
        //    }
        //    else
        //    {
        //        alarmCode = AlarmCodeList.Count == 2 ? AlarmCodeList[1] : string.Empty;

        //        int count = 120;
        //        int split = num / count;
        //        int last = num % count;

        //        int startAdr = GlobalPLCAddress.AlarmAddr;
        //        for (int i = 0; i < split; i++)
        //        {
        //            startAdr = startAdr + i * count;
        //            var list1 = Master2.ReadHoldingRegisters(startAdr, count).ToList();
        //            listAlarm.AddRange(list1);
        //        }
        //        if (last > 0)
        //        {
        //            startAdr = startAdr + count;
        //            listAlarm.AddRange(Master2.ReadHoldingRegisters(startAdr, last).ToList());
        //        }

        //        return listAlarm;
        //    }
        //}

        public void InnerPostAlarm(List<int> alarmList, string alarmCode, Dictionary<int, int> dicAlarm)
        {
            if (alarmList == null || alarmList.Count == 0)
                return;
            string code = string.Empty;
            for (int i = 0; i < alarmList.Count; i++)
            {
                int alramNew1 = 0;
                int alramNew2 = 0;

                if (alarmList[i] == 1) //地址1报警地址2不报警
                {
                    alramNew1 = 1;
                    alramNew2 = 0;
                }
                else if (alarmList[i] == 256) //地址1不报警地址2报警
                {
                    alramNew1 = 0;
                    alramNew2 = 1;
                }
                else if (alarmList[i] == 257)//地址1报警地址2报警
                {
                    alramNew1 = 1;
                    alramNew2 = 1;
                }

                int index = i * 2 + 1; //两个地址中的前一个
                int alarmOld1 = dicAlarm[index];
                int alarmOld2 = dicAlarm[index + 1];

                if (alramNew1 - alarmOld1 > 0) //地址1报警开始
                {
                    dicAlarm[index] = 1;
                    code = $"{alarmCode}{index.ToString("D3")}";
                    PostAlarm(code, AlarmStatus.Start);
                }
                else if (alramNew1 - alarmOld1 < 0) //地址1报警结束
                {
                    dicAlarm[index] = 0;
                    code = $"{alarmCode}{index.ToString("D3")}";
                    PostAlarm(code, AlarmStatus.End);
                }

                if (alramNew2 - alarmOld2 > 0) //地址2报警开始
                {
                    dicAlarm[index + 1] = 1;
                    code = $"{alarmCode}{(index + 1).ToString("D3")}";
                    PostAlarm(code, AlarmStatus.Start);
                }
                else if (alramNew2 - alarmOld2 < 0) //地址2报警结束
                {
                    dicAlarm[index + 1] = 0;
                    code = $"{alarmCode}{(index + 1).ToString("D3")}";
                    PostAlarm(code, AlarmStatus.End);
                }

            }
        }
        public virtual void PostAlarm()
        {
            try
            {
                if (Master.Initialized)
                {
                    var val = GetAlarmVal(1, out string alarmCode);
                    if (val == null && !val.Any())
                        return;
                    InnerPostAlarm(val, alarmCode, dicAlarm1);
                }
                if (Configs.StationType == Station.StationType.CapInput || Configs.StationType == Station.StationType.PSA1Load || Configs.StationType == Station.StationType.TrimInput
                  || Configs.StationType == Station.StationType.PSA2Unload || Configs.StationType == Station.StationType.PSA2Load)
                {
                    if (Master2.Initialized)
                    {
                        var val2 = GetAlarmVal(2, out string alarmCode2);
                        if (val2 == null && !val2.Any())
                            return;
                        InnerPostAlarm(val2, alarmCode2, dicAlarm2);
                    }
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"获取机台报警异常:{ex.ToString()}");
                HttpClientHelper.AddErrorLogInfo(Configs.StationID, "GetAlarm", null, $"获取机台报警异常:{ex.Message},{ex.InnerException?.Message}");
            }
        }
        public virtual void SendStatusAndAlarm()
        {
            try
            {
                return;
                PostStatus();
                PostAlarm();

            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }
        public void CheckMesConnectStatus(BaseResponseParams response)
        {
            Connected = response != null;
        }
        public void HandleException(Exception ex)
        {
            if (ex == null) return;
            if (ex.InnerException is MesConnException || ex is MesConnException)
            {
                Connected = false;
            }
            else if (ex is ArgumentErrorException exception)
            {
                HttpClientHelper.AddErrorLogInfo(Configs.StationID, exception.EventID, null, exception.Message + exception?.InnerException?.Message);
                MessageBoxMsg($"工站：{Configs.StationID}{Environment.NewLine}事件ID：{exception.EventID}{Environment.NewLine}信息:{exception.Message + exception?.InnerException?.Message}");
            }
            else
            {
                HttpClientHelper.AddErrorLogInfo(Configs.StationID, "Exception", null, ex.Message + ex?.InnerException?.Message + "【定位】" + ex.TargetSite.Name + "：" + ex.StackTrace);
            }
            //else
            //    LogsUtil.Instance.WriteError(ex.Message);
        }
        /// <summary>
        /// 更新配置文件
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void UpdateConfig(string key, string value)
        {
            lock (syncObject3)
            {
                Configuration cfa = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                cfa.AppSettings.Settings[key].Value = value;
                cfa.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }
        public bool CheckString(string str)
        {
            if (str.ToLower().Contains("err") || str.ToLower().Contains("noread") || string.IsNullOrEmpty(str))
                return false;
            else
                return true;
        }
        public virtual void Release()
        {
            bRunning = false;
            ThreadCheck1?.Abort();
            ThreadCheck1 = null;
            ThreadCheck2?.Abort();
            ThreadCheck2 = null;
            Master?.Release();
            Master2?.Release();
        }
        public string GetStaionType()
        {
            return Configs.StationType.ToString() ?? "";
        }
        public static void ShowLogInfo(string stationID, string eventID, EventIO eventIO, string msg)
        {
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                LogsUtil.Instance.AddEventParams(new LogInfo(stationID, eventID, eventIO, msg));
            }));
        }
        #region about plc read/write 
        /// <summary>
        /// DEBUG模式下才显示log到界面上
        /// </summary>
        /// <param name="stationID"></param>
        /// <param name="eventID"></param>
        /// <param name="msg"></param>
        public static void AddReadLogInfo(string stationID, string eventID, string msg)
        {
            //#if DEBUG
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                LogsUtil.Instance.AddEventParams(new LogInfo(stationID, eventID, EventIO.读取, msg));
            }));
            //#endif
        }
        /// <summary>
        ///  DEBUG模式下才显示log到界面上
        /// </summary>
        /// <param name="stationID"></param>
        /// <param name="eventID"></param>
        /// <param name="msg"></param>
        public static void AddWriteLogInfo(string stationID, string eventID, string msg)
        {
            //#if DEBUG
            Application.Current?.Dispatcher.Invoke(new Action(() =>
            {
                LogsUtil.Instance.AddEventParams(new LogInfo(stationID, eventID, EventIO.写入, msg));
            }));
            //#endif
        }
        #endregion
        public virtual void ReCount(string locationID)
        {

        }
        public virtual void FeedingCheck()
        {
            Queue<List<string>> SNInfosQueue = new Queue<List<string>>();
            var list = new List<string>();
            list.Add("1");
            list.Add("2");
            list.Add("3");
            SNInfosQueue.Enqueue(list);
            var list2 = new List<string>();
            list2.Add("1");
            list2.Add("2");
            list2.Add("3");
            SNInfosQueue.Enqueue(list2);
            var ss = SNInfosQueue.Dequeue();
            FeedingCheckRequest request = new FeedingCheckRequest(Configs, "SNXXXX");
            Task<FeedingCheckResponse> response = HttpClientHelper.SNFeedingCheckAsync(request, Configs.Url);

        }
        public virtual void SNCheckOut()
        {
            List<SNInfo> list = new List<SNInfo>();
            //list.Add(new SNInfo("sn1", "pass", new List<DCInfo>() { new DCInfo("item1", "20.4", "up", "down", "pass") }, new List<CompInfo>() { new CompInfo("CompID", 100) }));
            list.Add(new SNInfo("sn1", "pass", new List<DCInfo>() { }, new List<CompInfo>() { }));
            //list.Add(new SNInfo("sn1", "pass", null, null));
            CheckOutRequest request = new CheckOutRequest(Configs, "CSF-U66-31EPB1-020", Configs.Mold, 200, list);
            Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);

        }
        public virtual void SNCarrierCheck()
        {
            CarrierCheckRequest request = new CarrierCheckRequest(Configs, "CarrierIDXXXX");
            Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);

        }

        public virtual void SNCheckInAsync()
        {

            ////Show("1", 60000);
            ////Show("2", 2000);
            ////Show("3", 3000);
            ////Show("4", 4000);
            ////Show("5", 5000);
            return;
            CheckInRequest request = new CheckInRequest(Configs, "CSF-U66-31HBB1-011", "NULL");
            Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
        }
        public virtual void GlueCheckOut()
        {
            MessageBox.Show(bSimulation ? "当前结果默认为OK(若无扫码结果则NG)。" : "当前结果为立讯MES返回的结果");
            //GlueCheckOutRequest request = new GlueCheckOutRequest(Configs, "SNXXXX01");
            //Task<GlueCheckOutResponse> response = HttpClientHelper.GlueCheckOutAsync(request, Configs.Url);
        }
        public virtual void DataCollection()
        {
            DataCollectionRequest request = new DataCollectionRequest("CH08", "AVI", "AVI06-2", "YUYU", "TokenXXX", "FixSNXXX", "SNXX", "CarrierIDXX",
                    new List<DC_Info>() { new DC_Info("temp", "33") }, new List<CompInfo>() { new CompInfo("compId1", 1000) });
            Task<DataCollectionResponse> response = HttpClientHelper.DataCollectionAsync(request, Configs.Url);
        }
        public virtual void SNCarrierBind()
        {
            CarrierBindRequest request = new CarrierBindRequest(Configs, "CSF-U66-31HBB1-011", "CSF-U66-31EPB1-013", "Change", "1,2,3");
            Task<CarrierBindResponse> response = HttpClientHelper.SNCarrierBindAsync(request, Configs.Url);
        }
        public virtual void Status()
        {
            StatusRequest request = new StatusRequest(Configs, "3");
            Task<StatusResponse> response = HttpClientHelper.StatusAsync(request, Configs.Url);
        }
        public virtual void Alarm()
        {
            AlarmRequest request = new AlarmRequest(Configs, "2");
            Task<AlarmResponse> response = HttpClientHelper.AlarmAsync(request, Configs.Url);

        }
        public bool GetRet(BaseResponseParams resp)
        {
            //弹窗提示
            bool ret = (bool)resp?.Result.ToLower().Contains("pass") || (bool)resp?.Result.ToLower().Contains("alarm");
            //if (resp.Result.ToLower() == "fail")
            if (!ret)
            {
                //CapInput和TrimInput有多个SN_FeedingCheck,不进行弹窗提示。其余工站为返回Fail均弹窗提示
                if (!((Configs.StationType == Station.StationType.CapInput || Configs.StationType == Station.StationType.TrimInput) && resp.EventID == "SN_FeedingCheck"))
                    MessageBoxMsg($"工站：{Configs.StationID}{Environment.NewLine}事件：({resp.EventID}){Environment.NewLine}结果:{resp.Result};{Environment.NewLine}返回信息:{resp.Msg};");
                //MessageBoxMsg($"{Configs.StationID}({resp.EventID}):Result:{resp.Result};MSG:{resp.Msg}");
            }
            return ret;
        }

        public bool GetRetNew(BaseResponseParams resp)
        {
            bool ret = (bool)resp?.Result.ToLower().Contains("pass") || (bool)resp?.Result.ToLower().Contains("alarm");
            return ret;
        }
    }
}
