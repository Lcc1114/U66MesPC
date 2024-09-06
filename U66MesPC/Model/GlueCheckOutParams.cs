using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class GlueCheckOutRequest:BaseRequestParams
    {
        public static string _eventID = "Glue_CheckOut";
        public string SN { get; set; }

        public GlueCheckOutRequest()
        {
        }
        //OPID:1--胶水上线；2--胶水下线
        public GlueCheckOutRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string sN) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            SN = sN;
        }
        public GlueCheckOutRequest(SysConfigs config, string sN) : base(config, _eventID)
        {
            SN = sN;
        }
        public override string GetVaidData()
        {
            return $"SN:{SN} OPID:{OPID}";
        }
    }
    public class GlueCheckOutResponse : BaseResponseParams
    {
        public GlueCheckOutResponse() { }

        public GlueCheckOutResponse(string eventID, string result, string msg) : base(eventID, result, msg)
        {
        }
        public override string GetRetData()
        {
            return base.GetRetData();
        }
    }
}