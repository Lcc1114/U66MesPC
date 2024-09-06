using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class CheckInRequest: BaseRequestParams
    {
        public static string _eventID = "SN_CheckIN";
        public CheckInRequest()
        {
        }

        public CheckInRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string sN, string carrierID):base(_eventID, line,stationID,machineID,oPID,token,fixSN)
        {
            SN = sN;
            CarrierID = carrierID;
        }
        public CheckInRequest(SysConfigs config, string sn,string carrierID) : base(config, _eventID)
        {
            SN = sn;
            CarrierID = carrierID;
        }
        public string SN { get; set; }
        public string CarrierID { get; set; }
        public override string GetVaidData()
        {
            return $"SN:{SN} CarrierID:{CarrierID}";
        }

    }
    public class CheckInResponse : BaseResponseParams
    {
        public string SN { get; set; }
        public string CarrierID { get; set; }
        public CheckInResponse() { }

        public CheckInResponse(string eventID,string result,string msg,string sN, string carrierID):base(eventID,result,msg)
        {           
            SN = sN;
            CarrierID = carrierID;
        }
        public override string GetRetData()
        {
            return base.GetRetData() + $"CarrierID:{CarrierID} SN:{SN}";
        }
    }

}