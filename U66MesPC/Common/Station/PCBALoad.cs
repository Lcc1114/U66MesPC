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
    /// 对应工站：PCBA机台上料，所在模组：PCBA attach to cap
    /// </summary>
    public class PCBALoad : StationBase
    {
        //public Queue<List<SNInfo>> SNInfosQueue;
        //public Queue<string> CarrierIDQueue;
        public Dictionary<string, List<SNInfo>> CarrierAndSNMapping;
        public PCBALoad(SysConfigs sysConfig) : base(sysConfig)
        {
            /*SNInfosQueue = new Queue<List<SNInfo>>();*/
            //CarrierIDQueue = new Queue<string>();
            CarrierAndSNMapping = new Dictionary<string, List<SNInfo>>();
            Master.Initialzie();
            InitThread();
        }
        public Random random = new Random();
        public bool InnerCheckIn()
        {
            try
            {
                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);
                //AddReadLogInfo(Configs.StationID, "FromPLC", "载具码：" + carrierID);

                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);
                //AddWriteLogInfo(Configs.StationID, "ToPLC", "Reset CheckOut flag");

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

                AddWriteLogInfo(Configs.StationID, "ToPLC", $"CheckIn result:{ret}；CarrierID:{carrierID}");
                if (ret)
                {
                    List<SNInfo> list = new List<SNInfo>();
                    SN = response.Result.SN;
                    carrierID = response.Result.CarrierID;
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
                Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1); //写检查结果
                HandleException(ex);
                return false;
            }
            finally
            {
                //SendStatusAndAlarm();
            }
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
                int bCheckIn = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
                int bCheckOut = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr, 1)[0];
#if DEBUG
                Console.WriteLine($"CarrierCheck:{bCheckIn};CheckOut：{bCheckOut}；");
#endif
                if (bCheckIn == 1)
                {
                    ret &= InnerCheckIn();
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
        /// 以产品码出站；carrierID栏位放载具码
        /// </summary>
        /// <returns></returns>
        public override bool CheckOut()
        {
            try
            {
                if (CarrierAndSNMapping.Count == 0)
                {
                    throw new ArgumentErrorException("出站失败：待出站的信息为空");
                }
                string carrierID = CarrierAndSNMapping?.First().Key;
                //string carrierID = Master.GetStringFromMaster(PLCAddress.CheckOutInfoAddr,PLCAddress.CarrierInfoLength);
                var info = CarrierAndSNMapping.FirstOrDefault(p => p.Key == carrierID);
                CarrierAndSNMapping.Remove(carrierID);
                if (!bSimulation && (info.Key == null || info.Value.Count == 0))
                {
                    throw new ArgumentErrorException($"出站失败：载具码（{carrierID}）或产品码（{ info.Value?.Count()}个）无效");
                }
                var snInfosList = info.Value;

                CheckOutRequest request = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                bool ret = GetRet(response.Result);
                if (bSimulation)
                    ret = true;
                AddWriteLogInfo(Configs.StationID, "ToPLC", $"CheckOut：{ret}；CarrierID:{carrierID}");

                int addr = ret ? PLCAddress.CheckOutPassAddr : PLCAddress.CheckOutFailAddr;
                Master.WriteRegister(addr, 1);
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
                //Thread.Sleep(100);
                Master.WriteRegister(PLCAddress.CheckOutAddr, 0);
                //AddWriteLogInfo(Configs.StationID, "ToPLC", "Reset CheckOut flag");
                //SendStatusAndAlarm();
            }
        }
        public override void Release()
        {
            base.Release();
        }
        class PLCAddress
        {
            //public static int CarrierCheckAddr = 40000;
            //public static int CarrierCheckInfoAddr = 30075;
            //public static int CarrierCheckPassAddr = 50000;
            //public static int CarrierCheckFailAddr = 50001;

            //public static int CheckOutAddr = 40002;
            //public static int CheckOutInfoAddr = 1;
            //public static int CheckOutPassAddr = 50002;
            //public static int CheckOutFailAddr = 50003; 

            public static int CarrierInfoLength = 64;

            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 1000;
            public static int CarrierCheckFailAddr = 1001;

            public static int CheckOutAddr = 1;
            public static int CheckOutPassAddr = 1002;
            public static int CheckOutFailAddr = 1003;
        }
    }
}
