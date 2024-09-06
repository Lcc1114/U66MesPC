using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using U66MesPC.Common;

namespace U66MesPC.Model
{
    public class FlowCheckRequest : BaseRequestParams
    {
        public static string _eventID = "Process";
        //private static string _urlEventID = "SN_FeedingCheck";
        public string SN { get; set; }
        public FlowCheckRequest()
        {
        }

        public FlowCheckRequest(string line, string stationID, string machineID, string oPID, string token, string fixSN, string sN) : base(_eventID, line, stationID, machineID, oPID, token, fixSN)
        {
            SN = sN;
        }
        public FlowCheckRequest(SysConfigs config, string sN) : base(config, _eventID)
        {
            SN = sN;
            
        }
        public override string GetVaidData()
        {
            return $"SN:{SN}";
        }
    }
    public class FlowCheckResponse : BaseResponseParams
    {
        public string SN { get; set; }
        public FlowCheckResponse() { }

        public FlowCheckResponse(string eventID, string result, string msg, string sN) : base(eventID, result, msg)
        {
            SN = sN;
        }
        public override string GetRetData()
        {
            return base.GetRetData() + $"SN:{SN}";
        }
    }
}
