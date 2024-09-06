using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
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
    /// boat4下料工位
    /// </summary>
    public class PSA2Unload : StationBase
    {
        public Dictionary<string, List<SNInfo>> CarrierUnloadAndSNMapping;
        public Dictionary<string, string> CarrierUnloadAndSNExistsMapping;
        public PSA2Unload(SysConfigs sysConfig) : base(sysConfig)
        {
            CarrierUnloadAndSNMapping = new Dictionary<string, List<SNInfo>>();
            string info = ConfigurationManager.AppSettings["CarrierUnloadAndSNMapping"];
            if (info != "" && info != "0")
            {
                CarrierUnloadAndSNMapping = new JavaScriptSerializer().Deserialize<Dictionary<string, List<SNInfo>>>(info);
            }
            string info1 = ConfigurationManager.AppSettings["CarrierUnloadAndSNExistsMapping"];
            if (info != "" && info != "0")
            {
                CarrierUnloadAndSNExistsMapping = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(info);
            }
            Master.Initialzie();
            //检查连接状态
            if (!Master.Initialized)
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "初始化", null, $"连接PLC失败，IP:{Master.GetIP()},端口号：{Master.GetPort()};");
            Master2.Initialzie();
            //检查连接状态
            if (!Master2.Initialized)
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "初始化", null, $"连接PLC失败，IP:{Master2.GetIP()},端口号：{Master2.GetPort()};");
            InitThread();
            PSA2UnloadReadToolingSN();
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
                    int bCheckIn = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
                    int bCheckOut = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr, 1)[0];
                    string val = ConfigurationManager.AppSettings["IsNullRun"];
                    if (bCheckIn == 1)
                    {
                        Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);
                        //空跑
                        if (val == "1")
                        {
                            int[] sendInfo = new int[30];
                            for (int i = 0; i < 60; i += 2)
                            {
                                sendInfo[i / 2] = 0;
                            }
                            Master.WriteMultiRegisters(PLCAddress.SNInfoAddr, sendInfo);
                            Master.WriteRegister(PLCAddress.CarrierCheckPassAddr, 1); //写检查结果
                            //Master.WriteRegister(PLCAddress.CarrierCheckFailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= InnerCheckIn();
                    }
                    if (bCheckOut == 1)
                    {
                        Master.WriteRegister(PLCAddress.CheckOutAddr, 0);
                        if (val == "1")
                        {
                            Master.WriteRegister(PLCAddress.CheckOutPassAddr, 1);
                            Master.WriteRegister(PLCAddress.CheckOutFailInfoAddr, 0);
                        }
                        else
                            ret &= InnerCheckOut();
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

        public bool InnerCheckIn()
        {
            int resultID = 1; //1软件运行异常 2校验未通过 3.载具码为空  4.载具颜色不一致
            try
            {
                for (int i = 0; i < PSA2Unloadls.Count; i++)
                {
                    if (!string.IsNullOrEmpty(PSA2Unloadls[i]))
                    {
                        FeedingCheckRequest requestTooling = new FeedingCheckRequest(Configs, PSA2Unloadls[i]);
                        Task<FeedingCheckResponse> responseTooling = HttpClientHelper.SNFeedingCheckAsync(requestTooling, Configs.Url);
                        CheckMesConnectStatus(responseTooling.Result);
                        bool ret1 = GetRetNew(responseTooling.Result);
                        if (!ret1)
                        {
                            resultID = 2;
                            throw new ArgumentErrorException($"吸嘴码({PSA2Unloadls[i]})入站失败！错误信息：{responseTooling.Result.Msg}", "SN_FeedingCheck");
                        }
                    }
                }

                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "读取PLC", $"读取到PSA2_Unload工站载具扫码完成信号。载具码：{carrierID}");
                if (!CheckString(carrierID))
                {
                    resultID = 3;
                    throw new ArgumentErrorException($"进站失败：载具码({carrierID})无效！", "SN_CheckIN");
                }
                //if (CarrierUnloadAndSNMapping.ContainsKey(carrierID))
                //    CarrierUnloadAndSNMapping.Remove(carrierID);
                CarrierIDBindingBot4Tool carrierIDBindingBot4Tool = new CarrierIDBindingBot4Tool();
                if (carrierIDBindingBot4Tool.Query(carrierID) != null)
                    carrierIDBindingBot4Tool.Delete(carrierID);
                CarrierID = carrierID;
                CheckInRequest request = new CheckInRequest(Configs, carrierID, "NULL");
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRet(response.Result);
                //if (!ret)
                //{
                //    resultID = 2;
                //    throw new ArgumentErrorException($"PSA2_Unload工站入站失败！错误信息：{response.Result.Msg}", "PSA2_Unload入站");
                //}
                //60个产品，发给PLC有无，有为1，无为2
                int[] SNInfo = new int[60]; //有无产品
                int[] NGInfo = new int[60]; //NG产品
                StringBuilder sbNGInfo = new StringBuilder();
                List<SNInfo> list = new List<SNInfo>();
                List<string> SnColorList = new List<string>(); //产品信息集合
                SN = response.Result.SN;
                carrierID = response.Result.CarrierID;
                string[] productsSN = response.Result.SN.Split('-');
                string[] results = response.Result.Msg.Split(',');
                //获取ToolingSN
                //List<string> ls = new List<string>();
                //string path = Directory.GetCurrentDirectory() + "\\ToolingSN.txt";
                //if (File.Exists(path))
                //{
                //    foreach (string item in File.ReadAllLines(path))
                //    {
                //        ls.Add(item);
                //    }
                //}
                bool pass = false;
                for (int i = 0; i < Math.Min(productsSN.Length, results.Length); i++)
                {
                    var sn = (i < productsSN.Length) ? productsSN[i].Split('#')[0] : "";
                    var result = (i < results.Length) ? results[i] : "";
                    if (i < productsSN.Length && productsSN[i].Split('#').Length > 1)
                        SnColorList.Add(productsSN[i].Split('#')[1]);
                    if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                    {
                        SNInfo[i] = 0;
                        NGInfo[i] = 2;
                        continue;
                    }
                    else
                    {
                        pass = result.ToUpper() == "PASS";
                        SNInfo[i] = pass ? 1 : 0;
                        NGInfo[i] = pass ? 2 : 1;
                        //if (!pass)
                        //    sbNGInfo.Append($"{i}-");
                        List<CompInfo> compInfos = new List<CompInfo> { new CompInfo(GetToolingSN(i, PSA2Unloadls), 0) };
                        list.Add(new SNInfo(sn, result, new List<DCInfo>() { }, compInfos));
                    }
                    if (NGInfo[i] == 1)
                        sbNGInfo.Append($"{i + 1}-");
                }
                //检查单个SN的结果
                if (list.Count(s => s.Result.ToUpper() == "PASS") == 0)
                {
                    resultID = 2;
                    throw new ArgumentErrorException($"进站失败。PSA2_Unload上料载具码({carrierID})入站校验NG！该载具上的产品全部NG", "载具码入站");
                }
                //提取载具上的产品颜色。 WH1H1D00J150000HYY + 152 + 05A1
                int colorID = -1;
                if (SnColorList.Count > 0)
                {
                    bool retBool = !SnColorList.Any(s => s != SnColorList[0]);
                    if (retBool) //所有产品同一种颜色
                    {
                        if (!dicColor.Keys.Contains(SnColorList[0]))
                        {
                            ret = false;
                            resultID = 1;
                            throw new ArgumentErrorException($"载具码{carrierID}入站失败！提取产品颜色编码({SnColorList[0]})未在颜色列表中", "产品颜色提取");
                        }
                        ret = true;
                        colorID = dicColor[SnColorList[0]];
                        Master.WriteRegister(PLCAddress.CarrierColorAddr, colorID);

                    }
                    else //载具不合规，混入了不同颜色的产品
                    {
                        //发送ng给PLC
                        ret = false;
                        resultID = 4;
                        ShowLogInfo(Configs.StationID, "产品颜色校验", EventIO.错误, $"成品下料工位载具中混入了不同颜色产品。载具码({carrierID})；{SnColorList.Aggregate("产品颜色：", (s, b) => s + $"{b},")}");
                        throw new ArgumentErrorException($"成品下料工位载具中混入了不同颜色产品。载具码({carrierID})", "载具上所有产品颜色校验");
                    }
                    string str1 = ret ? "OK" : "NG";
                    AddWriteLogInfo(Configs.StationID, "ToPLC", $"成品下料工位载具检查结果{str1}。载具码({carrierID})，载具颜色：{SnColorList[0]}");
                }

                //sendInfo[i / 2] = 0; //代表都没有产品
                //sendInfo[i / 2] = 1; //代表前面一个有产品
                //sendInfo[i / 2] = 256; //代表后面一个有产品
                //sendInfo[i / 2] = 257; //代表全部有产品
                int[] sendInfo = new int[30];
                for (int i = 0; i < SNInfo.Length; i += 2)
                {
                    if (SNInfo[i] == 0 && SNInfo[i + 1] == 0) //都没有  当前产品和后一个产品
                    {
                        sendInfo[i / 2] = 0;
                    }
                    if (SNInfo[i] == 0 && SNInfo[i + 1] == 1) //前面一个没有产品 高位
                    {
                        sendInfo[i / 2] = 256;
                    }
                    if (SNInfo[i] == 1 && SNInfo[i + 1] == 0) //前面一个有产品 低位
                    {
                        sendInfo[i / 2] = 1;
                    }
                    if (SNInfo[i] == 1 && SNInfo[i + 1] == 1) //都有产品
                    {
                        sendInfo[i / 2] = 257;//包含高位和低位共两个产品
                    }
                }
                Master.WriteMultiRegisters(PLCAddress.NGInfoStartAddr, NGInfo);
                ShowLogInfo(Configs.StationID, "产品NG位置记录", EventIO.信息, $"载具码({carrierID}),记录下发给PLC的NG数据组:{NGInfo.Aggregate("", (s, b) => s + $"{b}-")}");
                ShowLogInfo(Configs.StationID, "产品NG位置记录", EventIO.信息, $"载具码({carrierID}),记录下发给PLC的NG穴位号:{sbNGInfo.ToString().TrimEnd('-')}");

                //if (CarrierUnloadAndSNExistsMapping.ContainsKey(carrierID))
                //    CarrierUnloadAndSNExistsMapping.Remove(carrierID);
                //CarrierUnloadAndSNExistsMapping.Add(carrierID, sendInfo.ToString());
                //UpdateConfig("CarrierUnloadAndSNExistsMapping", new JavaScriptSerializer().Serialize(CarrierUnloadAndSNExistsMapping));

                CarrierIDProductUnloadTool carrierIDProductUnloadTool = new CarrierIDProductUnloadTool();
                // string value1 = new JavaScriptSerializer().Serialize(CarrierUnloadAndSNExistsMapping);
                carrierIDProductUnloadTool.InsertData(carrierID, new CarrierIDProductUnload() { CarrierID = carrierID, SNData = sendInfo.Aggregate("", (s, b) => s + $"{b},").TrimEnd(',') });

                
                //Master.WriteMultiRegisters(PLCAddress.SNInfoAddr, sendInfo);  //先不发
                //string val = sendInfo.Aggregate("", (s, b) => s + $"{b}-");
                //ShowLogInfo(Configs.StationID, "产品所在位置记录", EventIO.信息, $"记录下发给PLC的产品数据组:{val}");

                //CarrierUnloadAndSNMapping.Add(carrierID, list);
                //string value = new JavaScriptSerializer().Serialize(CarrierUnloadAndSNMapping);
                //UpdateConfig("CarrierUnloadAndSNMapping", value);
                string value = new JavaScriptSerializer().Serialize(list);
                carrierIDBindingBot4Tool.Insert(new CarrierIDBindingBot4() { CarrierID = carrierID, SNData = value });
                ret = true;
                string resultInfo = ret ? "OK" : "NG";
                int addr = ret ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2_Unload工站载具扫码结果:{resultInfo}；载具码:{carrierID}");
                Master.WriteRegister(addr, 1); //写检查结果
                resultID = ret ? 0 : 2;
                //Master.WriteRegister(PLCAddress.CarrierCheckFailInfoAddr, resultID); //写异常原因
                int okCount = results.Count(x => x.ToLower() == "pass");
                Master.WriteRegister(PLCAddress.OkAddress, okCount);
                Master.WriteRegister(PLCAddress.NgAddress, results.Length - okCount);
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1); //写检查结果
                //Master.WriteRegister(PLCAddress.CarrierCheckFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
            finally
            {
                //SendStatusAndAlarm();
            }
        }
        private string GetToolingSN(int index, List<string> toolingSNList)
        {
            index++;
            string sn = "0";
            if (index > 30)
                index -= 30;
            int n = (int)Math.Ceiling(index / 3.0);
            if (n > 5)
                n -= 5;
            if (n < toolingSNList.Count)
                sn = toolingSNList[n - 1];
            return sn;
        }
        public override bool CheckOut()
        {
            try
            {
                bool ret = true;
                if (Master2.Initialized) //采集信息号前判断有没有连接
                {
                    int bCheckOut = Master2.ReadHoldingRegisters(PLCAddress.Boat4CheckInAddr, 1)[0];
                    string val = ConfigurationManager.AppSettings["IsNullRun"];
                    if (bCheckOut == 1)
                    {
                        Master2.WriteRegister(PLCAddress.Boat4CheckInAddr, 0);//重置检查请求标志
                        //空跑
                        if (val == "1")
                        {
                            Master2.WriteRegister(PLCAddress.Boat4CheckInPassAddr, 1); //写检查结果
                            Master2.WriteRegister(PLCAddress.Boat4CheckInFailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= InnerCheckOut1();
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
        public bool InnerCheckOut()
        {
            int resultID = 1;
            try
            {
                //TODO 成品下料机出战从PLC读取载具码
                //1.软件运行NG 2.绑定失败 3.出站失败 4.二维码不一致 5.boat5二维码为空 6.boat6二维码为空
                string carrierID = Master.GetStringFromMaster(PLCAddress.CheckOutInfoAddr, PLCAddress.CarrierInfoLength);

                AddReadLogInfo(Configs.StationID, "读取PLC", $"读取到PSA2_Unload工站出站信号。载具码：{carrierID}");

                //if (!CarrierUnloadAndSNMapping.ContainsKey(carrierID))
                //{
                //    resultID = 4; //出站二维码与入站二维码不一致
                //    throw new ArgumentErrorException($"PSA2_Unload出站失败。请确认机台上二维码是否与实物相匹配", "PSA2_Unload出站");
                //}
                CarrierIDBindingBot4Tool carrierIDBindingBot4Tool = new CarrierIDBindingBot4Tool();
                CarrierIDBindingBot4 carrierIDBindingBot4 = new CarrierIDBindingBot4();
                carrierIDBindingBot4 = carrierIDBindingBot4Tool.Query(carrierID);
                if (carrierIDBindingBot4 == null)
                {
                    resultID = 4; //出站二维码与入站二维码不一致
                    throw new ArgumentErrorException($"PSA2_Unload出站失败。请确认机台上二维码是否与实物相匹配", "PSA2_Unload出站");
                }
                //List<SNInfo> snInfosList = CarrierUnloadAndSNMapping[carrierID].Where(s => s.Result == "PASS").ToList();
                List<SNInfo> snInfosList1 = new JavaScriptSerializer().Deserialize<List<SNInfo>>(carrierIDBindingBot4.SNData);
                CheckOutRequest request = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList1);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                bool ret = GetRet(response.Result);
                if (ret)
                {
                    //CarrierUnloadAndSNMapping.Remove(carrierID); //成功后清除二维码数据,PLC那边可能再手动出站,反正进站的时候会判断是否有重码
                    carrierIDBindingBot4Tool.Delete(carrierID);
                }
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2_Unload工站出站结果：{resultInfo};载具码:{carrierID}");
                int addr = ret ? PLCAddress.CheckOutPassAddr : PLCAddress.CheckOutFailAddr;
                Master.WriteRegister(addr, 1);
                resultID = ret ? 0 : 3;
                Master.WriteRegister(PLCAddress.CheckOutFailInfoAddr, resultID); //写异常原因
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CheckOutFailAddr, 1);
                Master.WriteRegister(PLCAddress.CheckOutFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }

        }
        public bool InnerCheckOut1()
        {
            int resultID = 1; //1软件运行异常 2校验未通过 3.载具码为空  4.载具颜色不一致
            try
            {
                //TODO 成品下料机出战从PLC读取载具码
                //1.软件运行NG 2.绑定失败 3.出站失败 4.二维码不一致 5.boat5二维码为空 6.boat6二维码为空
                string carrierID = Master2.GetStringFromMaster(PLCAddress.Boat4CheckInInfoAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "读取PLC", $"读取到成品下料信号。载具码：{carrierID}");
                //if (!CarrierUnloadAndSNExistsMapping.ContainsKey(carrierID))
                //{
                //    resultID = 2; //出站二维码与入站二维码不一致
                //    throw new ArgumentErrorException($"PSA2_Unload出站失败。请确认机台上二维码{carrierID}是否与实物相匹配", "PSA2_Unload出站");
                //}
                CarrierIDProductUnloadTool carrierIDProductUnloadTool = new CarrierIDProductUnloadTool();
                var carrierIDBindingSNExists = carrierIDProductUnloadTool.Query(carrierID);
                if (carrierIDBindingSNExists == null)
                {
                    resultID = 2; //出站二维码与入站二维码不一致
                    throw new ArgumentErrorException($"PSA2_Unload进站失败。请确认机台上二维码{carrierID}是否与实物相匹配", "PSA2_Unload进站");
                }
                string snInfo = carrierIDBindingSNExists?.SNData;
                string[] str = snInfo.Split(',');
                if (str.Length < 30)
                {
                    resultID = 1;
                    throw new ArgumentErrorException($"PSA2_Unload进站失败。获取产品数据组失败，长度不足：{str.Length}", "PSA2_Unload进站");
                }
                int[] sendInfo = new int[30];
                for (int i = 0; i < 30; i++)
                {
                    sendInfo[i] = Convert.ToInt32(str[i]);
                }
                string val = sendInfo.Aggregate("", (s, b) => s + $"{b}-");
                ShowLogInfo(Configs.StationID, "产品所在位置记录", EventIO.信息, $"记录下发给PLC的产品数据组:{val}");
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"成品下料结果：OK;载具码:{carrierID}");
                Master2.WriteMultiRegisters(PLCAddress.SNInfoAddr, sendInfo);
                Master2.WriteRegister(PLCAddress.Boat4CheckInPassAddr, 1); //写检查结果
                Master2.WriteRegister(PLCAddress.Boat4CheckInFailInfoAddr, 0); //写异常原因
                return true;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.Boat4CheckInFailAddr, 1);
                Master.WriteRegister(PLCAddress.Boat4CheckInFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }
        class PLCAddress
        {
            //NG抛料工站
            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 1000;
            public static int CarrierCheckFailAddr = 1001;
            public static int CarrierCheckFailInfoAddr = 1004;
            public static int NGInfoStartAddr = 4000; //NG、OK的开始地址，连续60个

            public static int CarrierInfoLength = 64;

            public static int CheckOutAddr = 1; //出站请求
            public static int CheckOutPassAddr = 1002;
            public static int CheckOutFailAddr = 1003;
            public static int CheckOutInfoAddr = 3064;
            public static int CheckOutFailInfoAddr = 1005;



            public static int CarrierColorAddr = 1007;
            public static int SNInfoAddr = 1020;
            public static int SNInfoLength = 30;

            public static int CheckIsNullRun = 5;

            //成品下料工站
            public static int Boat4CheckInAddr = 0;
            // public static int CheckSNOKNewAdd = 1002;//2024/08/13 罗敏需求200地址回复1002结果，固定给OK
            public static int Boat4CheckInInfoAddr = 3000;
            public static int Boat4CheckInPassAddr = 1000;
            public static int Boat4CheckInFailAddr = 1001;
            public static int Boat4CheckInFailInfoAddr = 1004;
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
