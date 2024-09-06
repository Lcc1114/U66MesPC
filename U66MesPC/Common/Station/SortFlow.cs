using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Common.Exceptions;
using U66MesPC.Model;

namespace U66MesPC.Common.Station
{
    /// <summary>
    /// 钢片下料机分拣NG料
    /// </summary>
    class SortFlow : StationBase
    {
        Dictionary<string, List<SNInfo>> CarrierAndSNMapping;
        public SortFlow(SysConfigs sysConfig) : base(sysConfig)
        {
            CarrierAndSNMapping = new Dictionary<string, List<SNInfo>>();
            Master.Initialzie();
            InitThread();
        }

        /// <summary>
        /// MES回复对应穴位的NG;OK位置
        /// </summary>
        /// <returns></returns>
        public override bool CheckIn()
        {
            try
            {
                bool ret = true;
                int bCheckIn = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
#if DEBUG
                Console.WriteLine($"CarrierCheck:{bCheckIn};");
#endif
                if (bCheckIn == 1)
                {
                    ret &= InnerCheckIn();
                }
                return ret;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }

        }
        public bool InnerCheckIn()
        {
            try
            {
                string carrierID = Master.GetStringFromMasterAndNotClear(PLCAddress.CarrierCheckAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "FromPLC", "载具码：" + carrierID);
                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);
                AddWriteLogInfo(Configs.StationID, "ToPLC", "Reset CheckOut flag");

                if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR"))
                    throw new ArgumentErrorException($"载具码({carrierID})无效！", "SN_CheckIN");
                else
                    Master.WriteRegister(PLCAddress.GetCodeSuccessly, 0);
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
                    string[] results = response.Result.Msg.Split('-');
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
                    //int addr = bAllPass ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                    //Master.WriteRegister(addr, 1); //写检查结果
                    if (bAllPass)
                    {
                        if (CarrierAndSNMapping.ContainsKey(carrierID))
                            CarrierAndSNMapping.Remove(carrierID);
                        CarrierAndSNMapping.Add(carrierID, list);
                    }
                    AddWriteLogInfo(Configs.StationID, "ToPLC", $"CarrierCheck result:{bAllPass}");
                }
                else
                {
                    //Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1); //写检查结果
                }
                return ret;
            }
            catch (Exception ex)
            {
                //Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1); //写检查结果
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
                    throw new ArgumentErrorException("出站失败：待出站信息为空");
                }
                //string carrierID = Master.GetStringFromMaster(PLCAddress.CheckOutInfoAddr, PLCAddress.CarrierInfoLength);
                //if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR"))
                //{
                //    throw new ArgumentErrorException("出站失败，载具码无效", "SN_CheckOut");
                //}           
                var mapping = CarrierAndSNMapping.FirstOrDefault();
                CarrierAndSNMapping.Remove(mapping.Key);
                if (!bSimulation && mapping.Value.Count == 0)
                    throw new ArgumentErrorException($"出站失败：载具{mapping.Key}上无有效的产品码");

                CheckOutRequest request = new CheckOutRequest(Configs, mapping.Key, Configs.Mold, 0, mapping.Value);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                //bool ret = GetRet(response.Result);
                bool ret = response.Result.SN_Info.Any(x => x.SNResult.ToUpper() == "PASS");
                if (bSimulation)
                    ret = true;
                //int addr = ret ? PLCAddress.CheckOutPassAddr : PLCAddress.CheckOutFailAddr;
                //Master.WriteRegister(addr, 1);
                AddWriteLogInfo(Configs.StationID, "ToPLC", $"Checkout result:{ret}");
                return ret;
            }
            catch (Exception ex)
            {
                //Master.WriteRegister(PLCAddress.CheckOutFailAddr, 1);
                HandleException(ex);
                return false;
            }
            finally
            {
                //Master.WriteRegister(PLCAddress.CheckOutAddr, 0);
                AddWriteLogInfo(Configs.StationID, "ToPLC", "Reset CheckOut flag");
                // SendStatusAndAlarm();
            }
        }
        public override void Release()
        {
            base.Release();
        }
    }
    /// <summary>
    ///   //192.168.0.190港片下料机；产品NG寄存器地址40001-4060； 1-NG;0&null-OK
    /// </summary>
    class PLCAddress
    {
        public static int CarrierCheckAddr = 0;//载具码扫码完成 读plc
        public static int GetCodeSuccessly = 1000;//接收到载具码
        public static int CarrierCheckInfoAddr = 3000;//载具条码地址
        public static int IsOrNotDrew1 = 40001;
        public static int IsOrNotDrew2 = 40002;
        public static int IsOrNotDrew3 = 40003;
        public static int IsOrNotDrew4 = 40004;
        public static int IsOrNotDrew5 = 40005;
        public static int IsOrNotDrew6 = 40006;
        public static int IsOrNotDrew7 = 40007;
        public static int IsOrNotDrew8 = 40008;
        public static int IsOrNotDrew9 = 40009;
        public static int IsOrNotDrew10 = 40010;
        public static int IsOrNotDrew11 = 40011;
        public static int IsOrNotDrew12 = 40012;
        public static int IsOrNotDrew13 = 40013;
        public static int IsOrNotDrew14 = 40014;
        public static int IsOrNotDrew15 = 40015;
        public static int IsOrNotDrew16 = 40016;
        public static int IsOrNotDrew17 = 40017;
        public static int IsOrNotDrew18 = 40018;
        public static int IsOrNotDrew19 = 40019;
        public static int IsOrNotDrew20 = 40020;
        public static int IsOrNotDrew21 = 40021;
        public static int IsOrNotDrew22 = 40022;
        public static int IsOrNotDrew23 = 40023;
        public static int IsOrNotDrew24 = 40024;
        public static int IsOrNotDrew25 = 40025;
        public static int IsOrNotDrew26 = 40026;
        public static int IsOrNotDrew27 = 40027;
        public static int IsOrNotDrew28 = 40028;
        public static int IsOrNotDrew29 = 40029;
        public static int IsOrNotDrew30 = 40030;
        public static int IsOrNotDrew31 = 40031;
        public static int IsOrNotDrew32 = 40032;
        public static int IsOrNotDrew33 = 40033;
        public static int IsOrNotDrew34 = 40034;
        public static int IsOrNotDrew35 = 40035;
        public static int IsOrNotDrew36 = 40036;
        public static int IsOrNotDrew37 = 40037;
        public static int IsOrNotDrew38 = 40038;
        public static int IsOrNotDrew39 = 40039;
        public static int IsOrNotDrew40 = 40040;
        public static int IsOrNotDrew41 = 40041;
        public static int IsOrNotDrew42 = 40042;
        public static int IsOrNotDrew43 = 40043;
        public static int IsOrNotDrew44 = 40044;
        public static int IsOrNotDrew45 = 40045;
        public static int IsOrNotDrew46 = 40046;
        public static int IsOrNotDrew47 = 40047;
        public static int IsOrNotDrew48 = 40048;
        public static int IsOrNotDrew49 = 40049;
        public static int IsOrNotDrew50 = 40050;
        public static int IsOrNotDrew51 = 40051;
        public static int IsOrNotDrew52 = 40052;
        public static int IsOrNotDrew53 = 40053;
        public static int IsOrNotDrew54 = 40054;
        public static int IsOrNotDrew55 = 40055;
        public static int IsOrNotDrew56 = 40056;
        public static int IsOrNotDrew57 = 40057;
        public static int IsOrNotDrew58 = 40058;
        public static int IsOrNotDrew59 = 40059;
        public static int IsOrNotDrew60 = 40060;

        public static int CarrierInfoLength = 64;
    }
}

