using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Web.Script.Serialization;
using System.Windows;
using U66MesPC.Common.Exceptions;
using U66MesPC.Dal;
using U66MesPC.Dal.Tool;
using U66MesPC.Model;
using Timer = System.Timers.Timer;

namespace U66MesPC.Common.Station
{

    /// <summary>
    /// 对应工站：PSA2保压机，所在模组：PSA2 Attach
    /// </summary>
    public class PSA2Press : StationBase
    {
        Dictionary<string, List<SNInfo>> CarrierPressAndSNMapping;
        public Dictionary<string, string> CarrierAndNumberMappingStation140;
        public PSA2Press(SysConfigs sysConfig) : base(sysConfig)
        {
            CarrierPressAndSNMapping = new Dictionary<string, List<SNInfo>>();
            string info = ConfigurationManager.AppSettings["CarrierPressAndSNMapping"];
            if (info != "" && info != "0")
            {
                CarrierPressAndSNMapping = new JavaScriptSerializer().Deserialize<Dictionary<string, List<SNInfo>>>(info);
            }
            CarrierAndNumberMappingStation140 = new Dictionary<string, string>();
            string info1 = ConfigurationManager.AppSettings["CarrierAndNumberMappingStation140"];
            if (info1 != "" && info1 != "0")
            {
                CarrierAndNumberMappingStation140 = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(info1);
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
            PressHreadReadToolingSN();
        }
        public Random random = new Random();
        public bool InnerCheckIn()
        {
            int resultID = 1; //1软件运行异常 2校验未通过 3.载具码为空 
            try
            {
                #region
                //每次入站ToolingSN检
                //List<string> ls = new List<string>();
                //string path = Directory.GetCurrentDirectory() + "\\ToolingSN.txt";
                //if (File.Exists(path))
                //{
                for (int i = 0; i < PressHreadls.Count; i++)
                {
                    if (!string.IsNullOrEmpty(PressHreadls[i]))
                    {
                        FeedingCheckRequest requestTooling = new FeedingCheckRequest(Configs, PressHreadls[i]);
                        Task<FeedingCheckResponse> responseTooling = HttpClientHelper.SNFeedingCheckAsync(requestTooling, Configs.Url);
                        CheckMesConnectStatus(responseTooling.Result);
                        bool ret1 = GetRetNew(responseTooling.Result);
                        if (!ret1)
                        {
                            resultID = 2;
                            throw new ArgumentErrorException($"保压头SN({PressHreadls[i]})入站失败！错误信息：{responseTooling.Result.Msg}", "SN_FeedingCheck");
                        }
                        //ls.Add(item);
                    }
                }
                //foreach (string item in ls.)
                //{
                //    if (!string.IsNullOrEmpty(item))
                //    {
                //        FeedingCheckRequest requestTooling = new FeedingCheckRequest(Configs, item);
                //        Task<FeedingCheckResponse> responseTooling = HttpClientHelper.SNFeedingCheckAsync(requestTooling, Configs.Url);
                //        CheckMesConnectStatus(responseTooling.Result);
                //        bool ret1 = GetRetNew(responseTooling.Result);
                //        if (!ret1)
                //        {
                //            resultID = 2;
                //            throw new ArgumentErrorException($"保压头SN({item})入站失败！错误信息：{responseTooling.Result.Msg}", "SN_FeedingCheck");
                //        }
                //        ls.Add(item);
                //    }
                //}
                //}
                //else
                //{
                //    File.Create(path).Close();
                //    resultID = 1;
                //    throw new ArgumentErrorException($"请检查程序目录下是否存在ToolingSN文件", "SN_FeedingCheck");
                //}
                //if (ls.Count < 20)
                //{
                //    resultID = 1;
                //    throw new ArgumentErrorException($"请检查程序目录下是否存在ToolingSN文件配置的SN个数是否正确", "SN_FeedingCheck");
                //}
                #endregion

                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "读取PLC", "读取到PSA2保压扫码进站信号。载具码：" + carrierID);

                if (!CheckString(carrierID))
                {
                    resultID = 3;
                    throw new ArgumentErrorException($"载具码{carrierID}无效！为空", "SN_CheckIN");
                }
                CarrierID = carrierID;
                CarrierIDPressBindingSNTool carrierIDPressBindingSNTool = new CarrierIDPressBindingSNTool();
                if (carrierIDPressBindingSNTool.Query(carrierID) != null)
                    carrierIDPressBindingSNTool.Delete(carrierID);
                //if (CarrierPressAndSNMapping.ContainsKey(carrierID))
                //    CarrierPressAndSNMapping.Remove(carrierID);
                CheckInRequest request = new CheckInRequest(Configs, carrierID, "NULL");
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRetNew(response.Result);
                //if (!ret)
                //{
                //    resultID = 2;
                //    throw new ArgumentErrorException($"载具码{carrierID}入站失败！错误信息：{response.Result.Msg}", "SN_CheckIN");
                //}
                List<SNInfo> list = new List<SNInfo>();
                SN = response.Result.SN;
                carrierID = response.Result.CarrierID;
                string[] productsSN = response.Result.SN.Split('-');
                string[] results = response.Result.Msg.Split(',');
                for (int i = 0; i < Math.Min(productsSN.Length, results.Length); i++)
                {
                    var sn = (i < productsSN.Length) ? productsSN[i].Split('#')[0] : "";
                    var result = results[i];
                    if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                        continue;
                    else
                    {
                        //上传字段中保压对应工位与穴位60-30一列；1-30二列；10个压头中间间隔2个穴位；一个列，顺序压三次；
                        string workID = null;
                        if (i < 30)
                        {
                            workID = "02";
                        }
                        else
                        {
                            workID = "01";
                        }
                        string localNumber = Local(i);
                        if (i > 29)
                        {
                            int val = i - 30;
                            localNumber = Local(val);
                        }
                        string combination = workID + localNumber;//穴位
                        if (string.IsNullOrEmpty(combination))
                            combination = "0";
                        //01：1-10  02：11-20
                        int index = workID == "01" ? Convert.ToInt32(localNumber) : Convert.ToInt32(localNumber) + 10;
                        List<CompInfo> compInfos = new List<CompInfo> { new CompInfo(PressHreadls[index - 1], 0) };
                        list.Add(new SNInfo(sn, result, combination, new List<DCInfo>() { }, compInfos));
                    }
                }
                //检查单个SN的结果
                if (list.Count(s => s.Result.ToUpper() == "PASS") == 0)
                {
                    resultID = 2;
                    throw new ArgumentErrorException($"进站失败。保压工站上料载具码({carrierID})入站校验NG！该载具上的产品全部NG", "载具码入站");
                }
                ret = true;
                string value = new JavaScriptSerializer().Serialize(list);
                carrierIDPressBindingSNTool.Insert(new CarrierIDPressBindingSN() { CarrierID = carrierID, SNData = value });
                //CarrierPressAndSNMapping.Add(carrierID, list);
                //string value = new JavaScriptSerializer().Serialize(CarrierPressAndSNMapping);
                //UpdateConfig("CarrierPressAndSNMapping", value);
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2保压入站结果:{resultInfo};载具码:{carrierID}");
                int addr = ret ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                Master.WriteRegister(addr, 1); //写检查结果
                resultID = ret ? 0 : 2;
                Master.WriteRegister(PLCAddress.CarrierCheckFailInfoAddr, resultID); //写异常原因
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1); //写检查结果
                Master.WriteRegister(PLCAddress.CarrierCheckFailInfoAddr, resultID); //写异常原因
                HandleException(ex);
                return false;
            }
        }
        public string Local(int index)
        {
            if (index == 0)
            {
                return "01";
            }
            if (index == 1)
            {
                return "01";
            }
            if (index == 2)
            {
                return "01";
            }
            if (index == 3)
            {
                return "02";
            }
            if (index == 4)
            {
                return "02";
            }
            if (index == 5)
            {
                return "02";
            }
            if (index == 6)
            {
                return "03";
            }
            if (index == 7)
            {
                return "03";
            }
            if (index == 8)
            {
                return "03";
            }
            if (index == 9)
            {
                return "04";
            }
            if (index == 10)
            {
                return "04";
            }
            if (index == 11)
            {
                return "04";
            }
            if (index == 12)
            {
                return "05";
            }
            if (index == 13)
            {
                return "05";
            }
            if (index == 14)
            {
                return "05";
            }
            if (index == 15)
            {
                return "06";
            }
            if (index == 16)
            {
                return "06";
            }
            if (index == 17)
            {
                return "06";
            }
            if (index == 18)
            {
                return "07";
            }
            if (index == 19)
            {
                return "07";
            }
            if (index == 20)
            {
                return "07";
            }
            if (index == 21)
            {
                return "08";
            }
            if (index == 22)
            {
                return "08";
            }
            if (index == 23)
            {
                return "08";
            }
            if (index == 24)
            {
                return "09";
            }
            if (index == 25)
            {
                return "09";
            }
            if (index == 26)
            {
                return "09";
            }
            if (index == 27)
            {
                return "10";
            }
            if (index == 28)
            {
                return "10";
            }
            if (index == 29)
            {
                return "10";
            }
            return "0";
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
                    //int bClearBoat6Cache = Master.ReadHoldingRegisters(PLCAddress.ClearBoat6CacheAddr, 1)[0];


                    //if (bClearBoat6Cache == 1)
                    //{
                    //    Master.WriteRegister(PLCAddress.ClearBoat6CacheAddr, 0);
                    //    Master.WriteRegister(PLCAddress.ClearBoat6CachePassAddr, 1);
                    //}
                    if (bCheckIn == 1)
                    {
                        Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);
                        //空跑
                        string val = ConfigurationManager.AppSettings["IsNullRun"];
                        if (val == "1")
                        {
                            Master.WriteRegister(PLCAddress.CarrierCheckPassAddr, 1); //写检查结果
                            Master.WriteRegister(PLCAddress.CarrierCheckFailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= InnerCheckIn();
                    }
                    if (bCheckOut == 1)
                    {
                        Master.WriteRegister(PLCAddress.CheckOutAddr, 0);
                        //空跑
                        string val = ConfigurationManager.AppSettings["IsNullRun"];
                        if (val == "1")
                        {
                            Master.WriteRegister(PLCAddress.CheckOutPassAddr, 1);
                            Master.WriteRegister(PLCAddress.CheckOutFailInfoAddr, 0); //写异常原因
                        }
                        else
                            ret &= CheckOut();
                    }
                }
                if (Master2.Initialized) //采集信息号前判断有没有连接
                {
                    int mStation50CarrierIDSaveAddr = Master2.ReadHoldingRegisters(PLCAddress.Station140CarrierIDSaveAddr, 1)[0];
                    if (mStation50CarrierIDSaveAddr == 1)
                    {
                        Master2.WriteRegister(PLCAddress.Station140CarrierIDSaveAddr, 0);//50工位载具码保存触发地址
                        ret &= SaveCarrierIDToAppConfig(); //保存载具码
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.Station140CarrierIDSaveAddr, 0);//50工位载具码保存触发地址
                HandleException(ex);
                return false;
            }

        }
        public bool SaveCarrierIDToAppConfig()
        {
            try
            {
                bool ret = true;
                string mStation140CarrierIDAddr = Master2.GetStringFromMaster(PLCAddress.Station140CarrierIDAddr, 64);
                AddWriteLogInfo(Configs.StationID, "读取PLC", $"140工位上料出站信号。载具码和流道号：{mStation140CarrierIDAddr}");
                if (!CheckString(mStation140CarrierIDAddr))
                {
                    throw new ArgumentErrorException($"140工位读取载具码数据的值错误！错误值：{mStation140CarrierIDAddr}", "获取流道ID");
                }
                string mStation140NumberAddr;
                string carrierID;
                if (mStation140CarrierIDAddr.Split('#').Length >= 2)
                {
                    carrierID = mStation140CarrierIDAddr.Split('#')[0];
                    mStation140NumberAddr = mStation140CarrierIDAddr.Split('#')[1];
                }
                else
                {
                    ret = false;
                    throw new ArgumentErrorException($"140工位读取流道数据的值错误！错误值：{mStation140CarrierIDAddr}", "获取流道ID");
                }
                int PLCResult = ret ? PLCAddress.Station140PassAddr : PLCAddress.Station140FailAddr;//2024/08/14吴恒要求增加PLC心跳报警以及回复OK/NG信号
                AddWriteLogInfo("140工位保存载具码", "读取PLC", $"140工位读取并保存载具码。载具码：{carrierID},流道ID：{mStation140NumberAddr}");
                //if (CarrierAndNumberMappingStation140.ContainsKey(carrierID))
                //{
                //    CarrierAndNumberMappingStation140.Remove(carrierID);
                //}
                //CarrierAndNumberMappingStation140.Add(carrierID, mStation50NumberAddr.ToString()); //将载具码与流道存起来
                //string value = new JavaScriptSerializer().Serialize(CarrierAndNumberMappingStation140);
                //UpdateConfig("CarrierAndNumberMappingStation140", value);
                CarrierIDBindingNoNumber140Tool carrierIDBindingNoNumber140Tool = new CarrierIDBindingNoNumber140Tool();
                carrierIDBindingNoNumber140Tool.InsertData(carrierID, new CarrierIDBindingNoNumber140() { CarrierID = carrierID, NoNumber = mStation140NumberAddr });
                Master2.WriteRegister(PLCResult, 1);//140工位载具码保存成功地址
                return true;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.Station140FailAddr, 1);//140工位载具码保存失败地址
                Master2.WriteRegister(PLCAddress.Station140CarrierIDSaveAddr, 0);//50工位载具码保存触发地址
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
            int resultID = 1;
            try
            {
                //1.软件运行NG 2.绑定失败 3.出站失败 4.二维码不一致 5.boat5二维码为空 6.boat6二维码为空
                //保压出站从PLC出读取载具码
                string carrierID = Master.GetStringFromMaster(PLCAddress.CheckOutInfoAddr, PLCAddress.CarrierInfoLength);
                AddReadLogInfo(Configs.StationID, "读取PLC", $"读取到PSA2保压出站信号。载具码：{carrierID}");
                if (!CheckString(carrierID))
                {
                    resultID = 4; //载具码为空
                    throw new ArgumentErrorException($"保压出站失败：载具码({carrierID})无效！", "保压出站校验");
                }
                CarrierIDPressBindingSNTool carrierIDPressBindingSNTool = new CarrierIDPressBindingSNTool();
                CarrierIDPressBindingSN carrierIDPressBindingSN = carrierIDPressBindingSNTool.Query(carrierID);
                //if (!CarrierPressAndSNMapping.ContainsKey(carrierID))
                if (carrierIDPressBindingSN == null)
                {
                    resultID = 4; //出站二维码与入站二维码不一致
                    throw new ArgumentErrorException("PSA2保压出站失败。请确认机台上二维码是否与实物相匹配", "PSA2保压出站");
                }
                CarrierIDBindingNoNumber140Tool carrierIDBindingNoNumber140Tool = new CarrierIDBindingNoNumber140Tool();
                CarrierIDBindingNoNumber140 carrierIDBindingNoNumber140 = carrierIDBindingNoNumber140Tool.Query(carrierID);
                //检查流道ID是否存在
                //if (!CarrierAndNumberMappingStation140.ContainsKey(carrierID))
                if (carrierIDBindingNoNumber140 == null)
                {
                    carrierIDBindingNoNumber140 = new CarrierIDBindingNoNumber140();
                    carrierIDBindingNoNumber140.NoNumber = "1";
                    ShowLogInfo(Configs.StationID, "PSA2保压出站", EventIO.Warn, $"载具{carrierID}的流道信息为空。默认给1");
                    //resultID = 4; //boat6出站二维码与入站boat6二维码不一致
                    //throw new ArgumentErrorException($"PSA2保压出站失败！载具{carrierID}的流道信息为空。请确认机台上二维码是否与实物相匹配", "PSA2获取流道信息");
                }
                //获取流道ID

                //string flowID = CarrierAndNumberMappingStation140[carrierID];
                string flowID = carrierIDBindingNoNumber140.NoNumber;

                //List<SNInfo> snInfosList = CarrierPressAndSNMapping[carrierID].Where(s => s.Result.ToUpper() == "PASS").ToList();
                List<SNInfo> snInfosList = new JavaScriptSerializer().Deserialize<List<SNInfo>>(carrierIDPressBindingSN?.SNData);
                CheckOutRequest request = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList, flowID);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                bool ret = GetRet(response.Result);
                if (ret)
                {
                    //CarrierPressAndSNMapping.Remove(carrierID); //成功后清除二维码数据,PLC那边可能再手动出站,反正进站的时候会判断是否有重码
                    carrierIDPressBindingSNTool.Delete(carrierID);
                    //CarrierAndNumberMappingStation140.Remove(carrierID);
                    carrierIDBindingNoNumber140Tool.Delete(carrierID);
                }
                int okCount = snInfosList.Count(p => p.Result.ToLower() == "pass");
                int ngCount = snInfosList.Count - okCount;
                Master.WriteRegister(PLCAddress.OkAddress, okCount);
                Master.WriteRegister(PLCAddress.NgAddress, ngCount);
                string resultInfo = ret ? "OK" : "NG";
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA2保压出站结果:{resultInfo};载具码:{carrierID}");
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
        public override void Release()
        {
            base.Release();
        }
        class PLCAddress
        {
            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 1000;
            public static int CarrierCheckFailAddr = 1001;
            public static int CarrierCheckFailInfoAddr = 1004;

            public static int CarrierInfoLength = 64;

            public static int CheckOutAddr = 1;
            public static int CheckOutPassAddr = 1002;
            public static int CheckOutFailAddr = 1003;
            public static int CheckOutFailInfoAddr = 1005;
            public static int CheckOutInfoAddr = 3064;


            public static int ClearBoat6CacheAddr = 3;
            public static int ClearBoat6CachePassAddr = 1006;

            public static int CheckIsNullRun = 5;

            #region 140工位交互地址
            /// <summary>
            /// 140工位载具码保存触发地址
            /// </summary>
            public static int Station140CarrierIDSaveAddr = 0;
            /// <summary>
            /// 140工位载具码地址  载具码#流道ID
            /// </summary>
            public static int Station140CarrierIDAddr = 3000;
            /// <summary>
            /// 140工位载具码保存成功地址
            /// </summary>
            public static int Station140PassAddr = 1000;
            /// <summary>
            /// 140工位载具码保存失败地址
            /// </summary>
            public static int Station140FailAddr = 1001;
            #endregion

            #region 产能统计
            public static int OkAddress = 10937;
            public static int NgAddress = 10938;
            #endregion
        }
    }
}
