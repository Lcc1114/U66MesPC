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
    /// 对应工站：PSA1机台下料，所在模组：PSA Attach   
    /// 无用 MES不需要上传数据
    /// </summary>
    public class PSA1Unload : StationBase
    {
        Dictionary<string, List<SNInfo>> CarrierAndSNMapping;
        public PSA1Unload(SysConfigs sysConfig) : base(sysConfig)
        {
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
                AddReadLogInfo(Configs.StationID, "读取PLC", "PSA1_Unload工站上料扫码完成信号。载具码：" + carrierID);


                if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROE"))
                    throw new ArgumentErrorException($"载具码{carrierID}无效！", "SN_CheckIN");

                CarrierID = carrierID;
                CheckInRequest request = new CheckInRequest(Configs, "NULL", carrierID);
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRet(response.Result);
                if (bSimulation)
                    ret = true;
                if (ret)
                {
                    List<SNInfo> list = new List<SNInfo>();
                    SN = response.Result.SN;
                    carrierID = response.Result.CarrierID;
                    string[] productsSN = response.Result.SN.Split('-');
                    string[] results = response.Result.Msg.Split(',');
                    for (int i = 0; i < Math.Min(productsSN.Length, results.Length); i++)
                    {
                        var sn = productsSN[i];
                        var result = results[i];
                        if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                            continue;
                        else
                            list.Add(new SNInfo(sn, result, new List<DCInfo>() { }, new List<CompInfo>() { }));

                    }
                    bool bAllPass = !list.Any(s => s.Result.ToLower() != "pass");
                    int addr = bAllPass ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                    if (bAllPass)
                    {
                        if (CarrierAndSNMapping.ContainsKey(carrierID))
                            CarrierAndSNMapping.Remove(carrierID);
                        CarrierAndSNMapping.Add(carrierID, list);
                    }
                    int okCount = results.Where(x => x.ToLower() == "pass").Count();
                    Master.WriteRegister(PLCAddress.OkAddress, okCount);
                    Master.WriteRegister(PLCAddress.NgAddress, results.Length - okCount);
                    Master.WriteRegister(addr, 1); //写检查结果
                    AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA1_Unload工站上料载具检查结果:{bAllPass}");
                }
                else
                {
                    Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1); //写检查结果
                }
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
                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);
                //AddWriteLogInfo(Configs.StationID, "写入PLC", "Reset CheckOut flag");
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
                AddReadLogInfo(Configs.StationID, "读取PLC", "读取到PSA1_Unload工站下料出站信号");
                if (CarrierAndSNMapping.Count == 0)
                {
                    throw new ArgumentErrorException("出站失败：待出站信息为空");
                }

                var mapping = CarrierAndSNMapping.FirstOrDefault();
                CarrierAndSNMapping.Remove(mapping.Key);
                if (mapping.Value == null || mapping.Value.Count == 0)
                    throw new ArgumentErrorException($"出站失败：载具码{mapping.Key}上对应的产品码为空");

                CheckOutRequest request = new CheckOutRequest(Configs, mapping.Key, Configs.Mold, 0, mapping.Value);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                //bool ret = GetRet(response.Result);
                bool ret = response.Result.SN_Info.Any(x => x.SNResult.ToUpper() == "PASS");
                if (bSimulation)
                    ret = true;
                int addr = ret ? PLCAddress.CheckOutPassAddr : PLCAddress.CheckOutFailAddr;
                Master.WriteRegister(addr, 1);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA1_Unload工站下料出站结果:{ret}");
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
                Master.WriteRegister(PLCAddress.CheckOutAddr, 0);
                //AddWriteLogInfo(Configs.StationID, "写入PLC", "Reset CheckOut flag");
                // SendStatusAndAlarm();
            }
        }
        public override void Release()
        {
            base.Release();
        }
        class PLCAddress
        {
            public static int CarrierCheckAddr = 40000;
            public static int CarrierCheckInfoAddr = 30075;
            public static int CarrierCheckPassAddr = 50000;
            public static int CarrierCheckFailAddr = 50001;

            public static int CarrierInfoLength = 64;

            public static int CheckOutAddr = 40001;
            //public static int CheckOutInfoAddr = 30139;
            public static int CheckOutPassAddr = 50002;
            public static int CheckOutFailAddr = 50003;
            #region 产能良率统计
            /// <summary>
            /// Pass数量地址
            /// </summary>
            public static int OkAddress = 10937;
            /// <summary>
            /// Fail数量地址
            /// </summary>
            public static int NgAddress = 10938;
            #endregion
        }
    }
}
