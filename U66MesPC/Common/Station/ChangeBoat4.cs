using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using U66MesPC.Common.Exceptions;
using U66MesPC.Model;
using U66MesPC.Dal;
using U66MesPC.Dal.Tool;

namespace U66MesPC.Common.Station
{
    /// <summary>
    /// boat6绑boat4工位
    /// </summary>
    public class ChangeBoat4 : StationBase
    {

        /// <summary>
        /// 用于存放boat6信息
        /// </summary>
        Dictionary<string, List<SNInfo>> Carrier6AndSNMapping;

        public ChangeBoat4(SysConfigs sysConfig) : base(sysConfig)
        {
            Carrier6AndSNMapping = new Dictionary<string, List<SNInfo>>();
            string info = ConfigurationManager.AppSettings["Carrier6AndSNMapping"];
            if (info != "" && info != "0")
            {
                Carrier6AndSNMapping = new JavaScriptSerializer().Deserialize<Dictionary<string, List<SNInfo>>>(info);
            }
            Master.Initialzie();
            Master2.Initialzie();
            //检查连接状态
            if (!Master.Initialized)
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "初始化", null, $"连接PLC失败，IP:{Master.GetIP()},端口号：{Master.GetPort()};");
            if (!Master2.Initialized)
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "初始化", null, $"连接PLC失败，IP:{Master2.GetIP()},端口号：{Master2.GetPort()};");
            InitThread();
        }

        public override void InitThread()
        {
            base.InitThread();
            InitNewThread();
        }

        public override bool CheckIn()
        {
            try
            {
                bool ret = false;
                if (Master.Initialized)
                {
                    int bCheckIn = Master.ReadHoldingRegisters(PLCAddress.Carrier6CheckAddr, 1)[0];

                    if (bCheckIn == 1)//boat6扫码进站
                    {
                        Master.WriteRegister(PLCAddress.Carrier6CheckAddr, 0);
                        //空跑
                        string val = ConfigurationManager.AppSettings["IsNullRun"];
                        if (val == "1")
                        {
                            Master.WriteRegister(PLCAddress.Carrier6CheckPassAddr, 1); //写检查结果
                            Master.WriteRegister(PLCAddress.CheckOut6FailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= InnerCheckIn(); //boat6入
                    }

                }
                return ret;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }

        }

        public override bool CheckOut()
        {
            try
            {
                bool ret = true;
                if (Master2.Initialized) //采集信息号前判断有没有连接
                {
                    int bCheckCarrier4 = Master2.ReadHoldingRegisters(PLCAddress.Carrier4CheckAddr, 1)[0];
                    int bCheckOut4 = Master2.ReadHoldingRegisters(PLCAddress.CheckOut4Addr, 1)[0];
                    string val = ConfigurationManager.AppSettings["IsNullRun"];
                    if (bCheckCarrier4 == 1)
                    {
                        Master2.WriteRegister(PLCAddress.Carrier4CheckAddr, 0);//重置载具检查标志位
                        if (val == "1")
                        {
                            Master2.WriteRegister(PLCAddress.Carrier4CheckPassAddr, 1);
                            Master2.WriteRegister(PLCAddress.Carrier4CheckFailInfoAddr, 0);
                        }
                        else
                            ret &= CarrierCheck(); //boat4入站
                    }
                    if (bCheckOut4 == 1)
                    {
                        Master2.WriteRegister(PLCAddress.CheckOut4Addr, 0);//重置检查请求标志
                        //空跑
                        if (val == "1")
                        {
                            Master2.WriteRegister(PLCAddress.CheckOut4PassAddr, 1); //写检查结果
                            Master2.WriteRegister(PLCAddress.CheckOut4FailInfoAddr, 0); //写异常原因
                        }
                        else
                            InnerCheckOut(); //新的boat6绑boat4
                    }

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
        /// boat6绑boat4，然后boat4出站
        /// </summary>
        /// <returns></returns>
        private bool InnerCheckOut()
        {
            int resultID = 1; //默认软件异常
            try
            {
                AddReadLogInfo(Configs.StationID, "读取PLC", "读取到ChangeBoat4工站boat6绑boat4信号");
                CarrierIDBindingBot6 carrierIDBindingBot6 = new CarrierIDBindingBot6();

                //读取boat6载具码和boat4载具码。
                string carrier4ID = Master2.GetStringFromMaster(PLCAddress.Carrier4CheckOutInfoAddr, PLCAddress.CarrierInfoLength);//读取boat4载具码
                string carrier6ID = Master2.GetStringFromMaster(PLCAddress.Carrier6CheckOutInfoAddr, PLCAddress.CarrierInfoLength);//读取boat6载具码
                AddReadLogInfo(Configs.StationID, "读取PLC", $"ChangeBoat4工站,Boat6：{carrier6ID},Boat4：{carrier4ID}");

                if (!CheckString(carrier4ID))
                {
                    resultID = 5; //boat4载具码为空
                    throw new ArgumentErrorException($"Boat4载具码({carrier4ID})无效！为空", "读取PLC二维码");
                }
                if (!CheckString(carrier6ID))
                {
                    resultID = 6; //boat6载具码为空
                    throw new ArgumentErrorException($"Boat6载具码({carrier6ID})无效！为空", "读取PLC二维码");
                }
                //if (!Carrier6AndSNMapping.ContainsKey(carrier6ID))
                //{
                //    resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                //    throw new ArgumentErrorException("Boat6&Boat4绑定失败！Boat6的产品信息为空。请确认机台上二维码是否与实物相匹配", "boat6获取信息");
                //}
                CarrierIDBindingBot6Tool carrierIDBindingBot6Tool = new CarrierIDBindingBot6Tool();
                carrierIDBindingBot6 = carrierIDBindingBot6Tool.Query(carrier6ID);
                if (carrierIDBindingBot6 == null)
                {
                    resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                    throw new ArgumentErrorException("Boat6&Boat4绑定失败！Boat6的产品信息为空。请确认机台上二维码是否与实物相匹配", "boat6获取信息");
                }

                //取出boat6产品信息
                List<SNInfo> snInfosList = new JavaScriptSerializer().Deserialize<List<SNInfo>>(carrierIDBindingBot6?.SNData);
                CarrierBindRequest request = new CarrierBindRequest(Configs, carrier6ID, carrier4ID, "Change", "");
                Task<CarrierBindResponse> response = HttpClientHelper.SNCarrierBindAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);

                bool ret = GetRetNew(response.Result);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"Boat6&Boat4绑定结果：{ret};Boat6:{carrier6ID};Boat4:{carrier4ID}");
                if (ret) //boat6绑boat4成功
                {
                    ShowLogInfo(Configs.StationID, "boat4出站", EventIO.信息, "ChangeBoat4工站上料Boat4出站");
                    string carrierID = carrier4ID;
                    CheckOutRequest request1 = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList);  //拿boat4的治具码与缓存的boat6上的产品信息出站
                    Task<CheckOutResponse> response1 = HttpClientHelper.SNCheckOutAsync(request1, Configs.Url);
                    ret = GetRet(response1.Result);
                    if (ret)
                    {
                        //Carrier6AndSNMapping.Remove(carrier6ID);
                        carrierIDBindingBot6Tool.Delete(carrier6ID);
                    }
                }
                else
                {
                    resultID = 2; //Boat6&Boat4失败
                    throw new ArgumentErrorException($"Boat6&Boat4绑定失败！错误信息：{response.Result.Msg}", "Boat6&Boat4绑定");
                }
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"boat4出站结果:{resultInfo};boat4治具码:{carrier4ID}");
                int addr = ret ? PLCAddress.CheckOut4PassAddr : PLCAddress.CheckOut4FailAddr;
                Master2.WriteRegister(addr, 1); //写检查结果
                resultID = ret ? 0 : 3; //3 出站失败
                Master2.WriteRegister(PLCAddress.CheckOut4FailInfoAddr, resultID); //写异常原因
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.CheckOut4FailAddr, 1); //写检查结果
                Master2.WriteRegister(PLCAddress.CheckOut4FailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }

        //boat6扫码进站
        private bool InnerCheckIn()
        {
            int resultID = 1;
            try
            {
                string carrierID = Master.GetStringFromMaster(PLCAddress.Carrier6CheckInfoAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "读取PLC", $"读取到ChangeBoat4工站boat6扫码完成信号。载具码(boat6)：{carrierID}");
                if (!CheckString(carrierID))
                {
                    resultID = 3; //载具码为空
                    throw new ArgumentErrorException($"boat6载具码( {carrierID} )无效！", "检查载具码");
                }
                CarrierID = carrierID;
                bool ret = false;

                //if (Carrier6AndSNMapping.Keys.Contains(carrierID)) //有重码的移除再进站
                //    Carrier6AndSNMapping.Remove(carrierID);
                CarrierIDBindingBot6Tool carrierIDBindingBot6Tool = new CarrierIDBindingBot6Tool();
                if (carrierIDBindingBot6Tool.Query(carrierID) != null)
                {
                    carrierIDBindingBot6Tool.Delete(carrierID);
                }
                CheckInRequest request = new CheckInRequest(Configs, carrierID, "NULL");
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                ret = GetRet(response.Result);
                //if (ret)
                //{
                List<SNInfo> list = new List<SNInfo>();
                SN = response.Result.SN;
                string[] productsSN = response.Result.SN.Split('-');
                string[] results = response.Result.Msg.Split(',');

                for (int i = 0; i < Math.Min(productsSN.Length, results.Length); i++)
                {
                    var sn = (i < productsSN.Length) ? productsSN[i].Split('#')[0] : "";
                    var result = results[i];
                    //过滤
                    if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                        continue;
                    else
                        list.Add(new SNInfo(sn, result, new List<DCInfo>() { }, new List<CompInfo>() { }));
                }
                //检查单个SN的结果
                if (list.Count(s => s.Result.ToUpper() == "PASS") == 0)
                {
                    resultID = 2;
                    throw new ArgumentErrorException($"进站失败。ChangeBost4上料载具码({carrierID})入站校验NG！该载具上的产品全部NG", "载具码入站");
                }
                if (list.Count > 0)
                {
                    //Carrier6AndSNMapping.Add(carrierID, list); //将载具码与产品码存起来
                    //string value = new JavaScriptSerializer().Serialize(Carrier6AndSNMapping);
                    //UpdateConfig("Carrier6AndSNMapping", value);
                    string value = new JavaScriptSerializer().Serialize(list);
                    carrierIDBindingBot6Tool.Insert(new CarrierIDBindingBot6() { CarrierID = carrierID, SNData = value });
                }
                else
                {
                    resultID = 4;
                    throw new ArgumentErrorException($"进站失败：Boat6载具码上产品信息为空。信息个数:{list.Count}", "boat6校验");
                }
                //}
                ret = true;
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"ChangeBoat4工站boat6入站结果:{resultInfo};boat6治具码:{carrierID}");
                int addr = ret ? PLCAddress.Carrier6CheckPassAddr : PLCAddress.Carrier6CheckFailAddr;
                Master.WriteRegister(addr, 1); //写检查结果
                resultID = ret ? 0 : 2; //2.校验未通过
                Master.WriteRegister(PLCAddress.CheckOut6FailInfoAddr, resultID); //写异常原因
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.Carrier6CheckFailAddr, 1); //写检查结果
                Master.WriteRegister(PLCAddress.CheckOut6FailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }

        //boat4入站
        private bool CarrierCheck() //只校验，不存信息
        {
            int resultID = 1;
            try
            {
                string carrierID = Master2.GetStringFromMaster(PLCAddress.Carrier4CheckInfoAddr, PLCAddress.CarrierInfoLength);//读取boat4载具码
                AddReadLogInfo(Configs.StationID, "读取PLC", $"读取到ChangeBoat4工站boat4扫码完成信号。载具码(boat4)：{carrierID}");

                if (!CheckString(carrierID))
                {
                    resultID = 3; //载具码为空
                    throw new ArgumentErrorException($"Changeboat4入站Boat4载具码({carrierID})无效！", "检查载具码");
                }
                CarrierID = carrierID;

                CarrierCheckRequest request = new CarrierCheckRequest(Configs, carrierID);
                Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool result = GetRet(response.Result);

                string resultInfo = result ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"Changeboat4入站(Boat4)载具码校验结果:{resultInfo};载具码:{carrierID}");
                int addr = result ? PLCAddress.Carrier4CheckPassAddr : PLCAddress.Carrier4CheckFailAddr;
                Master2.WriteRegister(addr, 1);
                resultID = result ? 0 : 2; //2.校验未通过
                Master2.WriteRegister(PLCAddress.Carrier4CheckFailInfoAddr, resultID); //写异常原因
                return result;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.Carrier4CheckFailAddr, 1);
                Master2.WriteRegister(PLCAddress.Carrier4CheckFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }

        class PLCAddress
        {
            /// <summary>
            /// Boat6扫码入站
            /// </summary>
            public static int Carrier6CheckAddr = 0;
            public static int Carrier6CheckInfoAddr = 3000;
            public static int Carrier6CheckPassAddr = 1000;
            public static int Carrier6CheckFailAddr = 1001;
            public static int CheckOut6FailInfoAddr = 1004; //1软件运行异常2校验未通过 3.boat6载具码为空 4.载具码上产品信息为空

            /// <summary>
            /// Boat4扫码入站
            /// </summary>
            public static int Carrier4CheckAddr = 0;
            public static int Carrier4CheckInfoAddr = 3000;
            public static int Carrier4CheckPassAddr = 1000;
            public static int Carrier4CheckFailAddr = 1001;
            public static int Carrier4CheckFailInfoAddr = 1004; //1软件运行异常2校验未通过 3.boat4载具码为空 

            /// <summary>
            /// Boat4出站
            /// </summary>
            public static int CheckOut4Addr = 1;
            public static int CheckOut4PassAddr = 1002;
            public static int CheckOut4FailAddr = 1003;
            public static int CheckOut4FailInfoAddr = 1005; //boat4出站报错代码 1.软件运行NG 2.绑定失败 3.出站失败 4.二维码不一致 5
            public static int Carrier6CheckOutInfoAddr = 3128; //boat6码                                            //
            public static int Carrier4CheckOutInfoAddr = 3064; //boat4码

            public static int CarrierInfoLength = 64;
        }
    }
}
