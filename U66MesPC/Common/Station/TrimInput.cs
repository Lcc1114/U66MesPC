using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using U66MesPC.Common.Exceptions;
using U66MesPC.Model;

namespace U66MesPC.Common.Station
{
    /// <summary>
    /// 对应工站：载具绑定产品，所在模组：Trim load to Boat2
    /// </summary>
    public class TrimInput : StationBase
    {
        //ModbusTcpNet modbusTcpNet;
        public Queue<string> CarrierQueue;  //载具码队列
        public Queue<List<DCInfo>> ProductInfoQueue;  //产品码队列 item=产品码，value=穴位，result==>>PASS:已进站;FAIL进站失败，down==>>OK：合法；NG：不合法，up=对应的载具码
        Dictionary<string, List<DCInfo>> CarrierAndSNMapping;

        public Random random = new Random();
        public TrimInput(SysConfigs sysConfig) : base(sysConfig)
        {
            //
            CarrierAndSNMapping = new Dictionary<string, List<DCInfo>>();
            Master.Initialzie();
            Master2.Initialzie();
            InitThread();
        }
        public override void InitThread()
        {
            base.InitThread();
            InitNewThread();
        }
        /// <summary>
        /// 如果载具不合法，停机报警，人为取出 载具合法性核对
        /// </summary>
        /// <returns></returns>
        public bool CarrierCheck()
        {
            try
            {
                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);//读取载具码
                //AddReadLogInfo(Configs.StationID, "FromPLC", "载具码：" + carrierID);

                if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR"))
                    throw new ArgumentErrorException($"进站失败：载具码({carrierID})无效！", "SN_CheckIN");

                CarrierID = carrierID;

                CarrierCheckRequest request = new CarrierCheckRequest(Configs, carrierID);
                Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);

                bool result = GetRet(response.Result);
                if (bSimulation)
                    result = true;
                int addr = result ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                Master.WriteRegister(addr, 1);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"CarrierChec:{carrierID} result:{result}");
                //result = false;
                //if (!result)
                //    MessageBoxMsg($"载具{carrierID}进站失败,Result:{response?.Result?.Result},{response?.Result?.Msg}");
                return result;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1);
                HandleException(ex);
                //MessageBoxMsg($"{ex.Message}");

                return false;
            }
            finally
            {
                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);//重置载具检查标志位
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset CarrierCheck flag");
                //SendStatusAndAlarm();
            }
        }
        /// <summary>
        /// 如果Cap不合法，则抛掉 核对Cap合法性
        /// </summary>
        /// <returns></returns>
        public bool SNFeedingCheck(string productSN)
        {
            FeedingCheckRequest request = new FeedingCheckRequest(Configs, productSN);
            Task<FeedingCheckResponse> response = HttpClientHelper.SNFeedingCheckAsync(request, Configs.Url);
            CheckMesConnectStatus(response.Result);
            return GetRet(response.Result);
        }
        public bool ProductSNCheck()
        {
            try
            {
                List<DCInfo> productInfoList = new List<DCInfo>();  //item=产品码，value=穴位
                string sn = string.Empty, carrierSN1 = string.Empty, carrierSN2 = string.Empty;
                //var lastInfos = CarrierAndSNMapping.FirstOrDefault(c => c.Key == carrierSN);
                //bool bReCheck = lastInfos.Key != null;
                if (false) //第二次复检只需找到上次上传失败的产品SN
                {
#if false
                    //int addr;
                    //var points = lastInfos.Value.Where(s => s.Result == "FAIL").Select(s => s.Value).ToList(); //
                    //for (int j = 0; j < points.Count(); j++)
                    //{
                    //    int pt = Convert.ToInt32(points[j]);
                    //    if (pt < 24)
                    //        addr = startAddr + pt * PLCAddress.ProductSNInfoLength;
                    //    else
                    //        addr = startAddr + (pt + 1) * PLCAddress.ProductSNInfoLength;
                    //    sn = Master2.GetStringFromMaster(startAddr + pt * PLCAddress.ProductSNInfoLength, PLCAddress.ProductSNInfoLength);
                    //    //productInfoList.Add(new DCInfo() { Item = sn, Value = pt.ToString() });
                    //    var pInfo = lastInfos.Value.FirstOrDefault(p => p.Value == points[j]);
                    //    if (pInfo != null)
                    //        pInfo.Down = "ReCheck";
                    //}
                    //DCInfo[] array = new DCInfo[lastInfos.Value.Count];
                    //lastInfos.Value.CopyTo(array);
                    //productInfoList = array.ToList();
#endif
                }
                else
                {
                    int n = 1, startAddr = PLCAddress.ProductSNCheckInfoAddr1;
                    for (int i = 0; i < 31; i++)
                    {
                        if (i == 0)
                            carrierSN1 = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.CarrierInfoLength);
                        else
                        {
                            sn = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.ProductSNInfoLength);
                            productInfoList.Add(new DCInfo() { Item = sn, Value = n++.ToString() });
                        }
                    }
                    n = 31;
                    startAddr = PLCAddress.ProductSNCheckInfoAddr2;
                    for (int i = 0; i < 31; i++)
                    {
                        if (i == 0)
                            carrierSN2 = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.CarrierInfoLength);
                        else
                        {
                            sn = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.ProductSNInfoLength);
                            productInfoList.Add(new DCInfo() { Item = sn, Value = n++.ToString() });
                        }
                    }
                    n = 61;
                    startAddr = PLCAddress.ProductSNCheckInfoAddr3;
                    for (int i = 0; i < 31; i++)
                    {
                        if (i == 0)
                            carrierSN1 = Master2.GetStringFromMasterAndNotClear(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.CarrierInfoLength);
                        else
                        {
                            sn = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.ProductSNInfoLength);
                            productInfoList.Add(new DCInfo() { Item = sn, Value = n++.ToString() });
                        }
                    }
                    n = 91;
                    startAddr = PLCAddress.ProductSNCheckInfoAddr4;
                    for (int i = 0; i < 31; i++)
                    {
                        if (i == 0)
                            carrierSN2 = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.CarrierInfoLength);
                        else
                        {
                            sn = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.ProductSNInfoLength);
                            productInfoList.Add(new DCInfo() { Item = sn, Value = n++.ToString() });
                        }
                    }
                }
                SN = productInfoList.First().Item;
                Master2.WriteRegister(PLCAddress.ProductSNCheckAddr1, 0);//重置产品码请求标志
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset ProductSNCheck flag");
#if DEBUG
                productInfoList.ForEach(p => Console.WriteLine(p.Item + " " + p.Value));
                var ss = productInfoList.OrderBy(p => p.Value);
#endif
                if (string.IsNullOrEmpty(carrierSN1) || carrierSN1.Equals("ERROR"))
                    throw new ArgumentErrorException($"检查产品码合法性失败：载具码({carrierSN1})无效！", "SN_FeedingCheck");

                if (!carrierSN1.Equals(carrierSN2))
                    throw new ArgumentErrorException($"两列的载具码({carrierSN1},{carrierSN2})不一致！", "SN_FeedingCheck");

                for (int i = 0; i < productInfoList.Count; i++)
                {
                    var p = productInfoList[i];
                    //if (bReCheck && p.Down != "ReCheck")
                    //    continue;
                    p.Up = carrierSN1; //载具码
                    if (p.Item.Equals("ERROR") || string.IsNullOrEmpty(p.Item))
                    {
                        p.Result = "ERROR";
                        ShowLogInfo(Configs.StationID, "SN_FeedingCheck", EventIO.Warn, $"警告：穴位{p.Value}上的产品码({p.Item})无效，忽略合法性检查");
                    }
                    else
                    {
                        if (SNFeedingCheck(productInfoList[i].Item))
                        {
                            CheckInRequest request = new CheckInRequest(Configs, p.Item, $"{carrierSN1}_{p.Value}");
                            Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                            CheckMesConnectStatus(response.Result);
                            p.Result = response.Result?.Result;
                            if (p.Result.ToLower() == "error")
                                break;
                        }
                        if (bSimulation)
                            p.Result = "PASS";
                    }

                }
                bool ret = !(bool)(productInfoList?.Any(p => p.Result?.ToLower() != "pass"));
                ret = true;
                productInfoList = productInfoList?.Where(p => p.Result?.ToLower() == "pass").ToList();
                //if (bReCheck)
                //    CarrierAndSNMapping.Remove(CarrierID);
                if (CarrierAndSNMapping.ContainsKey(carrierSN1))
                    CarrierAndSNMapping.Remove(carrierSN1);
                CarrierAndSNMapping.Add(carrierSN1, productInfoList);

                int retAddr = ret ? PLCAddress.ProductSNCheckPassAddr : PLCAddress.ProductSNCheckFailAddr;
                Master2.WriteRegister(retAddr, 1);//写检查结果
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Feedback ProductSN Result:{ret}");
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.ProductSNCheckFailAddr, 1);
                HandleException(ex);
                return false;
            }
            finally
            {

            }
        }
        /// <summary>
        /// 检查载具码
        /// </summary>
        /// <returns></returns>
        public override bool CheckIn()
        {
            try
            {
                bool ret = true;
                int bCheckCarrier = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
#if DEBUG
                Console.WriteLine($"FromPLC：CarrierCheckAddr:{bCheckCarrier}；");
#endif
                if (bCheckCarrier == 1)
                {
                    ret &= CarrierCheck();
                }
                return ret;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }
        private bool InnerCheckOut()
        {
            try
            {
                //string carrierSN = CarrierQueue.Dequeue();
                //var productInfo = ProductInfoQueue.Dequeue();if
                if (CarrierAndSNMapping.Count == 0)
                {
                    throw new ArgumentErrorException($"出站失败：待出站信息为空！");
                }
                string carrierID = Master2.GetStringFromMaster(PLCAddress.ProductSNCheckInfoAddr3, PLCAddress.CarrierInfoLength);
                int flowID1 = Master2.GetRegisterValAndClear(PLCAddress.CheckOutFlowIDAddr1, 1); //流道1
                int flowID2 = Master2.GetRegisterValAndClear(PLCAddress.CheckOutFlowIDAddr2, 1); //流道2
                string flowID = string.Empty;
                if (flowID1 == 1)
                    flowID = "1";
                else if (flowID2 == 1)
                    flowID = "2";

                var mapping = CarrierAndSNMapping.FirstOrDefault(kv => kv.Key == carrierID);
                CarrierAndSNMapping.Remove(carrierID);
                if (mapping.Value == null || mapping.Value.Count == 0)
                    throw new ArgumentErrorException($"出站失败：载具({carrierID})上无有效产品码");

                //var productInfo = mapping.Value;
                List<SNInfo> list = new List<SNInfo>();
                mapping.Value?.ForEach(p =>
                {
                    list.Add(new SNInfo(p.Item, p.Result, new List<DCInfo>() { }, new List<CompInfo>() { }));
                });
                CheckOutRequest request = new CheckOutRequest(Configs, carrierID, Configs.Mold, 0, list, flowID);
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool ret = GetRet(response.Result);
                if (bSimulation)
                    ret = true;

                int addr = ret ? PLCAddress.CheckOutPassAddr : PLCAddress.CheckOutFailAddr;
                Master2.WriteRegister(addr, 1);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"CheckOut:{ret};CarrierID:{carrierID}");
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(PLCAddress.CheckOutFailAddr, 1);
                HandleException(ex);
                return false;
            }
            finally
            {
                Thread.Sleep(100);
                Master2.WriteRegister(PLCAddress.CheckOutAddr, 0);
                //AddWriteLogInfo(Configs.StationID, "写入PLC", "Reset CheckOut flag");
                //SendStatusAndAlarm();
            }
        }
        public override void SNCheckInAsync()
        {
            Master2.WriteRegister(50001, 1);
        }

        /// <summary>
        ///  以产品码出站；carrierID栏位放载具码
        /// </summary>
        public override bool CheckOut()
        {
            try
            {
                bool ret = true;
                int bCheckProductSN1 = Master2.ReadHoldingRegisters(PLCAddress.ProductSNCheckAddr1, 1)[0];
                int bCheckOut = Master2.ReadHoldingRegisters(PLCAddress.CheckOutAddr, 1)[0];
                //AddReadLogInfo(Configs.StationID, "FromPLC", $"ProductSNCheck:{bCheckProductSN1}；CheckOut:{bCheckOut}");
#if DEBUG   
                Console.WriteLine($"FromPLC：ProductSNCheck1:{bCheckProductSN1}；CheckOut:{bCheckOut}");
#endif
                if (bCheckProductSN1 == 1)
                {
                    ret &= ProductSNCheck();
                }
                if (bCheckOut == 1)
                {
                    ret &= InnerCheckOut();
                }
                return ret;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
            finally
            {
                //SendStatusAndAlarm();
            }
        }

        public override void Release()
        {
            base.Release();
            //modbusTcpNet?.ConnectClose();
        }
        /// <summary>
        /// plc address
        /// </summary>
        class PLCAddress
        {
            //public static int CarrierCheckAddr = 40000;
            //public static int CarrierCheckInfoAddr = 30075;//Master
            //public static int CarrierCheckPassAddr = 50000;
            //public static int CarrierCheckFailAddr = 50001;

            //public static int ProductSNCheckAddr1 = 40000;
            //public static int ProductSNCheckInfoAddr1 = 30075;
            //public static int ProductSNCheckInfoAddr2 = 32059;
            //public static int ProductSNCheckInfoAddr3 = 34043;
            //public static int ProductSNCheckInfoAddr4 = 36027;
            //public static int ProductSNCheckPassAddr = 50000;
            //public static int ProductSNCheckFailAddr = 50001;

            //public static int CheckOutAddr = 40002;
            //public static int CheckOutCarrierInfoAddr = 30075;
            //public static int CheckOutFlowIDAddr1 = 40004;
            //public static int CheckOutFlowIDAddr2 = 40005;
            //public static int CheckOutPassAddr = 50004;
            //public static int CheckOutFailAddr = 50005;

            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 1000;
            public static int CarrierCheckFailAddr = 1001;

            public static int ProductSNCheckAddr1 = 0;
            public static int ProductSNCheckInfoAddr1 = 3000;
            public static int ProductSNCheckInfoAddr2 = 4984;
            public static int ProductSNCheckInfoAddr3 = 6968;
            public static int ProductSNCheckInfoAddr4 = 8952;
            public static int ProductSNCheckPassAddr = 1000;
            public static int ProductSNCheckFailAddr = 1001;

            public static int CheckOutAddr = 2;
            public static int CheckOutCarrierInfoAddr = 3000;
            public static int CheckOutFlowIDAddr1 = 4;
            public static int CheckOutFlowIDAddr2 = 5;
            public static int CheckOutPassAddr = 1004;
            public static int CheckOutFailAddr = 1005;

            public static int CarrierInfoLength = 64;
            public static int ProductSNInfoLength = 64;  //单个产品码的长度
        }
    }
}

