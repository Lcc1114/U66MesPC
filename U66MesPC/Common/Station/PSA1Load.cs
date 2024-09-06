using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using U66MesPC.Common.Exceptions;
using U66MesPC.Dal;
using U66MesPC.Dal.Tool;
using U66MesPC.Model;
using Timer = System.Timers.Timer;
using System.Configuration;
using System.IO;
using System.Windows;
using System.Web.Script.Serialization;

namespace U66MesPC.Common.Station
{

    /// <summary>
    /// 对应工站：PSA1机台上料，所在模组：PSA Attach
    /// </summary>
    public class PSA1Load : StationBase
    {
        public Dictionary<string, List<SNInfo>> CarrierAndSNMapping;
        public static Dictionary<string, string> CarrierAndNumberMapping;
        public PSA1Load(SysConfigs sysConfig) : base(sysConfig)
        {
            CarrierAndSNMapping = new Dictionary<string, List<SNInfo>>();
            CarrierAndNumberMapping = new Dictionary<string, string>();
            string info = ConfigurationManager.AppSettings["CarrierAndNumberMappingStation50"];
            if (info != "" && info != "0")
            {
                CarrierAndNumberMapping = new JavaScriptSerializer().Deserialize<Dictionary<string, string>>(info);
            }
            Master.Initialzie();
            //检查连接状态
            if (!Master.Initialized)
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "Initialize", null, $"连接PLC失败，IP:{Master.GetIP()},端口号：{Master.GetPort()};");
            Master2.Initialzie();
            //检查连接状态
            if (!Master2.Initialized)
                HttpClientHelper.AddErrorLogInfo(sysConfig.StationID, "Initialize", null, $"连接PLC失败，IP:{Master2.GetIP()},端口号：{Master2.GetPort()};");
            InitThread();
        }
        public override void InitThread()
        {
            base.InitThread();
            InitNewThread();
        }
        public override bool CheckOut()
        {
            try
            {
                bool ret = true;
                if (Master2.Initialized) //采集信息号前判断有没有连接
                {
                    int mStation50CarrierIDSaveAddr = Master2.ReadHoldingRegisters(PLCAddress.Station50CarrierIDSaveAddr, 1)[0];
                    if (mStation50CarrierIDSaveAddr == 1)
                    {
                        Master2.WriteRegister(PLCAddress.Station50CarrierIDSaveAddr, 0);//50工位载具码保存触发地址
                        ret &= SaveCarrierIDToAppConfig(); //保存载具码
                    }
                }
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.Station50CarrierIDSaveAddr, 0);//50工位载具码保存触发地址
                Master2.WriteRegister(PLCAddress.Station50FailAddr, 1);//50工位载具码保存失败地址
                HandleException(ex);
                return false;
            }
        }
        public bool SaveCarrierIDToAppConfig()
        {
            try
            {
                string mStation50CarrierIDAddr = Master2.GetStringFromMaster(PLCAddress.Station50CarrierIDAddr, 64);
                #region SaveCarrierIDToAppConfig
                //AddWriteLogInfo(Configs.StationID, "读取PLC", $"50工位上料出站信号。载具码和流道号：{mStation50CarrierIDAddr}");
                //if (!CheckString(mStation50CarrierIDAddr))
                //{
                //    throw new ArgumentErrorException($"50工位读取载具码数据的值错误！错误值：{mStation50CarrierIDAddr}", "获取流道ID");
                //}
                //int mStation50NumberAddr;
                //string carrierID;
                //if (mStation50CarrierIDAddr.Split('#').Length >= 2)
                //{
                //    carrierID = mStation50CarrierIDAddr.Split('#')[0];
                //    mStation50NumberAddr = Convert.ToInt32(mStation50CarrierIDAddr.Split('#')[1]);
                //}
                //else
                //    throw new ArgumentErrorException($"50工位读取流道数据的值错误！错误值：{mStation50CarrierIDAddr}", "获取流道ID");

                //AddWriteLogInfo("50工位保存载具码", "读取PLC", $"50工位读取并保存载具码。载具码：{carrierID},流道ID：{mStation50NumberAddr}");
                //if (CarrierAndNumberMapping.ContainsKey(carrierID))
                //{
                //    CarrierAndNumberMapping.Remove(carrierID);
                //}
                //CarrierAndNumberMapping.Add(carrierID, mStation50NumberAddr.ToString()); //将载具码与流道存起来
                //string value = new JavaScriptSerializer().Serialize(CarrierAndNumberMapping);
                //UpdateConfig("CarrierAndNumberMappingStation50", value);
                #endregion
                #region SaveCarrierIDToSQLLite
                AddWriteLogInfo(Configs.StationID, "读取PLC", $"50工位上料出站信号。载具码和流道号：{mStation50CarrierIDAddr}");
                if (!CheckString(mStation50CarrierIDAddr))
                {
                    throw new ArgumentErrorException($"50工位读取载具码数据的值错误！错误值：{mStation50CarrierIDAddr}", "获取流道ID");
                }
                int mStation50NumberAddr = 0;
                string carrierID = "";
                bool ret = true;
                if (mStation50CarrierIDAddr.Split('#').Length >= 2)
                {
                    carrierID = mStation50CarrierIDAddr.Split('#')[0];
                    mStation50NumberAddr = Convert.ToInt32(mStation50CarrierIDAddr.Split('#')[1]);
                }
                else
                {
                    ret = false;
                    throw new ArgumentErrorException($"50工位读取流道数据的值错误！错误值：{mStation50CarrierIDAddr}", "获取流道ID");
                }
                int PLCResult = ret ? PLCAddress.Station50PassAddr : PLCAddress.Station50FailAddr;//2024/08/14吴恒要求增加PLC心跳报警以及回复OK/NG信号
                AddWriteLogInfo("50工位保存载具码", "读取PLC", $"50工位读取并保存载具码。载具码：{carrierID},流道ID：{mStation50NumberAddr}");
                CarrierIDBindingNoNumber50Tool carrierIDBindingNoNumberTool = new CarrierIDBindingNoNumber50Tool();
                carrierIDBindingNoNumberTool.InsertData(carrierID, new CarrierIDBindingNoNumber50() { CarrierID = carrierID, NoNumber = mStation50NumberAddr.ToString() });
                #endregion
                Master2.WriteRegister(PLCResult, 1);//50工位载具码保存成功地址
                return true;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.Station50FailAddr, 1);//50工位载具码保存失败地址
                Master2.WriteRegister(PLCAddress.Station50CarrierIDSaveAddr, 0);//50工位载具码保存触发地址
                HandleException(ex);
                return false;
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
                if (Master.Initialized) //采集信息号前判断有没有连接
                {
                    int bCheckOut = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr, 1)[0];
                    if (bCheckOut == 1)
                    {
                        Master.WriteRegister(PLCAddress.CheckOutAddr, 0);
                        //空跑
                        int val = Master.ReadHoldingRegisters(PLCAddress.CheckIsNullRun, 1)[0];

                        if (val == 1)
                        {
                            UpdateConfig("IsNullRun", val.ToString());
                            Master.WriteRegister(PLCAddress.CheckOutResultAddr, 1);
                        }
                        else
                        {
                            UpdateConfig("IsNullRun", "0");
                            ret &= CheckOutNew();
                        }
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
        public bool CheckOutNew()
        {
            int resultID = 3; //1.OK 2.NG 3.软件运行NG 4.载具码无效 5.载具码入站校验NG 6.载具码出站NG，
                              //7.载具中混入了不同颜色产品
            try
            {
                #region
                //List<string> ls = new List<string>();
                ////List<SNInfo> list1 = new List<SNInfo>();
                //string path = Directory.GetCurrentDirectory() + "\\ToolingSN.txt";
                //if (File.Exists(path))
                //{
                //    foreach (string item in File.ReadAllLines(path))
                //    {
                //        if (!string.IsNullOrEmpty(item))
                //        {
                //            FeedingCheckRequest requestTooling = new FeedingCheckRequest(Configs, item);
                //            Task<FeedingCheckResponse> responseTooling = HttpClientHelper.SNFeedingCheckAsync(requestTooling, Configs.Url);
                //            CheckMesConnectStatus(responseTooling.Result);
                //            bool ret1 = GetRetNew(responseTooling.Result);
                //            if (!ret1)
                //            {
                //                resultID = 2;
                //                throw new ArgumentErrorException($"ToolingSN入站失败！错误信息：{responseTooling.Result.Msg}", "SN_FeedingCheck");
                //            }
                //        }
                //    }
                //    //using (StreamReader sr = new StreamReader(path))
                //    //{
                //    //    while (!string.IsNullOrEmpty(sr.ReadToEnd()))
                //    //    {
                //    //        ls.Add(sr.ReadLine());
                //    //    }
                //    //}
                //}
                //else
                //{
                //    File.Create(path).Close();
                //    MessageBox.Show("请检查程序目录下ToolingSN文件");
                //}
                //ls.ForEach(a => list1.Add(new SNInfo(a.ToString(), "", new List<DCInfo>() { }, new List<CompInfo>() { })));
                #endregion
                //每次入站ToolingSN检
                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierInfoAddr, PLCAddress.CarrierInfoLength);
                AddWriteLogInfo(Configs.StationID, "读取PLC", $"PSA1上料出站信号。载具码：{carrierID}");

                if (!CheckString(carrierID))
                {
                    resultID = 4;
                    throw new ArgumentErrorException($"进站失败：PSA1上料载具码({carrierID})无效！", "载具码校验");
                }
                CarrierID = carrierID;
                CheckInRequest request = new CheckInRequest(Configs, carrierID, carrierID);
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRetNew(response.Result); //校验载具码OK         
                //if (!ret)
                //{
                //    resultID = 2;
                //    throw new ArgumentErrorException($"进站失败。PSA1上料载具码({carrierID})入站校验NG！错误信息：{response.Result.Msg}", "载具码入站");
                //}

                //解析返回信息
                List<SNInfo> list = new List<SNInfo>(); //产品信息集合
                List<string> color = new List<string>();//产品颜色集合
                SN = response.Result.SN;
                carrierID = response.Result.CarrierID;
                string[] productsSN = response.Result.SN.Split('-');
                string[] results = response.Result.Msg.Split(',');
                for (int i = 0; i < Math.Max(productsSN.Length, results.Length); i++)
                {
                    var sn = (i < productsSN.Length) ? productsSN[i].Split('#')[0] : "";
                    var result = (i < results.Length) ? results[i] : "";
                    if (i < productsSN.Length && productsSN[i].Split('#').Length > 1 && !string.IsNullOrEmpty(productsSN[i].Split('#')[1]))
                        color.Add(productsSN[i].Split('#')[1]);
                    if (string.IsNullOrEmpty(sn) || string.IsNullOrEmpty(result) || sn.Equals("0"))
                        continue;
                    else
                        list.Add(new SNInfo(sn, result, new List<DCInfo>() { }, new List<CompInfo>() { }));
                }
                //检查单个SN的结果
                if (list.Count(s => s.Result.ToUpper() == "PASS") == 0)
                {
                    resultID = 2;
                    throw new ArgumentErrorException($"进站失败。PSA1上料载具码({carrierID})入站校验NG！该载具上的产品全部NG", "载具码入站");
                }
                ShowLogInfo(Configs.StationID, "入站校验OK", EventIO.信息, $"PSA1上料工站载具检查结果OK");
                //int colorID = -1;
                if (color.Count >= 1) //检查颜色是否一致
                {
                    bool retBool = !color.Any(s => s != color[0]);
                    if (retBool) //所有产品同一种颜色
                    {
                        if (!dicColor.Keys.Contains(color[0]))
                        {
                            resultID = 2;
                            throw new ArgumentErrorException($"载具码{carrierID}入站失败！提取产品颜色编码({color[0]})未在颜色列表中", "产品颜色提取");
                        }
                        ret = true;
                        //colorID = dicColor[color[0]];
                        //Master.WriteRegister(PLCAddress.CarrierColorAddr, colorID);
                    }
                    else //载具不合规，混入了不同颜色的产品
                    {
                        //发送ng给PLC
                        resultID = 2;
                        ret = false;
                        ShowLogInfo(Configs.StationID, "产品颜色校验", EventIO.错误, $"PSA1上料工位载具中混入了不同颜色产品。载具码({carrierID})；{color.Aggregate("产品颜色：", (s, b) => s + $"{b},")}");
                        throw new ArgumentErrorException($"PSA1上料工位载具中混入了不同颜色产品。载具码({carrierID})", "载具上所有产品颜色校验");
                    }
                    string rst = ret ? "OK" : "NG";
                    AddWriteLogInfo(Configs.StationID, "ToPLC", $"PSA1上料工位载具检查结果{rst}。载具码({carrierID})，载具颜色：{color[0]}");
                }
                int okCount = results.Where(x => x.ToLower() == "pass").Count();
                //Master.WriteRegister(PLCAddress.OkAddress, okCount);
                //Master.WriteRegister(PLCAddress.NgAddress, results.Length - okCount);
                Master.WriteMultiRegisters(PLCAddress.OkAddress, new int[] { okCount, results.Length - okCount });

                var snInfosList = list;
                CheckOutRequest request1 = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, snInfosList);
                Task<CheckOutResponse> response1 = HttpClientHelper.SNCheckOutAsync(request1, Configs.Url);
                ret = GetRetNew(response1.Result);
                //ret = response1.Result.SN_Info.Any(x => x.SNResult.ToUpper() == "PASS");
                if (!ret)
                {
                    resultID = 2;
                    throw new ArgumentErrorException($"出站失败。PSA1上料载具码({carrierID})出站NG！错误信息：{response1.Result.Msg}", "载具码出站");
                }

                string infoResult = ret ? "OK" : "NG";
                resultID = ret ? 1 : 2; //OK or NG
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"PSA1上料工站载具{carrierID}出站结果：{infoResult};载具码:{carrierID}");
                Master.WriteRegister(PLCAddress.CheckOutResultAddr, resultID);
                return ret;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CheckOutResultAddr, resultID);
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
            public static int CheckIsNullRun = 5;

            public static int CarrierInfoLength = 64;

            public static int CheckOutAddr = 0;
            public static int CarrierInfoAddr = 3000;
            /// <summary>
            /// TO_PLC出站结果地址
            /// </summary>
            public static int CheckOutResultAddr = 1000;
            #region 50工位交互地址
            /// <summary>
            /// 50工位载具码保存触发地址
            /// </summary>
            public static int Station50CarrierIDSaveAddr = 0;
            /// <summary>
            /// 50工位载具码地址  载具码#流道ID
            /// </summary>
            public static int Station50CarrierIDAddr = 3000;
            /// <summary>
            /// 50工位载具码保存成功地址
            /// </summary>
            public static int Station50PassAddr = 1000;
            /// <summary>
            /// 50工位载具码保存失败地址
            /// </summary>
            public static int Station50FailAddr = 1001;

            #endregion
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
