using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using U66MesPC.Common.Exceptions;
using U66MesPC.Dal;
using U66MesPC.Model;
using Timer = System.Timers.Timer;

namespace U66MesPC.Common.Station
{

    /// <summary>
    /// 对应工站：E-Flex机台下料，所在模组：E-Flex Attach to Cap&PCBA
    /// </summary>
    public class EFlexUnLoad : StationBase
    {
        public Queue<List<SNInfo>> SNInfosQueue;
        public Queue<string> CarrierIDQueue;
        public List<string> ClipIDList; //弹夹码列表 只保存最新的弹夹码
        public Dictionary<string, List<SNInfo>> CarrierAndSNMapping;
        public EFlexUnLoad(SysConfigs sysConfig) : base(sysConfig)
        {
            CarrierAndSNMapping = new Dictionary<string, List<SNInfo>>();
            SNInfosQueue = new Queue<List<SNInfo>>();
            CarrierIDQueue = new Queue<string>();
            ClipIDList = new List<string>();
            Master.Initialzie();
            InitThread();
        }
        public Random random = new Random();
        public override void SNCheckInAsync()
        {
            // Master.WriteRegister(PLCAddress.ClipSNCheckPassAddr, 1);
            string clipID = Master.GetStringFromMaster(PLCAddress.ClipSNCheckInfoAddr, PLCAddress.ClipSNInfoLength);

            var s221 = Master.GetStringFromMasterAndNotClear(30139, 64);
            var s22 = Master.GetStringFromMasterAndNotClear(30075, 64);
        }
        public bool ClipSNCheck(int station)
        {
            try
            {
                string clipID = Master.GetStringFromMaster(station == 1 ? PLCAddress.ClipSNCheckInfoAddr : PLCAddress.ClipSNCheckInfoAddr2, PLCAddress.ClipSNInfoLength);
                //AddReadLogInfo(Configs.StationID, "FromPLC", "弹夹：" + clipID);

                if (string.IsNullOrEmpty(clipID) || clipID.Equals("ERROR"))
                    throw new ArgumentErrorException($"{station}#弹夹检查失败：弹夹码{clipID}无效！", "Carrier_Check");

                CarrierCheckRequest request = new CarrierCheckRequest(Configs, clipID);
                Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);

                bool result = GetRet(response.Result);
                if (bSimulation)
                    result = true;
                if (result)
                {
                    ClipIDList.Add(clipID);
                }
                int addr = -1;
                if (station == 1)
                    addr = result ? PLCAddress.ClipSNCheckPassAddr : PLCAddress.ClipSNCheckFailAddr;
                else if (station == 2)
                    addr = result ? PLCAddress.ClipSNCheckPassAddr2 : PLCAddress.ClipSNCheckFailAddr2;
                Master.WriteRegister(addr, 1);//弹夹检查结果
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"{station}#ClipCheck result:{result};弹夹：{clipID}");
                return result;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(station == 1 ? PLCAddress.ClipSNCheckFailAddr : PLCAddress.ClipSNCheckFailAddr2, 1);//弹夹检查结果
                HandleException(ex);
                return false;
            }
            finally
            {
                //Thread.Sleep(100);
                Master.WriteRegister(station == 1 ? PLCAddress.ClipSNCheckAddr : PLCAddress.ClipSNCheckAddr2, 0);//重置检查请求标志
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset ProductSNCheck flag");
            }
        }
        /// <summary>
        /// 核对载具的合法性
        /// </summary>
        /// <returns></returns>
        public bool InnerCheckIn()
        {
            try
            {
                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);
                //AddReadLogInfo(Configs.StationID, "FromPLC", "载具码：" + carrierID);

                if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR"))
                    throw new ArgumentErrorException($"进站失败：载具码({carrierID})无效！", "SN_CheckIN");
                CarrierID = carrierID;

                CheckInRequest request = new CheckInRequest(Configs, carrierID, carrierID);
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRet(response.Result);
                if (bSimulation)
                    ret = true;
                int addr = ret ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                AddWriteLogInfo(Configs.StationID, "ToPLC", $"CarrierCheck({carrierID}): result:{ret}");
                if (ret)
                {
                    List<SNInfo> list = new List<SNInfo>();
                    carrierID = response.Result.CarrierID;
                    SN = response.Result.SN;
                    string[] productsSN = response.Result.SN.Split('-');
                    string[] results = response.Result.Msg.Split('-');
                    for (int i = 0; i < Math.Max(productsSN.Length, results.Length); i++)
                    {
                        var sn = (i < productsSN.Length) ? productsSN[i] : "";
                        var result = (i < results.Length) ? results[i] : "";
                        if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                            continue;
                        else
                            list.Add(new SNInfo(sn, result, new List<DCInfo>() { }, new List<CompInfo>() { }));

                    }
                    //CarrierIDQueue.Enqueue(carrierID);
                    //SNInfosQueue.Enqueue(list);
                    if (CarrierAndSNMapping.ContainsKey(carrierID))
                        CarrierAndSNMapping.Remove(carrierID);
                    CarrierAndSNMapping.Add(carrierID, list);
                }
                Master.WriteRegister(addr, 1); //写检查结果
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1);
                HandleException(ex);
                return false;
            }
            finally
            {
                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);//重置请求标志
                //AddWriteLogInfo(Configs.StationID, "ToPLC", $"Reset CheckIn flag");
            }
        }
        public void ClipSNCheckOut(int station)
        {
            AddWriteLogInfo(Configs.StationID, "ToPLC", $"{station}#弹夹出站请求");
            string clipSN = string.Empty;
            if (ClipIDList.Any())
            {
                clipSN = ClipIDList.Aggregate("", (s, b) => s + $"{b};");
                ClipIDList.Clear();
            }
            Master.WriteRegister(station == 1 ? PLCAddress.ClipSNCheckOutOKAddr : PLCAddress.ClipSNCheckOutOKAddr2, 1);
            Master.WriteRegister(station == 1 ? PLCAddress.ClipSNCheckOutAddr : PLCAddress.ClipSNCheckOutAddr2, 0);
            AddWriteLogInfo(Configs.StationID, "ToPLC", $"{station}#弹夹{clipSN}出站OK");
        }
        /// <summary>
        /// MES回复对应穴位的CAP SN
        /// </summary>
        /// <returns></returns>
        public override bool CheckIn()
        {
            try
            {
                bool ret = true;
                int bCheckCarrier = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
                int bCheckClipSN = Master.ReadHoldingRegisters(PLCAddress.ClipSNCheckAddr, 1)[0];
                int bCheckClipSN2 = Master.ReadHoldingRegisters(PLCAddress.ClipSNCheckAddr2, 1)[0];
                int bCheckOut = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr, 1)[0];
                int bClipSNCheckOut = Master.ReadHoldingRegisters(PLCAddress.ClipSNCheckOutAddr, 1)[0];
                int bClipSNCheckOut2 = Master.ReadHoldingRegisters(PLCAddress.ClipSNCheckOutAddr2, 1)[0];
#if DEBUG
                Console.WriteLine($"CarrierCheck:{bCheckCarrier}；ClipSNCheck:{bCheckClipSN}；CheckOut:{bCheckOut};ClipCheckout:{bClipSNCheckOut}");
#endif
                if (bClipSNCheckOut == 1)
                {
                    ClipSNCheckOut(1);
                }
                if (bClipSNCheckOut2 == 1)
                {
                    ClipSNCheckOut(2);
                }
                if (bCheckCarrier == 1)
                {
                    ret &= InnerCheckIn();
                }
                if (bCheckClipSN == 1)
                {
                    ret &= ClipSNCheck(1);
                }
                if (bCheckClipSN2 == 1)
                {
                    ret &= ClipSNCheck(2);
                }
                if (bCheckOut == 1)
                {
                    ret &= CheckOut();
                }
                return ret;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }
        /// <summary>
        /// 绑定Boat1及弹夹；BindType设定为“BindTray”
        /// </summary>
        /// <returns></returns>
        public bool CarrierBind(string carrierID)
        {
            if (!ClipIDList.Any())
            {
                throw new ArgumentErrorException("绑定失败：弹夹码为空", "Carrier_Bind");
            }
            string clipID = ClipIDList.First();

            CarrierBindRequest request = new CarrierBindRequest(Configs, carrierID, clipID, "BindTray", "");
            Task<CarrierBindResponse> response = HttpClientHelper.SNCarrierBindAsync(request, Configs.Url);
            CheckMesConnectStatus(response.Result);
            return GetRet(response?.Result);
        }
        /// <summary>
        /// SN放CAP SN，CarrierID栏位放Boat1 SN出站；
        /// </summary>
        /// <returns></returns>
        public override bool CheckOut()
        {
            try
            {
                if (CarrierAndSNMapping.Count == 0)
                {
                    throw new ArgumentErrorException("出站失败：出站信息为空");
                }
                var info = CarrierAndSNMapping.First();
                string carrierID = info.Key;
                var snInfosList = info.Value;
                CarrierAndSNMapping.Remove(carrierID);
                if (!bSimulation)
                {
                    if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR"))
                        throw new ArgumentNullException($"出站失败，载具码({carrierID})无效！");
                    if (!bSimulation && (snInfosList == null || snInfosList.Count == 0))
                    {
                        throw new ArgumentErrorException($"出站失败，载具{carrierID}上无有效的产品码", "SN_CheckOut");
                    }
                }
                //List<SNInfo> list = new List<SNInfo>();
                //list.Add(new SNInfo("sn1", "pass", new List<DCInfo>() { new DCInfo("item1", "20.4", "up", "down", "pass") }, new List<CompInfo>() { new CompInfo("CompID", 100) }));
                CheckOutRequest request = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                bool ret = GetRet(response.Result);
                if (bSimulation)
                    ret = true;
                if (ret)
                {
                    ret = CarrierBind(carrierID);
                    if (bSimulation)
                        ret = true;
                    AddWriteLogInfo(Configs.StationID, "ToPLC", $"CheckOut result:{ret};CarrierID:{carrierID}");
                    Master.WriteRegister(ret ? PLCAddress.CheckOutPassAddr : PLCAddress.CheckOutFailAddr, 1);
                }
                else
                    Master.WriteRegister(PLCAddress.CheckOutFailAddr, 1);
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CheckOutFailAddr, 1);
                HandleException(ex);
                return false;
            }
            finally
            {
                Master.WriteRegister(PLCAddress.CheckOutAddr, 0);//重置请求标志
                //AddWriteLogInfo(Configs.StationID, "ToPLC", $"Reset CheckOut flag");
                //SendStatusAndAlarm();
            }
        }
        public override void Release()
        {
            base.Release();
        }
        class PLCAddress
        {
            //载具码检查
            //public static int CarrierCheckAddr = 40000;
            //public static int CarrierCheckInfoAddr = 30075;
            //public static int CarrierCheckPassAddr = 50000;
            //public static int CarrierCheckFailAddr = 50001;
            //弹夹码检查
            //public static int ClipSNCheckAddr = 40001;
            //public static int ClipSNCheckInfoAddr = 30164;
            //public static int ClipSNCheckInfoAddr = 30139;
            //public static int ClipSNCheckPassAddr = 50002;
            //public static int ClipSNCheckFailAddr = 50003;

            //public static int CheckOutAddr = 40002;
            //public static int CheckOutClipSNAddr = 40002;
            //public static int CheckOutCarrierSNAddr = 40002;
            //public static int CheckOutPassAddr = 50004;
            //public static int CheckOutFailAddr = 50005;

            //public static int CarrierInfoLength = 64;
            //public static int ClipSNInfoLength = 64;

            //public static int ClipSNCheckOutAddr = 40003;
            //public static int ClipSNCheckOutOKAddr = 50006;
            //public static int ClipSNCheckOutNGAddr = 50007;

            //public static int CarrierInfoLength = 64;
            //public static int ClipSNInfoLength = 64;

            //载具码检查
            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 1000;
            public static int CarrierCheckFailAddr = 1001;
            //弹夹码1检查
            public static int ClipSNCheckAddr = 5;
            public static int ClipSNCheckInfoAddr = 3064;
            public static int ClipSNCheckPassAddr = 1005;
            public static int ClipSNCheckFailAddr = 1006;

            //弹夹码2检查
            public static int ClipSNCheckAddr2 = 10;
            public static int ClipSNCheckInfoAddr2 = 3128;
            public static int ClipSNCheckPassAddr2 = 1010;
            public static int ClipSNCheckFailAddr2 = 1011;

            public static int CheckOutAddr = 1;
            public static int CheckOutPassAddr = 1002;
            public static int CheckOutFailAddr = 1003;

            public static int CarrierInfoLength = 64;
            public static int ClipSNInfoLength = 64;

            public static int ClipSNCheckOutAddr = 6;
            public static int ClipSNCheckOutOKAddr = 1007;
            public static int ClipSNCheckOutNGAddr = 1008;

            public static int ClipSNCheckOutAddr2 = 11;
            public static int ClipSNCheckOutOKAddr2 = 1012;
            public static int ClipSNCheckOutNGAddr2 = 1013;
        }
    }
}
