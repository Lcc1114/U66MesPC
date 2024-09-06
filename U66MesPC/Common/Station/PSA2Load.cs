using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;
using U66MesPC.Common.Exceptions;
using U66MesPC.Dal;
using U66MesPC.Dal.Tool;
using U66MesPC.Model;
using Timer = System.Timers.Timer;

namespace U66MesPC.Common.Station
{

    /// <summary>
    /// 对应工站：Boat5&Boat6 倒盘，所在模组：PSA Attach
    /// </summary>
    public class PSA2Load : StationBase
    {
        /// <summary>
        /// 用于存放boat5信息
        /// </summary>
        Dictionary<string, List<SNInfo>> Carrier5AndSNMapping;

        public override void SNCheckInAsync()
        {
            //Master2.WriteRegister(PLCAddress.Carrier6CheckFailAddr, 1);
            CheckInRequest request = new CheckInRequest(Configs, "CSF-U66-31HBB1-027", "NULL");
            Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
        }
        public PSA2Load(SysConfigs sysConfig) : base(sysConfig)
        {
            Carrier5AndSNMapping = new Dictionary<string, List<SNInfo>>();
            string info = ConfigurationManager.AppSettings["Carrier5AndSNMapping"];
            if (info != "" && info != "0")
            {
                Carrier5AndSNMapping = new JavaScriptSerializer().Deserialize<Dictionary<string, List<SNInfo>>>(info);
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
        /// <summary>
        /// MES回复对应穴位的CAP SN
        /// </summary>
        /// <returns></returns>
        public override bool CheckIn()
        {
            try
            {
                bool ret = true;
                if (Master.Initialized) //采集信息号前判断有没有连接
                {
                    int bCheckCarrier5 = Master.ReadHoldingRegisters(PLCAddress.Carrier5CheckAddr, 1)[0];

                    if (bCheckCarrier5 == 1)
                    {
                        Master.WriteRegister(PLCAddress.Carrier5CheckAddr, 0);
                        //空跑
                        string val = ConfigurationManager.AppSettings["IsNullRun"];
                        if (val == "1")
                        {
                            Master.WriteRegister(PLCAddress.Carrier5CheckPassAddr, 1); //写检查结果
                            Master.WriteRegister(PLCAddress.Carrier5CheckFailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= InnerCheckIn(); //载具5校验
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
        /// SN放CAP SN，CarrierID栏位放Boat6 SN出站；
        /// </summary>
        /// <returns></returns>
        public override bool CheckOut()
        {
            try
            {
                bool ret = true;
                if (Master2.Initialized) //采集信息号前判断有没有连接
                {
                    int bCheckCarrier6 = Master2.ReadHoldingRegisters(PLCAddress.Carrier6CheckAddr, 1)[0];
                    int bCheckOut = Master2.ReadHoldingRegisters(PLCAddress.CheckOut6Addr, 1)[0];
                    string val = ConfigurationManager.AppSettings["IsNullRun"];
                    if (bCheckCarrier6 == 1)
                    {
                        Master2.WriteRegister(PLCAddress.Carrier6CheckAddr, 0);//重置载具检查标志位
                        //空跑
                        if (val == "1")
                        {
                            Master2.WriteRegister(PLCAddress.Carrier6CheckPassAddr, 1);
                            Master2.WriteRegister(PLCAddress.Carrier6CheckFailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= CarrierCheck(); //载具6合法性核对
                    }
                    if (bCheckOut == 1)
                    {
                        Master2.WriteRegister(PLCAddress.CheckOut6Addr, 0);//重置检查请求标志
                        //空跑
                        if (val == "1")
                        {
                            Master2.WriteRegister(PLCAddress.CheckOut6PassAddr, 1); //写检查结果
                            Master2.WriteRegister(PLCAddress.CheckOut6FailInfoAddr, 0); //写异常原因
                        }
                        else
                            InnerCheckOut();
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


        //核对boat5上所有产品状态，保存产品SN 
        public bool InnerCheckIn()
        {
            //// 1软件运行异常 2校验未通过 3.boat4载具码为空 4.载具码上产品信息为空
            int resultID = 1;
            try
            {
                string carrierID = Master.GetStringFromMaster(PLCAddress.Carrier5CheckInfoAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "读取PLC", "PSA2_Load工站上料载具boat5扫码完成信号。载具(boat5)码：" + carrierID);

                if (!CheckString(carrierID))
                {
                    resultID = 3; //载具码为空
                    throw new ArgumentErrorException($"进站失败：Boat5载具码({carrierID})无效！", "boat5入站校验");
                }
                CarrierID = carrierID;
                bool ret = false;

                //if (Carrier5AndSNMapping.Keys.Contains(carrierID)) //移除已存在的boat5
                //    Carrier5AndSNMapping.Remove(carrierID);
                CarrierIDBindingBot5Tool carrierIDBindingBot5Tool = new CarrierIDBindingBot5Tool();
                if (carrierIDBindingBot5Tool.Query(carrierID) != null)
                    carrierIDBindingBot5Tool.Delete(carrierID);
                CheckInRequest request = new CheckInRequest(Configs, carrierID, "NULL");
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                ret = GetRet(response.Result);
                //ret = true;  //测试

                List<SNInfo> list = new List<SNInfo>();
                SN = response.Result.SN;
                string[] productsSN = response.Result.SN.Split('-');
                string[] results = response.Result.Msg.Split(',');
                for (int i = 0; i < Math.Max(productsSN.Length, results.Length); i++)
                {
                    var sn = (i < productsSN.Length) ? productsSN[i].Split('#')[0] : "";
                    var result = results[i];
                    if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                        continue;
                    else
                        list.Add(new SNInfo(sn, result, new List<DCInfo>() { }, new List<CompInfo>() { }));
                }
                //检查单个SN的结果
                if (list.Count(s => s.Result.ToUpper() == "PASS") == 0)
                {
                    resultID = 2;
                    throw new ArgumentErrorException($"进站失败。PSA2上料载具码({carrierID})入站校验NG！该载具上的产品全部NG", "载具码入站");
                }
                if (list.Count > 0)
                {
                    string value = new JavaScriptSerializer().Serialize(list);
                    carrierIDBindingBot5Tool.Insert(new CarrierIDBindingBot5() { CarrierID = carrierID, SNData = value });
                    //Carrier5AndSNMapping.Add(carrierID, list); //将载具码与产品码存起来
                    //string value = new JavaScriptSerializer().Serialize(Carrier5AndSNMapping);
                    //UpdateConfig("Carrier5AndSNMapping", value);
                }
                else  //测试
                {
                    resultID = 4;
                    throw new ArgumentErrorException($"进站失败：Boat5载具码上产品信息为空。信息个数:{list.Count}", "boat5校验");
                }
                ret = true;
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2_Load工站上料载具boat5校验结果:{resultInfo}；载具码:{carrierID}");
                int addr = ret ? PLCAddress.Carrier5CheckPassAddr : PLCAddress.Carrier5CheckFailAddr;
                Master.WriteRegister(addr, 1); //写检查结果
                resultID = ret ? 0 : 2; //2.校验未通过
                Master.WriteRegister(PLCAddress.Carrier5CheckFailInfoAddr, resultID); //写异常原因
                int okCount = results.Where(x => x.ToLower() == "pass").Count();
                Master.WriteRegister(PLCAddress.OkAddress, okCount);
                Master.WriteRegister(PLCAddress.NgAddress, results.Length - okCount);
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.Carrier5CheckFailAddr, 1); //写检查结果
                Master.WriteRegister(PLCAddress.Carrier5CheckFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }
        /// <summary>
        /// 核对载具boat6的合法性
        /// </summary>
        /// <returns></returns>
        public bool CarrierCheck()
        {
            int resultID = 1;
            try
            {
                string carrierID = Master2.GetStringFromMaster(PLCAddress.Carrier6CheckInfoAddr, PLCAddress.CarrierInfoLength);//读取载具码
                AddReadLogInfo(Configs.StationID, "读取PLC", "PSA2_Load工站上料载具boat6扫码完成信号。载具(Boat6)码：" + carrierID);

                if (!CheckString(carrierID))
                {
                    resultID = 3; //载具码为空
                    throw new ArgumentErrorException($"Boat6载具码({carrierID})无效！为空", "Carrier_Check");
                }
                CarrierID = carrierID;
                CarrierCheckRequest request = new CarrierCheckRequest(Configs, carrierID);
                Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool result = GetRet(response.Result);
                //result = true;  //测试
                string resultInfo = result ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2_Load工站载具(Boat6)校验结果:{resultInfo};治具码:{carrierID}");
                int addr = result ? PLCAddress.Carrier6CheckPassAddr : PLCAddress.Carrier6CheckFailAddr;
                Master2.WriteRegister(addr, 1);
                resultID = result ? 0 : 2; //2.校验未通过
                Master2.WriteRegister(PLCAddress.Carrier6CheckFailInfoAddr, resultID); //写异常原因
                return result;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.Carrier6CheckFailAddr, 1);
                Master2.WriteRegister(PLCAddress.Carrier6CheckFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }


        /// <summary>
        /// 绑定Boat5&Boat6，然后boat6出站。SN栏位放Boat5 SN，CarrierID栏位放Boat6 SN，BindType栏位放Change
        /// </summary>
        /// <returns></returns>
        public bool InnerCheckOut()
        {
            int resultID = 1; //默认软件异常
            try
            {
                AddReadLogInfo(Configs.StationID, "读取PLC", "读取到PSA2_Load工站boat5绑boat6信号");
                CarrierIDBindingBot5 carrierIDBindingBot5 = new CarrierIDBindingBot5();
                CarrierIDBindingNoNumber50 carrierIDBindingNoNumber = new CarrierIDBindingNoNumber50();
                //读取boat5载具码和boat6载具码。
                string carrier5ID = Master2.GetStringFromMaster(PLCAddress.Carrier5CheckOutInfoAddr, PLCAddress.CarrierInfoLength);//读取boat5载具码
                string carrier6ID = Master2.GetStringFromMaster(PLCAddress.Carrier6CheckOutInfoAddr, PLCAddress.CarrierInfoLength);//读取boat6载具码
                AddReadLogInfo(Configs.StationID, "读取PLC", $"PSA2_Load工站,Boat5：{carrier5ID},Boat6：{carrier6ID}");
                if (!CheckString(carrier5ID))
                {
                    resultID = 5; //boat5载具码为空
                    throw new ArgumentErrorException($"Boat5载具码({carrier5ID})无效！", "读取PLC二维码");
                }
                if (!CheckString(carrier6ID))
                {
                    resultID = 6; //boat6载具码为空
                    throw new ArgumentErrorException($"Boat6载具码({carrier6ID})无效！", "读取PLC二维码");
                }
                //if (!Carrier5AndSNMapping.ContainsKey(carrier5ID))
                //{
                //    resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                //    throw new ArgumentErrorException("Boat5&Boat6绑定失败！Boat5的产品信息为空。请确认机台上二维码是否与实物相匹配", "boat5获取信息");
                //}
                CarrierIDBindingBot5Tool carrierIDBindingBot5Tool = new CarrierIDBindingBot5Tool();
                carrierIDBindingBot5 = carrierIDBindingBot5Tool.Query(carrier5ID);
                if (carrierIDBindingBot5 == null)
                {
                    resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                    throw new ArgumentErrorException("Boat5&Boat6绑定失败！Boat5的产品信息为空。请确认机台上二维码是否与实物相匹配", "boat5获取信息");
                }
                //检查流道ID是否存在
                //if (!PSA1Load.CarrierAndNumberMapping.ContainsKey(carrier5ID))
                //{
                //    resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                //    throw new ArgumentErrorException("Boat5&Boat6绑定失败！Boat5的流道信息为空。请确认机台上二维码是否与实物相匹配", "boat5获取流道信息");
                //}
                CarrierIDBindingNoNumber50Tool carrierIDBindingNoNumberTool = new CarrierIDBindingNoNumber50Tool();
                carrierIDBindingNoNumber = carrierIDBindingNoNumberTool.Query(carrier5ID);//carrier6ID
                if (carrierIDBindingNoNumber == null)
                {
                    carrierIDBindingNoNumber = new CarrierIDBindingNoNumber50();
                    carrierIDBindingNoNumber.NoNumber = "1";
                    ShowLogInfo(Configs.StationID, "PSA2保压出站", EventIO.Warn, $"载具{carrier5ID}的流道信息为空。默认给1");
                    //resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                    //throw new ArgumentErrorException("Boat5&Boat6绑定失败！Boat5的流道信息为空。请确认机台上二维码是否与实物相匹配", "boat5获取流道信息");
                }
                List<SNInfo> snInfosList = new JavaScriptSerializer().Deserialize<List<SNInfo>>(carrierIDBindingBot5?.SNData);
                //List<SNInfo> snInfosList = Carrier5AndSNMapping[carrier5ID];
                //boat5绑boat6
                CarrierBindRequest request = new CarrierBindRequest(Configs, carrier5ID, carrier6ID, "Change", "");
                Task<CarrierBindResponse> response = HttpClientHelper.SNCarrierBindAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRetNew(response.Result);
                ShowLogInfo(Configs.StationID, "boat5绑boat6", EventIO.信息, $"PSA2_Load工站boat5绑boat6结果：{ret};Boat5:{carrier5ID};Boat6:{carrier6ID}");
                //ret = true; //测试
                if (ret) //boat5绑boat6成功
                {
                    //boat5绑boat6后boat6直接出站
                    ShowLogInfo(Configs.StationID, "boat6出站", EventIO.信息, "PSA2_Load工站上料Boat6出站");
                    string carrierID = carrier6ID;
                    //获取流道ID
                    string flowID = carrierIDBindingNoNumber.NoNumber;
                    CheckOutRequest request1 = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList, flowID);
                    Task<CheckOutResponse> response1 = HttpClientHelper.SNCheckOutAsync(request1, Configs.Url);
                    ret = GetRet(response1.Result);
                    //ret = true; //测试
                    if (ret)
                    {
                        //Carrier5AndSNMapping.Remove(carrier5ID); //成功后清除二维码数据,PLC那边可能再手动出站,反正进站的时候会判断是否有重码
                        //PSA1Load.CarrierAndNumberMapping.Remove(carrier5ID);
                        carrierIDBindingBot5Tool.Delete(carrier5ID);
                        carrierIDBindingNoNumberTool.Delete(carrier5ID);
                    }
                }
                else
                {
                    resultID = 2; //boat5绑boat6失败
                    throw new ArgumentErrorException($"Boat5&Boat6绑定失败！错误信息：{response.Result.Msg}", "Boat5&Boat6绑定");
                }
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2_Load工站上料Boat6出站结果:{resultInfo};载具码:{carrier6ID}");
                int addr = ret ? PLCAddress.CheckOut6PassAddr : PLCAddress.CheckOut6FailAddr;
                Master2.WriteRegister(addr, 1); //写检查结果
                resultID = ret ? 0 : 3; //3 出站失败
                Master2.WriteRegister(PLCAddress.CheckOut6FailInfoAddr, resultID); //写异常原因
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.CheckOut6FailAddr, 1); //写检查结果
                Master2.WriteRegister(PLCAddress.CheckOut6FailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }

        public override void Release()
        {
            base.Release();
        }
        class PLCAddress
        {
            /// <summary>
            /// boat5扫码入站
            /// </summary>
            public static int Carrier5CheckAddr = 0;
            public static int Carrier5CheckInfoAddr = 3000;
            public static int Carrier5CheckPassAddr = 1000;
            public static int Carrier5CheckFailAddr = 1001;
            public static int Carrier5CheckFailInfoAddr = 1006;// 1软件运行异常2校验未通过 3.boat4载具码为空 4.载具码上产品信息为空

            /// <summary>
            /// boat6扫码入站
            /// </summary>
            public static int Carrier6CheckAddr = 0;
            public static int Carrier6CheckInfoAddr = 3000;
            public static int Carrier6CheckPassAddr = 1000;
            public static int Carrier6CheckFailAddr = 1001;
            public static int Carrier6CheckFailInfoAddr = 1006; //1软件运行异常2校验未通过 3.载具码为空

            /// <summary>
            /// boat6出站
            /// </summary>
            public static int CheckOut6Addr = 1;
            public static int CheckOut6PassAddr = 1002;
            public static int CheckOut6FailAddr = 1003;
            public static int CheckOut6FailInfoAddr = 1007; //1.软件运行NG 2.绑定失败 3.出站失败 4.二维码与实物不一致 5.boat5二维码为空 6.boat6二维码为空
            public static int Carrier5CheckOutInfoAddr = 3128;
            public static int Carrier6CheckOutInfoAddr = 3064;

            public static int CarrierInfoLength = 64;

            public static int CheckIsNullRun = 5;
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
