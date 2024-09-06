using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Common.Exceptions;
using U66MesPC.Model;

namespace U66MesPC.Common.Station
{
    public class ProductInfo
    {
        public string SN { get; set; }
        public string ACPoint { get; set; } //穴位号
        public string BIN { get; set; } //BIN值
        public string LocationID { get; set; } //下料位ID
        public ProductInfo(string sn, string acPoint, string bin, string locationID)
        {
            SN = sn;
            ACPoint = acPoint;
            BIN = bin;
            LocationID = locationID;
        }
    }
    /// <summary>
    /// 对应工站：Binning，所在模组：M7 分BIN机
    /// </summary>
    public class Binning : StationBase
    {
        public List<ProductInfo> ProductSNList11; //60工位的1穴产品
        public List<ProductInfo> ProductSNList12; //60工位的2穴产品
        public List<ProductInfo> ProductSNList21; //70工位的1穴产品
        public List<ProductInfo> ProductSNList22; //70工位的2穴产品
        public List<ProductInfo> ProductSNList31; //80工位的1穴产品
        public List<ProductInfo> ProductSNList32; //80工位的2穴产品
        public Queue<string> CarrierIDQueue;
        public Binning(SysConfigs sysConfig) : base(sysConfig)
        {
            ProductSNList11 = new List<ProductInfo>();
            ProductSNList12 = new List<ProductInfo>();
            ProductSNList21 = new List<ProductInfo>();
            ProductSNList22 = new List<ProductInfo>();
            ProductSNList31 = new List<ProductInfo>();
            ProductSNList32 = new List<ProductInfo>();
            CarrierIDQueue = new Queue<string>();
            Master.Initialzie();
            InitThread();
        }

        public override bool CheckIn()
        {
            try
            {
                bool ret = true;
                int bCheckCarrier = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
                int bCheckProductSN1 = Master.ReadHoldingRegisters(PLCAddress.ProductSNCheckAddr1, 1)[0];
                int bCheckProductSN2 = Master.ReadHoldingRegisters(PLCAddress.ProductSNCheckAddr2, 1)[0];
                int bCheckOut1 = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr1, 1)[0];
                int bCheckOut2 = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr2, 1)[0];
                int bCheckOut3 = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr3, 1)[0];
                int bCheckOut4 = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr4, 1)[0];
                int bCheckOut5 = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr5, 1)[0];
                int bCheckOut6 = Master.ReadHoldingRegisters(PLCAddress.CheckOutAddr6, 1)[0];
#if DEBUG
                Console.WriteLine($"FromPLC：CarrierCheckAddr:{bCheckCarrier}；bCheckProductSN1:{bCheckProductSN1};bCheckProductSN2:{bCheckProductSN2}");
#endif
                if (bCheckCarrier == 1)
                {
                    ret &= CarrierCheck();
                }
                if (bCheckProductSN1 == 1)
                {
                    ProductSNCheck(1);
                }
                if (bCheckProductSN2 == 1)
                {
                    ProductSNCheck(2);
                }
                if (bCheckOut1 == 1)
                {
                    InnerCheckOut(1);
                }
                if (bCheckOut2 == 1)
                {
                    InnerCheckOut(2);
                }
                if (bCheckOut3 == 1)
                {
                    InnerCheckOut(3);
                }
                if (bCheckOut4 == 1)
                {
                    InnerCheckOut(4);
                }
                if (bCheckOut5 == 1)
                {
                    InnerCheckOut(5);
                }
                if (bCheckOut6 == 1)
                {
                    InnerCheckOut(6);
                }
                return ret;
            }
            catch (Exception ex)
            {
                HandleException(ex);
                return false;
            }
        }
        public bool ProductSNCheck(int station)
        {
            try
            {
                string productID = Master.GetStringFromMaster(station == 1 ? PLCAddress.ProductSNCheckInfoAddr1 : PLCAddress.ProductSNCheckInfoAddr2, PLCAddress.CarrierInfoLength);//读取产品码
                //AddReadLogInfo(Configs.StationID, "FromPLC", $"{station}#产品码：{productID}");
                Master.WriteRegister(station == 1 ? PLCAddress.ProductSNCheckAddr1 : PLCAddress.ProductSNCheckAddr2, 0);//重置载具检查标志位

                if (!bSimulation && (string.IsNullOrEmpty(productID) || productID.Equals("ERROR")))
                    throw new ArgumentErrorException($"{station}#产品码({productID})无效！", "SN_CarrierCheck");

                CheckInRequest request = new CheckInRequest(Configs, productID, "NULL");
                Task<CheckInResponse> response = HttpClientHelper.SNCheckInAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                bool result = GetRet(response.Result);
                if (bSimulation)
                    result = true;

                //int addr;
                //if(station==1)
                //{
                //    addr = result ? PLCAddress.ProductSNCheckPassAddr1 : PLCAddress.ProductSNCheckFailAddr1;
                //}
                //else
                //{
                //    addr = result ? PLCAddress.ProductSNCheckPassAddr2 : PLCAddress.ProductSNCheckFailAddr2;
                //}
                //Master.WriteRegister(addr, 1);
                int locationID = 50;
                if (result)
                {
                    if (response.Result.Msg == "6#")
                        locationID = 60;
                    else if (response.Result.Msg == "7#")
                        locationID = 70;
                    else if (response.Result.Msg == "8#")
                        locationID = 80;
                    else
                        locationID = 50; //NG下料位
                }
                else
                    locationID = 50;
                if (result)
                {
                    var pInfo = new ProductInfo(productID, string.Empty, response.Result.CarrierID, response.Result.Msg);
                    if (station == 1 && locationID == 60)
                        ProductSNList11.Add(pInfo);
                    else if (station == 2 && locationID == 60)
                        ProductSNList12.Add(pInfo);
                    else if (station == 1 && locationID == 70)
                        ProductSNList21.Add(pInfo);
                    else if (station == 2 && locationID == 70)
                        ProductSNList22.Add(pInfo);
                    else if (station == 1 && locationID == 80)
                        ProductSNList31.Add(pInfo);
                    else if (station == 2 && locationID == 80)
                        ProductSNList32.Add(pInfo);
                }
                Master.WriteRegister(station == 1 ? PLCAddress.ProductSNCheckPointAddr1 : PLCAddress.ProductSNCheckPointAddr2, locationID); //写下料位
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"Feedback CarrierCheck result:{result}");
                return result;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(station == 1 ? PLCAddress.ProductSNCheckPointAddr1 : PLCAddress.ProductSNCheckPointAddr2, 50); //写下料位
                HandleException(ex);
                return false;
            }
            finally
            {
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset CarrierCheck flag");
                //SendStatusAndAlarm();
            }
        }
        public override void ReCount(string locationID)
        {
            if (locationID.Equals("6#"))
            {
                Master.WriteRegister(PLCAddress.Location1RecountAddr, 1);
            }
            else if (locationID.Equals("7#"))
            {
                Master.WriteRegister(PLCAddress.Location2RecountAddr, 1);
            }
            else if (locationID.Equals("8#"))
            {
                Master.WriteRegister(PLCAddress.Location3RecountAddr, 1);
            }
        }
        public bool InnerCheckOut(int station)
        {
            List<ProductInfo> list = null;
            int acPointAddr = -1;
            int countAddr = -1;
            int retAddr = -1;
            int checkOutAddr = -1;
            if (station == 1)
            {
                list = ProductSNList11;
                acPointAddr = PLCAddress.CheckOutACPointAddr1;
                countAddr = PLCAddress.CheckOutTotalCountAddr1;
                retAddr = PLCAddress.CheckOutResultAddr1;
                checkOutAddr = PLCAddress.CheckOutAddr1;
            }

            else if (station == 2)
            {
                list = ProductSNList12;
                acPointAddr = PLCAddress.CheckOutACPointAddr2;
                countAddr = PLCAddress.CheckOutTotalCountAddr2;
                retAddr = PLCAddress.CheckOutResultAddr2;
                checkOutAddr = PLCAddress.CheckOutAddr2;
            }

            else if (station == 3)
            {
                list = ProductSNList21;
                acPointAddr = PLCAddress.CheckOutACPointAddr3;
                countAddr = PLCAddress.CheckOutTotalCountAddr3;
                retAddr = PLCAddress.CheckOutResultAddr3;
                checkOutAddr = PLCAddress.CheckOutAddr3;
            }
            else if (station == 4)
            {
                list = ProductSNList22;
                acPointAddr = PLCAddress.CheckOutACPointAddr4;
                countAddr = PLCAddress.CheckOutTotalCountAddr4;
                retAddr = PLCAddress.CheckOutResultAddr4;
                checkOutAddr = PLCAddress.CheckOutAddr4;
            }
            else if (station == 5)
            {
                list = ProductSNList31;
                acPointAddr = PLCAddress.CheckOutACPointAddr5;
                countAddr = PLCAddress.CheckOutTotalCountAddr5;
                retAddr = PLCAddress.CheckOutResultAddr5;
                checkOutAddr = PLCAddress.CheckOutAddr5;
            }
            else if (station == 6)
            {
                list = ProductSNList32;
                acPointAddr = PLCAddress.CheckOutACPointAddr6;
                countAddr = PLCAddress.CheckOutTotalCountAddr6;
                retAddr = PLCAddress.CheckOutResultAddr6;
                checkOutAddr = PLCAddress.CheckOutAddr6;
            }
            try
            {
                //string carrierID = CarrierIDQueue.Dequeue();

                if (list.Count == 0)
                {
                    throw new ArgumentErrorException($"{station}#产品出站失败：待出站信息为空");
                }

                int acPoint = Master.GetRegisterValAndClear(acPointAddr, 1);//读取穴位号
                //var count = Master.GetRegisterValAndClear(countAddr, 1);//读取下料位累计的数量
                int count = acPoint;
                AddReadLogInfo(Configs.StationID, "FromPLC", $"{station}#穴位号：{acPoint}；累计数量：{count}");

                var productInfo = list?.FirstOrDefault();
                list.RemoveAt(0);
                List<SNInfo> sNInfos = new List<SNInfo>();
                sNInfos.Add(new SNInfo(productInfo.SN, productInfo.BIN, new List<DCInfo>() { }, new List<CompInfo>() { }));
                CheckOutRequest request = new CheckOutRequest(Configs, "NULL", acPoint.ToString(), count, sNInfos, productInfo.LocationID, "#");
                Task<CheckOutResponse> response = HttpClientHelper.SNCheckOutAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);
                //bool result = GetRet(response.Result);
                bool result = response.Result.SN_Info.Any(x => x.SNResult.ToUpper() == "PASS");
                if (bSimulation)
                    result = true;
                Master.WriteRegister(retAddr, result ? 1 : 2);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"CheckOut result:{result}");
                return result;
            }
            catch (Exception ex)
            {
                Master.WriteRegister(retAddr, 2);
                HandleException(ex);
                return false;
            }
            finally
            {
                Master.WriteRegister(checkOutAddr, 0);
                AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset CheckOut flag");
                //SendStatusAndAlarm();
            }
        }
        public bool CarrierCheck()
        {
            try
            {
                string carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);//读取载具码
                //AddReadLogInfo(Configs.StationID, "FromPLC", "载具码：" + carrierID);
                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);//重置载具检查标志位

                if (!bSimulation && (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR")))
                    throw new ArgumentErrorException($"载具码({carrierID})无效！", "SN_CarrierCheck");
                CarrierID = carrierID;

                CarrierCheckRequest request = new CarrierCheckRequest(Configs, carrierID);
                Task<CarrierCheckResponse> response = HttpClientHelper.CarrierCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response.Result);

                bool result = GetRet(response.Result);
                if (bSimulation)
                    result = true;
                if (result)
                    CarrierIDQueue.Enqueue(carrierID);
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
                //AddWriteLogInfo(Configs.StationID, "写入PLC", $"Reset CarrierCheck flag");
                //SendStatusAndAlarm();
            }
        }
        class PLCAddress
        {
            public static int CarrierCheckAddr = 2;
            public static int CarrierCheckInfoAddr = 3000;
            public static int CarrierCheckPassAddr = 2002;
            public static int CarrierCheckFailAddr = 2003;

            public static int ProductSNCheckAddr1 = 0;
            public static int ProductSNCheckInfoAddr1 = 3064;
            //public static int ProductSNCheckPassAddr1 = 50000;
            //public static int ProductSNCheckFailAddr1 = 50001;
            public static int ProductSNCheckPointAddr1 = 2000;  //产品的下料位


            public static int ProductSNCheckAddr2 = 1;
            public static int ProductSNCheckInfoAddr2 = 3128;
            //public static int ProductSNCheckPassAddr2 = 50000;
            //public static int ProductSNCheckFailAddr2 = 50001;
            public static int ProductSNCheckPointAddr2 = 2001;  //产品的下料位

            public static int CarrierInfoLength = 64;

            public static int CheckOutAddr1 = 3;
            public static int CheckOutACPointAddr1 = 3192;  //穴位号
            public static int CheckOutTotalCountAddr1 = 3192;  //所属下料位的累计数量
            public static int CheckOutResultAddr1 = 2004;    // 1=PASS,2=NG
            //public static int CheckOutPassAddr1 = 50002;
            //public static int CheckOutFailAddr1 = 50003;

            public static int CheckOutAddr2 = 4;
            public static int CheckOutACPointAddr2 = 3193;  //穴位号
            public static int CheckOutTotalCountAddr2 = 9193;  //所属下料的累计数量
            public static int CheckOutResultAddr2 = 2005;
            //public static int CheckOutPassAddr2 = 50002;
            //public static int CheckOutFailAddr2 = 50003;

            public static int CheckOutAddr3 = 5;
            public static int CheckOutACPointAddr3 = 3194;  //穴位号
            public static int CheckOutTotalCountAddr3 = 3194;  //所属下料位的累计数量
            public static int CheckOutResultAddr3 = 2006;
            //public static int CheckOutPassAddr3 = 50002;
            //public static int CheckOutFailAddr3 = 50003;

            public static int CheckOutAddr4 = 6;
            public static int CheckOutACPointAddr4 = 3195;  //穴位号
            public static int CheckOutTotalCountAddr4 = 3195;  //所属下料的累计数量
            public static int CheckOutResultAddr4 = 2007;
            //public static int CheckOutPassAddr4 = 50002;
            //public static int CheckOutFailAddr4 = 50003;

            public static int CheckOutAddr5 = 7;
            public static int CheckOutACPointAddr5 = 3196;  //穴位号
            public static int CheckOutTotalCountAddr5 = 3196;  //所属下料的累计数量
            public static int CheckOutResultAddr5 = 2008;
            //public static int CheckOutPassAddr5 = 50002;
            //public static int CheckOutFailAddr5 = 50003;

            public static int CheckOutAddr6 = 8;
            public static int CheckOutACPointAddr6 = 3197;  //穴位号
            public static int CheckOutTotalCountAddr6 = 3197;  //所属下料的累计数量
            public static int CheckOutResultAddr6 = 2009;
            //public static int CheckOutPassAddr6 = 2009;
            //public static int CheckOutFailAddr6 = 50003;

            public static int Location1RecountAddr = 40001;
            public static int Location2RecountAddr = 40001;
            public static int Location3RecountAddr = 40001;

        }
    }
}
