using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using U66MesPC.Common.Exceptions;
using U66MesPC.Model;

namespace U66MesPC.Common.Station
{

    /// <summary>
    /// 对应工站：载具绑定产品，所在模组：Cap load to Boat1
    /// </summary>
    public class CapInput : StationBase
    {
        //ModbusTcpNet modbusTcpNet;
        public Queue<string> CarrierQueue;  //载具码队列
        public Queue<List<DCInfo>> ProductInfoQueue;  //产品码队列 item=产品码，value=穴位，result==>>PASS:已进站;FAIL进站失败，down==>>OK：合法；NG：不合法，up=对应的载具码
        Dictionary<string, List<DCInfo>> CarrierAndSNMapping;
        public Random random = new Random();
        public CapInput(SysConfigs sysConfig) : base(sysConfig)
        {
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
        public override void SNCheckInAsync()
        {
            var ss0 = Master2.GetStringFromMasterAndNotClear(8568, 64);
            //var bt=BitConverter.GetBytes(ss0);
            //var bt1=BitConverter.GetBytes(64);
            //var s=BitConverter.ToInt16(new byte[4] { 0, 0, 0, 1 }, 0);
            //Master.WriteRegister(50000, 1);
            //var n = Master.ReadHoldingRegisters(50000, 1)[0];
            //string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);//读取载具码
            //string carrierID1 = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);//读取载具码

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
                if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROE"))
                {
                    throw new ArgumentErrorException($"载具码{carrierID}无效", "Carrier_Check");
                }
                CarrierID = carrierID;

                CarrierCheckRequest request = new CarrierCheckRequest(Configs, carrierID);
                Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);

                bool result = GetRet(response.Result);
                if (bSimulation)
                    result = true;
                int addr = result ? PLCAddress.CarrierCheckPassAddr : PLCAddress.CarrierCheckFailAddr;
                Master.WriteRegister(addr, 1);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"CarrierCheck result:{result};CarrierID:{carrierID}");
                return result;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CarrierCheckFailAddr, 1);
                HandleException(ex);
                return false;
            }
            finally
            {
                Thread.Sleep(100);
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
        public bool ProductSNCheck(int stationID)
        {

            string str = stationID == 1 ? "前工站" : "后工站";
            try
            {
                List<DCInfo> productInfoList = new List<DCInfo>();  //item=产品码，value=穴位
                string sn;
                string carrierSN1 = string.Empty;
                string carrierSN2 = string.Empty;
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
                    int n = 1, startAddr = 0;
                    startAddr = stationID == 1 ? PLCAddress.ProductSNCheckInfoAddr11 : PLCAddress.ProductSNCheckInfoAddr21;
                    for (int i = 0; i < 25; i++)
                    {
                        if (i == 0)
                            carrierSN1 = Master2.GetStringFromMasterAndNotClear(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.CarrierInfoLength);
                        else
                        {
                            sn = Master2.GetStringFromMaster(startAddr + i * PLCAddress.ProductSNInfoLength, PLCAddress.ProductSNInfoLength);
                            productInfoList.Add(new DCInfo() { Item = sn, Value = n++.ToString() });
                        }
                    }
                    startAddr = stationID == 1 ? PLCAddress.ProductSNCheckInfoAddr12 : PLCAddress.ProductSNCheckInfoAddr22;
                    for (int i = 0; i < 25; i++)
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
                Master2.WriteRegister(stationID == 1 ? PLCAddress.ProductSNCheckAddr1 : PLCAddress.ProductSNCheckAddr2, 0);//重置产品码请求标志
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset ProductSNCheck flag");

                if (string.IsNullOrEmpty(carrierSN1) || carrierSN1.Equals("ERROE") || carrierSN1.Equals("ERROR"))
                {
                    throw new ArgumentErrorException($"{str}检查产品码失败，载具码{carrierSN1}无效", "SN_FeedingCheck");
                }
                if (!carrierSN1.Equals(carrierSN2))
                    throw new ArgumentErrorException($"{str}两列的载具码（{carrierSN1},{carrierSN2}）不一致！", "SN_FeedingCheck");

                SN = productInfoList.First().Item;
                //CarrierID = carrierSN1;
#if DEBUG
                Console.WriteLine($"{carrierSN1}   {carrierSN2}");
                productInfoList.ForEach(p => Console.WriteLine($"{p.Item}   {p.Value}"));
#endif

                for (int i = 0; i < productInfoList.Count; i++)
                {
                    var p = productInfoList[i];
                    //if (bReCheck && p.Down != "ReCheck")
                    //    continue;
                    p.Up = carrierSN1; //载具码
                    if (p.Item.Equals("ERROE") || string.IsNullOrEmpty(p.Item) || p.Item.Equals("ERROR"))
                    {
                        p.Result = "ERROR";
                        ShowLogInfo(Configs.StationID, "SN_FeedingCheck", EventIO.Warn, $"{str}警告：穴位{p.Value}的产品码{p.Item}无效，忽略合法性检查");
                    }
                    else
                    {
                        if (SNFeedingCheck(productInfoList[i].Item))
                        {
                            CheckInRequest request = new CheckInRequest(Configs, p.Item, $"{carrierSN1}_{p.Value}");
                            Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                            CheckMesConnectStatus(response.Result);
                            p.Result = response.Result?.Result ?? "ERROR";
                            if (p.Result == "ERROR")
                                break;
                        }
                        if (bSimulation)
                            p.Result = "PASS";

                    }

                }
                bool ret = !(bool)(productInfoList?.Any(p => p.Result?.ToLower() != "pass"));
                ret = true;
                productInfoList = productInfoList.Where(p => p.Result?.ToLower() == "pass").ToList();
                //if (bReCheck)
                //    CarrierAndSNMapping.Remove(CarrierID);
                if (CarrierAndSNMapping.ContainsKey(carrierSN1))
                    CarrierAndSNMapping.Remove(carrierSN1);
                CarrierAndSNMapping.Add(carrierSN1, productInfoList);

                int retAddr;
                if (stationID == 1)
                {
                    retAddr = ret ? PLCAddress.ProductSNCheckPassAddr1 : PLCAddress.ProductSNCheckFailAddr1;
                }
                else
                {
                    retAddr = ret ? PLCAddress.ProductSNCheckPassAddr2 : PLCAddress.ProductSNCheckFailAddr2;
                }
                Master2.WriteRegister(retAddr, 1);//写检查结果
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"{str}ProductSN Result:{ret}");
                return ret;
            }
            catch (Exception ex)
            {
                Master2.WriteRegister(stationID == 1 ? PLCAddress.ProductSNCheckFailAddr1 : PLCAddress.ProductSNCheckFailAddr2, 1);
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
        private bool InnerCheckOut(int stationID)
        {
            string str = stationID == 1 ? "前工站" : "后工站";
            try
            {
                //string carrierSN = CarrierQueue.Dequeue();
                //var productInfo = ProductInfoQueue.Dequeue();

                if (!bSimulation && CarrierAndSNMapping.Count == 0)
                {
                    throw new ArgumentErrorException($"{str}出站失败：待出站信息为空");
                }
                string carrierID = Master2.GetStringFromMaster(stationID == 1 ? PLCAddress.ProductSNCheckInfoAddr11 : PLCAddress.ProductSNCheckInfoAddr21, PLCAddress.CarrierInfoLength);
                int flowID1 = Master2.GetRegisterValAndClear(PLCAddress.CheckOutFlowIDAddr1, 1); //流道1
                int flowID2 = Master2.GetRegisterValAndClear(PLCAddress.CheckOutFlowIDAddr2, 1); //流道2
                string flowID = string.Empty;
                if (flowID1 == 1)
                    flowID = "1";
                else if (flowID2 == 1)
                    flowID = "2";

                var mapping = CarrierAndSNMapping.FirstOrDefault(kv => kv.Key == carrierID);
                CarrierAndSNMapping.Remove(carrierID);
                if (!bSimulation && (mapping.Value == null || mapping.Value.Count == 0))
                    throw new ArgumentErrorException($"{str}出站失败：该载具码{carrierID}无有效产品码");
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
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"{str} CheckOut Result:{ret}");
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
                Master2.WriteRegister(stationID == 1 ? PLCAddress.CheckOutAddr1 : PLCAddress.CheckOutAddr2, 0);
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"{str}Reset CheckOut flag");
                //SendStatusAndAlarm();
            }
        }
        //public override void SNCheckInAsync()
        //{

        //    //CheckInRequest request = new CheckInRequest(Configs, "LL1H0H1001510000HYD+082+05A1", "CSF-U66-11CCB1-006_37");
        //    //Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
        //}
        /// <summary>
        ///  以产品码出站；carrierID栏位放载具码
        /// </summary>
        public override bool CheckOut()
        {
            try
            {
                bool ret = true;
                int bCheckProductSN1 = Master2.ReadHoldingRegisters(PLCAddress.ProductSNCheckAddr1, 1)[0];
                int bCheckProductSN2 = Master2.ReadHoldingRegisters(PLCAddress.ProductSNCheckAddr2, 1)[0];
                int bCheckOut1 = Master2.ReadHoldingRegisters(PLCAddress.CheckOutAddr1, 1)[0];
                int bCheckOut2 = Master2.ReadHoldingRegisters(PLCAddress.CheckOutAddr2, 1)[0];
#if DEBUG
                Console.WriteLine($"FromPLC：ProductSNCheck1:{bCheckProductSN1}；ProductSNCheck2:{bCheckProductSN2}CheckOut1:{bCheckOut1};CheckOut2:{bCheckOut2}");
#endif
                if (bCheckProductSN1 == 1)
                {
                    ret &= ProductSNCheck(1);
                }
                if (bCheckProductSN2 == 1)
                {
                    ret &= ProductSNCheck(2);
                }
                if (bCheckOut1 == 1)
                {
                    ret &= InnerCheckOut(1);
                }
                if (bCheckOut2 == 1)
                {
                    ret &= InnerCheckOut(2);
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
            //Master
            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 1000;
            public static int CarrierCheckFailAddr = 1001;

            //Master2  40000-40007 FromPlc  50000-50007 写入PLC
            public static int ProductSNCheckAddr1 = 0;
            public static int ProductSNCheckInfoAddr11 = 3000; //第一列的起始地址
            public static int ProductSNCheckInfoAddr12 = 4984; //第二列的起始地址
            public static int ProductSNCheckPassAddr1 = 1000;
            public static int ProductSNCheckFailAddr1 = 1001;

            public static int ProductSNCheckAddr2 = 1;
            public static int ProductSNCheckInfoAddr21 = 6968;
            public static int ProductSNCheckInfoAddr22 = 8952;
            public static int ProductSNCheckPassAddr2 = 1002;
            public static int ProductSNCheckFailAddr2 = 1003;

            public static int CheckOutAddr1 = 2;  //前工站
            public static int CheckOutAddr2 = 3;  //后工站
            //public static int CheckOutCarrierInfoAddr = 1;
            public static int CheckOutFlowIDAddr1 = 4;
            public static int CheckOutFlowIDAddr2 = 5;
            public static int CheckOutPassAddr = 1004;
            public static int CheckOutFailAddr = 1005;


            //Master
            //public static int CarrierCheckAddr = 40000;
            //public static int CarrierCheckInfoAddr = 30075;
            //public static int CarrierCheckPassAddr = 50000;
            //public static int CarrierCheckFailAddr = 50001;
            ////Master2  40000-40007 FromPlc  50000-50007 写入PLC
            //public static int ProductSNCheckAddr1 = 40000;
            //public static int ProductSNCheckInfoAddr11 = 30075; //第一列的起始地址
            //public static int ProductSNCheckInfoAddr12 = 32059; //第二列的起始地址
            //public static int ProductSNCheckPassAddr1 = 50000;
            //public static int ProductSNCheckFailAddr1 = 50001;

            //public static int ProductSNCheckAddr2 = 40001;
            //public static int ProductSNCheckInfoAddr21 = 34043;
            //public static int ProductSNCheckInfoAddr22 = 36027;
            //public static int ProductSNCheckPassAddr2 = 50002;
            //public static int ProductSNCheckFailAddr2 = 50003;

            //public static int CheckOutAddr1 = 40002;  //前工站
            //public static int CheckOutAddr2 = 40003;  //后工站
            //public static int CheckOutCarrierInfoAddr = 1;
            //public static int CheckOutFlowIDAddr1 = 40004;
            //public static int CheckOutFlowIDAddr2 = 40005;
            //public static int CheckOutPassAddr = 50004;
            //public static int CheckOutFailAddr = 50005;

            public static int CarrierInfoLength = 64;
            public static int ProductSNInfoLength = 64;  //单个产品码的长度
        }
    }
}
