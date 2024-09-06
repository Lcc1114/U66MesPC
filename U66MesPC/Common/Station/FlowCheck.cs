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
    ///  PCBA 和EFlex
    /// </summary>
    public class FlowCheck : StationBase
    {
        public FlowCheck(SysConfigs sysConfig) : base(sysConfig)
        {
            Master.Initialzie();
            InitThread();
        }
        /// <summary>
        ///1、Pass：当前boat没有作业，可在下一个平行工站作业
        ///2、Alarm：当前boat已经做过，不能再重复作业
        ///3、Fail：当前baot有部分产品已做过，部分产品未做，需报警停机取出确认
        /// </summary>
        private bool CarrierCheck()
        {
            try
            {
                var carrierID = Master.GetStringFromMaster(PLCAddress.CarrierCheckInfoAddr, PLCAddress.CarrierInfoLength);//读取载具码
                //AddReadLogInfo(Configs.StationID, "FromPLC", "载具码：" + carrierID);
                Master.WriteRegister(PLCAddress.CarrierCheckAddr, 0);//重置载具检查标志位
                if (string.IsNullOrEmpty(carrierID) || carrierID.Equals("ERROR"))
                    throw new ArgumentErrorException($"流程核对失败：载具码({carrierID})无效！", "SN_FeedingCheck");
                CarrierID = carrierID;
                //AddWriteLogInfo(Configs.StationID, "ToPLC", $"Reset CarrierCheck flag");
                FlowCheckRequest request = new FlowCheckRequest(Configs, carrierID);
                Task<FlowCheckResponse> response = HttpClientHelper.FlowCheckAsync(request, Configs.Url);
                CheckMesConnectStatus(response?.Result);
                string result = response?.Result?.Result.ToLower() ?? "ERROR";
                if (bSimulation)
                    result = "pass";
                if (result == "pass") //生料
                    Master.WriteRegister(PLCAddress.CarrierCheckWaitProduceAddr, 1);//载具检查结果
                else if (result == "alarm") //熟料
                    Master.WriteRegister(PLCAddress.CarrierCheckOKAddr, 1);//载具检查结果
                else
                    Master.WriteRegister(PLCAddress.CarrierCheckNGAddr, 1);//载具检查结果

                AddWriteLogInfo(Configs.StationID, "ToPLC", $"CarrierCheck({carrierID} ):result:{result}");
                return result == "pass";
            }
            catch (Exception ex)
            {
                Master.WriteRegister(PLCAddress.CarrierCheckNGAddr, 1);//载具检查结果
                HandleException(ex);
                return false;
            }
            finally
            {

                //SendStatusAndAlarm();
            }
        }
        public override bool CheckIn()
        {
            try
            {
                bool ret = true;
                int bCheckCarrier = Master.ReadHoldingRegisters(PLCAddress.CarrierCheckAddr, 1)[0];
#if DEBUG
                Console.WriteLine($"CarrierCheck:{bCheckCarrier}；");
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
        class PLCAddress
        {
            //public static int CarrierCheckAddr = 40000;
            //public static int CarrierCheckInfoAddr = 30125;
            ///// <summary>
            /////1、Pass：当前boat没有作业，可在下一个平行工站作业
            /////2、Alarm：当前boat已经做过，不能再重复作业
            /////3、Fail：当前baot有部分产品已做过，部分产品未做，需报警停机取出确认
            ///// </summary>
            //public static int CarrierCheckOKAddr = 50010;  //PASS  生料
            //public static int CarrierCheckWaitProduceAddr = 50009;  //  熟料
            //public static int CarrierCheckNGAddr = 50011;  //Fail

            public static int CarrierCheckAddr = 0;
            public static int CarrierCheckInfoAddr = 3000;

            public static int CarrierCheckOKAddr = 1009;  //熟料
            public static int CarrierCheckWaitProduceAddr = 1010;  //  生料
            public static int CarrierCheckNGAddr = 1011;  //Fail
            public static int CarrierInfoLength = 64;
        }
    }
}
